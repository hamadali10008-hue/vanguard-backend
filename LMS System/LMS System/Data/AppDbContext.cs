using LMS_System.Models;
using Microsoft.EntityFrameworkCore;

namespace LMS_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        
    }
}