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
        private readonly string? _sqliteConn;
        private readonly string? _sqlServerConn;

        public MessagesSyncService(IConfiguration config)
        {
            _sqliteConn = config.GetConnectionString("SQLiteDatabases");
            _sqlServerConn = config.GetConnectionString("SqlServer");
        }

        public void SyncMessagesFromSQLServerToSQLite()
        {
            try
            {
                using var sqlite = new SqliteConnection(_sqliteConn);
                using var sqlServer = new SqlConnection(_sqlServerConn);

                sqlite.Open();
                sqlServer.Open();

                string selectSql = @"
                    SELECT MessageId, ThreadId, UserId, ExternalUser, MessageType, CalculationData, Comments, IsSynced, CreatedAt
                    FROM UKC_Messages";

                using var selectCmd = new SqlCommand(selectSql, sqlServer);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string messageId = reader.GetGuid(0).ToString();
                    string checkSql = "SELECT COUNT(*) FROM Messages WHERE MessageId = @MessageId";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@MessageId", messageId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO Messages (
                                MessageId, ThreadId, UserId, ExternalUser, MessageType, CalculationData, Comments, IsSynced, CreatedAt)
                            VALUES (
                                @MessageId, @ThreadId, @UserId, @ExternalUser, @MessageType, @CalculationData, @Comments, @IsSynced, @CreatedAt)";

                        using var insertCmd = new SqliteCommand(insertSql, sqlite);
                        AddParemeters(insertCmd, reader);
                        insertCmd.ExecuteNonQuery();
                        insertedCount++;
                    }
                    else
                    {
                        string updateSql = @"
                            Update Messages SET
                                ThreadId = @ThreadId, 
                                UserId = @UserId, 
                                ExternalUser = @ExternalUser, 
                                MessageType = @MessageType, 
                                CalculationData = @CalculationData, 
                                Comments = @Comments,
                                IsSynced = @IsSynced,
                                CreatedAt = @CreatedAt
                            WHERE MessageId = @MessageId";

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

        public void SyncMessagesFromSQLiteToSQLServer()
        {
            try
            {
                using var sqlite = new SqliteConnection(_sqliteConn);
                using var sqlServer = new SqlConnection(_sqlServerConn);

                sqlite.Open();
                sqlServer.Open();

                string selectSqlite = @"
                    SELECT MessageId, ThreadId, UserId, ExternalUser, MessageType, CalculationData, Comments, IsSynced, CreatedAt
                    FROM Messages";

                using var selectCmd = new SqliteCommand(selectSqlite, sqlite);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string messageId = reader.GetString(0);
                    string checkSql = "SELECT COUNT(*) FROM UKC_Messages WHERE MessageId = @MessageId";
                    using var checkCmd = new SqlCommand(checkSql, sqlServer);
                    checkCmd.Parameters.AddWithValue("@MessageId", messageId);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO UKC_Messages (
                                MessageId, ThreadId, UserId, ExternalUser, MessageType, CalculationData, Comments, IsSynced, CreatedAt)
                            VALUES (
                                @MessageId, @ThreadId, @UserId, @ExternalUser, @MessageType, @CalculationData, @Comments, @IsSynced, @CreatedAt)";

                        using var insertCmd = new SqlCommand(insertSql, sqlServer);
                        AddParemeters(insertCmd, reader);
                        insertCmd.ExecuteNonQuery();
                        insertedCount++;
                    }
                    else
                    {
                        string updateSql = @"
                            Update UKC_Messages SET
                                ThreadId = @ThreadId, 
                                UserId = @UserId, 
                                ExternalUser = @ExternalUser, 
                                MessageType = @MessageType, 
                                CalculationData = @CalculationData, 
                                Comments = @Comments,
                                IsSynced = @IsSynced,
                                CreatedAt = @CreatedAt
                            WHERE MessageId = @MessageId";

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