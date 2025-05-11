using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Runtime.Serialization;
using Services;
using Microsoft.Data.SqlClient;


class Program
{
    static IConfiguration? config;
    static void LogError(string message)
    {
        string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logDirectory);

        string logPath = Path.Combine(logDirectory, "error.log");
        string logMessage = $"[{DateTime.Now}] {message}{Environment.NewLine}";
        File.AppendAllText(logPath, logMessage);
    }

    static void Main(string[] args)
    {
        try 
        {
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string sqliteConnString = config.GetConnectionString("Sqlite");
            string sqlServerConnString = config.GetConnectionString("SqlServer");

            string vesselId = config["SyncSettings:VesselId"];
            string threadId = config["SyncSettings:ThreadId"];

            using var sqlite = new SqliteConnection(sqliteConnString);
            using var sqlServer = new SqlConnection(sqlServerConnString);

            sqlite.Open();
            sqlServer.Open();

            // CreateNewDatabase();

            Console.WriteLine("Starting Full Syncronization...");

            var vesselParticularsSync = new VesselParticularsSyncService();
            vesselParticularsSync.Sync(sqlite, sqlServer, vesselId);

            var hydrostaticTableSync = new HydrostaticTableSyncService();
            hydrostaticTableSync.Sync(sqlite, sqlServer, vesselId);

            var threadsSync = new ThreadsSyncService();
            threadsSync.SyncThreadsFromSQLServerToSqlite(vesselId, sqlite, sqlServer);
            threadsSync.SyncThreadsFromSQLiteToSqlServer(sqlite, sqlServer);

            var messagesSync = new MessagesSyncService();
            messagesSync.SyncMessagesFromSQLServerToSQLite(threadId, sqlite, sqlServer);
            messagesSync.SyncMessagesFromSQLiteToSQLServer(sqlite, sqlServer);

            var ChangeLogSync = new ChangeLogSyncService();
            ChangeLogSync.SyncChangeLogFromSQLServerToSqlite(vesselId, sqlite, sqlServer);
            ChangeLogSync.SyncChangeLogFromSQLiteToSqlServer(sqlite, sqlServer);

            Console.WriteLine("All Syncronization Tasks Completed.");
        }
        catch (Exception ex)
        {
            string errorMsg = $"An unexpected error occured: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }
    /*
     // To create the new database with required tables
        static void CreateNewDatabase()
        {
            Console.WriteLine("Enter a new SQLite Database name (e.g., CoupaVessel.db):");
            string dbName = Console.ReadLine();

            try
            {
                //To Ensure it ends with .db
                if (!dbName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                {
                    dbName += ".db";
                }

                // To create direction into Databases Folder
                string relativePath = Path.Combine("Databases", dbName);
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

                // Create if not exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Database '{dbName}' already exists at: {fullPath}");
                }
                else
                {
                    DbInitializer.CreateDatabaseWithTabels(fullPath);
                    Console.WriteLine($"Database '{dbName}' created successfully with required tables.");
                }

                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error during database creation: {ex.Message}";
                Console.WriteLine(errorMsg);
                LogError(errorMsg);
            }
        }
    */
}
