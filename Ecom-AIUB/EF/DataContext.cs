using Ecom_AIUB.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecom_AIUB.EF
{
    public class DataContext : DbContext 
    {
        public DataContext(DbContextOptions options): base(options) {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Category { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Carts { get; set; }

    }
}
