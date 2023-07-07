// DbUp Migrator console application
using DbUp.Helpers;
using DbUp.ScriptProviders;
using DbUp;
using DbUp.Engine;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

class HistoricalTrackingJournal : IJournal
{
    private readonly string _connectionString;

    public HistoricalTrackingJournal(string connectionString)
    {
        _connectionString = connectionString;
    }

    void IJournal.EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
    {
        var schemaInformationQuery = @"
			USE [VC2];
			SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SchemaVersions' AND TABLE_SCHEMA = 'dbo'";
        using var command = dbCommandFactory();
        command.CommandText = schemaInformationQuery;
        command.CommandType = CommandType.Text;
        var reader = command.ExecuteScalar();

        if (reader == null)
        {
            string tableCreationSQL = @"
						USE  [VC2];
						CREATE TABLE SchemaVersions (
						[Id] INT IDENTITY(1,1) NOT NULL
						, [ScriptName] NVARCHAR(255) NOT NULL
						, [Applied] DATETIME NOT NULL
						, CONSTRAINT pk_SchemaVersions_Id PRIMARY KEY NONCLUSTERED (Id)
						)
					CREATE TABLE HistoricalDates(
						[Id] INT IDENTITY(1,1) NOT NULL
						, SchemaVersionsId INT not null
						, DeploymentDate DATETIME NOT NULL
						, CONSTRAINT pk_HistoricalDates_Id PRIMARY KEY NONCLUSTERED (Id)
					)

					ALTER TABLE [VC2].[dbo].HistoricalDates
					ADD CONSTRAINT FK_SchemaVersions_SchmaVersionsId
					FOREIGN KEY (SchemaVersionsId)
					REFERENCES [VC2].[dbo].SchemaVersions (Id)
					ON DELETE CASCADE
					ON UPDATE CASCADE;
					";
            command.CommandText = tableCreationSQL;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    string[] IJournal.GetExecutedScripts()
    {
        var schemaInformationQuery = @"
			USE [VC2];
			SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SchemaVersions' AND TABLE_SCHEMA = 'dbo'";
        using SqlConnection checkingConnection = new SqlConnection(_connectionString);
        checkingConnection.Open();
        using SqlCommand checkingCommand = new SqlCommand(schemaInformationQuery, checkingConnection);
        checkingCommand.CommandText = schemaInformationQuery;
        checkingCommand.CommandType = CommandType.Text;
        var readerQuery = checkingCommand.ExecuteScalar();

        if (readerQuery == null)
        {
            return Array.Empty<string>();
        }

        string queryString = @"USE  [VC2];
			SELECT ScriptName FROM [SchemaVersions]";

        var result = new List<string>();

        using SqlConnection connection = new SqlConnection(_connectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand(queryString, connection);
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result.ToArray();
    }

    void IJournal.StoreExecutedScript(SqlScript script, Func<IDbCommand> dbCommandFactory)
    {
        string insertionSQL = @$"
						USE  [VC2];
						IF EXISTS(SELECT 1 FROM [SchemaVersions] WHERE ScriptName = @scriptName)
						BEGIN
							UPDATE [SchemaVersions]
							SET Applied = @applied
							WHERE ScriptName = @scriptName 
						END
						ELSE
						BEGIN
							INSERT INTO [SchemaVersions] (ScriptName, Applied) 
							VALUES (@scriptName, @applied);
						END

						INSERT INTO [HistoricalDates](SchemaVersionsId, DeploymentDate)
						SELECT Id, Applied
						FROM [SchemaVersions]
						WHERE ScriptName = @scriptName
						AND Applied = @applied
					";
        using var command = dbCommandFactory();

        var scriptNameParam = command.CreateParameter();
        scriptNameParam.ParameterName = "scriptName";
        scriptNameParam.Value = script.Name;
        command.Parameters.Add(scriptNameParam);

        var appliedParam = command.CreateParameter();
        appliedParam.ParameterName = "applied";
        appliedParam.Value = DateTime.Now;
        command.Parameters.Add(appliedParam);

        command.CommandText = insertionSQL;
        command.CommandType = CommandType.Text;
        command.ExecuteNonQuery();
    }
}


internal class Program
{
    //Path should be a directory
    static List<string> SortedDateModifiedScripts (string scriptsPath)
    {
        List<string> sorted = new List<string>();
        DirectoryInfo directory = new DirectoryInfo(scriptsPath);
        FileInfo[] files = directory.GetFiles();
        var sortedFiles = files.OrderBy(f => f.LastWriteTime);

        foreach (FileInfo file in sortedFiles)
        {
            sorted.Add(file.FullName);
        }
        return sorted;

    }
    static int Main(string[] args)
    {
        var connectionString="";
        var scriptsPath="";

        if (args.Length == 2)
        {
             connectionString = args[0];
             scriptsPath = args[1];
        }
        else
        {
             connectionString = "Data Source=MARKOPC\\MSSQLSERVER2;Initial Catalog=VC;Integrated Security=True;Encrypt=False";
             scriptsPath = "C:\\Users\\38975\\Desktop\\PAIN\\Script\\";
        }
        Console.WriteLine("Start executing predeployment scripts...");
        string preDeploymentScriptsPath = Path.Combine(scriptsPath, "PreDeployment");
        var preDeploymentScriptsExecutor =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsFromFileSystem(preDeploymentScriptsPath, new FileSystemScriptOptions
                {
                    IncludeSubDirectories = true
                })
                .LogToConsole()
                .JournalTo(new NullJournal())
                .Build();

        var preDeploymentUpgradeResult = preDeploymentScriptsExecutor.PerformUpgrade();

        if (!preDeploymentUpgradeResult.Successful)
        {
            return ReturnError(preDeploymentUpgradeResult.Error.ToString());
        }

        ShowSuccess();

        List<string> MigrationList = SortedDateModifiedScripts(Path.Combine(scriptsPath, "Migrations"));
        foreach (string script in MigrationList)
        {
            Console.WriteLine(script);

            Console.WriteLine("Start executing migration scripts...");
            var upgrader =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsFromFileSystem(script)
                    .LogToConsole()
                    .JournalToSqlTable("dbo", "MigrationsJournal")
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                return ReturnError(result.Error.ToString());
            }
        }

        ShowSuccess();

        Console.WriteLine("Start executing postdeployment scripts...");
        string postdeploymentScriptsPath = Path.Combine(scriptsPath, "PostDeployment");
        var postDeploymentScriptsExecutor =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsFromFileSystem(postdeploymentScriptsPath, new FileSystemScriptOptions
                {
                    IncludeSubDirectories = true
                })
                .LogToConsole()
                .JournalTo(new NullJournal())
                .Build();

        var postdeploymentUpgradeResult = postDeploymentScriptsExecutor.PerformUpgrade();

        if (!postdeploymentUpgradeResult.Successful)
        {
            return ReturnError(postdeploymentUpgradeResult.Error.ToString());
        }

        ShowSuccess();

        
        return 0;
    }

    private static void ShowSuccess()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
    }

    private static int ReturnError(string error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ResetColor();
        return -1;
    }
}