using MonsterTCG.Controller.Interface;
using MonsterTCG.Model.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Controller
{
    public abstract class Controller : IHttpController
    {
        public abstract bool ProcessRequest(HttpRequestEventArgs e);
    }
}
