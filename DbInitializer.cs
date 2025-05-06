using System;
using System.IO;
using Microsoft.Data.Sqlite;

public static class DbInitializer
{
    public static void CreateDatabaseWithTabels(string dbFullPath)
    {
        string dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbFullPath);
        string connectionString = $"Data Source={dbPath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            string createChangeLog = @"
                CREATE TABLE IF NOT EXISTS ChangeLog (
                    QueryId TEXT PRIMARY KEY,
                    QueryText TEXT,
                    VesselId TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Owner NVARCHAR(100),
                    IsSynced BOOLEAN DEFAULT 0,
                    IsProcessed BOOLEAN DEFAULT 0
                    );";

            string createEmailLogs = @"
                CREATE TABLE IF NOT EXISTS EmailLogs (
                    EmailID INTEGER NOT NULL UNIQUE,
                    Sender varchar(100) NOT NULL,
                    Recipient varchar(100) NOT NULL,
                    MailBody TEXT NOT NULL,
                    Subject INTEGER NOT NULL,
                    IsSent BOOLEAN DEFAULT 0,
                    PRIMARY KEY (EmailID AUTOINCREMENT)
                    );";

            string createHydrostaticTable = @"
                CREATE TABLE IF NOT EXISTS HydrostaticTable (
                    RowID TEXT PRIMARY KEY,
                    VesselId NVARCHAR(50) NOT NULL, -- Vessel IMO number
                    RefNo INTEGER NOT NULL, -- Reference number
                    Draft DOUBLE NOT NULL, -- Draft value
                    Displacement DOUBLE NOT NULL, -- Displacement value
                    TPC DOUBLE NOT NULL, -- TPC value
                    Cb DOUBLE NOT NULL, -- Block Coef. value
                    FOREIGN KEY (VesselId) REFERENCES VesselParticulars (VesselId)
                    );";

            string createMessages = @"
                CREATE TABLE IF NOT EXISTS Messages (
                    MessageId TEXT PRIMARY KEY,
                    ThreadId TEXT NOT NULL, -- matches Threads.ThreadId
                    UserId TEXT, -- matches Users(UserId); nullable if HQ user isn't in the vessel DB
                    ExternalUser VARCHAR(100), -- Stores the external user's identifier or name
                    MessageType VARCHAR(50) NOT NULL, -- e.g., 'CaptainSubmission' or 'HQFeedback'
                    CalculationData TEXT, -- UKC calculation details (for captain submissions)
                    Comments TEXT, -- HQ feedback or additional remarks
                    IsSynced INTEGER NOT NULL DEFAULT 0, -- Indicates whether this message has been synced with the cloud
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ThreadId) REFERENCES Threads (ThreadId),
                    FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE SET NULL
                    );";

            string createPorts = @"
                CREATE TABLE IF NOT EXISTS Ports (
                    PortID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UNLOCODE VARCHAR(10),
                    Country VARCHAR(50) NOT NULL,
                    Name VARCHAR(255) NOT NULL
                    );";

            string createRoles = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoleName VARCHAR(50) NOT NULL UNIQUE, -- e.g., 'Admin', 'User'
                    Description VARCHAR(255)
                    );";

            string createThreads = @"
                CREATE TABLE IF NOT EXISTS Threads (
                    ThreadId TEXT,
                    Status VARCHAR(50) NOT NULL,
                    IsSynced INTEGER NOT NULL DEFAULT 0,
                    PortID INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    VesselId TEXT NOT NULL,
                    PRIMARY KEY (ThreadId),
                    FOREIGN KEY (PortID) REFERENCES Ports (PortID),
                    FOREIGN KEY (VesselId) REFERENCES VesselParticulars (VesselId)
                    );";

            string createUsers = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId TEXT PRIMARY KEY, -- UUID as TEXT
                    Username VARCHAR(50) NOT NULL UNIQUE,
                    FullName NVARCHAR(50) NOT NULL,
                    PasswordHash VARCHAR(255) NOT NULL, -- Secure hash of the user's password
                    Email VARCHAR(100),
                    RoleId INTEGER NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CreatedBy VARCHAR(50) NULL,
                    LastLogin DATETIME NULL,
                    FOREIGN KEY (RoleId) REFERENCES Roles (RoleId)
                    );";

            string createVesselParticulars = @"
                CREATE TABLE IF NOT EXISTS VesselParticulars (
                    VesselId TEXT PRIMARY KEY, -- Vessel IMO number
                    DanaosId TEXT, -- Vessel Danaos ID
                    VesselType NVARCHAR(100) NOT NULL DEFAULT 'M/T',
                    VesselName NVARCHAR(100) NOT NULL,
                    VesselSize NVARCHAR(100) NOT NULL, -- e.g., 'VLCC', 'SUEZMAX', 'AFRAMAX'
                    IsActive BOOLEAN NOT NULL DEFAULT 1, -- Indicates whether the vessel is active
                    IsConnected BOOLEAN NOT NULL DEFAULT 0, -- Indicates whether the vessel is connected to the UKC cloud service
                    DraftUnits NVARCHAR(50) NOT NULL DEFAULT 'M-cms', -- Draft units (e.g., 'M-cms', 'FT-IN')
                    DraftIncrement DOUBLE NOT NULL DEFAULT 0.20, -- Draft increment in Hydrostatic Tables
                    CbMode NVARCHAR(50) NOT NULL DEFAULT 'Hydrostatic Tables',
                    MinDraft DOUBLE NOT NULL, -- Minimum draft value
                    MaxDraft DOUBLE NOT NULL, -- Maximum draft value
                    MinDraftDisplacement DOUBLE NOT NULL, -- Minimum draft displacement
                    MaxDraftDisplacement DOUBLE NOT NULL, -- Maximum draft displacement
                    LightShipWeight DOUBLE NOT NULL, -- Light ship weight
                    LBP DOUBLE NOT NULL, -- Length between perpendiculars
                    LOA DOUBLE NOT NULL, -- Length overall
                    Breadth DOUBLE NOT NULL, -- Breadth
                    Depth DOUBLE NOT NULL, -- Depth
                    SummerDraft DOUBLE NOT NULL, -- Summer draft
                    SummerDisplacement DOUBLE NOT NULL, -- Summer displacement
                    SummerDWT DOUBLE NULL, -- Summer deadweight
                    FWA DOUBLE NOT NULL, -- Fore perpendicular to waterline
                    TPC DOUBLE NOT NULL, -- TPC at summer draft
                    KeelToMast DOUBLE NOT NULL, -- Keel to masthead [m]
                    KeelToMastFolded DOUBLE NULL, -- Keel to masthead when mast is folded (only for AFRAMAX & SUEZMAX sizes) [m]
                    APToMast DOUBLE NOT NULL, -- AP to masthead [m]
                    MaxSpeed DOUBLE NOT NULL, -- Maximum speed [knots]
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );";

            string createsqlite_sequence = @"
                CREATE TABLE IF NOT EXISTS sqlite_sequence (
                    name, 
                    seq
                    );";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createChangeLog;
                command.ExecuteNonQuery();

                command.CommandText = createEmailLogs;
                command.ExecuteNonQuery();

                command.CommandText = createHydrostaticTable;
                command.ExecuteNonQuery();

                command.CommandText = createMessages;
                command.ExecuteNonQuery();

                command.CommandText = createPorts;
                command.ExecuteNonQuery();

                command.CommandText = createRoles;
                command.ExecuteNonQuery();

                command.CommandText = createThreads;
                command.ExecuteNonQuery();

                command.CommandText = createUsers;
                command.ExecuteNonQuery();

                command.CommandText = createVesselParticulars;
                command.ExecuteNonQuery();

                command.CommandText = createsqlite_sequence;
                command.ExecuteNonQuery();
            }
        }
    }
}