using System;
using MySql.Data.MySqlClient;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.MariaDB.Sinks
{
    public class MariaDBAuditSink : ILogEventSink
    {
        private readonly string _connectionString;
        private readonly MariaDBSinkCore _core;

        public MariaDBAuditSink(
            string connectionString,
            IFormatProvider formatProvider,
            MariaDBSinkOptions options,
            string tableName,
            bool autoCreateTable
        )
        {
            _connectionString = connectionString;

            _core = new MariaDBSinkCore(connectionString, formatProvider, options, tableName, autoCreateTable);
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    var commandText = _core.GetInsertCommandText();

                    using (var cmd = new MySqlCommand(commandText, conn))
                    {
                        foreach (var (column, value) in _core.GetColumnsAndValues(logEvent))
                        {
                            cmd.Parameters.AddWithValue(column, value);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write log event to the database due to following error: {1}", ex.Message);
                throw;
            }
        }
    }
}
