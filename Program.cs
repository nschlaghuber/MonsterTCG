using MonsterTCG;
using MonsterTCG.Controller;
using MonsterTCG.Http;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Service.BattleService;
using Npgsql;


const bool rebuildTables = false;


var httpServer = new HttpServer();

var datasourceBuilder =
    new NpgsqlDataSourceBuilder(
        "Host=localhost;Username=postgres;Password=;Database=monsterTCG;Include Error Detail=true;");
var dataSource = datasourceBuilder.Build();
Console.WriteLine("Database connection has been established.");

var tableBuilder = new TableBuilder(dataSource);

if (rebuildTables)
{
    await tableBuilder.DropTables();
}

await tableBuilder.EnsureTablesExists();

var repositories = new List<object>
{
    new CardRepository(dataSource),
    new BattleRepository(dataSource),
};
repositories.Add(new TradeRepository(dataSource, repositories.OfType<ICardRepository>().First()));
repositories.Add(new UserRepository(dataSource, 
    repositories.OfType<ICardRepository>().First(),
    repositories.OfType<ITradeRepository>().First()));
repositories.Add(new PackageRepository(dataSource, repositories.OfType<ICardRepository>().First()));

var services = new List<object>()
{
    new BattleService(),
};

var controllers = new List<Controller>
{
    new UserController(repositories.OfType<IUserRepository>().First()),
    new PackageController(repositories.OfType<IPackageRepository>().First(),
        repositories.OfType<IUserRepository>().First()),
    new CardController(repositories.OfType<ICardRepository>().First(),
        repositories.OfType<IUserRepository>().First()),
    new BattleController(repositories.OfType<IBattleRepository>().First(),
        repositories.OfType<IUserRepository>().First(),
        services.OfType<IBattleService>().First()),
    new TradeController(repositories.OfType<ITradeRepository>().First(),
        repositories.OfType<IUserRepository>().First(),
        repositories.OfType<ICardRepository>().First())
};

httpServer.IncomingRequest += ProcessRequest;

httpServer.StartServer(10001);

while (true)
{
}

async Task ProcessRequest(object sender, HttpRequestEventArgs e)
{
    foreach (var controller in controllers)
    {
        if (await controller.ProcessRequest(e))
        {
            return;
        }
    }

    e.Reply(new HttpResponse(System.Net.HttpStatusCode.BadRequest, "Invalid Path/Method"));
}