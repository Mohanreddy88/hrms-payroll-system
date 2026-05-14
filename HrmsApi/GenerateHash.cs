// Quick utility to generate BCrypt hash
// Run: dotnet run --project HrmsApi GenerateHash.cs

using System;

class GenerateHash
{
    static void Main(string[] args)
    {
        var password = "admin123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine("Password: " + password);
        Console.WriteLine("BCrypt Hash: " + hash);
        Console.WriteLine("");
        Console.WriteLine("Verification: " + BCrypt.Net.BCrypt.Verify(password, hash));
    }
}
