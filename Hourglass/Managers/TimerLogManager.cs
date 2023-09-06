using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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