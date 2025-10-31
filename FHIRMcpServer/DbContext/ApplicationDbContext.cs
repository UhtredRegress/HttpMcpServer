using FHIRMcpServer.Model;
using Microsoft.EntityFrameworkCore;

namespace FHIRMcpServer.DbContext;

public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
           
    }
    
    public DbSet<Patient> Patients { get; set; }
    
}