using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Util
{
    public static class TokenUtil
    {
        public static string GetTokenFromUsername(string username)
        {
            return username + "-mtcgToken";
        }

        public static string GetUsernameFromToken(string token)
        {
            return token.Replace("-mtcgToken", null);
        }
    }
}
