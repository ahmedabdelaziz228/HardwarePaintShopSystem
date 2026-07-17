using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run --project tools/LicenseGenerator -- <customer> <machine-id> [yyyy-MM-dd|permanent] [edition]");
    return 1;
}

var customer = args[0].Trim();
var machineId = args[1].Trim().ToUpperInvariant();
var expiration = args.Length > 2 ? args[2].Trim() : "permanent";
var edition = args.Length > 3 ? args[3].Trim() : "Retail";
DateTime? expiresAt = null;
if (!expiration.Equals("permanent", StringComparison.OrdinalIgnoreCase))
{
    if (!DateTime.TryParse(expiration, out var parsed))
    {
        Console.Error.WriteLine("Expiration must be yyyy-MM-dd or permanent.");
        return 2;
    }
    expiresAt = DateTime.SpecifyKind(parsed.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
}

var keyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "seller-tools", "license-private-key.pem"));
if (!File.Exists(keyPath))
{
    Console.Error.WriteLine($"Private key not found: {keyPath}");
    return 3;
}
var payload = JsonSerializer.SerializeToUtf8Bytes(new
{
    CustomerName = customer, MachineId = machineId, Edition = edition, ExpiresAt = expiresAt
});
using var rsa = RSA.Create();
rsa.ImportFromPem(await File.ReadAllTextAsync(keyPath));
var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
Console.WriteLine($"{Base64Url(payload)}.{Base64Url(signature)}");
return 0;

static string Base64Url(byte[] value)
    => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
