using System.Text;
using System.Text.Json;
using McpServer.Model;

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
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
        
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
                    _config.GetValue<string>("ODOO:DATABASE") ?? throw new InvalidDataException("Error with configuration database name is null in env"),
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
        
        var result = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc",
            payload);
        var json = await result.Content.ReadFromJsonAsync<JsonElement>();
        var res = json.GetProperty("result")[0];
        if (json.TryGetProperty("error", out var error))
        {
            _logger.LogInformation(error.ToString());
            throw new InvalidDataException(error.ToString());
        }

        return res.GetProperty("company_id")[0].GetInt32();
    }

    public async Task<string> RetrieveRevenueAndExpensesInQuarter(int uid, string apiKey, int companyId, string startRange, string endRange)
    {
        _logger.LogInformation("Create payload to make request to odoo api");
        
        
        
        var domain = new List<object[]>
        {
            new object[] { "date", ">=", startRange },
            new object[] { "date", "<=", endRange },
            new object[] { "move_id.state", "=", "posted" },
            new object[] { "account_id.internal_group", "in", new[] { "income", "expense" } },
            new object[] {"company_id", "=", companyId}
        };
        
        // Query move lines
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
                    "account.move.line",
                    "search_read",
                    new object[] { domain.ToArray() },
                    new Dictionary<string, object>
                    {
                        { "fields", new[] { "account_id", "debit", "credit", "balance", "date", "name" } }
                    }
                }
            },
            id = 2
        };

        
        _logger.LogInformation("Start making http request to get the report id");
        
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        if (json.TryGetProperty("error", out var error))
        {
            _logger.LogInformation(error.ToString());
            throw new InvalidDataException(error.ToString());
        }
        
        return string.Join(',',json.GetProperty("result").EnumerateArray());
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

        _logger.LogInformation("Start making http request to get sales order");
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", 
            payload);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> QueryTopSellingProduct(int uid, string apiKey)
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
                    "sale.order.line",
                    "read_group",
                    new object[]
                    {
                        new object[]
                        {
                            new object[] { "state", "in", new string[] { "sale", "done" } }
                        }, // domain
                        new string[] { "product_id", "product_uom_qty:sum" }, // fields
                        new string[] { "product_id" }, // groupby
                        0, // offset    
                        10, // limit
                        "product_uom_qty desc" // orderby
                    }
                }
            },
            id = 1
        };
        
        _logger.LogInformation("Start making http request to get the top selling product");
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", 
            payload);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<Invoice> GetUnpaidInvoiceByName(int uid, string apiKey, string invoiceName)
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
                    "account.move",
                    "search_read",
                    new object[]
                    {
                        new object[]
                        {
                            new object[] { "name", "=", invoiceName }
                        }
                    },
                    new
                    {
                        fields = new[] { "id", "partner_id", "amount_total", "currency_id" },
                        limit = 1
                    }
                }
            }
        };

        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
        var json = await response.Content.ReadAsStringAsync(); 
        
        using var obj = JsonDocument.Parse(json);

        if (obj.RootElement.TryGetProperty("error", out var error))
        {
            _logger.LogInformation("There was error while trying to get the invoice with the name {InvoiceName}", invoiceName);
            throw new InvalidDataException(error.GetString());
        }
        
        var resultJson = obj.RootElement.GetProperty("result");

     
        if (resultJson.GetArrayLength() <= 0 )
        {
            _logger.LogInformation("Not found any result with the invoice name {InvoiceName}", invoiceName);
            throw new InvalidDataException($"Not found unpaid invoice with the name {invoiceName}");
        }
        
        
        return JsonSerializer.Deserialize<Invoice>(resultJson[0]);
    }

    public async Task<int> GetJournalIdByName(int uid, string apiKey, string journalName)
    {
        _logger.LogInformation("Create payload to make request by journal id by {name}", journalName);
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
                    _config["ODOO:DATABASE"],  // Your database name
                    uid,                       // User ID (from login)
                    apiKey,                    // API key or password
                    "account.journal",         // Model name
                    "search_read",             // Method name
                    new object[]
                    {
                        new object[]
                        {
                            new object[] { "name", "ilike", journalName } // <-- search by name
                        }
                    },
                    new
                    {
                        fields = new[] { "id", "name", "type" },
                        limit = 1
                    }
                }
            }
        };
        
        _logger.LogInformation("Start making http request to get the journal id");
        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload); 
        var json = await response.Content.ReadAsStringAsync();
        
        using var obj = JsonDocument.Parse(json);
        if (obj.RootElement.TryGetProperty("error", out var error))
        {
            _logger.LogInformation("Error while trying to make request");
            throw new InvalidDataException(error.GetString());
        }
        
        return obj.RootElement.GetProperty("result")[0].GetProperty("id").GetInt32();
    }

    public async Task<int> CreatePaymentRecord(int uid, string apiKey, Invoice invoice, int journalId, string invoiceName)
    {
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
                    "account.payment.register",
                    "create",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "amount", invoice.Amount }, // Payment amount
                            { "journal_id", journalId },   // Bank or Cash Journal ID
                            { "payment_date", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                            { "communication", $"Payment for Invoice {invoiceName}" }
                        }
                    },
                    new Dictionary<string, object> // CONTEXT
                    {
                        { "context", new Dictionary<string, object>
                            {
                                { "active_model", "account.move" },
                                { "active_ids", new int[] { invoice.Id } }
                            }
                        }
                    }
                }
            }
        };

        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
        var json = await response.Content.ReadAsStringAsync();  
        
        using var obj = JsonDocument.Parse(json);

        if (obj.RootElement.TryGetProperty("error", out var error))
        {
            _logger.LogInformation("Error while trying to make request");
            throw new InvalidDataException(error.GetRawText());
        }
        
        return obj.RootElement.GetProperty("result").GetInt32();
    }

    public async Task<bool> PostPaymentWithId(int uid, string apiKey, int paymentId, int invoiceId)
    {
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
                    "account.payment.register",
                    "action_create_payments",
                    new object[] { paymentId },
                    new Dictionary<string, object>
                    {
                        { "context", new Dictionary<string, object>
                            {
                                { "active_model", "account.move" },
                                { "active_ids", new int[] { invoiceId } }
                            }
                        }
                    }
                }
            }
        };

        var response = await _http.PostAsJsonAsync($"{_config["ODOO:BASE_URL"]}/jsonrpc", payload);
        var json = await response.Content.ReadAsStringAsync();  

        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result");
        return result.GetProperty("res_id").ValueKind == JsonValueKind.Number && result.GetProperty("res_id").GetInt32() != 0;
    }
}