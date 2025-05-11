using System;
using System.Data.Common;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class ChangeLogSyncService
    {
        public void SyncChangeLogFromSQLServerToSqlite(string vesselId, SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSql = @"
                    SELECT QueryId
                        ,QueryText
                        ,VesselId
                        ,Timestamp
                        ,Owner
                        ,IsSynced
                        ,IsProcessed
                    FROM UKC_ChangeLog
                    WHERE VesselId = @VesselId
                    ";

                using var selectCmd = new SqlCommand(selectSql, sqlServer);
                selectCmd.Parameters.AddWithValue("@VesselId", vesselId);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string queryId = reader.GetString(0);
                    string checkSql = "SELECT COUNT(*) FROM ChangeLog WHERE QueryId = @QueryId";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@QueryId", queryId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO ChangeLog (
                                QueryId
                                ,QueryText
                                ,VesselId
                                ,Timestamp
                                ,Owner
                                ,IsSynced
                                ,IsProcessed
                                )
                            VALUES (
                                @QueryId
                                ,@QueryText
                                ,@VesselId
                                ,@Timestamp
                                ,@Owner
                                ,@IsSynced
                                ,@IsProcessed
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
                            UPDATE ChangeLog
                            SET QueryText = @QueryText
                                ,VesselId = @VesselId
                                ,Timestamp = @Timestamp
                                ,Owner = @Owner
                                ,IsSynced = @IsSynced
                                ,IsProcessed = @IsProcessed
                            WHERE QueryId = @QueryId
                            ";

                        using var updateCmd = new SqliteCommand(updateSql, sqlite);
                        AddParemeters(updateCmd, reader);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"ChangeLog table syncronized from SQL Server to SQLite. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing ChangeLog: {ex.Message}");
                LogError("ChangeLog", ex.Message);
            }
        }

        public void SyncChangeLogFromSQLiteToSqlServer(SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSql = @"
                    SELECT QueryId
                    ,QueryText
                    ,VesselId
                    ,Timestamp
                    ,Owner
                    ,IsSynced
                    ,IsProcessed
                FROM ChangeLog
                ";

                using var selectCmd = new SqliteCommand(selectSql, sqlite);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string queryId = reader.GetString(0);
                    string checkSql = "SELECT COUNT(*) FROM UKC_ChangeLog WHERE QueryId = @QueryId";
                    using var checkCmd = new SqlCommand(checkSql, sqlServer);
                    checkCmd.Parameters.AddWithValue("@QueryId", queryId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO UKC_ChangeLog (
                                QueryId
                                ,QueryText
                                ,VesselId
                                ,Timestamp
                                ,Owner
                                ,IsSynced
                                ,IsProcessed
                                )
                            VALUES (
                                @QueryId
                                ,@QueryText
                                ,@VesselId
                                ,@Timestamp
                                ,@Owner
                                ,@IsSynced
                                ,@IsProcessed
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
                            UPDATE UKC_ChangeLog
                            SET QueryText = @QueryText
                                ,VesselId = @VesselId
                                ,Timestamp = @Timestamp
                                ,Owner = @Owner 
                                ,IsSynced = @IsSynced
                                ,IsProcessed = @IsProcessed
                            WHERE QueryId = @QueryId
                            ";

                        using var updateCmd = new SqlCommand(updateSql, sqlServer);
                        AddParemeters(updateCmd, reader);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"ChangeLog table syncronized from SQLite to SQL Server. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing ChangeLog: {ex.Message}");
                LogError("ChangeLog", ex.Message);
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