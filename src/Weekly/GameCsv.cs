namespace Ant.Weekly;

public class GameCsv
{
   public DateOnly Date { get; set; } = DateOnly.MinValue;
   public string WhiteName { get; set; } = string.Empty;
   public string BlackName { get; set; } = string.Empty;
   public string Result { get; set; } = string.Empty;
   public string TimeControl { get; set; } = string.Empty;
   public string Site { get; set; } = string.Empty;
   public string Event { get; set; } = string.Empty;
   public string Section { get; set; } = string.Empty;
   public string Stage { get; set; } = string.Empty;
   public int Round { get; set; }

   public GameResult GetResult()
   {
      switch (Result.RemoveSpaces())
      {
         case "1-0":
            return GameResult.WhiteWin;
         case "0-1":
            return GameResult.BlackWin;
         case "1/2-1/2":
            return GameResult.Draw;
         case "---":
         case "*":
         case "":
         default:
            return GameResult.None;
      }
   }

   public static string GetResultString(GameResult gameResult)
   {
      switch (gameResult)
      {
         case GameResult.WhiteWin:
            return "1-0";
         case GameResult.BlackWin:
            return "0-1";
         case GameResult.Draw:
            return "1/2-1/2";
         case GameResult.None:
         default:
            return "*";
      }
   }

   // For exporting player's games

   public int WhiteRating { get; set; }
   public int BlackRating { get; set; }
   public int WhiteNewRating { get; set; }
   public int WhitePerf { get; set; }
   public int WhitePerfAvgOpp { get; set; }
   public int BlackNewRating { get; set; }
   public int BlackPerf { get; set; }
   public int BlackPerfAvgOpp { get; set; }

   // After the game, from the perspective of the named player
   public string PlayerName { get; set; } = string.Empty;
   public string OpponentName { get; set; } = string.Empty;
   public string PlayerColor { get; set; } = string.Empty;
   public double PlayerResult { get; set; } = 0.0;

   public int PlayerElo { get; set; }
   public int PlayerGlicko { get; set; }
   public int PlayerDeviation { get; set; }
   public int PlayerClubRating { get; set; }
   public int PlayerClubRatingAvgOpp { get; set; }
   public int PlayerEloPerf { get; set; }
   public int PlayerEloPerfAvgOpp { get; set; }
   public int PlayerGlickoPerf { get; set; }
   public int PlayerGlickoPerfAvgOpp { get; set; }
   public int PlayerClubPerf { get; set; }
   public int PlayerClubPerfAvgOpp { get; set; }
}
