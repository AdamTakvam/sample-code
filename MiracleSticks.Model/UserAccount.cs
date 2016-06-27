using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MiracleSticks.Model
{
    public class UserAccount
    {
        public UserAccount()
        {
            Registrations = new List<ServerEndPoint>();
        }

        public int Id { get; set; }

        public string UserName { get; set; }

        public string FirstName { get; set; }
        
        public string LastName { get; set; }

        public bool Administrator { get; set; }

        public string PasswordHash { get; set; }

        public string GroupID { get; set; }

        public DateTime Created { get; set; }

        public string Comments { get; set; }

        public List<ServerEndPoint> Registrations { get; set; }
    }
}
