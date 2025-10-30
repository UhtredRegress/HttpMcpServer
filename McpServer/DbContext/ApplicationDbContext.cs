using McpServer.Model;
using Microsoft.EntityFrameworkCore;

namespace McpServer.DbContext;

public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
           
    }
    
    public DbSet<Patient> Patients { get; set; }
    
}