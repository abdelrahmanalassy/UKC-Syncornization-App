using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Runtime.Serialization;
using Services;


class Program
{
    static IConfiguration config;

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

            //CreateNewDatabase();

            Console.WriteLine("Starting Full Syncronization...");

            SyncHydrostaticTable();
            SyncVesselParticularsTable();

            SyncThreadsTableFromSQLServerToSQLite();
            SyncThreadsTableFromSQLiteToSQLServer();

            SyncMessagesTableFromSQLServerToSQLite();
            SyncMessagesTableFromSQLiteToSQLServer();

            SyncChangeLogTableFromSQLServerToSQLite();
            SyncChangeLogTableFromSQLiteToSQLServer();

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
    static void SyncHydrostaticTable()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var hydroSync = new HydrostaticTableSyncService(config);
            hydroSync.Sync();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Hydrostatic table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncVesselParticularsTable()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var particularsSync = new VesselParticularsSyncService(config);
            particularsSync.Sync();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Vessel Particulars table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncThreadsTableFromSQLServerToSQLite()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var threadsSync = new ThreadsSyncService(config);
            threadsSync.SyncThreadsFromSQLServerToSqlite();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Threads table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncThreadsTableFromSQLiteToSQLServer()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var threadsSync = new ThreadsSyncService(config);
            threadsSync.SyncThreadsFromSQLiteToSqlServer();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Threads table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncMessagesTableFromSQLServerToSQLite()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var messagesSync = new MessagesSyncService(config);
            messagesSync.SyncMessagesFromSQLServerToSQLite();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncMessagesTableFromSQLiteToSQLServer()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var messagesSync = new MessagesSyncService(config);
            messagesSync.SyncMessagesFromSQLiteToSQLServer();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncMessagesTableForBothDatabases()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var messagesSync = new MessagesSyncService(config);
            messagesSync.SyncMessagesFromSQLiteToSQLServer();
            messagesSync.SyncMessagesFromSQLServerToSQLite();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncChangeLogTableFromSQLServerToSQLite()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var changeLogSync = new ChangeLogSyncService(config);
            changeLogSync.SyncChangeLogFromSQLServerToSqlite();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncChangeLogTableFromSQLiteToSQLServer()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var changeLogSync = new ChangeLogSyncService(config);
            changeLogSync.SyncChangeLogFromSQLiteToSqlServer();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }

    static void SyncChangeLogTableForBothDatabases()
    {
        Console.WriteLine("Start Syncing...");

        try
        {
            var changeLogSync = new ChangeLogSyncService(config);
            changeLogSync.SyncChangeLogFromSQLServerToSqlite();
            changeLogSync.SyncChangeLogFromSQLiteToSqlServer();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Failed synchronized from Messages table to SQL Server: {ex.Message}";
            Console.WriteLine(errorMsg);
            LogError(errorMsg);
        }
    }
}
