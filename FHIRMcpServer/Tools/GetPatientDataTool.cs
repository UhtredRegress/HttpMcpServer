using System.ComponentModel;
using System.Globalization;
using FHIRMcpServer;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public class GetPatientDataTool
{
    private readonly IEpicClient _epicClient;
    private readonly IPostgresService _postgresService;

    public GetPatientDataTool(IEpicClient epicClient, IPostgresService postgresService)
    {
        _epicClient = epicClient;
        _postgresService = postgresService;
    }
    

    [McpServerTool, Description(
         "This tool helps get patient data, extract json result string to show how many patient found and " +
         "detail of information found, after that prompt to the user if they want to use the tool to persist data into postgres, keep result of this tools to be the input of Persist Data Tool call")]
    public async Task<string> GetPatientInformation(string family, string given, string birthdate)
    {
        try
        {
            if (string.IsNullOrEmpty(family) || string.IsNullOrEmpty(given) || string.IsNullOrEmpty(birthdate))
            {
                return "Bad request these field cannot be null";
            }

            if (!DateTime.TryParseExact(
                    birthdate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return "Bad request the birth date format must be yyyy-MM-dd";
            }
    
            var accessToken = await _epicClient.GetAccessToken();
            
            return await _epicClient.GetInformationOfPatient(family, given, birthdate, accessToken);
            
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    [McpServerTool,
     Description(
         "This tool help persistence data for patient. This use input which is the result from Get Patient Information tool and also use given and family name from this call. This tool cannot be called if user haven't made any successfully call to register")]
    public async Task<string> PersistPatientInformation(string family, string given, string jsonResult)
    {
        try
        {
            return await _postgresService.PersistPatentData(family, given, jsonResult);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}