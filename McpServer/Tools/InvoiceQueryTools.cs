using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public class InvoiceQueryTools
{
    private readonly ILogger<InvoiceQueryTools> _logger;
    private readonly IOdooClient _client;

    public InvoiceQueryTools(ILogger<InvoiceQueryTools> logger, IOdooClient client)
    {
        _logger = logger;
        _client = client;
    }
    

    [McpServerTool, Description("Query unpaid invoice in quarter")]
    public async Task<string> QueryUnpaidInvoice(string username, string password, int quarter, int year)
    {
        if (quarter < 1 || quarter > 4)
        {
            _logger.LogError("Invalid quarter");
            return "Error due to invalid quarter";
        }

        StringBuilder start = new StringBuilder(year.ToString());
        StringBuilder end = new StringBuilder(year.ToString());
        if (quarter == 1)
        {
            start.Append("-01-01");
            end.Append("-03-31");
        } else if (quarter == 2)
        {
            start.Append("-04-01");
            end.Append("-06-30");
        } else if (quarter == 3)
        {
            start.Append("-07-01");
            end.Append("-09-30");
        } else
        {
            start.Append("-10-01");
            end.Append("-12-31");
        }

        try
        {
            _logger.LogInformation("Start login to authenticated before query");   
            int uid = await _client.LoginOdoo(username, password);
            
            _logger.LogInformation("The result id is {Uid}", uid);
            
            _logger.LogInformation("Start query unpaid invoice in the range {start} - {end}", start.ToString(), end.ToString());
            return await _client.QueryUnpaidInvoiceInQuarter(uid, password, start.ToString(), end.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return "Error while querying unpaid invoice";
        }
    }
    
    
}