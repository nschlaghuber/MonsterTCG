using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MonsterTCG.Model.Http
{
    public enum HttpMethod
    {
        GET, POST, PUT, DELETE
    }

    public class HttpRequest
    {
        public string PlainMessage { get; set; }
        public HttpMethod Method { get; set; }
        public List<HttpHeader> Headers { get; set; } 
        public string Path { get; set; } = string.Empty;
        public string Payload { get; set; }

        public HttpRequest(string plainMessage)
        {
            PlainMessage = plainMessage;
            Payload = string.Empty;

            string[] lines = plainMessage.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            bool inheaders = true;
            List<HttpHeader> headers = new();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    string[] inc = lines[0].Split(' ');
                    Method = Enum.Parse<HttpMethod>(inc[0]);
                    Path = inc[1];
                }
                else if (inheaders)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        inheaders = false;
                    }
                    else { headers.Add(new HttpHeader(lines[i])); }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Payload)) { Payload += "\r\n"; }
                    Payload += lines[i];
                }
            }

            Headers = headers.ToList();
        }
    }
}
