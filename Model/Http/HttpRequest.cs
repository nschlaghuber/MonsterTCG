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
        GET,
        POST,
        PUT,
        DELETE
    }

    public class HttpRequest
    {
        public string PlainMessage
        {
            get
            {
                if (_plainMessage == null)
                {
                    return $"{Method} {Path} HTTP/1.1\r\n" +
                           $"Host: localhost:10001\r\n" +
                           $"User-Agent: curl/8.4.0\r\n" +
                           $"Accept: */*\r\n" +
                           $"Content-Type: application/json\r\n" +
                           $"Content-Length: {(string.IsNullOrEmpty(Payload) ? Encoding.ASCII.GetByteCount(Payload) : "0")}\r\n" +
                           $"\r\n" +
                           $"{Payload}";
                }

                return _plainMessage;
            }
            set => _plainMessage = value;
        }

        private string? _plainMessage;
        public HttpMethod Method { get; set; }
        public List<HttpHeader> Headers { get; init; } = new();
        public string Path { get; set; } = string.Empty;
        public string Payload { get; init; } = "";

        public HttpRequest()
        {
        }

        public HttpRequest(string plainMessage)
        {
            PlainMessage = plainMessage;
            Payload = string.Empty;

            var lines = plainMessage.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var inHeaders = true;
            List<HttpHeader> headers = new();

            for (var i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    var inc = lines[0].Split(' ');
                    Method = Enum.Parse<HttpMethod>(inc[0]);
                    Path = inc[1];
                }
                else if (inHeaders)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        inHeaders = false;
                    }
                    else
                    {
                        headers.Add(new HttpHeader(lines[i]));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Payload))
                    {
                        Payload += "\r\n";
                    }

                    Payload += lines[i];
                }
            }

            Headers = headers.ToList();
        }

        public (string, string)? GetAuthorizationHeader()
        {
            var authHeader = Headers.FirstOrDefault(header => header.Name == "Authorization");
            if (authHeader is null)
            {
                return null;
            }

            var parts = authHeader.Value.Split(' ');
            return (parts[0], parts[1]);
        }

        public virtual string? GetBearerToken()
        {
            var authHeader = GetAuthorizationHeader();

            return authHeader is { Item1: "Bearer" } ? authHeader.Value.Item2 : null;
        }
    }
}