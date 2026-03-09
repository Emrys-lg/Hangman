using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

List<Game> GamesList = new List<Game>();

#region Setup
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

#endregion

app.MapGet("/", () => "Game Rules : Guess the word choosed by the other.");

int id = 1;
//create a game
app.MapPost("/game/new", (string word) =>
{
    Random rnd = new Random();
    Game game = new Game(id, word);
    GamesList.Add(game);
    id++;

    return $"new room created, roomName: Room{game.Id}";
});

//list all playing game
app.MapGet("/game/list", () =>
{
    List<Game> GamesInProgress = GamesList;
    foreach (Game game in GamesList)
    {
        if (game.State != GameState.inProgress) GamesInProgress.Remove(game);
    }

    foreach (Game gameFinished in GamesInProgress)
    {
        return $"Room{gameFinished.Id}";
    }
    return null;
});

//guess with char
app.MapPost("/game/{id}/char", (int id, char guess) =>
{
    Game selectedGame = null;
    foreach (Game game in GamesList)
    {
        if (game.Id == id) selectedGame = game;
    }
    if (selectedGame == null) return $"no game found";
    selectedGame.Tries.Add(guess.ToString());
    if (selectedGame.Word.Contains(guess)) return $"Correct char";
    else return $"Incorrect char";
});

//guess with string
app.MapPost("/game/{id}/word", (int id, string guess) =>
{
    Game selectedGame = null;
    
    foreach (Game game in GamesList)
    {
        if (game.Id == id) selectedGame = game;
    }
    if (selectedGame == null) return $"no game found";
    selectedGame.Tries.Add(guess);
    if (selectedGame.Word == guess)
    {
        selectedGame.State = GameState.victory;
        return $"Correct word. End of the game.";
    }
    else return $"Incorrect word";
});

//delete game
app.MapPost("/game/{id}/delete", (int id) =>
{
    foreach(Game game in GamesList)
    {
        if(game.Id== id) GamesList.Remove(game);
        else return $"game not found";
    }
    return null;
});

//list game finished
app.MapGet("/game/finished", () =>
{
    List<Game> GamesFinished = new List<Game>();
    foreach (Game game in GamesList)
    {
        if (game.State != GameState.inProgress) GamesFinished.Add(game);
    }

    foreach (Game gameFinished in GamesFinished)
    {
        return $"Room{gameFinished.Id}";
    }
    return null;
});

//game history for an ended game
app.MapGet("/game/{id}/history", (int id) =>
{
    Game selectedGame = null;
    foreach (Game game in GamesList)
    {
        if(game.Id == id && game.State != GameState.inProgress)
        {
            selectedGame = game;
        }
    }
    if (selectedGame == null) return $"no game found";
    else
    {
        foreach (string history in selectedGame.Tries)
        {
            return $"{history}";
        }
    }
    return null;
});

app.Run();

class Game
{
    public int Id;
    public string Word;
    public GameState State;
    public List<string>Tries;

    public Game(int _id, string _word)
    {
        Id = _id;
        Word = _word;
        State = GameState.inProgress;
        Tries = new List<string>();
    }
}

enum GameState
{
    inProgress,
    victory,
    defeat
}
