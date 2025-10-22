using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public class SalesOrderTools
{
    private readonly ILogger<SalesOrderTools> _logger;
    private readonly IOdooClient _odoClient;

    public SalesOrderTools(IOdooClient odoClient, ILogger<SalesOrderTools> logger)
    {
        _odoClient = odoClient;
        _logger = logger;
    }
    
    [McpServerTool]
    public async Task<string> GetSalesOrder(string username, string apiKey, int quarter, int year)
    {
        try
        {
            _logger.LogInformation("Start authenticate user");
            var uid = await _odoClient.LoginOdoo(username, apiKey);

            (string start, string end) = QuarterHelper.RetrieveQuarterRangeInTheYear(quarter, year);
            _logger.LogInformation("Start query sales order in the range ");
            return await _odoClient.RetriveSalesOrderInQuarter(uid, apiKey, start, end);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogInformation(ex.Message);
            return ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return "There was an error while performing operation please try again later";
        }
    }
}