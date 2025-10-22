using McpServer.Model;

namespace McpServer;

public interface IOdooClient
{
    Task<int> LoginOdoo(string username, string apiKey);
    Task<string> QueryUnpaidInvoiceInQuarter(int uid, string apiKey, string startRange, string endRange);
    Task<int> RetrieveProfitAndLossReportId(int uid, string apiKey);
    Task<int> RetrieveCompanyId(int uid, string apiKey);
    Task<string> RetrieveProfitAndLossReport(int uid, string apiKey,int reportId, int companyId, string startRange, string endRange);
    Task<string> RetriveSalesOrderInQuarter(int uid, string apiKey, string startRange, string endRange);
    
}