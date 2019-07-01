# serilog-sinks-mysql

A Serilog sink that writes events to MySQL/MariaDB. This sink will write the log event to a table. Important properties can also be written to their own separate columns. Properties by default are written to Text column and are formatted as JSON (custom formatter can be provided for them). This sink was hugelly inspired by [MSSqlServer sink](https://github.com/serilog/serilog-sinks-mssqlserver).

## Configuration

### Sink Configuration Options

Sink configuration accept the following arguments, though not necessarily in this order. Use of named arguments is strongly recommended.

* `connectionString`
* `tableName`
* `autoCreateTable`
* `useBulkInsert`
* `options`
* `period`
* `batchPostingLimit`
* `formatProvider`

### Audit Sink Configuration Options

Audit sink configuration accept the following arguments, though not necessarily in this order. Use of named arguments is strongly recommended.

* `connectionString`
* `tableName`
* `autoCreateTable`
* `options`
* `formatProvider`

### Basic Arguments

At minimum, `connectionString` is required.

If `tableName` is omitted, it defaults to `Logs`.

If `autoCreateTable` is `true` (defaults to `false`), the sink will create a basic table by `tableName` name if it doesn't yet exist. The table will have all the columns provided in `MySqlSinkOptions.PropertiesToColumnsMapping`. All of them will be created as `TEXT` columns, it's users responsibility to change them to the correct data type.

If `useBulkInsert` is `true` (defaults to `true`), the batch will be inserted as single bulk insert operation, otherwise (if set to `false`) it will insert log events one by one. Tests were performed with 5000 entries, the average for bulk insert was 0.787s, and for separate inserts it was 42.833s. The tradeoff here is - with separate insert statements you wont loose that much of data on failure. If you choose to use bulk inserts - be carefull regarding `max_allowed_packet` which determines maximum single SQL statement, that is sent to the server, size in bytes. Depending on your table structure insert statements can vary, so you must determine the batch size accordingly.

Like other sinks, `restrictedToMinimumLevel` controls the `LogEventLevel` messages that are processed by this sink.

This is a "periodic batching sink." The sink will queue a certain number of log events before they're actually written to MySQL/MariaDB as a bulk insert operation. There is also a timeout period so that the batch is always written even if it has not been filled. By default, the batch size is 50 rows and the timeout is 5 seconds. You can change these through by setting the `batchPostingLimit` and `period` arguments.

Refer to the Serilog Wiki's explanation of [Format Providers](https://github.com/serilog/serilog/wiki/Formatting-Output#format-providers) for details about the `formatProvider` arguments.

### Configuration Samples

#### Code-Only

All sink features are configurable from code.

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MySql(
    	connectionString: @"server=...",
    	tableName: "Logs",
    	autoCreateTable: true,
    	useBulkInsert: false,
    	options: new MySqlSinkOptions()
    	)
    .CreateLogger();
```

```csharp
var log = new LoggerConfiguration()
    .AuditTo.MySql(
    	connectionString: @"server=...",
    	tableName: "Logs",
    	autoCreateTable: true,
    	options: new MySqlSinkOptions()
    	)
    .CreateLogger();
```

### Configuration File

They also can be configured through _Microsoft.Extensions.Configuration_ sources, including .NET Core's `appsettings.json` file. We assume, that you've installed [serilog-settings-configuration](https://github.com/serilog/serilog-settings-configuration) package.

```json
{
    "Serilog": {
        "Using": [
            "Serilog.Sinks.MySql"
        ],
        "MinimumLevel": "Debug",
        "WriteTo": [
            {
                "Name": "MySql",
                "Args": {
                    "connectionString": "server=...",
                    "autoCreateTable": false,
                    "tableName": "Logs",
                    "restrictedToMinimumLevel": "Warning",
                    "batchPostingLimit": 1000,
                    "period": "0.00:00:30",
                    "options": {
                        "PropertiesToColumnsMapping": {
                            "Exception": "Exception",
                            "Level": "Level",
                            "Message": "Message",
                            "MessageTemplate": "MessageTemplate",
                            "Properties": "Properties",
                            "Timestamp": "TimeStamp"
                        },
                        "TimestampInUtc": true,
                        "ExcludePropertiesWithDedicatedColumn": true,
                        "EnumsAsInts": true
                    }
                }
            }
        ]
    }
}
```

```json
{
    "Serilog": {
        "Using": [
            "Serilog.Sinks.MySql"
        ],
        "MinimumLevel": "Debug",
        "AuditTo": [
            {
                "Name": "MySql",
                "Args": {
                    "connectionString": "server=...",
                    "autoCreateTable": false,
                    "tableName": "Logs"
                }
            }
        ]
    }
}
```