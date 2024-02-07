﻿using MonsterTCG.Model.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Controller.Interface
{
    public interface IHttpController
    {
        public bool ProcessRequest(HttpRequestEventArgs e);
    }
}
