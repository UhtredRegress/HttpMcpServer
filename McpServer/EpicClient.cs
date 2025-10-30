using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace McpServer;

public class EpicClient : IEpicClient
{
    private readonly IConfiguration _config;
    private readonly HttpClient _client;
    private readonly ILogger<EpicClient> _logger;

    public EpicClient(IConfiguration config,  HttpClient client, ILogger<EpicClient> logger)
    {
        _config = config;
        _client = client;
        _logger = logger;
    }
    
    public async Task<string> GetAccessToken()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string pemPath = Path.Combine(home, "privatekey.pem");
        string pem = File.ReadAllText(pemPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha384);
        
        var payload = new JwtPayload
        {
            { "iss", _config["EPIC:CLIENT_ID"] },
            { "sub", _config["EPIC:CLIENT_ID"] },
            { "aud", _config["EPIC:AUDIENCE"] },
            { "jti", Guid.NewGuid().ToString() },
            { "exp", DateTimeOffset.UtcNow.AddMinutes(4).ToUnixTimeSeconds() }
        };
        
        var header = new JwtHeader(credentials);
        var jwt = new JwtSecurityToken(header, payload);
        
        var handler = new JwtSecurityTokenHandler();
        var assertion = handler.WriteToken(jwt);
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _config["EPIC:CLIENT_ID"]),
            new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", assertion )
        });
        
       
        var response = await _client.PostAsync("https://fhir.epic.com/interconnect-fhir-oauth/oauth2/token", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Epic token request failed: {response.StatusCode} - {responseBody}");
            throw new Exception($"Epic token request failed: {response.StatusCode} - {responseBody}");
        }
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    public async Task<string> GetInformationOfPatient(string family, string given, string birthdate, string accessToken)
    {
        if (string.IsNullOrEmpty(_config["EPIC:HOST"]))
        {
            _logger.LogError("Cannot get the EPIC:HOST environment key value");
            throw new Exception("The epic host is not found in the environment");
        }  
        
        _client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
        _client.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", accessToken);
        
        string url = _config["EPIC:HOST"] + "api/FHIR/R4/Patient?" + $"family={family}&given={given}&birthdate={birthdate}";
        _logger.LogInformation("Epic host request received: {url}", url);
        var response = await _client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}