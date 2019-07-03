using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MariaDB.Sinks
{
    public class MariaDBSink : PeriodicBatchingSink
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        private readonly string _connectionString;
        private readonly MariaDBSinkCore _core;
        private readonly bool _useBulkInsert;

        public MariaDBSink(
            string connectionString,
            IFormatProvider formatProvider,
            int batchPostingLimit,
            TimeSpan period,
            MariaDBSinkOptions options,
            string tableName,
            bool autoCreateTable,
            bool useBulkInsert
        ) : base(batchPostingLimit, period)
        {
            _connectionString = connectionString;
            _useBulkInsert = useBulkInsert;

            _core = new MariaDBSinkCore(connectionString, formatProvider, options, tableName, autoCreateTable);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var logEvents = events.ToList();
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    if (_useBulkInsert)
                    {
                        await BulkInsert(logEvents, connection).ConfigureAwait(false);
                    }
                    else
                    {
                        await Insert(logEvents, connection).ConfigureAwait(false);
                    }
                }
                    
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write {0} log events to the database due to following error: {1}", logEvents.Count(), ex.Message);
            }
        }

        private async Task BulkInsert(IReadOnlyList<LogEvent> events, MySqlConnection connection)
        {
            var commandText = _core.GetInsertCommandText(events.Count);

            using (var cmd = new MySqlCommand(commandText, connection))
            {
                for (var i = 0; i < events.Count; i++)
                {
                    foreach (var (column, value) in _core.GetColumnsAndValues(events[i]))
                    {
                        cmd.Parameters.AddWithValue($"{column}{i}", value);
                    }
                }

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task Insert(IEnumerable<LogEvent> events, MySqlConnection connection)
        {
            var commandText = _core.GetInsertCommandText();

            foreach (var log in events)
            {
                try
                {
                    using (var cmd = new MySqlCommand(commandText, connection))
                    {
                        foreach (var (column, value) in _core.GetColumnsAndValues(log))
                        {
                            cmd.Parameters.AddWithValue(column, value);
                        }

                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Unable to write log event to the database due to following error: {0}", ex.Message);
                }
            }
        }
    }
}
