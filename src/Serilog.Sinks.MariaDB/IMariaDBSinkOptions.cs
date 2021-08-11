
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Serilog.Sinks.MariaDB
{
    public interface IMariaDBSinkOptions
    {
        /// <summary>
        /// Event property name to SQL table name mapping
        /// </summary>
        Dictionary<string, string> PropertiesToColumnsMapping { get; set; }

        /// <summary>
        /// If true, uses hash of message template string (to save space)
        /// </summary>
        [DefaultValue(false)]
        bool HashMessageTemplate { get; set; }

        /// <summary>
        /// If true, uses UTC timestamp instead of local time
        /// </summary>
        [DefaultValue(true)]
        bool TimestampInUtc { get; set; }

        [DefaultValue(false)]
        bool ExcludePropertiesWithDedicatedColumn { get; set; }

        /// <summary>
        /// If true, uses enum int value instead of name
        /// </summary>
        [DefaultValue(false)]
        bool EnumsAsInts { get; set; }

        /// <summary>
        /// Older records than this timespan will be periodically deleted
        /// </summary>
        [DefaultValue(null)]
        TimeSpan? LogRecordsExpiration { get; set; }

        /// <summary>
        /// Interval of calling delete query to purge old records
        /// </summary>
        [DefaultValue(0)]
        TimeSpan LogRecordsCleanupFrequency { get; set; }

        /// <summary>
        /// Chunk size for DELETE operation (used in `LIMIT x`)
        /// </summary>
        [DefaultValue(2000)]
        int DeleteChunkSize { get; set; }
    }
}
