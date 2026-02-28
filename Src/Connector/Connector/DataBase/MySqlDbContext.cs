using System;
using System.Data.Entity;
using MySql.Data.EntityFramework;

namespace Connector.DataBase
{
    /// <summary>
    /// Minimal DbContext สำหรับ MySQL ที่ไม่มี model validation
    /// ใช้สำหรับ raw SQL queries เท่านั้น
    /// </summary>
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class MySqlDbContext : DbContext
    {
        public MySqlDbContext(string connectionString) : base(connectionString)
        {
            Database.SetInitializer<MySqlDbContext>(null);
            Configuration.ValidateOnSaveEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // ไม่ต้อง build model เลยสำหรับ MySQL
            // ใช้เฉพาะ Database.SqlQuery และ Database.ExecuteSqlCommand
        }
    }
}
