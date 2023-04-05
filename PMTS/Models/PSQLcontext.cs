using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using PMTS.Models;

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
        public DbSet<PMTS.Models.Tournament> Tournament { get; set; }
        public DbSet<PMTS.Models.Bird> Bird { get; set; } = default!;
    }

    public class User
    {
        public int Id { get; set;}

        [Required(ErrorMessage = "Įveskite naudotojo vardą.")]
        [MaxLength(20, ErrorMessage = "Naudotojo vardas per ilgas.")]
        [MinLength(3, ErrorMessage = "Naudotojo vardas per trumpas.")]
        public string Name { get; set;}

        [Required(ErrorMessage = "Įveskite el. pašto adresą.")]
        [MaxLength(320, ErrorMessage = "El. paštas per ilgas.")]
        [MinLength(6, ErrorMessage = "El. paštas per trumpas.")]
        public string Email { get; set;}

        [Required(ErrorMessage = "Įveskite slaptažodį.")]
        [MaxLength(72, ErrorMessage = "Slaptažodis per ilgas.")]
        [MinLength(8, ErrorMessage = "Slaptažodis turi būti bent 8 simbolių ilgio.")]
        public string Password { get; set;}
    }
}