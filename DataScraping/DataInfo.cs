using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataScraping
{
    public class DataInfo
    {
        [Key]
        public Guid Id { get; set; }

        public string CompanyName { get; set; }
        public string CUI { get; set; }
        public string RegistDate { get; set; }
        public string MFINANCE { get; set; }
        public string Localitate { get; set; }
        public string District { get; set; }
        public string CodPostal { get; set; }
        public string SediuSocial { get; set; }
        public string CompanyStatus { get; set; }
        public string SocialCapital { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Web { get; set; }
        public string ExtendedData { get; set; }
        public string NrOfBranches { get; set; }
        public string Owners { get; set; }
        public string Url { get; set; }
    }

    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DS.db");
            options.UseSqlite($"Data Source={dbFilePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataInfo>().HasKey(d => d.Id);
            modelBuilder.Entity<DataInfo>().Property(d => d.CUI).IsRequired();
            modelBuilder.Entity<DataInfo>().Property(d => d.CompanyName);
            modelBuilder.Entity<DataInfo>().Property(d => d.RegistDate);
            modelBuilder.Entity<DataInfo>().Property(d => d.MFINANCE);
            modelBuilder.Entity<DataInfo>().Property(d => d.Localitate);
            modelBuilder.Entity<DataInfo>().Property(d => d.District);
            modelBuilder.Entity<DataInfo>().Property(d => d.CodPostal);
            modelBuilder.Entity<DataInfo>().Property(d => d.SediuSocial);
            modelBuilder.Entity<DataInfo>().Property(d => d.CompanyStatus);
            modelBuilder.Entity<DataInfo>().Property(d => d.SocialCapital);
            modelBuilder.Entity<DataInfo>().Property(d => d.Phone);
            modelBuilder.Entity<DataInfo>().Property(d => d.Email);
            modelBuilder.Entity<DataInfo>().Property(d => d.Web);
            modelBuilder.Entity<DataInfo>().Property(d => d.ExtendedData);
            modelBuilder.Entity<DataInfo>().Property(d => d.NrOfBranches);
            modelBuilder.Entity<DataInfo>().Property(d => d.Owners);

            modelBuilder.Entity<DataInfo>().HasIndex(d => d.CUI);
        }

        public DbSet<DataInfo> DataInfos { get; set; }
    }
}