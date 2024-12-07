using Microsoft.EntityFrameworkCore;
namespace wsn_keboo;
using wsn_keboo.Data;

public class YourDbContext : DbContext
{
    public DbSet<DataReceiveCom> DataReceiveComs { get; set; }

    public DbSet<Node> Nodes { get; set; }
    public YourDbContext()
    {
    }

    public YourDbContext(DbContextOptions<YourDbContext> options)
        : base(options)
    { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=DataReceiveComs.db");
        base.OnConfiguring(optionsBuilder);
    }

    
}
