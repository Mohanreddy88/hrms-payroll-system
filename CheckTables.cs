using Npgsql;

var conn = "Host=tramway.proxy.rlwy.net;Port=38982;Database=railway;Username=postgres;Password=xusGNETCKDhzNAyFZiUExKifFDhScohN;SSL Mode=Require;Trust Server Certificate=true";

using var pgConn = new NpgsqlConnection(conn);
try
{
    pgConn.Open();
    Console.WriteLine("✓ Connected to PostgreSQL\n");
    
    var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name", pgConn);
    using var reader = cmd.ExecuteReader();
    
    Console.WriteLine("Tables in database:");
    int count = 0;
    while (reader.Read())
    {
        Console.WriteLine($"  - {reader.GetString(0)}");
        count++;
    }
    
    if (count == 0)
    {
        Console.WriteLine("\n⚠ No tables found. Migrations haven't run yet.");
        Console.WriteLine("Check Railway deployment logs to see if migration failed.");
    }
    else
    {
        Console.WriteLine($"\n✓ Found {count} tables");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
