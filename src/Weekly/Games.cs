using CsvHelper;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ant.Weekly;

public class Games
{
   private Players _Players;

   public Games(Players players)
   {
      _Players = players;
   }

   public List<Game> All { get; set; } = new();

   public int Count => All.Count;

   public void List(DateOnly from, DateOnly to, Player? player)
   {
      var games = GetGames(from, to, player);

      Out.Write($"\nFound {games.Count()} games between {from:yyyy.MM.dd} and {to:yyyy.MM.dd}\n");

      Out.Write(Game.GameTableHeaderStringBuilder().ToString());
      Out.Write();

      foreach (var game in games)
      {
         Out.Write(game.GameTableStringBuilder().ToString());
      }
   }

   public IEnumerable<Game> GetGames(DateOnly from, DateOnly to, Player? player)
   {
      var query = All.Where((g) => g.Date >= from && g.Date <= to);

      if (player != null)
      {
         query = query.Where((g) => g.WhitePlayer == player || g.BlackPlayer == player);
      }

      return query;
   }

   public void Load()
   {
      Log.Info("Importing game records from csv data file");
      var games = ImportFromCsv();

      Log.Info("Processing game records");

      foreach (var g in games)
      {
         if (g.GetResult() == GameResult.None)
         {
            continue;
         }

         if (g.WhiteName.Trim().ToLower() == g.BlackName.Trim().ToLower())
         {
            var msg = $"Same name {g.WhiteName} for both white player and black player in a game";
            Out.Error(msg);
            throw new InvalidOperationException(msg);
         }

         Player? whitePlayer;
         try
         {
            whitePlayer = _Players.ByName[g.WhiteName];
         }
         catch (KeyNotFoundException)
         {
            whitePlayer = null;
         }
         if (whitePlayer == null)
         {
            Log.Warning("Unknown white player '{0}' - creating player record", g.WhiteName);
            whitePlayer = _Players.AddNewPlayer(g.WhiteName);
         }

         Player? blackPlayer;
         try
         {
            blackPlayer = _Players.ByName[g.BlackName];
         }
         catch (KeyNotFoundException)
         {
            blackPlayer = null;
         }
         if (blackPlayer == null)
         {
            Log.Warning("Unknown black player '{0}' - creating player record", g.BlackName);
            blackPlayer = _Players.AddNewPlayer(g.BlackName);
         }

         var game = new Game(g.Date, whitePlayer, blackPlayer, g.GetResult(), g.TimeControl, g.Site, g.Event, g.Section, g.Stage, g.Round);
         All.Add(game);
      }

      Log.Info("Loaded {0} game records", All.Count);
   }

   public List<GameCsv> ImportFromCsv()
   {
      var currentDirectory = Environment.CurrentDirectory;
      var fileInfo = new FileInfo(Path.Join(currentDirectory, $"{App.S.Data.GamesCsvFileName}.csv"));

      var games = new List<GameCsv>();
      using (var reader = new StreamReader(fileInfo.FullName))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<GameCsvMap>();
         while (csv.Read())
         {
            switch (csv.GetField(0))
            {
               case "":
               case "Count":
               case "Total":
                  break;
               default:
                  games.Add(csv.GetRecord<GameCsv>());
                  break;
            }
         }
      }
      return games;
   }

   public void ExportPlayerGames(Player player)
   {
      var games = new List<GameCsv>();
      
      foreach (var g in player.Games.OrderBy(g => g.Date))
      {
         var opponent = g.WhitePlayer == player ? g.BlackPlayer.Name : g.WhitePlayer.Name;
         var rating = player == g.WhitePlayer ? g.WhiteRating : g.BlackRating;
         var color = player == g.WhitePlayer ? "White" : "Black";
         var result = player == g.WhitePlayer ? g.WhiteResult : g.BlackResult;

         games.Add(new GameCsv()
         {
            Date = g.Date,
            WhiteName = g.WhitePlayer.Name,
            BlackName = g.BlackPlayer.Name,
            Result = GameCsv.GetResultString(g.Result),
            TimeControl = g.TimeControl,
            Site = g.Site,
            Event = g.Event,
            Section = g.Section,
            Stage = g.Stage,
            Round = g.Round,

            WhiteRating = (int)(g.WhiteRating?.Value ?? 0),
            BlackRating = (int)(g.BlackRating?.Value ?? 0),
            WhiteNewRating = (int)(g.WhiteRating?.New?.Value ?? 0),
            WhitePerf = (int)(g.WhiteRating?.New?.Perf ?? 0),
            WhitePerfAvgOpp = (int)(g.WhiteRating?.New?.PerfAvgOpp ?? 0),
            BlackNewRating = (int)(g.BlackRating?.New?.Value ?? 0),
            BlackPerf = (int)(g.BlackRating?.New?.Perf ?? 0),
            BlackPerfAvgOpp = (int)(g.BlackRating?.New?.PerfAvgOpp ?? 0),

            PlayerName = player.Name,
            OpponentName = opponent,
            PlayerColor = color,
            PlayerResult = result,

            PlayerElo = (int)(rating?.New?.Elo ?? 0),
            PlayerGlicko = (int)(rating?.New?.Glicko ?? 0),
            PlayerDeviation = (int)(rating?.New?.Deviation ?? 0),
            PlayerClubRating = (int)(rating?.New?.ClubRating ?? 0),
            PlayerClubRatingAvgOpp = (int)(rating?.New?.ClubRatingAvgOpp ?? 0),
            PlayerEloPerf = (int)(rating?.New?.EloPerf ?? 0),
            PlayerEloPerfAvgOpp = (int)(rating?.New?.EloPerfAvgOpp ?? 0),
            PlayerGlickoPerf = (int)(rating?.New?.GlickoPerf ?? 0),
            PlayerGlickoPerfAvgOpp = (int)(rating?.New?.GlickoPerfAvgOpp ?? 0),
            PlayerClubPerf = (int)(rating?.New?.ClubPerf ?? 0),
            PlayerClubPerfAvgOpp = (int)(rating?.New?.ClubPerfAvgOpp ?? 0),
         });
      }

      var playerName = player.Name.RemoveSpaces();
      var fileName = playerName.Length > 0 ? string.Format($"{playerName}.csv") : "player.csv";

      var fileInfo = new FileInfo(fileName);
      if (fileInfo.Exists)
      {
         fileInfo.Delete();
      }

      using (var writer = new StreamWriter(fileInfo.FullName))
      using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<PlayerExportGameCsvMap>();
         csv.WriteRecords(games);
      }

      Out.Write($"\nExported {games.Count} games for {player.Name}\n");
   }

   public static void ExportPlannedGames(IEnumerable<Game> games, bool final)
   {
      string fileName;
      if (final)
      {
         fileName = string.Format($"{App.S.Data.PlannedGamesFileName}.csv");
      }
      else
      {
         fileName = string.Format($"{App.S.Data.PlannedGamesPartialFileName}.csv");
      }

      var fileInfo = new FileInfo(fileName);
      if (fileInfo.Exists)
      {
         fileInfo.Delete();
      }

      using (var writer = new StreamWriter(fileInfo.FullName))
      using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<PlannedGameCsvMap>();
         csv.WriteRecords(games);
      }
   }
}
