using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core.CustomTypeProviders;


namespace CarLotDB
{
    [DynamicLinqType]
    public class CarLotContext : DbContext
    {
        public DbSet<Auto> Autos { get; set; }
        public DbSet<Collision> Collisions { get; set; }

        public CarLotContext(DbContextOptions<CarLotContext> options)
            : base(options) { }
    }
}