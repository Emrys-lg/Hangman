using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

#region Setup
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "PenduAPI";
    config.Title = "PenduAPI v1";
    config.Version = "v1";
});

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite("Data Source=games.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "PenduAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}
#endregion

app.MapGet("/", () => "Game Rules: Guess the word chosen by the other player.");

// CREATE NEW GAME
app.MapPost("/game/new", (GameDbContext db, string word) =>
{
    Game game = new Game
    {
        Word = word,
        State = GameState.inProgress,
        Error = 0
    };
    db.Games.Add(game);
    db.SaveChanges();

    return $"New room created: Room{game.Id}";
});

// LIST ALL PLAYING GAMES
app.MapGet("/game/list", (GameDbContext db) =>
{
    return db.Games
        .Where(g => g.State == GameState.inProgress)
        .Select(g => $"Room{g.Id}")
        .ToList();
});

// GUESS A CHARACTER
app.MapPost("/game/{id}/char", (GameDbContext db, int id, char guess) =>
{
    var game = db.Games.Find(id);
    if (game == null) return "No game found";
    if (game.State != GameState.inProgress) return "Game already finished";

    game.Tries.Add(guess.ToString());

    if (game.Word.Contains(guess))
    {
        db.SaveChanges();
        return "Correct char";
    }
    else
    {
        game.Error++;
        if (game.Error >= 10)
        {
            game.State = GameState.defeat;
            db.SaveChanges();
            return "Too many attempts, game lost";
        }
        db.SaveChanges();
        return "Incorrect character";
    }
});

// GUESS A WORD
app.MapPost("/game/{id}/word", (GameDbContext db, int id, string guess) =>
{
    var game = db.Games.Find(id);
    if (game == null) return "No game found";
    if (game.State != GameState.inProgress) return "Game already finished";

    game.Tries.Add(guess);

    if (game.Word == guess)
    {
        game.State = GameState.victory;
        db.SaveChanges();
        return "Correct word. End of the game.";
    }
    else
    {
        game.Error++;
        if (game.Error >= 10)
        {
            game.State = GameState.defeat;
            db.SaveChanges();
            return "Too many attempts, game lost";
        }
        db.SaveChanges();
        return "Incorrect word";
    }
});

// DELETE GAME
app.MapDelete("/game/{id}", (GameDbContext db, int id) =>
{
    var game = db.Games.Find(id);
    if (game == null) return "Room not found";

    db.Games.Remove(game);
    db.SaveChanges();

    return $"Room{id} deleted";
});

// LIST FINISHED GAMES
app.MapGet("/game/finished", (GameDbContext db) =>
{
    return db.Games
        .Where(g => g.State != GameState.inProgress)
        .Select(g => $"Room{g.Id}")
        .ToList();
});

// GAME HISTORY
app.MapGet("/game/{id}/history", (GameDbContext db, int id) =>
{
    var game = db.Games.Find(id);
    if (game == null || game.State == GameState.inProgress)
        return new List<string> { "No ended game found with this id" };

    return game.Tries;
});

app.Run();

#region MODELS
public class Game
{
    public int Id { get; set; }
    public string Word { get; set; }
    public GameState State { get; set; }
    public List<string> Tries { get; set; } = new();
    public int Error { get; set; }

    public Game() { }
}

public enum GameState
{
    inProgress,
    victory,
    defeat
}

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<Game> Games { get; set; }
}
#endregion