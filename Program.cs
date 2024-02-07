using MonsterTCG;
using MonsterTCG.Controller;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using Npgsql;


const bool rebuildTables = false;


var httpServer = new HttpServer();

var datasourceBuilder = new NpgsqlDataSourceBuilder("Host=localhost;Username=postgres;Password=;Database=monsterTCG;Include Error Detail=true;");
var dataSource = datasourceBuilder.Build();
Console.WriteLine("Database connection has been established.");

var tableBuilder = new TableBuilder(dataSource);

if (rebuildTables)
{
    await tableBuilder.DropTables();
}
await tableBuilder.EnsureTablesExists();

var repositories = new List<Repository>
{
    new UserRepository(dataSource),
    new CardRepository(dataSource)
};
repositories.Add(new PackageRepository(dataSource, repositories.OfType<CardRepository>().First()));

var controllers = new List<Controller>
{
    new UserController(repositories.OfType<UserRepository>().First()),
    new PackageController(repositories.OfType<PackageRepository>().First(),
        repositories.OfType<CardRepository>().First(),
        repositories.OfType<UserRepository>().First())
};

httpServer.IncomingRequest += ProcessRequest;

httpServer.StartServer(10001);

while (true)
{
}

void ProcessRequest(object sender, HttpRequestEventArgs e)
{
    foreach (var controller in controllers)
    {
        if (controller.ProcessRequest(e))
        {
            return;
        }
    }

    e.Reply(System.Net.HttpStatusCode.BadRequest, "Invalid Path/Method");
}