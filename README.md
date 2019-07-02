# serilog-sinks-mysql

A Serilog sink that writes events to MySQL/MariaDB. This sink will write the log event to a table. Important properties can also be written to their own separate columns. Properties by default are written to Text column and are formatted as JSON (custom formatter can be provided for them). This sink was hugelly inspired by [MSSqlServer sink](https://github.com/serilog/serilog-sinks-mssqlserver).

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

#### Configuration File

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
                            "Timestamp": "Timestamp"
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

## MySqlSinkOptions Object

Features of the log table and how we persist data are defined by changing properties on a `MySqlSinkOptions` object:

* `PropertiesToColumnsMapping`
* `PropertiesFormatter`
* `TimestampInUtc`
* `ExcludePropertiesWithDedicatedColumn`
* `EnumsAsInts`

### PropertiesToColumnsMapping

This is a dictionary of all the columns. The key is what we take from the log event, while the value is column name in the database. Here you can include custom columns also, that are taken from the log event `Properties` object. Default value for `PropertiesToColumnsMapping` is shown below.

```csharp
var propertiesToColumns = new Dictionary<string, string>()
        {
            ["Exception"] = "Exception",
            ["Level"] = "LogLevel",
            ["Message"] = "Message",
            ["MessageTemplate"] = "MessageTemplate",
            ["Properties"] = "Properties",
            ["Timestamp"] = "Timestamp",
        };
```

### PropertiesFormatter

This is a `Func<IReadOnlyDictionary<string, LogEventPropertyValue>, string>` delegate which allows you to modify how data for `Properties` column is formatted. By default we format `Properties` as `JSON`, but if you want to override it - you can using this object property.

### TimestampInUtc

If this property is set to `true` (defaults to `true`), the timestamp saved in UTC time standard. Otherwise, it is saved in local time of the machine that issued the log event. For example, if the event is written at 07:00 Eastern time, the Eastern timezone is +4:00 relative to UTC, so after UTC conversion the time stamp will be 11:00. Offset is stored as +0:00 but this is not the GMT time zone because UTC does not use offsets (by definition). To state this another way, the timezone is discarded and unrecoverable. UTC is a representation of the date and time exclusive of timezone information. This makes it easy to reference time stamps written from different or changing timezones.

### ExcludePropertiesWithDedicatedColumn

This property allows you to exclude log event properties that match custom column, otherwise it defaults to `false` and all properties are written out to the `Properties` column.

### EnumsAsInts

By default, enums are not stored as integers. If you want to store them as integer value - set this option to `true` and make changes to the database table accordingly.

## Table Definition

If you don't use automatic creation of the table, you'll have to create a log event table in your database. If you use automatic table creation - you'll have to adjust column types, as the creation now defines all columns as text column. The table definition shown below reflects the default configuration using automatic table creation without changing any sink options.

**IMPORTANT:** If you create your log event table ahead of time, the sink configuration (PropertiesToColumnsMapping) must _exactly_ match that table, or errors are likely to occur.

```sql
CREATE TABLE IF NOT EXISTS `Logs` (
	`Timestamp` TEXT NULL,
	`Level` TEXT NULL,
	`Message` TEXT NULL,
	`MessageTemplate` TEXT NULL,
	`Exception` TEXT NULL,
	`Properties` TEXT NULL
)
```

But the definition below would make more sense for initial database.

```sql
CREATE TABLE IF NOT EXISTS `Logs` (
	`Timestamp` DATETIME DEFAULT NULL,
	`Level` VARCHAR(15) DEFAULT NULL,
	`Message` TEXT NULL,
	`MessageTemplate` TEXT NULL,
	`Exception` TEXT NULL,
	`Properties` TEXT NULL
)
```

## Troubleshooting

### Always check SelfLog first

After configuration is complete, this sink runs through a number of checks to ensure consistency. Some configuration issues result in an exception, but others may only generate warnings through Serilog's `SelfLog` feature. At runtime, exceptions are silently reported through `SelfLog`. Refer to [Debugging and Diagnostics](https://github.com/serilog/serilog/wiki/Debugging-and-Diagnostics#selflog) in the main Serilog documentation to enable `SelfLog` output.

### Always call Log.CloseAndFlush

Any Serilog application should _always_ call `Log.CloseAndFlush` before shutting down. This is especially important in sinks like this one. It is a "periodic batching sink" which means log event records are written in batches for performance reasons. Calling `Log.CloseAndFlush` should guarantee any batch in memory will be written to the database (but read the Visual Studio note below). You may wish to put the `Log.CloseAndFlush` call in a `finally` block in console-driven apps where a `Main` loop controls the overall startup and shutdown process. Refer to the _Serilog.AspNetCore_ sample code for an example. More exotic scenarios like dependency injection may warrant hooking the `ProcessExit` event when the logger is registered as a singleton:

```csharp
AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
```

### Test outside of Visual Studio

When you exit an application running in debug mode under Visual Studio, normal shutdown processes may be interrupted. Visual Studio issues a nearly-instant process kill command when it decides you're done debugging. This is a particularly common problem with ASP.NET and ASP.NET Core applications, in which Visual Studio instantly terminates the application as soon as the browser is closed. Even `finally` blocks usually fail to execute. If you aren't seeing your last few events written, try testing your application outside of Visual Studio.