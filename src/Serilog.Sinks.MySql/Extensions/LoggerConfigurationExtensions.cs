using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.MySql.Sinks;

namespace Serilog.Sinks.MySql.Extensions
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration MySql(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            IFormatProvider formatProvider = null,
            int batchPostingLimit = MySqlSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            MySqlSinkOptions options = null,
            string tableName = "Logs",
            bool autoCreateTable = false,
            bool useBulkInsert = true,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
            )
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            return loggerConfiguration.Sink(
                new MySqlSink(
                    connectionString,
                    formatProvider,
                    batchPostingLimit,
                    period ?? MySqlSink.DefaultPeriod,
                    options ?? new MySqlSinkOptions(),
                    tableName,
                    autoCreateTable,
                    useBulkInsert
                ),
                restrictedToMinimumLevel
            );
        }

        public static LoggerConfiguration MySql(
            this LoggerAuditSinkConfiguration loggerAuditConfiguration,
            string connectionString,
            IFormatProvider formatProvider = null,
            MySqlSinkOptions options = null,
            string tableName = "Logs",
            bool autoCreateTable = false,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
            )
        {
            if (loggerAuditConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerAuditConfiguration));
            }

            return loggerAuditConfiguration.Sink(
                new MySqlAuditSink(
                    connectionString,
                    formatProvider,
                    options ?? new MySqlSinkOptions(),
                    tableName,
                    autoCreateTable
                ),
                restrictedToMinimumLevel
            );
        }
    }
}
