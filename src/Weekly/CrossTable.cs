namespace Ant.Weekly;

public class CrossTable
{
   private List<Player> _Players;
   private Dictionary<(Player player, Player opponent), (int count, double score)> _Table;

   public CrossTable(List<Player> players)
   {
      _Table = new();
      _Players = players;
   }

   public void Update()
   {
      foreach (var player in _Players)
      {
         var allOpponents = player.Games
            .Select(g => g.Opponent(player))
            .ToHashSet();

         foreach (var opponent in allOpponents)
         {
            var count = player.CountGamesAgainst(opponent, DateOnly.FromDateTime(DateTime.Now));
            var score = player.ScoreAgainst(opponent, DateOnly.FromDateTime(DateTime.Now));
            // to exclude opponents with a planned but not yet played game
            // avoiding 0.0 / 0 table entries
            if (count > 0)
            {
               _Table.Add((player, opponent), (count, score));
            }
         }
      }
   }

   public void Export()
   {
      var refFileName = string.Format($"{App.S.Data.CrossRefCsvFileName}.csv");
      var tableFileName = string.Format($"{App.S.Data.CrossTableCsvFileName}.csv");
      
      var refFileInfo = new FileInfo(refFileName);
      var tableFileInfo = new FileInfo(tableFileName);

      if (refFileInfo.Exists)
      {
         refFileInfo.Delete();
      }

      if (tableFileInfo.Exists)
      {
         tableFileInfo.Delete();
      }

      var playersByElo = _Players
         .OrderByDescending(p => p.Rating.Value)
         .ToList();

      using (var refWriter = new StreamWriter(refFileInfo.FullName))
      using (var tableWriter = new StreamWriter(tableFileInfo.FullName))
      {
         // Opponent names
         tableWriter.Write("\"\",\"\",\"\",\"\",");
         foreach (var player in playersByElo)
         {
            tableWriter.Write($"\"{player.Name}\",");
         }
         tableWriter.Write("\n");

         // Opponent net score
         tableWriter.Write("\"\",\"\",\"\",\"\",");
         foreach (var player in playersByElo)
         {
            tableWriter.Write($"\"{player.NetScore():f1}\",");
         }
         tableWriter.Write("\n");

         // Opponent games count
         tableWriter.Write("\"\",\"\",\"\",\"\",");
         foreach (var player in playersByElo)
         {
            tableWriter.Write($"\"{player.Games.Count}\",");
         }
         tableWriter.Write("\n");

         // Opponent ratings
         tableWriter.Write("\"\",\"\",\"\",\"\",");
         foreach (var player in playersByElo)
         {
            tableWriter.Write($"\"{player.Rating.Value:f0}\",");
         }
         tableWriter.Write("\n");

         refWriter.Write($"\"Name\",\"Net Score\",\"Net Game Count\",\"Elo\",\"Opponent\",\"Opponent Net Score\",\"Opponent Net Game Count\",\"Opponent Elo\",\"Score Against Opponent\",\"Game Count Against Opponent\"\n");

         // Players
         foreach (var player in playersByElo)
         {
            // Player name and rating
            tableWriter.Write($"\"{player.Name}\",");
            tableWriter.Write($"\"{player.NetScore():f1}\",");
            tableWriter.Write($"\"{player.Games.Count}\",");
            tableWriter.Write($"\"{player.Rating.Value:f0}\",");
            
            // Score and count against each opponent
            foreach (var opponent in playersByElo)
            {
               int count;
               double score;
               var str = string.Empty;
               try
               {
                  (count, score) = _Table[(player, opponent)];
                  str = $"\"{score,3:f1} / {count,3}\",";
               }
               catch
               {
                  count = 0;
                  score = 0.0;
                  str = "\"\",";
               }
               finally
               {
                  tableWriter.Write(str);
               }

               // Write to the reference table
               if (count > 0)
               {
                  // Player name and rating
                  refWriter.Write($"\"{player.Name}\",");
                  refWriter.Write($"\"{player.NetScore():f1}\",");
                  refWriter.Write($"\"{player.Games.Count}\",");
                  refWriter.Write($"\"{player.Rating.Value:f0}\",");

                  // Opponent name and rating
                  refWriter.Write($"\"{opponent.Name}\",");
                  refWriter.Write($"\"{opponent.NetScore():f1}\",");
                  refWriter.Write($"\"{opponent.Games.Count}\",");
                  refWriter.Write($"\"{opponent.Rating.Value:f0}\",");

                  // Count and score
                  refWriter.Write($"\"{score:f1}\",");
                  refWriter.Write($"\"{count}\",");
               
                  refWriter.Write("\n");
               }
            }
            tableWriter.Write("\n");
         }
      }
   }
}
