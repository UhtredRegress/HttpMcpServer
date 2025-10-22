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
    
    public async Task<int> LoginOdoo(string username, string apiKey)
    {
        _logger.LogInformation("Create payload to make http request to odoo api to login to the server");
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "common",
                method = "login",
                args = new object[] { _config["ODOO:DATABASE"], username, apiKey }
            },
            id = 1
        };
        
        _logger.LogInformation("Start making http request to odoo api to login to the server");
        var response = await _http.PostAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        
        var result = doc.RootElement.GetProperty("result");

        if (result.ValueKind == JsonValueKind.False)
        {
            throw new InvalidDataException("The login username and password is not correct, please try again");
        }
        else
        {
            return result.GetInt32();
        }
    }

    public async Task<string> QueryUnpaidInvoiceInQuarter(int uid, string apiKey, string startRange, string endRange)
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
                    apiKey,
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
            id = 2
        };
        
        _logger.LogInformation("Start making http request to odoo api to retrieve ");
        var response = await _http.PostAsync(
            $"{_config["ODOO:BASE_URL"]}/jsonrpc",
            new StringContent(JsonSerializer.Serialize(queryPayload), Encoding.UTF8, "application/json")
        );

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<int> RetrieveProfitAndLossReportId(int uid, string apiKey)
    {
        
        _logger.LogInformation("Create payload to make request to odoo to get the report id ");
        var payload = new
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
                    apiKey,
                    "account.report",
                    "search",
                    new object[]
                    {
                        new object[]
                        {
                            new object[] { "name", "=", "Profit and Loss" }
                        } 
                    }
                }
            },
            id = 1
        };
        
        _logger.LogInformation("Start making http request to get the report id");
        var res = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
            
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var element =  json.GetProperty("result");
        if (element.GetArrayLength() <= 0)
        {
            _logger.LogInformation("Response to request the report id is broken");
            throw new InvalidDataException("Request to the report id is broken, please check report id or api key you provided");
        }
        
        return element.GetInt32();
    }

    public async Task<int> RetrieveCompanyId(int uid, string apiKey)
    {
        _logger.LogInformation("Create payload to make request to odoo api");
        var payload = new
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
                    uid,                      // the authenticated user ID
                    apiKey,                   // the user’s password or API key
                    "res.users",              // the model
                    "read",                   // the method
                    new object[] { uid },     // record IDs to read
                    new
                    {
                        fields = new string[] { "company_id" } // fields to fetch
                    }
                }
            },
            id = 1
        };
        
        var result = await _http.PostAsJsonAsync($"{_config["BASE_URL"]}/jsonrpc",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        var json = await result.Content.ReadFromJsonAsync<JsonElement>();
        var res = json.GetProperty("result")[0];
        if (json.TryGetProperty("error", out var error))
        {
            _logger.LogInformation(error.ToString());
            throw new InvalidDataException(error.ToString());
        }

        return res.GetProperty("company_id")[0].GetInt32();
    }

    public async Task<string> RetrieveProfitAndLossReport(int uid, string apiKey,int reportId, int companyId, string startRange, string endRange)
    {
        _logger.LogInformation("Create payload to make request to odoo api");
        var payload = new
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
                    apiKey,
                    "account.financial.html.report",
                    "get_html",
                    new object[]
                    {
                        reportId, // Example: 2 (the id of the Profit & Loss report)
                        new Dictionary<string, object>
                        {
                            { "date_from", "2025-01-01" },
                            { "date_to", "2025-03-31" },
                            { "company_id", companyId },
                            { "comparison", false }
                        }
                    }
                }
            },
            id = 1
        };
        
        _logger.LogInformation("Start making http request to get the report id");
        
        var response = await _http.PostAsJsonAsync($"{_config["BASE_URL"]}/jsonrpc", 
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        if (json.TryGetProperty("error", out var error))
        {
            _logger.LogInformation(error.ToString());
            throw new InvalidDataException(error.ToString());
        }
        
        return json.GetProperty("result").GetProperty("html")[0].GetString();
    }

    public async Task<string> RetriveSalesOrderInQuarter(int uid, string apiKey, string startRange, string endRange)
    {
        _logger.LogInformation("Create payload to make request to odoo ");
        var payload = new
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
                    apiKey,
                    "sale.order",
                    "search_read",
                    new object[]
                    {
                        new object[]
                        {
                            new object[] { "date_order", ">=", startRange },
                            new object[] { "date_order", "<=", endRange },
                            new object[] { "state", "in", new [] { "sale", "done" } }
                        }
                    },
                    new Dictionary<string, object>
                    {
                        { "fields", new string[] { "name", "partner_id", "amount_total", "date_order", "state" } },
                        { "limit", 0 },
                        { "order", "date_order desc" }
                    }
                }
            },
            id = 1
        };

        _logger.LogInformation("Start making http request to get the report id");
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", 
            payload);
        return await response.Content.ReadAsStringAsync();
    }
}