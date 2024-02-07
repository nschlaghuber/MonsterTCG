using MonsterTCG.Model.Http;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonsterTCG
{
    public class HttpServer
    {
        private Socket? listener;

        public event HttpRequestEventHandler? IncomingRequest;
        public bool Active { get; set; } = false;

        public void StartServer(int port)
        {
            if(Active) return;

            Active = true;

            listener = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, port));

            listener.Listen();

            Console.WriteLine($"Tcp Server has started listening on {IPAddress.Loopback}:{port}.");

            byte[] buffer = new byte[1024];

            while (Active)
            {
                Socket client = listener.Accept();

                string data = string.Empty;
                do
                {
                    int dataLength = client.Receive(buffer);
                    data += Encoding.ASCII.GetString(buffer, 0, dataLength);
                } while (String.IsNullOrEmpty(data));

                IncomingRequest?.Invoke(this, new HttpRequestEventArgs(client, new HttpRequest(data)));
            }
        }
    }
}
