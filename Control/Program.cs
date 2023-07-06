using DbUp;
using System.Reflection;

internal class Program
{
    static int Main(string[] args)
    {
        var connectionString =
            args.FirstOrDefault()
            ?? "Data Source=MARKOPC\\SQLEXPRESS;Initial Catalog=VC;Integrated Security=True;Encrypt=False";

        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }

}