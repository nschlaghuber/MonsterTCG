using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonsterTCG.Model.Http
{
    public class HttpRequestEventArgs : EventArgs
    {
        protected readonly Socket Client;
        public HttpRequestEventArgs(Socket client, HttpRequest request)
        {
            this.Client = client;
            Request = request;
        }
        public virtual HttpRequest Request { get; }

        public virtual string? GetBearerToken()
        {
            var authHeader = Request.GetAuthorizationHeader();

            return authHeader is { Item1: "Bearer" } ? authHeader.Value.Item2 : null;
        }

        public virtual void Reply(HttpStatusCode status, string? payload)
        {
            var responseBuilder = new StringBuilder();

            switch (status)
            {
                case HttpStatusCode.OK:
                    responseBuilder.AppendLine("HTTP/1.1 200 OK"); break;
                case HttpStatusCode.Created:
                    responseBuilder.AppendLine("HTTP/1.1 201 Created"); break;
                case HttpStatusCode.NoContent:
                    responseBuilder.AppendLine("HTTP/1.1 204 No Content"); break;
                case HttpStatusCode.BadRequest:
                    responseBuilder.AppendLine("HTTP/1.1 400 Bad Request"); break;
                case HttpStatusCode.Unauthorized:
                    responseBuilder.AppendLine("HTTP/1.1 401 Unauthorized"); break;
                case HttpStatusCode.Forbidden:
                    responseBuilder.AppendLine("HTTP/1.1 403 Forbidden"); break;
                case HttpStatusCode.NotFound:
                    responseBuilder.AppendLine("HTTP/1.1 404 Not Found"); break;
                case HttpStatusCode.Conflict:
                    responseBuilder.AppendLine("HTTP/1.1 409 Conflict"); break;
                case HttpStatusCode.InternalServerError:
                    responseBuilder.AppendLine("HTTP/1.1 500 Internal Server Error"); break;
                default:
                    responseBuilder.AppendLine("HTTP/1.1 418 I'm a Teapot"); break;
            }

            responseBuilder.AppendLine("Content-Type: text/plain");

            responseBuilder.AppendLine($"Content-Length: {(payload != null ? Encoding.ASCII.GetByteCount(payload) : "0")}");

            if (!string.IsNullOrEmpty(payload)) 
            {
                responseBuilder.AppendLine();
                responseBuilder.Append(payload); 
            }
            var test = responseBuilder.ToString();
            var buffer = Encoding.ASCII.GetBytes(responseBuilder.ToString());
            Client.Send(buffer);
            Client.Close();
            Client.Dispose();
        }
    }
}
