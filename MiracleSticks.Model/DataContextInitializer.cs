using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using MiracleSticks.Model;

namespace MiracleSticks.Model
{
    public class BlogContextInitializer : DropCreateDatabaseIfModelChanges<DataContext>
    {
        protected override void Seed(DataContext context)
        {
            // Tom
            var acct = new UserAccount
            {
                UserName = "TomCharvet",
                FirstName = "Tom",
                LastName = "Charvet",
                GroupID = "1",
                Administrator = true,
                Created = DateTime.Now
            };
            context.Accounts.Add(acct);

            // Adam
            acct = new UserAccount
            {
                UserName = "AdamTakvam",
                FirstName = "Adam",
                LastName = "Takvam",
                Administrator = true,
                PasswordHash = "Ga3quUA/KofcTsDBfW3U0h6qt/U=",
                GroupID = "2",
                Created = DateTime.Now
            };
            context.Accounts.Add(acct);

            base.Seed(context);
        }
    }
}
