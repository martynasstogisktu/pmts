using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PMTS.Models
{
    public class PSQLcontext : DbContext
    {
        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=localhost;Database=PMTS;Username=postgres;Password=VG$2zcF1&kLXS@JFaY");
        public PSQLcontext(DbContextOptions<PSQLcontext> options) 
            : base(options)
        { }
    }

    public class User
    {
        public int Id { get; set;}
        public string Name { get; set;}
        public string Email { get; set;}
        public string Password { get; set;}
    }
}
