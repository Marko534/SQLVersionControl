// DbUp Migrator console application
using DbUp.Helpers;
using DbUp.ScriptProviders;
using DbUp;
using DbUp.Engine;
using System.Data;

class test : IJournal
{
    void IJournal.EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
    {
        throw new NotImplementedException();
    }

    string[] IJournal.GetExecutedScripts()
    {
        throw new NotImplementedException();
    }

    void IJournal.StoreExecutedScript(SqlScript script, Func<IDbCommand> dbCommandFactory)
    {
        throw new NotImplementedException();
    }
}

internal class Program
{
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
             connectionString = "Data Source=MARKOPC\\SQLEXPRESS;Initial Catalog=VC;Integrated Security=True;Encrypt=False";
             scriptsPath = "C:\\Users\\38975\\Desktop\\Control\\Control\\Script\\";
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

        Console.WriteLine("Start executing migration scripts...");
        var migrationScriptsPath = Path.Combine(scriptsPath, "Migrations");
        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsFromFileSystem(migrationScriptsPath, new FileSystemScriptOptions
                {
                    IncludeSubDirectories = true
                })
                .LogToConsole()
                .JournalToSqlTable("dbo", "MigrationsJournal")
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            return ReturnError(result.Error.ToString());
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
            return ReturnError(result.Error.ToString());
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