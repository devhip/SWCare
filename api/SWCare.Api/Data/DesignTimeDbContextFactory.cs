using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SWCare.Api.Data;

/// <summary>Used by dotnet ef only, so migrations do not need the app's configuration.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SWCareDbContext>
{
    public SWCareDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SWCareDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=SWCare;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new SWCareDbContext(options);
    }
}
