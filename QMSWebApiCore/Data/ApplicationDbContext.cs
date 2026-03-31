using Microsoft.EntityFrameworkCore;
using QMSWebApiCore.Models;

namespace QMSWebApiCore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //public DbSet<M_GateIn> Gates { get; set; }
        //public DbSet<M_GateOut> GateOuts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
    }
}
