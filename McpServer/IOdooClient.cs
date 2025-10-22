using McpServer.Model;

namespace McpServer;

public interface IOdooClient
{
    Task<int> LoginOdoo(string username, string password);
    Task<string> QueryUnpaidInvoiceInQuarter(int uid, string password, string startRange, string endRange);
}