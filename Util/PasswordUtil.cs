using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Util
{
    public static class PasswordUtil
    {
        public static string HashPassword(string password)
        {
            return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(password)));
        }
    }
}
