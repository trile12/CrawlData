using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Windows.Shapes;

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
    }

    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DS.db");
            options.UseSqlite($"Data Source={dbFilePath}");
        }

        public DbSet<DataInfo> DataInfos { get; set; }
    }
}
