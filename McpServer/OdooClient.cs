using System.Text;
using System.Text.Json;

namespace McpServer;

public class OdooClient : IOdooClient
{
    private readonly ILogger<OdooClient> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public OdooClient(ILogger<OdooClient> logger, IConfiguration config, HttpClient http)
    {
        _logger = logger;
        _config = config;
        _http = http;
    }
    
    public async Task<int> LoginOdoo(string username, string password)
    {
        _logger.LogInformation("Create payload to make http request to odoo api");
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "common",
                method = "login",
                args = new object[] { _config["ODOO:DATABASE"], username, password }
            },
            id = 1
        };
        
        var response = await _http.PostAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("result").GetInt32();
    }

    public async Task<string> QueryUnpaidInvoiceInQuarter(int uid, string password, string startRange, string endRange)
    {
        _logger.LogInformation("Create payload to make http request to odoo api");
        var domain = new object[]
        {
            new object[] { "move_type", "=", "out_invoice" },
            new object[] { "invoice_date", ">=", startRange },
            new object[] { "invoice_date", "<=", endRange },
            new object[] { "payment_state", "=", "not_paid" }
        };

        var queryPayload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "object",
                method = "execute_kw",
                args = new object[]
                {
                    _config["ODOO:DATABASE"],
                    uid,
                    password,
                    "account.move",
                    "search_read",
                    new object[] { domain },
                    new
                    {
                        fields = new[] { "name", "partner_id", "amount_total", "invoice_date", "payment_state" },
                        limit = 0
                    }
                }
            },
            id = 1
        };
        
        _logger.LogInformation("Start making http request to odoo api to retrieve ");
        var response = await _http.PostAsync(
            $"{_config["Odoo:BASE_URL"]}/jsonrpc",
            new StringContent(JsonSerializer.Serialize(queryPayload), Encoding.UTF8, "application/json")
        );

        return await response.Content.ReadAsStringAsync();
    }
}