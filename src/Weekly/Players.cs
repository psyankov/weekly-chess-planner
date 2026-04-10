using CsvHelper;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace Ant.Weekly;

public class Players
{
   public Games Games { get; }

   public Players()
   {
      Games = new Games(this);
   }

   public List<Player> All { get; set; } = new();

   public Dictionary<string, Player> ByName { get; set; } = new();

   public void Load()
   {
      var players = ImportFromCsv();

      foreach (var p in players)
      {
         var player = new Player(p);
         All.Add(player);
         AddToDictionary(player);
      }
      All.Sort();

      Log.Info("Loaded {0} player records", All.Count);
   }

   public void Add(Player player)
   {
      if (!All.Contains(player))
      {
         All.Add(player);
         AddToDictionary(player);
      }
   }

   public static List<PlayerCsv> ImportFromCsv()
   {
      var currentDirectory = Environment.CurrentDirectory;
      var fileInfo = new FileInfo(Path.Join(currentDirectory, $"{App.S.Data.PlayersCsvFileName}.csv"));

      Log.Info($"Importing player records from csv data file '{fileInfo.FullName}'");

      var players = new List<PlayerCsv>();
      using (var reader = new StreamReader(fileInfo.FullName))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<PlayerCsvMap>();
         while (csv.Read())
         {
            switch (csv.GetField(0))
            {
               case "":
               case "Count":
               case "Total":
                  break;
               default:
                  players.Add(csv.GetRecord<PlayerCsv>());
                  break;
            }
         }
      }

      fileInfo = new FileInfo(Path.Join(currentDirectory, $"{App.S.Data.RatingsCsvFileName}.csv"));

      Log.Info($"Import initial rating records from csv data file '{fileInfo.FullName}'");
      var ratings = new List<PlayerCsv>();

      if (File.Exists(fileInfo.FullName))
      {
         using (var reader = new StreamReader(fileInfo.FullName))
         using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
         {
            csv.Context.RegisterClassMap<PlayerRatingCsvMap>();
            ratings.AddRange(csv.GetRecords<PlayerCsv>().ToList());
         }
      }
      else
      {
         Out.Warning($"Initial ratings file '{fileInfo.FullName}' not found - all players will be assigned default initial ratings!");
      }

      foreach (var player in players)
      {
         var rating = ratings.FirstOrDefault(r => r.Name == player.Name);
         if (rating != null)
         {
            player.InitialEloEstablished = rating.InitialEloEstablished;
            player.InitialElo = rating.InitialElo;
            player.InitialGlicko = rating.InitialGlicko;
            player.InitialDeviation = rating.InitialDeviation;
            player.InitialClubRating = rating.InitialClubRating;
         }
         else
         {
            Log.Warning($"Player {player.Name} has no initial rating - default values will be assigned.");
            // Initial Elo will be based on performance and initial Glicko will be default value
            // when the player class instance is created - here we are simply recording imported values
            player.InitialEloEstablished = false;
            player.InitialElo = (int)App.S.Rating.InitialElo;
            player.InitialGlicko = (int)App.S.Rating.InitialGlicko;
            player.InitialDeviation = (int)App.S.Rating.InitialDeviation;
            player.InitialClubRating = (int)App.S.Rating.InitialClubRating;
         }
      }

      foreach (var rating in ratings)
      {
         var player = players.FirstOrDefault(p => p.Name == rating.Name);
         if (player == null)
         {
            Log.Warning($"Initial ratings list player '{rating.Name}' has no active player record - creating a new player");
            player = new PlayerCsv()
            {
               Name = rating.Name,
               InitialEloEstablished = rating.InitialEloEstablished,
               InitialElo = rating.InitialElo,
               InitialGlicko = rating.InitialGlicko,
               InitialDeviation = rating.InitialDeviation,
               InitialClubRating = rating.InitialClubRating
            };
            players.Add(player);
         }
      }

      return players;
   }

   public Player AddNewPlayer(string name)
   {
      var player = new Player(name);
      All.Add(player);
      AddToDictionary(player);
      return player;
   }

   public void AddToDictionary(Player player)
   {
      if (!(ByName.ContainsKey(player.Name) || ByName.ContainsKey(player.Name.ToLower())))
      {
         ByName.Add(player.Name, player);
         if (player.Name != player.Name.ToLower())
         {
            ByName.Add(player.Name.ToLower(), player);
         }
      }
      else
      {
         var msg = string.Format("Player '{0}' has a duplicate record", player.Name);
         Out.Error(msg);
         throw new InvalidOperationException(msg);
      }
   }

   public void List(bool all, bool inactive, bool sortByName)
   {
      var query = all ? All
         : inactive && !all ? All.Where((p) => !p.Active)
         : All.Where((p) => p.Active);

      query = sortByName ? query : query.OrderByDescending((p) => p.Rating.Value).ThenBy((r) => r.Name);

      var count = query.Count();

      Out.Write();
      Out.Write(Player.GetShortInfoHeader());
      Out.Write();

      foreach (var player in query)
      {
         Out.Write(player.GetShortInfo());
      }

      Out.Write();
      Out.Write("{0} players", count);
      Out.Write();
   }

   public Player? GetPlayer(string name)
   {
      if (string.IsNullOrEmpty(name))
      {
         return null;
      }

      var players = All.Where(p => p.Name.ToLower().Contains(name.ToLower())).ToList();

      if (players.Count == 0)
      {
         Out.Error($"Player record does not exit for name '{name}'");
         return null;
      }
      else if (players.Count == 1)
      {
         return players[0];
      }
      else
      {
         Out.Error($"Multiple player records exit for name '{name}'");
         foreach (var p in players)
         {
            Console.WriteLine($"   {p.Name}");
         }
         Out.Write("Player name must be unique\n");
         return null;
      }
   }

   public void ExportRatings()
   {
      var fileName = string.Format($"{App.S.Data.RatingsCsvFileName}.csv");
      var fileInfo = new FileInfo(fileName);

      if (fileInfo.Exists)
      {
         fileInfo.Delete();
      }

      var records = new List<PlayerCsv>();
      foreach (var player in All)
      {
         var oneRecord = new PlayerCsv()
         {
            Name = player.Name,
            Active = player.Active,
            NetScore = player.NetScore(),
            GamesCount = player.Games.Count,
            WinRatio = player.Games.Count == 0 ? 0 : Math.Round(player.NetScore() / player.Games.Count, 3),
            Elo = (int)player.Rating.Elo,
            Glicko = (int)player.Rating.Glicko,
            Deviation = (int)player.Rating.Deviation,
            ClubRating = (int)player.Rating.ClubRating,
            ClubRatingAvgOpp = (int)player.Rating.ClubRatingAvgOpp,
            EloPerf = (int)player.Rating.EloPerf,
            EloPerfAvgOpp = (int)player.Rating.EloPerfAvgOpp,
            GlickoPerf = (int)player.Rating.GlickoPerf,
            GlickoPerfAvgOpp = (int)player.Rating.GlickoPerfAvgOpp,
            ClubPerf = (int)player.Rating.ClubPerf,
            ClubPerfAvgOpp = (int)player.Rating.ClubPerfAvgOpp,
            InitialEloEstablished = player.InitialRating.EloEstablished,
            InitialElo = (int)player.InitialRating.Elo,
            InitialGlicko = (int)player.InitialRating.Glicko,
            InitialDeviation = (int)player.InitialRating.Deviation,
            InitialClubRating = (int)player.InitialRating.ClubRating,
         };
         records.Add(oneRecord);
      }

      using (var writer = new StreamWriter(fileInfo.FullName))
      using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<PlayerRatingExportCsvMap>();
         csv.WriteRecords(records.OrderByDescending((r) => r.Elo).ThenBy((r) => r.Name));
      }
   }
}
