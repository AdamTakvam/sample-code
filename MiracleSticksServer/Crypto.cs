using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MiracleSticksServer
{
    public class Crypto
    {
        public static string EncryptPassword(string password)
        {
            SHA1CryptoServiceProvider crypto = new SHA1CryptoServiceProvider();
            byte[] hashBytes = crypto.ComputeHash(Encoding.Unicode.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
