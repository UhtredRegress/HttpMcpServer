using FHIRMcpServer.DbContext;
using FHIRMcpServer.Model;

namespace FHIRMcpServer;

public class PostgresService : IPostgresService
{
    private readonly ApplicationDbContext _context;

    public PostgresService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<string> PersistPatentData(string family, string given, string data)
    {
        var patientData = new Patient() { FamilyName = family, GivenName = given, Data = data };
        await _context.Patients.AddAsync(patientData);
        await _context.SaveChangesAsync();
        return "Successfully persisted patient data";
    }
}