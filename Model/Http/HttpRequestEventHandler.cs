using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Model.Http
{
    public delegate Task HttpRequestEventHandler(object sender, HttpRequestEventArgs e);
}
