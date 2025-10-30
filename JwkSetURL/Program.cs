using System.Security.Cryptography.X509Certificates;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

Env.Load("../.env");

var keyId = Environment.GetEnvironmentVariable("EPIC:KEYID");

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string pemPath = Path.Combine(home, "publickey509.pem");

string pem = File.ReadAllText(pemPath);
var cert = X509Certificate2.CreateFromPem(pem);


// Extract RSA public key
using var rsa = cert.GetRSAPublicKey();

// Export modulus/exponent for JWK
var parameters = rsa.ExportParameters(false);

string Base64UrlEncode(byte[] input) =>
    Convert.ToBase64String(input)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

var jwk = new
{
    kty = "RSA",
    kid = keyId,
    use = "sig",
    alg = "RS384", // must match your signing algorithm
    n = Base64UrlEncode(parameters.Modulus),
    e = Base64UrlEncode(parameters.Exponent)
};

app.MapGet("/.well-known/jwks.json", () => new { keys = new[] { jwk } });

app.Run();