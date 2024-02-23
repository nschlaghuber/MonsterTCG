using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonsterTCG.Model.Deck;

namespace MonsterTCG.Model.Http
{
    public record HttpResponse(HttpStatusCode Status, string? Message, FormatType FormatType = FormatType.Json);
}
