using MonsterTCG.Model.Http;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MonsterTCG
{
    public class HttpServer
    {
        private string CONNECTION_STRING = "Host=localhost;Username=postgres;Password=;Database=monsterTCG";

        private TcpListener? _tcpListener;

        public event HttpRequestEventHandler? IncomingRequest;
        public bool Active { get; set; } = false;

        public void StartServer(int port)
        {
            if(Active) return;

            Active = true;

            _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));

            _tcpListener.Start();

            Console.WriteLine($"Tcp Server has started listening on {IPAddress.Loopback}:{port}.");

            byte[] buffer = new byte[1024];

            while (Active)
            {
                TcpClient tcpClient = _tcpListener.AcceptTcpClient();

                string data = string.Empty;
                while (tcpClient.GetStream().DataAvailable || (string.IsNullOrEmpty(data)))
                {
                    int dataLength = tcpClient.GetStream().Read(buffer, 0, buffer.Length);
                    data += Encoding.ASCII.GetString(buffer, 0, dataLength);
                }

                IncomingRequest?.Invoke(this, new HttpRequestEventArgs(tcpClient, new HttpRequest(data)));
            }
        }
    }
}
