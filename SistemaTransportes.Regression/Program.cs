namespace SistemaTransportes.Regression;

public static class Program
{
    private static int _passed = 0;
    private static int _total = 0;

    public static void Check(bool condition, string message)
    {
        _total++;
        if (condition)
        {
            _passed++;
            Console.WriteLine($"[PASS] {message}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
            throw new Exception($"Check failed: {message}");
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running SqlChecks regression suite...\n");
        await SqlChecks.RunAsync();
        Console.WriteLine($"\nSUCCESS: All {_passed}/{_total} checks passed!");
    }
}
