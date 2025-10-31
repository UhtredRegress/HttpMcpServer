using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public class RetrieveReportTools
{
    private readonly ILogger<RetrieveReportTools> _logger;
    private readonly IOdooClient _client;

    public RetrieveReportTools(ILogger<RetrieveReportTools> logger, IOdooClient client)
    {
        _logger = logger;
        _client = client;
    }
    
    [McpServerTool, Description("This tool help aggregate total revenue and expenses in a quarter")]
    public async Task<string> GetRevenueAndExpensesInQuarter(string username, string apikey, int quarter, int year)
    {
        try
        {
            _logger.LogInformation("Start login to odoo to retrieve uid");
            var uid = await _client.LoginOdoo(username, apikey);
            
            _logger.LogInformation("Retrieve company id of user");
            var companyId = await _client.RetrieveCompanyId(uid, apikey);

            (string start, string end) = QuarterHelper.RetrieveQuarterRangeInTheYear(quarter, year);

            _logger.LogInformation("Retrieve report content");
            return await _client.RetrieveRevenueAndExpensesInQuarter(uid, apikey, companyId, start, end);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogInformation(ex.Message);
            return ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return "There was an error retrieving report";
        }
    }
}