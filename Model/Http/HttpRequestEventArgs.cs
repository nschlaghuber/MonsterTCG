using System.Net;
using System.Net.Sockets;
using System.Text;
using MonsterTCG.Model.Deck;

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

        public virtual void Reply(HttpResponse response)
        {
            var responseBuilder = new StringBuilder();

            switch (response.Status)
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

            responseBuilder.Append("Content-Type: ");
            switch (response.FormatType)
            {
                case FormatType.Json:
                    responseBuilder.AppendLine("application/json");
                    break;
                case FormatType.Plain:
                    responseBuilder.AppendLine("text/plain");
                    break;
            }

            responseBuilder.AppendLine($"Content-Length: {(response.Message is not null ? Encoding.ASCII.GetByteCount(response.Message) : "0\r\n")}");

            if (!string.IsNullOrEmpty(response.Message)) 
            {
                responseBuilder.AppendLine();
                responseBuilder.Append(response.Message); 
            }
            var buffer = Encoding.ASCII.GetBytes(responseBuilder.ToString());
            Client.Send(buffer);
            Client.Close();
            Client.Dispose();
        }
    }
}
