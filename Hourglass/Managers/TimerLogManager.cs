using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data.SQLite;
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
                        StartTime TEXT,
                        EndTime TEXT,
                        Expired INTEGER,
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

            OnTimerStopped(label: null);

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

        public void OnTimerStopped(string label, bool expired = false)
        {
            if (connectionString == null)
                return;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString).OpenAndReturn())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    UPDATE RawTimerLog
                    SET EndTime = @EndTime, Expired = @Expired
                    WHERE EndTime IS NULL {(label != null ? "AND Label = @Label" : "")}
                ";
                command.Parameters.AddWithValue("@EndTime", DateTime.Now);
                command.Parameters.AddWithValue("@Expired", expired);
                command.Parameters.AddWithValue("@Label", label);

                command.ExecuteNonQuery();
            }
        }
    }

}
