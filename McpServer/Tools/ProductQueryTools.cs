using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public class ProductQueryTools
{
    private readonly IOdooClient _client;
    private readonly ILogger<ProductQueryTools> _logger;

    public ProductQueryTools(IOdooClient client,  ILogger<ProductQueryTools> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    [McpServerTool]
    public async Task<string> QueryTopSellingProduct(string username, string apiKey)
    {
        try
        {
            _logger.LogInformation("Start login to authenticate user");
            var uid = await _client.LoginOdoo(username, apiKey);

            _logger.LogInformation("Start query top selling product");
            return await _client.QueryTopSellingProduct(uid, apiKey);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogInformation("Error while login to authenticate user");
            return ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return "There was an error while querying top selling product";
        }
       
    }
}