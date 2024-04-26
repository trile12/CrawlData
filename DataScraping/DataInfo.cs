using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataScraping
{
    public class DataInfo
    {
        [Key]
        public Guid Id { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string Description { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext() : base(CreateConnection(), true) { }

        static DbConnection CreateConnection()
        {
            var connection = DbProviderFactories.GetFactory("System.Data.SqlClient").CreateConnection();
            connection.ConnectionString = "Data Source=DESKTOP-PMO3106;Initial Catalog=scrapeddb;User ID=sa;Password=123";
            return connection;
        }

        public DbSet<DataInfo> DataInfos { get; set; }
    }
}
