using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PingMe.Data;

namespace PingMe.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Server=127.0.0.1;Port=3307;Database=pingme_dev;User=root;Password=root;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=None;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString,
            new MySqlServerVersion(new Version(8, 0, 46)));

        return new AppDbContext(optionsBuilder.Options);
    }
}