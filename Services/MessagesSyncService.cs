using System;
using System.Data.Common;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class MessagesSyncService
    {
        public void SyncMessagesFromSQLServerToSQLite(string vesselId, SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSql = @"
                    SELECT MessageId
                        ,m.ThreadId
                        ,UserId
                        ,ExternalUser
                        ,MessageType
                        ,CalculationData
                        ,Comments
                        ,m.IsSynced
                        ,m.CreatedAt
                    FROM UKC_Messages m
                    LEFT JOIN UKC_Threads t ON m.ThreadId = t.ThreadId
                    WHERE t.VesselId = @VesselId
                    ";

                using var selectCmd = new SqlCommand(selectSql, sqlServer);
                selectCmd.Parameters.AddWithValue("@VesselId", vesselId);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string messageId = reader.GetGuid(0).ToString("D").ToUpper();
                    string threadId = reader.GetGuid(1).ToString("D").ToUpper();
                    string checkSql = "SELECT COUNT(*) FROM Messages WHERE MessageId = @MessageId";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@MessageId", messageId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO Messages (
                                MessageId
                                ,ThreadId
                                ,UserId
                                ,ExternalUser
                                ,MessageType
                                ,CalculationData
                                ,Comments
                                ,IsSynced
                                ,CreatedAt
                                )
                            VALUES (
                                @MessageId
                                ,@ThreadId
                                ,@UserId
                                ,@ExternalUser
                                ,@MessageType
                                ,@CalculationData
                                ,@Comments
                                ,@IsSynced
                                ,@CreatedAt
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
                            UPDATE Messages
                            SET ThreadId = @ThreadId
                                ,UserId = @UserId
                                ,ExternalUser = @ExternalUser
                                ,MessageType = @MessageType
                                ,CalculationData = @CalculationData
                                ,Comments = @Comments
                                ,IsSynced = @IsSynced
                                ,CreatedAt = @CreatedAt
                            WHERE MessageId = @MessageId
                            ";

                        using var updateCmd = new SqliteCommand(updateSql, sqlite);
                        AddParemeters(updateCmd, reader);                     
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Messages table syncronized from SQL Server to SQLite. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Messages: {ex.Message}");
                LogError("Messages", ex.Message);
            }
        }

        public void SyncMessagesFromSQLiteToSQLServer(SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSqlite = @"
                    SELECT MessageId
                        ,ThreadId
                        ,UserId
                        ,ExternalUser
                        ,MessageType
                        ,CalculationData
                        ,Comments
                        ,IsSynced
                        ,CreatedAt
                    FROM Messages
                    ";

                using var selectCmd = new SqliteCommand(selectSqlite, sqlite);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string messageId = reader.GetGuid(0).ToString();
                    string threadId = reader.GetString(1);
                    string checkSql = "SELECT COUNT(*) FROM UKC_Messages WHERE MessageId = @MessageId";
                    using var checkCmd = new SqlCommand(checkSql, sqlServer);
                    checkCmd.Parameters.AddWithValue("@MessageId", messageId);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO UKC_Messages (
                                MessageId
                                ,ThreadId
                                ,UserId
                                ,ExternalUser
                                ,MessageType
                                ,CalculationData
                                ,Comments
                                ,IsSynced
                                ,CreatedAt
                                )
                            VALUES (
                                @MessageId
                                ,@ThreadId
                                ,@UserId
                                ,@ExternalUser
                                ,@MessageType
                                ,@CalculationData
                                ,@Comments
                                ,@IsSynced
                                ,@CreatedAt
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
                            UPDATE UKC_Messages
                            SET ThreadId = @ThreadId
                                ,UserId = @UserId
                                ,ExternalUser = @ExternalUser
                                ,MessageType = @MessageType
                                ,CalculationData = @CalculationData
                                ,Comments = @Comments
                                ,IsSynced = @IsSynced
                                ,CreatedAt = @CreatedAt
                            WHERE MessageId = @MessageId
                            ";

                        using var updateCmd = new SqlCommand(updateSql, sqlServer);
                        AddParemeters(updateCmd, reader);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Messages table syncronized from SQLite to SQL Server. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Messages: {ex.Message}");
                LogError("Messages", ex.Message);
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