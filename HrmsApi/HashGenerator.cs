using System;

namespace HrmsApi;

/// <summary>
/// Utility to generate BCrypt password hash
/// Usage: dotnet run --project HrmsApi HashGenerator
/// </summary>
public class HashGenerator
{
    public static void GenerateHash()
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("BCrypt Password Hash Generator");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        
        string password = "admin123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"BCrypt Hash:");
        Console.WriteLine(hash);
        Console.WriteLine();
        
        // Verify it works
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verification Test: {(isValid ? "PASS ✓" : "FAIL ✗")}");
        Console.WriteLine();
        
        Console.WriteLine("SQL Update Command:");
        Console.WriteLine("-------------------");
        Console.WriteLine($"UPDATE Users SET PasswordHash = '{hash}' WHERE Username = 'mohan.net88@gmail.com';");
        Console.WriteLine();
        Console.WriteLine("===========================================");
    }
}
