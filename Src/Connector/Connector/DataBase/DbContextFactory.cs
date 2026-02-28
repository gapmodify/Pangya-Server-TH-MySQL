using System;
using System.Data.Entity;

namespace Connector.DataBase
{
    public static class DbContextFactory
    {
        public static DbContext Create()
        {
            string dbEngine = DatabaseConfig.GetDbEngine();
            
            if (dbEngine == "mysql")
            {
                return new MySqlDbContext(DatabaseConfig.GetConnectionString());
            }
            else
            {
                throw new NotSupportedException(
                    $"Database engine '{dbEngine}' is not supported. Only MySQL is supported. " +
                    "Please set DBENGINE=mysql in your configuration file.");
            }
        }
        
        public static DbContext CreateX()
        {
            string dbEngine = DatabaseConfig.GetDbEngine();
            
            if (dbEngine == "mysql")
            {
                return new MySqlDbContext(DatabaseConfig.GetConnectionString());
            }
            else
            {
                throw new NotSupportedException(
                    $"Database engine '{dbEngine}' is not supported. Only MySQL is supported. " +
                    "Please set DBENGINE=mysql in your configuration file.");
            }
        }
        
        public static MySqlDbContext CreateMySql()
        {
            return new MySqlDbContext(DatabaseConfig.GetConnectionString());
        }
        
        public static bool IsMySql()
        {
            return DatabaseConfig.GetDbEngine() == "mysql";
        }
    }
}
