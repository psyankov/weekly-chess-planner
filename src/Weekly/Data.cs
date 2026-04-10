using CsvHelper;
using System.Globalization;

namespace Ant.Weekly;

public class Data
{
   public Players Players { get; }
   public Games Games { get; }

   public Data()
   {
      Players = new Players();
      Games = Players.Games;
   }

   public void Load()
   {
      Players.Load();
      Games.Load();
      UpdateDataModel();

      var crossTable = new CrossTable(Players.All);
      crossTable.Update();
      crossTable.Export();
   }

   public void UpdateDataModel(bool exportRatings = true)
   {
      Games.All.Sort((a, b) => a.Date.CompareTo(b.Date));

      Players.All.ForEach(p => p.PrepareHistory(Games));
      Players.All.ForEach(g => g.ResetPlanningData());
      Players.All.ForEach(p => p.ResetRating());
      Games.All.ForEach(g => g.UpdateRating());
      Games.All.ForEach(g => g.UpdateLastPlayed());
      Players.All.ForEach(p => p.UpdateOpponentVariety());

      // to avoid unit tests failing due to the locked file do not export during test
      if (exportRatings)
      {
         Players.ExportRatings();
      }
   }

   public void AddPlannedGames(IEnumerable<Game> games)
   {
      Games.All.AddRange(games);
      UpdateDataModel();
   }

   public static List<MatchCsv> ImportMatchListFromCsv()
   {
      var matchList = new List<MatchCsv>();
      var currentDirectory = Environment.CurrentDirectory;
      var fileInfo = new FileInfo(Path.Join(currentDirectory, $"{App.S.Data.MatchCsvFileName}.csv"));

      if (!fileInfo.Exists)
      {
         Out.Warning($"Pre-defined match list csv file '{fileInfo.Name}' does not exist");
         return matchList;
      }

      Log.Info($"Importing pre-defined match list from csv data file '{fileInfo.FullName}'");

      using (var reader = new StreamReader(fileInfo.FullName))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
         csv.Context.RegisterClassMap<MatchCsvMap>();
         while (csv.Read())
         {
            switch (csv.GetField(0))
            {
               case "":
               case "Count":
               case "Total":
                  break;
               default:
                  var match = csv.GetRecord<MatchCsv>();
                  if (match.White.Trim() != match.Black.Trim())
                  {
                     matchList.Add(match);
                  }
                  break;
            }
         }
      }

      if (matchList.Count > 0)
      {
         Out.Write($"Imported {matchList.Count} pre-defined match pairs from csv file modified {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
      }

      return matchList;
   }
}
