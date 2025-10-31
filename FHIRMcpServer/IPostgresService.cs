namespace FHIRMcpServer;

public interface IPostgresService
{
    Task<string> PersistPatentData(string family, string given, string data);
}