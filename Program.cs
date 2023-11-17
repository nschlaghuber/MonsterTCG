using MonsterTCG;
using MonsterTCG.Controller;
using MonsterTCG.Model.Http;


HttpServer httpServer = new HttpServer();

httpServer.IncomingRequest += _ProcessRequest;

UserController userController = new UserController(httpServer);

httpServer.StartServer(10001);

while (true) { }

void _ProcessRequest(object sender, HttpRequestEventArgs e)
{
    Console.WriteLine("Request received");
}