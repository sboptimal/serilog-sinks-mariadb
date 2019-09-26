using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Serilog.Sinks.MariaDB
{
    internal class PeriodicCleanup
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _columnNameWithTime;
        private readonly TimeSpan _recordsExpiration;
        private readonly TimeSpan _cleanupFrequency;
        private readonly bool _timeInUtc;
        private Timer _cleanupTimer;

        public PeriodicCleanup(string connectionString, string tableName, string columnNameWithTime, TimeSpan recordsExpiration, TimeSpan cleanupFrequency, bool timeInUtc)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _columnNameWithTime = columnNameWithTime;
            _recordsExpiration = recordsExpiration;
            _cleanupFrequency = cleanupFrequency;
            _timeInUtc = timeInUtc;
        }

        public void Start()
        {
            _cleanupTimer = new Timer(EnsureCleanup, null, 2000, (int)_cleanupFrequency.TotalMilliseconds);
        }

        private void EnsureCleanup(object state)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    var sql = $"DELETE FROM `{_tableName}` WHERE `{_columnNameWithTime}` < @expiration";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        var deleteFromTime = _timeInUtc
                            ? DateTimeOffset.UtcNow - _recordsExpiration
                            : DateTimeOffset.Now - _recordsExpiration;

                        cmd.Parameters.AddWithValue("expiration", deleteFromTime);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine("Periodic database cleanup failed: "+ex.ToString());
            }
        }

    }

}
