using System.Security.Cryptography;
using System.Text;

namespace TournamentApi.Security;

public class PasswordHasher
{
    public string Hash(string password)
    {
        
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(passwordBytes);

     
        return Convert.ToHexString(hashBytes);
    }

    public bool Verify(string password, string hash)
    {
        var computedHash = Hash(password);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}