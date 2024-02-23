using MonsterTCG.Model.Http;

namespace MonsterTCG.Controller
{
    public abstract class Controller
    {
        public abstract Task<bool> ProcessRequest(HttpRequestEventArgs e);
    }
}
