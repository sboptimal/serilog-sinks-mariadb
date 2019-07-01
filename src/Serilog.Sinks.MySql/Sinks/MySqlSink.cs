using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MySql.Sinks
{
    public class MySqlSink : PeriodicBatchingSink
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        private readonly string _connectionString;
        private readonly MySqlSinkCore _core;
        private readonly bool _useBulkInsert;

        public MySqlSink(
            string connectionString,
            IFormatProvider formatProvider,
            int batchPostingLimit,
            TimeSpan period,
            MySqlSinkOptions options,
            string tableName,
            bool autoCreateTable,
            bool useBulkInsert
        ) : base(batchPostingLimit, period)
        {
            _connectionString = connectionString;
            _useBulkInsert = useBulkInsert;

            _core = new MySqlSinkCore(connectionString, formatProvider, options, tableName, autoCreateTable);
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
            using (var trx = connection.BeginTransaction())
            {
                try
                {
                    var commandText = _core.GetInsertCommandText();

                    foreach (var log in events)
                    {
                        using (var cmd = new MySqlCommand(commandText, connection, trx))
                        {
                            foreach (var (column, value) in _core.GetColumnsAndValues(log))
                            {
                                cmd.Parameters.AddWithValue(column, value);
                            }

                            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }


                    await trx.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    await trx.RollbackAsync().ConfigureAwait(false);
                    throw;
                }
            }
        }
    }
}
