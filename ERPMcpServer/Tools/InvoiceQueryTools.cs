using System.ComponentModel;
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
    public async Task<string> QueryUnpaidInvoice(string username, string apiKey, int quarter, int year)
    {
       
        try
        {
            (string start, string end) = QuarterHelper.RetrieveQuarterRangeInTheYear(quarter, year);
            
            _logger.LogInformation("Start login to authenticated before query");
            int uid = await _client.LoginOdoo(username, apiKey);

            _logger.LogInformation("The result id is {Uid}", uid);

            _logger.LogInformation("Start query unpaid invoice in the range {start} - {end}", start.ToString(),
                end.ToString());
            return await _client.QueryUnpaidInvoiceInQuarter(uid, apiKey, start.ToString(), end.ToString());
        }
        catch (InvalidDataException ex)
        {
            _logger.LogInformation("Authentication process is failed");
            return ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return "Error while querying unpaid invoice";
        }
    }


    [McpServerTool, Description("Mark unpaid invoice as paid")]
    public async Task<string> MarkPaidInvoice(string username, string apiKey, string invoiceName , string journalName)
    {
        try
        {
            _logger.LogInformation("Start authenticate user before query");
            int uid = await _client.LoginOdoo(username, apiKey);

            _logger.LogInformation("Start find unpaid invoice");
            var foundUnpaidInvoice = await _client.GetUnpaidInvoiceByName(uid, apiKey, invoiceName);
            
            _logger.LogInformation("Find journal id by journal name");
            var journalId = await _client.GetJournalIdByName(uid, apiKey, journalName);
            
            _logger.LogInformation("Create payment record in database");
            var paymentId = await _client.CreatePaymentRecord(uid, apiKey, foundUnpaidInvoice, journalId, invoiceName);
            
            _logger.LogInformation("Post payment to mark as paid");
            var result = await _client.PostPaymentWithId(uid, apiKey, paymentId, foundUnpaidInvoice.Id);

            if (result == false)
            {
                return "Cannot mark this invoice as paid";
            }
            return "Success";
        }
        catch (InvalidDataException ex)
        {
            _logger.LogInformation("Catch Invalid Data Exception from the caller");
            return ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return "Error while mark unpaid invoice as paid";
        }
    }
}