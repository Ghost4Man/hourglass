using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.SQLite;
using Dapper;
using Hourglass.Properties;

namespace Hourglass.Managers
{
    public class TimerLogManager : Manager
    {
        /// <summary>
        /// Singleton instance of the <see cref="TimerLogManager"/> class.
        /// </summary>
        public static readonly TimerLogManager Instance = new TimerLogManager();

        private string connectionString;

        /// <summary>
        /// Prevents a default instance of the <see cref="TimerLogManager"/> class from being created.
        /// </summary>
        private TimerLogManager()
        {
        }

        public override void Initialize()
        {
            string dbFilePath = Settings.Default.TimerLogDbFilePath;

            if (string.IsNullOrWhiteSpace(dbFilePath))
            {
                MessageBox.Show($"No {nameof(Settings.TimerLogDbFilePath)} found.");
                return;
            }

            connectionString = $"Data Source={dbFilePath};Version=3;";
            Console.Error.WriteLine($"Logging to timer log SQLite DB with connection string: `{connectionString}`");

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RawTimerLog (
                        StartTime TEXT NOT NULL,
                        EndTime TEXT,
                        StopReason TEXT,
                        Label TEXT
                    ) STRICT;
                    CREATE TABLE IF NOT EXISTS EditedTasks (
                        Id INTEGER PRIMARY KEY,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT NOT NULL,
                        Label TEXT NOT NULL,
                        Depth INTEGER NOT NULL,
                        Tags TEXT
                    ) STRICT;
                ";
                command.ExecuteNonQuery();
            }
        }

        public void OnTimerStarted(string label)
        {
            if (connectionString == null)
                return;

            OnTimerStopped(label: null, TimerStopReason.AutoClosed);

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO RawTimerLog (StartTime, Label)
                    VALUES (@StartTime, @Label);
                ";
                command.Parameters.AddWithValue("@StartTime", DateTime.Now);
                command.Parameters.AddWithValue("@Label", label);

                command.ExecuteNonQuery();
            }
        }

        public void OnTimerStopped(string label, TimerStopReason stopReason)
        {
            if (connectionString == null)
                return;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    UPDATE RawTimerLog
                    SET EndTime = @EndTime, StopReason = @StopReason
                    WHERE EndTime IS NULL {(label != null ? "AND Label = @Label" : "")}
                ";
                command.Parameters.AddWithValue("@EndTime", DateTime.Now);
                command.Parameters.AddWithValue("@StopReason", stopReason.ToString());
                command.Parameters.AddWithValue("@Label", label);

                command.ExecuteNonQuery();
            }
        }

        public async Task<IEnumerable<TimerLogEntry>> GetRawTimerLogForDay(DateTime day)
        {
            if (connectionString == null)
                return null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                return await connection.QueryAsync<TimerLogEntry>(
                    "SELECT * FROM RawTimerLog " +
                    "WHERE StartTime LIKE @DateLike OR EndTime LIKE @DateLike",
                    new { DateLike = $"{day:yyyy'-'MM'-'dd} %" });
            }
        }

        public async Task<IEnumerable<Task>> GetTasks(DateTime day)
        {
            if (connectionString == null)
                return null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                return await connection.QueryAsync<Task>(
                    "SELECT * FROM EditedTasks " +
                    "WHERE StartTime LIKE @DateLike OR EndTime LIKE @DateLike",
                    new { DateLike = $"{day:yyyy'-'MM'-'dd} %" });
            }
        }

        public async Task<IEnumerable<string>> SearchRecentTaskLabels(string searchedText, bool caseSensitive)
        {
            if (connectionString == null)
                return null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                return await connection.QueryAsync<string>(
                    "SELECT DISTINCT Label FROM (" +
                    "   SELECT Label, EndTime FROM EditedTasks " +
                    "   UNION " +
                    "   SELECT Label, EndTime FROM RawTimerLog " +
                    "   ORDER BY EndTime DESC " +
                    ") " +
                    (caseSensitive
                        ? "WHERE instr(Label, @searchedText) > 0 "
                        : "WHERE instr(lower(Label), lower(@searchedText)) > 0 ") +
                    "LIMIT 8",
                    new { searchedText });
            }
        }

        public Task GetTaskById(int taskId)
        {
            if (connectionString == null)
                return null;

            using (IDbConnection dbConnection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                string query = "SELECT * FROM EditedTasks WHERE Id = @taskId";
                return dbConnection.QueryFirstOrDefault<Task>(query, new { taskId });
            }
        }

        public async System.Threading.Tasks.Task UpsertTask(Task task)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (connectionString == null)
                return;

            using (IDbConnection dbConnection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                // Insert a new task or update if it exists
                string query = @"
                    INSERT INTO EditedTasks (Id, StartTime, EndTime, Label, Tags, Depth)
                        VALUES (@Id, @StartTime, @EndTime, @Label, @Tags, @Depth)
                    ON CONFLICT DO UPDATE
                        SET StartTime = @StartTime, EndTime = @EndTime, Label = @Label, Tags = @Tags, Depth = @Depth
                    RETURNING Id
                ";

                int taskId = await dbConnection.QuerySingleAsync<int>(query, task);
                task.Id = taskId;
            }
        }

        public async System.Threading.Tasks.Task DeleteTask(int id)
        {
            if (connectionString == null)
                return;

            using (IDbConnection dbConnection = new SQLiteConnection(connectionString).OpenAndReturn())
            {
                string sql = "DELETE FROM EditedTasks WHERE Id = @Id";
                await dbConnection.ExecuteAsync(sql, new { Id = id });
            }
        }
    }

}

namespace Hourglass
{
    public class TimerLogEntry
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimerStopReason StopReason { get; set; }
        public string Label { get; set; }
    }

    public class Task
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Label { get; set; }
        public int Depth { get; set; }
        public string Tags { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public enum TimerStopReason
    {
        Unknown = 0,
        Stopped,
        Paused,
        Expired,
        Renamed,

        /// <summary>
        /// Occurs when a timer log event is automatically ended when starting
        /// a new timer (e.g. because it was left running due to an error).
        /// </summary>
        AutoClosed,
    }
}