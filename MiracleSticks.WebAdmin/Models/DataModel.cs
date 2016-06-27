using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using MiracleSticks.Model;

namespace MiracleSticks.WebAdmin.Models
{
    public static class DataModel
    {
        private static SqlConnection conn = null;
        private static readonly object initLock = new object();
        
        public static DataContext CreateContext()
        { 
            lock (initLock)
            {
                if (conn == null)
                {
                    string dbConnectionName = ConfigurationManager.AppSettings["DbConnection"];
                    if (String.IsNullOrEmpty(dbConnectionName))
                        throw new ConfigurationErrorsException("No database connection specified. Add an appSettings entry \"DbConnection\" in web.config which indicates the connection string you want to use.");

                    conn = new SqlConnection(ConfigurationManager.ConnectionStrings[dbConnectionName].ConnectionString);
                }
                return new DataContext(conn);
            }
        }

        public static void Dispose()
        {
            lock(initLock)
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                }
            }
        }
    }
}