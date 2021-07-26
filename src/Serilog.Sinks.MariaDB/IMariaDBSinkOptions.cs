
using System;
using System.Collections.Generic;

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
        bool HashMessageTemplate { get; set; }

        /// <summary>
        /// If true, uses UTC timestamp instead of local time
        /// </summary>
        bool TimestampInUtc { get; set; }

        bool ExcludePropertiesWithDedicatedColumn { get; set; }

        /// <summary>
        /// If true, uses enum int value instead of name
        /// </summary>
        bool EnumsAsInts { get; set; }

        /// <summary>
        /// Older records than this timespan will be periodically deleted
        /// </summary>
        TimeSpan? LogRecordsExpiration { get; set; }

        /// <summary>
        /// Interval of calling delete query to purge old records
        /// </summary>
        TimeSpan LogRecordsCleanupFrequency { get; set; }

        /// <summary>
        /// Chunk size for DELETE operation (used in `LIMIT x`)
        /// </summary>
        int DeleteChunkSize { get; set; }
    }
}
