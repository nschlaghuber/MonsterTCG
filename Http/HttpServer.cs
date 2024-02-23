using System.Net;
using System.Net.Sockets;
using System.Text;
using MonsterTCG.Model.Http;

namespace MonsterTCG.Http
{
    public class HttpServer
    {
        private Socket? _listener;

        public event HttpRequestEventHandler? IncomingRequest;
        public bool Active { get; set; } = false;

        public void StartServer(int port)
        {
            if(Active) return;

            Active = true;

            _listener = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, port));

            _listener.Listen();

            Console.WriteLine($"Tcp Server has started listening on {IPAddress.Loopback}:{port}.");

            var buffer = new byte[1024];

            while (Active)
            {
                var client = _listener.Accept();

                var data = string.Empty;
                do
                {
                    var dataLength = client.Receive(buffer);
                    data += Encoding.ASCII.GetString(buffer, 0, dataLength);
                } while (string.IsNullOrEmpty(data));

                IncomingRequest?.Invoke(this, new HttpRequestEventArgs(client, new HttpRequest(data)));
            }
        }
    }
}
