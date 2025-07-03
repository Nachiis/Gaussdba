using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussdb
{
    public class SqlSugar : Singleton<SqlSugar>, IDisposable
    {
        private readonly int Port = 8000;
        private readonly string Host = "110.41.119.192";
        private readonly string UserName = "db_user20";
        private readonly string Password = "db_user20@123";
        private readonly string Database = "db_zjut";
        private readonly string SearchPath = "db_user20";

        public SqlSugarClient? db;

        public void Init()
        {
            Connect();
        }

        public void Connect()
        {
            try
            {
                ConnectionConfig connectionConfig = new ConnectionConfig()
                {
                    ConnectionString = $"Host={Host};Port={Port};Username={UserName};Password={Password};Database={Database};SearchPath={SearchPath};No Reset On Close=true;",
                    DbType = DbType.GaussDB,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute,
                };
                db = new SqlSugarClient(connectionConfig);
            }
            catch (Exception e)
            {
                Utils.PrintError(e);
            }
            finally
            {
                if (db != null)
                {
                    Utils.PrintInfo($"Connected to {Host} successfully.");
                }
                else
                {
                    Utils.PrintError("Failed to connect to database.");
                }
            }
        }

        override public void Dispose()
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
                Utils.PrintInfo("Database connection disposed.");
            }
        }
    }
}
