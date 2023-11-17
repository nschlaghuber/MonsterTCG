using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Model.Http
{
    public class HttpRequestEventArgs : EventArgs
    {
        protected TcpClient _Client;
        public HttpRequestEventArgs(TcpClient client, HttpRequest request)
        {
            _Client = client;
            Request = request;
        }
        public virtual HttpRequest Request { get; private set; }

        public virtual void Reply(HttpStatusCode status, string? payload = null)
        {
            string data;

            switch (status)
            {
                case HttpStatusCode.OK:
                    data = "HTTP/1.1 200 OK\n"; break;
                case HttpStatusCode.BadRequest:
                    data = "HTTP/1.1 400 Bad Request\n"; break;
                case HttpStatusCode.NotFound:
                    data = "HTTP/1.1 404 Not Found\n"; break;
                default:
                    data = "HTTP/1.1 418 I'm a Teapot\n"; break;
            }

            if (string.IsNullOrEmpty(payload))
            {
                data += "Content-Length: 0\n";
            }
            data += "Content-Type: text/plain\n";

            if (!string.IsNullOrEmpty(payload)) { data += payload; }

            byte[] buffer = Encoding.ASCII.GetBytes(data);
            _Client.GetStream().Write(buffer, 0, buffer.Length);
            _Client.Close();
            _Client.Dispose();
        }
    }
}
