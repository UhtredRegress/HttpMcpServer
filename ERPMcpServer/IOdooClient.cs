using McpServer.Model;

namespace McpServer;

public interface IOdooClient
{
    Task<int> LoginOdoo(string username, string apiKey);
    Task<string> QueryUnpaidInvoiceInQuarter(int uid, string apiKey, string startRange, string endRange);
    Task<int> RetrieveCompanyId(int uid, string apiKey);
    Task<string> RetrieveRevenueAndExpensesInQuarter(int uid, string apiKey, int companyId, string startRange, string endRange);
    Task<string> RetriveSalesOrderInQuarter(int uid, string apiKey, string startRange, string endRange); 
    Task<string> QueryTopSellingProduct(int uid, string apiKey);
    Task<Invoice> GetUnpaidInvoiceByName(int uid, string apiKey, string invoiceName);
    Task<int> GetJournalIdByName(int uid, string apiKey, string journalName);
    Task<int> CreatePaymentRecord(int uid, string apiKey, Invoice invoice, int journalId, string invoiceName);
    Task<bool> PostPaymentWithId(int uid, string apiKey, int paymentId, int invoiceId);
}