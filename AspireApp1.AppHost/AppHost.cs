using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

List<Game> GamesList = new List<Game>();
int id = 1;

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
    List<string> playingRooms = new List<string>();
    foreach (Game game in GamesList)
    {
        if (game.State == GameState.inProgress) playingRooms.Add($"Room{game.Id}");
    }
    return playingRooms;
});

//guess with char
app.MapPost("/game/{id}/char", (int id, char guess) =>
{
    Game selectedGame = null;
    foreach (Game game in GamesList)
    {
        if (game.Id == id)
        {
            selectedGame = game;
            break;
        }
    }
    if (selectedGame == null) return $"no game found";
    if (selectedGame.State != GameState.inProgress) return "Game already finished";
    selectedGame.Tries.Add(guess.ToString());
    if (selectedGame.Word.Contains(guess)) return $"Correct char";
    else
    {
        if (selectedGame.Error >= 10)
        {
            selectedGame.State = GameState.defeat; return $"To many attempt, game lost";
        }
        else
        {
            selectedGame.Error++;
            return $"Incorrect character";
        }

    }
});

//guess with string
app.MapPost("/game/{id}/word", (int id, string guess) =>
{
    Game selectedGame = null;
    
    foreach (Game game in GamesList)
    {
        if (game.Id == id)
        {
            selectedGame = game; 
            break;
        }
    }
    if (selectedGame == null) return $"no game found";
    if (selectedGame.State != GameState.inProgress) return "Game already finished";
    selectedGame.Tries.Add(guess);
    if (selectedGame.Word == guess)
    {
        selectedGame.State = GameState.victory;
        return $"Correct word. End of the game.";
    }
    else
    {
        if (selectedGame.Error >= 10)
        {
            selectedGame.State = GameState.defeat; return $"To many attempt, game lost";
        }
        else
        {
            selectedGame.Error++;
            return $"Incorrect word";
        }
    }
});

//delete game
app.MapPost("/game/{id}/delete", (int id) =>
{
    Game selectedRoom = null;
    foreach(Game game in GamesList)
    {
        if(game.Id== id) selectedRoom = game; break;
    }
    if (selectedRoom != null)
    {
        GamesList.Remove(selectedRoom);
        return $"Room{selectedRoom.Id} deleted";
    }
    else return $"Incorect room to delete";
});

//list game finished
app.MapGet("/game/finished", () =>
{
    List<string> finishedRooms = new List<string>();
    foreach (Game game in GamesList)
    {
        if (game.State != GameState.inProgress) finishedRooms.Add($"Room{game.Id}");
    }
    return finishedRooms;
});

//game history for an ended game
app.MapGet("/game/{id}/history", (int id) =>
{
    Game selectedGame = null;

    foreach (Game game in GamesList)
    {
        if (game.Id == id && game.State != GameState.inProgress)
        {
            selectedGame = game;
            break;
        }
    }

    if (selectedGame == null)
        return new List<string> { "no ended game found with this id" };

    return selectedGame.Tries;
});

app.Run();

class Game
{
    public int Id { get; set; }
    public string Word { get; set; }
    public GameState State { get; set; }
    public List<string> Tries { get; set; } = new();

    public int Error { get; set; }

    public Game(int _id, string _word)
    {
        Id = _id;
        Word = _word;
        State = GameState.inProgress;
        Error = 0;
    }
}

enum GameState
{
    inProgress,
    victory,
    defeat
}
