using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.MariaDB.Sinks;

namespace Serilog.Sinks.MariaDB.Extensions
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration MariaDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            IFormatProvider formatProvider = null,
            int batchPostingLimit = MariaDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            MariaDBSinkOptions options = null,
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
                new MariaDBSink(
                    connectionString,
                    formatProvider,
                    batchPostingLimit,
                    period ?? MariaDBSink.DefaultPeriod,
                    options ?? new MariaDBSinkOptions(),
                    tableName,
                    autoCreateTable,
                    useBulkInsert
                ),
                restrictedToMinimumLevel
            );
        }

        public static LoggerConfiguration MariaDB(
            this LoggerAuditSinkConfiguration loggerAuditConfiguration,
            string connectionString,
            IFormatProvider formatProvider = null,
            MariaDBSinkOptions options = null,
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
                new MariaDBAuditSink(
                    connectionString,
                    formatProvider,
                    options ?? new MariaDBSinkOptions(),
                    tableName,
                    autoCreateTable
                ),
                restrictedToMinimumLevel
            );
        }
    }
}
