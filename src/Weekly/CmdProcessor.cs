namespace Ant.Weekly;

public static class CmdProcessor
{
   public static Data AppInit()
   {
      var data = new Data();
      data.Load();

      Out.Write("{0} players", data.Players.All.Count);
      Out.Write("{0} games\n", data.Games.Count);

      return data;
   }

   public static void ListPlayers(bool all, bool inactive, bool sortByName)
   {
      var data = AppInit();
      data.Players.List(all, inactive, sortByName);
   }

   public static void ShowFullPlayerInfo(string playerName, bool verbose)
   {
      var data = AppInit();
      var player = data.Players.GetPlayer(playerName);
      if (player != null)
      {
         player?.ListFullInfo(verbose);
      }
      else
      {
         Out.Error("Player '{0}' not found", playerName);
      }
   }

   public static void ListGames(DateOnly from, DateOnly to, string playerName)
   {
      var data = AppInit();
      Player? player = null;
      if (!string.IsNullOrEmpty(playerName))
      {
         player = data.Players.GetPlayer(playerName);
         if (player == null)
         {
            return;
         }
      }
      data.Games.List(from, to, player);
   }

   public static void ExportPlayer(string playerName)
   {
      var data = AppInit();
      Player? player = null;
      if (!string.IsNullOrEmpty(playerName))
      {
         player = data.Players.GetPlayer(playerName);
         if (player != null)
         {
            data.Games.ExportPlayerGames(player);
         }
         else
         {
            return;
         }
      }
   }

   public static void Plan(bool version1, bool version2, bool random, bool currentWeek)
   {
      var data = AppInit();
      data.Players.List(false, false, false);

      var startDate = DateOnly.FromDateTime(DateTime.Now).StartOfWeek(DayOfWeek.Monday);
      startDate = currentWeek ? startDate : startDate.AddDays(7);

      var matchList = Data.ImportMatchListFromCsv();
      var planner = new Planner(data, matchList, startDate);
      planner.PlanWeek(version1, version2, random);
   }
}
