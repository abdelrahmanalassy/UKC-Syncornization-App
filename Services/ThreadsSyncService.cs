using System;
using System.Data.Common;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class ThreadsSyncService
    {
        public void SyncThreadsFromSQLServerToSqlite(string vesselId, SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSql = @"
                    SELECT ThreadId, Status, IsSynced, PortID, CreatedAt, UpdatedAt, VesselId
                    FROM UKC_Threads
                    WHERE VesselId = @VesselId";

                using var selectCmd = new SqlCommand(selectSql, sqlServer);
                selectCmd.Parameters.AddWithValue("@VesselId", vesselId);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string threadId = reader.GetGuid(0).ToString();
                    string checkSql = "SELECT COUNT(*) FROM Threads WHERE ThreadId = @ThreadId";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@ThreadId", threadId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO Threads (
                                ThreadId
                                ,STATUS
                                ,IsSynced
                                ,PortID
                                ,CreatedAt
                                ,UpdatedAt
                                ,VesselId
                                )
                            VALUES (
                                @ThreadId
                                ,@Status
                                ,@IsSynced
                                ,@PortID
                                ,@CreatedAt
                                ,@UpdatedAt
                                ,@VesselId
                                )
                            ";

                        using var insertCmd = new SqliteCommand(insertSql, sqlite);
                        AddParemeters(insertCmd, reader);
                        insertCmd.ExecuteNonQuery();
                        insertedCount++;
                    }
                    else
                    {
                        string updateSql = @"
                            UPDATE Threads
                            SET STATUS = @Status
                                ,IsSynced = @IsSynced
                                ,PortID = @PortID
                                ,CreatedAt = @CreatedAt
                                ,UpdatedAt = @UpdatedAt
                            WHERE VesselId = @VesselId
                            ";

                        using var updateCmd = new SqliteCommand(updateSql, sqlite);
                        AddParemeters(updateCmd, reader);                     
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Threads table syncronized from SQL Server to SQLite. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Threads: {ex.Message}");
                LogError("Threads", ex.Message);
            }
        }

        public void SyncThreadsFromSQLiteToSqlServer(SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {

                string selectSqlite = @"
                    SELECT ThreadId, Status, IsSynced, PortID, CreatedAt, UpdatedAt, VesselId
                    FROM Threads";

                using var selectCmd = new SqliteCommand(selectSqlite, sqlite);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string threadId = reader.GetString(0);
                    string checkSql = "SELECT COUNT(*) FROM UKC_Threads WHERE ThreadId = @ThreadId";
                    using var checkCmd = new SqlCommand(checkSql, sqlServer);
                    checkCmd.Parameters.AddWithValue("@ThreadId", threadId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO UKC_Threads (
                                ThreadId
                                ,STATUS
                                ,IsSynced
                                ,PortID
                                ,CreatedAt
                                ,UpdatedAt
                                ,VesselId
                                )
                            VALUES (
                                @ThreadId
                                ,@Status
                                ,@IsSynced
                                ,@PortID
                                ,@CreatedAt
                                ,@UpdatedAt
                                ,@VesselId
                                )
                            ";

                        using var insertCmd = new SqlCommand(insertSql, sqlServer);
                        AddParemeters(insertCmd, reader);
                        insertCmd.ExecuteNonQuery();
                        insertedCount++;
                    }
                    else
                    {
                        string updateSql = @"
                            UPDATE UKC_Threads
                            SET STATUS = @Status
                                ,IsSynced = @IsSynced
                                ,PortID = @PortID
                                ,CreatedAt = @CreatedAt
                                ,UpdatedAt = @UpdatedAt
                                ,VesselId = @VesselId
                            WHERE ThreadId = @ThreadId
                            ";

                        using var updateCmd = new SqlCommand(updateSql, sqlServer);
                        AddParemeters(updateCmd, reader);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Threads table syncronized from SQLite to SQL Server. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Threads: {ex.Message}");
                LogError("Threads", ex.Message);
            }
        }

        private void AddParemeters(DbCommand cmd, DbDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                var param = cmd.CreateParameter();
                param.ParameterName = "@" + reader.GetName(i);
                param.Value = value;
                cmd.Parameters.Add(param);
            }
        }

        private void LogError(string table, string message)
        {
            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                Directory.CreateDirectory(logDirectory);

                string logFile = Path.Combine(logDirectory, "sync-errors.log");
                string logMessage = $"[{DateTime.Now}] [Table: {table}] {message}{Environment.NewLine}";

                File.AppendAllText(logFile, logMessage);
            }
            catch
            {
                Console.WriteLine("Failed to write to log file.");
            }
        }
    }
}