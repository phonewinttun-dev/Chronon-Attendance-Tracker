using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    private const string BaseConn = "Host=db.dzeubdrqbzbudbblvxep.supabase.co;Database=postgres;Username=postgres;Password=ACSTSupabase1735#;SSL Mode=Require;Trust Server Certificate=true;Keepalive=30;Timeout=30;Command Timeout=30";

    static async Task Main(string[] args)
    {
        string connCurrentStr = $"{BaseConn};Port=6543";
        string connCurrentFixStr = $"{BaseConn};Port=6543;No Reset On Close=true;Max Auto Prepare=0";
        string connDirectStr = $"{BaseConn};Port=5432;Maximum Pool Size=10";

        Console.WriteLine("--- Starting DB Connection Tests ---");
        
        Console.WriteLine("\n[1] Testing Current Conn String (Port 6543 - Pooler without fix)...");
        await RunTestAsync(connCurrentStr);

        Console.WriteLine("\n[2] Testing Current Conn String with PgBouncer Fix (Port 6543 + No Reset On Close + Max Auto Prepared Items=0)...");
        await RunTestAsync(connCurrentFixStr);

        Console.WriteLine("\n[3] Testing Direct Conn String (Port 5432 + Max Pool Size = 10)...");
        await RunTestAsync(connDirectStr);
        
        Console.WriteLine("\n--- Tests Completed ---");
    }

    static async Task RunTestAsync(string connectionString)
    {
        // 1. Single warm-up connection
        try
        {
            var swWarm = Stopwatch.StartNew();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT 1", conn))
                {
                    await cmd.ExecuteScalarAsync();
                }
            }
            swWarm.Stop();
            Console.WriteLine($"Warm-up connection took: {swWarm.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warm-up failed: {ex.Message}");
            return;
        }

        // 2. Run 10 parallel queries to simulate concurrent requests
        int parallelCount = 10;
        var tasks = new Task[parallelCount];
        var swParallel = Stopwatch.StartNew();

        for (int i = 0; i < parallelCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(async () =>
            {
                var swEach = Stopwatch.StartNew();
                try
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        using (var cmd = new NpgsqlCommand("SELECT 1", conn))
                        {
                            await cmd.ExecuteScalarAsync();
                        }
                    }
                    swEach.Stop();
                    // Console.WriteLine($"  Query {index} took: {swEach.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    swEach.Stop();
                    Console.WriteLine($"  Query {index} failed after {swEach.ElapsedMilliseconds} ms: {ex.Message}");
                }
            });
        }

        await Task.WhenAll(tasks);
        swParallel.Stop();
        Console.WriteLine($"Parallel execution of {parallelCount} queries took: {swParallel.ElapsedMilliseconds} ms total");
    }
}
