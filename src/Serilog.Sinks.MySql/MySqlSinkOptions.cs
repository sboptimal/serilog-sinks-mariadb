using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.MySql
{
    public class MySqlSinkOptions
    {
        public Dictionary<string, string> PropertiesToColumnsMapping { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Exception"] = "Exception",
            ["Level"] = "LogLevel",
            ["Message"] = "Message",
            ["MessageTemplate"] = "MessageTemplate",
            ["Properties"] = "Properties",
            ["Timestamp"] = "Timestamp",
        };

        public Func<IReadOnlyDictionary<string, LogEventPropertyValue>, string> PropertiesFormatter { get; set; } = DefaultFormatter;
        public bool TimestampInUtc { get; set; } = true;
        public bool ExcludePropertiesWithDedicatedColumn { get; set; } = false;
        public bool EnumsAsInts { get; set; } = false;

        private static string DefaultFormatter(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            var valueFormatter = new JsonValueFormatter(null);
            var propertiesBuilder = new StringBuilder();

            using (var writer = new StringWriter(propertiesBuilder))
            {
                var delimiter = "";
                writer.Write("{");

                foreach (var property in properties)
                {
                    writer.WriteLine(delimiter);
                    delimiter = ",";

                    writer.Write("\t");
                    JsonValueFormatter.WriteQuotedJsonString(property.Key, writer);
                    writer.Write(": ");
                    valueFormatter.Format(property.Value, writer);
                }

                writer.WriteLine();
                writer.Write("}");
            }

            return propertiesBuilder.ToString();
        }
    }
}
