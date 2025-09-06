using EAAppGameListBuilder;
using System.Diagnostics;
using System.Text.Json;


var query = "query{gameSearch(filter:{gameTypes:[BASE_GAME],prereleaseGameTypes:[OPEN_BETA]},paging:{limit:9999}){items{slug,title,gameType,prereleaseGameType,keyArtImage{path},packArtImage{path},logoImage{path},__typename}}}";
var httpClient = new HttpClient()
{
    BaseAddress = new Uri("https://service-aggregation-layer.juno.ea.com")
};
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36 Edg/139.0.0.0");

var gameSearchUrl = $"graphql?query={query}";
var gameSearchFile = "game_search.json";
var gameSearchResponseString = string.Empty;
if (File.Exists(gameSearchFile))
{
    gameSearchResponseString = File.ReadAllText(gameSearchFile);
}
else
{
    gameSearchResponseString = await httpClient.GetStringAsync(gameSearchUrl);
    File.WriteAllText(gameSearchFile, gameSearchResponseString);
}

var gameSearch = JsonSerializer.Deserialize<SearchResponse>(gameSearchResponseString);
if (gameSearch?.Data?.GameSearch?.Items is null)
{
    Console.WriteLine("Could not get search results.");
    return;
}

if (gameSearch.Data.GameSearch.HasNextPage)
{
    Console.WriteLine("It happened, there is finally a second page. Time to update the code to support pagination.");
}

var jsonData = JsonSerializer.Serialize(gameSearch?.Data?.GameSearch?.Items);

File.WriteAllText("ea_app_titles.json", jsonData);


Debugger.Break();
