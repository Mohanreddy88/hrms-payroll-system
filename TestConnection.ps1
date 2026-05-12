$connectionString = "Host=tramway.proxy.rlwy.net;Port=38982;Database=railway;Username=postgres;Password=xusGNETCKDhzNAyFZiUExKifFDhScohN;SSL Mode=Require;Trust Server Certificate=true"

Add-Type -Path "C:\Users\HP\.nuget\packages\npgsql\10.0.2\lib\net8.0\Npgsql.dll"

$conn = New-Object Npgsql.NpgsqlConnection($connectionString)
try {
    $conn.Open()
    Write-Host "Connected to PostgreSQL successfully!" -ForegroundColor Green
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"
    $reader = $cmd.ExecuteReader()
    
    Write-Host "Tables in database:" -ForegroundColor Cyan
    $count = 0
    while ($reader.Read()) {
        Write-Host "  - $($reader[0])"
        $count++
    }
    $reader.Close()
    
    if ($count -eq 0) {
        Write-Host "No tables found. Migrations may not have run yet." -ForegroundColor Yellow
    }
    else {
        Write-Host "Found $count tables" -ForegroundColor Green
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
finally {
    $conn.Close()
}
