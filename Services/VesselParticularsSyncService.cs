using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class VesselParticularsSyncService
    {
        public void Sync(string vesselId, SqliteConnection sqlite, SqlConnection sqlServer)
        {
            try
            {
                string selectSqlServer = @"
                    SELECT VesselId
                        ,DanaosId
                        ,VesselType
                        ,VesselName
                        ,VesselSize
                        ,IsActive
                        ,IsConnected
                        ,DraftUnits
                        ,DraftIncrement
                        ,CbMode
                        ,MinDraft
                        ,MaxDraft
                        ,MinDraftDisplacement
                        ,MaxDraftDisplacement
                        ,LightShipWeight
                        ,LBP
                        ,LOA
                        ,Breadth
                        ,Depth
                        ,SummerDraft
                        ,SummerDisplacement
                        ,SummerDWT
                        ,FWA
                        ,TPC
                        ,KeelToMast
                        ,KeelToMastFolded
                        ,APToMast
                        ,MaxSpeed
                        ,CreatedAt
                        ,UpdatedAt
                    FROM UKC_VesselParticulars
                    WHERE VesselId = @VesselId
                    ";

                using var selectCmd = new SqlCommand(selectSqlServer, sqlServer);
                selectCmd.Parameters.AddWithValue("@VesselId", vesselId);
                using var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string vesselIdFromDb = reader.GetString(0);
                    string checkSql = "SELECT COUNT(*) FROM VesselParticulars WHERE VesselId = @VesselId";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@VesselId", vesselId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO VesselParticulars (
                                VesselId
                                ,DanaosId
                                ,VesselType
                                ,VesselName
                                ,VesselSize
                                ,IsActive
                                ,IsConnected
                                ,DraftUnits
                                ,DraftIncrement
                                ,CbMode
                                ,MinDraft
                                ,MaxDraft
                                ,MinDraftDisplacement
                                ,MaxDraftDisplacement
                                ,LightShipWeight
                                ,LBP
                                ,LOA
                                ,Breadth
                                ,Depth
                                ,SummerDraft
                                ,SummerDisplacement
                                ,SummerDWT
                                ,FWA
                                ,TPC
                                ,KeelToMast
                                ,KeelToMastFolded
                                ,APToMast
                                ,MaxSpeed
                                ,CreatedAt
                                ,UpdatedAt
                                )
                            VALUES (
                                @VesselId
                                ,@DanaosId
                                ,@VesselType
                                ,@VesselName
                                ,@VesselSize
                                ,@IsActive
                                ,@IsConnected
                                ,@DraftUnits
                                ,@DraftIncrement
                                ,@CbMode
                                ,@MinDraft
                                ,@MaxDraft
                                ,@MinDraftDisplacement
                                ,@MaxDraftDisplacement
                                ,@LightShipWeight
                                ,@LBP
                                ,@LOA
                                ,@Breadth
                                ,@Depth
                                ,@SummerDraft
                                ,@SummerDisplacement
                                ,@SummerDWT
                                ,@FWA
                                ,@TPC
                                ,@KeelToMast
                                ,@KeelToMastFolded
                                ,@APToMast
                                ,@MaxSpeed
                                ,@CreatedAt
                                ,@UpdatedAt
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
                            UPDATE VesselParticulars
                            SET DanaosId = @DanaosId
                                ,VesselType = @VesselType
                                ,VesselName = @VesselName
                                ,VesselSize = @VesselSize
                                ,IsActive = @IsActive
                                ,IsConnected = @IsConnected
                                ,DraftUnits = @DraftUnits
                                ,DraftIncrement = @DraftIncrement
                                ,CbMode = @CbMode
                                ,MinDraft = @MinDraft
                                ,MaxDraft = @MaxDraft
                                ,MinDraftDisplacement = @MinDraftDisplacement
                                ,MaxDraftDisplacement = @MaxDraftDisplacement
                                ,LightShipWeight = @LightShipWeight
                                ,LBP = @LBP
                                ,LOA = @LOA
                                ,Breadth = @Breadth
                                ,Depth = @Depth
                                ,SummerDraft = @SummerDraft
                                ,SummerDisplacement = @SummerDisplacement
                                ,SummerDWT = @SummerDWT
                                ,FWA = @FWA
                                ,TPC = @TPC
                                ,KeelToMast = @KeelToMast
                                ,KeelToMastFolded = @KeelToMastFolded
                                ,APToMast = @APToMast
                                ,MaxSpeed = @MaxSpeed
                                ,CreatedAt = @CreatedAt
                                ,UpdatedAt = @UpdatedAt
                                WHERE VesselId = @VesselId
                            ";

                        using var updateCmd = new SqliteCommand(updateSql, sqlite);
                        AddParemeters(updateCmd, reader);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"VesselParticulars data syncronized to SQLite. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing VesselParticulars: {ex.Message}");
                LogError("VesselParticulars", ex.Message);
            }
        }

        private void AddParemeters(SqliteCommand cmd, SqlDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                cmd.Parameters.AddWithValue("@" + reader.GetName(i), value);
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