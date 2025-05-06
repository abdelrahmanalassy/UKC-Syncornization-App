using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SQLitePCL;

namespace Services
{
    public class HydrostaticTableSyncService
    {
        private readonly string? _sqliteConn;
        private readonly string? _sqlServerConn;

        public HydrostaticTableSyncService(IConfiguration config)
        {
            _sqliteConn = config.GetConnectionString("SQLiteDatabases");
            _sqlServerConn = config.GetConnectionString("SqlServer");
        }

        public void Sync()
        {
            try
            {
                using var sqlite = new SqliteConnection(_sqliteConn);
                using var sqlServer = new SqlConnection(_sqlServerConn);



                string selectSqlServer = @"
                    SELECT RowID, VesselId, RefNo, Draft, Displacement, TPC, Cb
                    FROM UKC_HydrostaticTable";

                using var selectCmd = new SqlCommand(selectSqlServer, sqlServer);

                sqlite.Open();
                sqlServer.Open();

                var reader = selectCmd.ExecuteReader();

                int insertedCount = 0;

                while (reader.Read())
                {
                    string rowId = reader.GetGuid(0).ToString();
                    string vesselId = reader.GetString(1);
                    int refNo = reader.GetInt32(2);
                    double draft = reader.GetDouble(3);
                    double displacement = reader.GetDouble(4);
                    double tpc = reader.GetDouble(5);
                    double cb = reader.GetDouble(6);

                    // To check if records exists in SQLite
                    string checkSql = "SELECT COUNT(*) FROM HydrostaticTable WHERE RowID = @RowID";
                    using var checkCmd = new SqliteCommand(checkSql, sqlite);
                    checkCmd.Parameters.AddWithValue("@RowID", rowId);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        string insertSql = @"
                            INSERT INTO HydrostaticTable
                                (RowID, VesselId, RefNo, Draft, Displacement, TPC, Cb)
                            VALUES (@RowID, @VesselId, @RefNo, @Draft, @Displacement, @TPC, @Cb)";

                        using var insertCmd = new SqliteCommand(insertSql, sqlite);
                        insertCmd.Parameters.AddWithValue("@RowID", rowId);
                        insertCmd.Parameters.AddWithValue("@VesselId", vesselId);
                        insertCmd.Parameters.AddWithValue("@RefNo", refNo);
                        insertCmd.Parameters.AddWithValue("@Draft", draft);
                        insertCmd.Parameters.AddWithValue("@Displacement", displacement);
                        insertCmd.Parameters.AddWithValue("@TPC", tpc);
                        insertCmd.Parameters.AddWithValue("@Cb", cb);

                        insertCmd.ExecuteNonQuery();
                        insertedCount++;
                    }
                    else
                    {
                        string updateSql = @"
                            Update HydrostaticTable 
                            SET
                                VesselId = @VesselId, RefNo = @RefNo, Draft = @Draft, Displacement = @Displacement, TPC = @TPC, Cb = @Cb
                            WHERE RowID = @RowID";

                        using var updateCmd = new SqliteCommand(updateSql, sqlite);
                        updateCmd.Parameters.AddWithValue("@RowID", rowId);
                        updateCmd.Parameters.AddWithValue("@VesselId", vesselId);
                        updateCmd.Parameters.AddWithValue("@RefNo", refNo);
                        updateCmd.Parameters.AddWithValue("@Draft", draft);
                        updateCmd.Parameters.AddWithValue("@Displacement", displacement);
                        updateCmd.Parameters.AddWithValue("@TPC", tpc);
                        updateCmd.Parameters.AddWithValue("@Cb", cb);

                        updateCmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Hydrostatic Table data syncronized to SQLite. ({insertedCount} new records inserted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Hydrostatic Table: {ex.Message}");
                LogError("HydrostaticTable", ex.Message);
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