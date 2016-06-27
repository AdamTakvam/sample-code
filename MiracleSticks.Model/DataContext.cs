using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using MiracleSticks.Model;

namespace MiracleSticks.Model
{
    public class DataContext : DbContext
    {
        public DataContext(DbConnection conn)
            :base(conn, true)
        {
            Database.SetInitializer(new BlogContextInitializer());
        }

        public DbSet<UserAccount> Accounts { get; set; }
    }
}
