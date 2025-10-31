namespace FHIRMcpServer;

public interface IEpicClient
{
     Task<string> GetAccessToken();
     Task<string> GetInformationOfPatient(string family, string given, string birthdate, string accessToken);
}