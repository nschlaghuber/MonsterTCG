using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Util
{
    public static class PasswordUtil
    {
        public static string HashPassword(string password)
        {
            var test = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("")));
            return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password)));
        }
    }
}
