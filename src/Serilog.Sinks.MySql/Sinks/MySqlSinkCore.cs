using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.MySql.Sinks
{
    internal sealed class MySqlSinkCore
    {
        private readonly string _tableName;
        private readonly IFormatProvider _formatProvider;
        private readonly MySqlSinkOptions _options;

        public MySqlSinkCore(
            string connectionString,
            IFormatProvider formatProvider,
            MySqlSinkOptions options,
            string tableName,
            bool autoCreateTable
        )
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            _tableName = tableName;
            _formatProvider = formatProvider;
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (autoCreateTable)
            {
                try
                {
                    var tableCreator = new SqlTableCreator(connectionString, _tableName, _options.PropertiesToColumnsMapping.Values);
                    tableCreator.CreateTable();
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine($"Exception creating table {_tableName}:\n{ex}");
                }
            }
        }

        public IEnumerable<(string, object)> GetColumnsAndValues(LogEvent logEvent)
        {
            foreach (var column in GetStandardColumnNamesAndValues(logEvent))
            {
                yield return column;
            }

            foreach (var column in ConvertPropertiesToColumn(logEvent.Properties))
            {
                yield return column;
            }
        }

        private IEnumerable<(string, object)> GetStandardColumnNamesAndValues(LogEvent logEvent)
        {
            foreach (var map in _options.PropertiesToColumnsMapping) //TODO: test how our lib works with Serilog.Exceptions enricher
            {
                if (map.Key.Equals("Message", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return (map.Value, logEvent.RenderMessage(_formatProvider));
                    continue;
                }

                if (map.Key.Equals("MessageTemplate", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return (map.Value, logEvent.MessageTemplate.Text);
                    continue;
                }

                if (map.Key.Equals("Level", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return (map.Value, _options.EnumsAsInts ? (object)logEvent.Level : logEvent.Level.ToString());
                    continue;
                }

                if (map.Key.Equals("Timestamp", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return (map.Value, _options.TimestampInUtc ? logEvent.Timestamp.ToUniversalTime().DateTime : logEvent.Timestamp.DateTime);
                    continue;
                }

                if (map.Key.Equals("Exception", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return (map.Value, logEvent.Exception?.ToString());
                    continue;
                }

                if (map.Key.Equals("Properties", StringComparison.InvariantCultureIgnoreCase))
                {
                    var properties = logEvent.Properties.AsEnumerable();

                    if (_options.ExcludePropertiesWithDedicatedColumn)
                    {
                        properties = properties
                            .Where(i => !_options.PropertiesToColumnsMapping.ContainsKey(i.Key));
                    }

                    yield return (map.Value, _options.PropertiesFormatter(
                        new ReadOnlyDictionary<string, LogEventPropertyValue>(
                            properties.ToDictionary(k => k.Key, v => v.Value)
                        )
                    ));
                }
            }
        }

        public string GetInsertCommandText(int rows = 0)
        {
            var commandBuilder = new StringBuilder();
            var columnNames = _options.PropertiesToColumnsMapping
                .Select(i => i.Value)
                .ToList();

            commandBuilder.AppendLine($"INSERT INTO {_tableName} ({string.Join(", ", columnNames)})");
            commandBuilder.AppendLine($"VALUES");

            if (rows == 0)
            {
                commandBuilder.Append("(");
                commandBuilder.AppendLine(string.Join(", ", columnNames.Select(param => $"@{param}")));
                commandBuilder.Append(")");
            }

            for (var i = 0; i < rows; i++)
            {
                commandBuilder.Append("(");
                commandBuilder.Append(string.Join(", ", columnNames.Select(param => $"@{param}{i}")));
                commandBuilder.Append(")");

                if (i != rows - 1)
                {
                    commandBuilder.AppendLine(",");
                }
            }

            return commandBuilder.ToString();
        }

        private IEnumerable<(string, object)> ConvertPropertiesToColumn(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var property in properties)
            {
                if (!_options.PropertiesToColumnsMapping.TryGetValue(property.Key, out var columnName))
                {
                    continue;
                }

                if (!(property.Value is ScalarValue scalarValue))
                {
                    var sb = new StringBuilder();
                    using (var writer = new StringWriter(sb))
                    {
                        property.Value.Render(writer, formatProvider: _formatProvider);
                    }

                    yield return (columnName, sb.ToString());
                    continue;
                }

                if (scalarValue.Value == null)
                {
                    yield return (columnName, DBNull.Value);
                    continue;
                }

                var isEnum = scalarValue.Value.GetType().IsEnum;

                if (isEnum && !_options.EnumsAsInts)
                {
                    yield return (columnName, scalarValue.Value.ToString());
                    continue;
                }

                yield return (columnName, scalarValue.Value);
            }
        }
    }
}
