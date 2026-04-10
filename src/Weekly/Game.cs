using System.Text;

namespace Ant.Weekly;

public class Game
{
   public DateOnly Date { get; set; } = DateOnly.MinValue;
   public DayOfWeek Day => Date.DayOfWeek;
   public Player WhitePlayer { get; set; }
   public Player BlackPlayer { get; set; }
   public GameResult Result { get; private set; } = GameResult.None;
   public string Mode { get; set; } = "OTB";
   public string Termination { get; set; } = "normal";
   public string TimeControl { get; set; } = string.Empty;
   public string Site { get; set; } = string.Empty;
   public string Event { get; set; } = "Casual";
   public string Section { get; set; } = string.Empty;
   public string Stage { get; set; } = string.Empty;
   public int Round { get; set; }

   public DateOnly LastPlayed { get; set; } = DateOnly.MinValue;
   public int LastPlayedWeeksAgo { get; set; }
   public string LastPlayedWeeksAgoString
   {
      get => LastPlayed == DateOnly.MinValue ? "---" : LastPlayedWeeksAgo.ToString();
   }

   public Rating? WhiteRating { get; set; }
   public Rating? BlackRating { get; set; }

   public Game(DateOnly date, Player whitePlayer, Player blackPlayer, GameResult result)
      : this(date, whitePlayer, blackPlayer, result, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0) { }

   public Game(DateOnly date, Player whitePlayer, Player blackPlayer, GameResult result, string timeControl, string site, string eventName, string section, string stage, int round)
   {
      WhitePlayer = whitePlayer;
      BlackPlayer = blackPlayer;

      Date = date;
      Result = result;
      TimeControl = timeControl == "*" ? string.Empty : timeControl;
      Site = site == "*" ? string.Empty : site;
      Event = eventName == "*" ? string.Empty : eventName;
      Section = section == "*" ? string.Empty : section;
      Stage = stage == "*" ? string.Empty : stage;
      Round = round;
   }

   public Game(Player player1, Player player2, ColorAssignment colorAssignment)
   {
      Result = GameResult.None;
      Date = DateOnly.MaxValue;
      DayOptions = player1.Availability.Against(player2);

      switch (colorAssignment)
      {
         case ColorAssignment.Requested:
            WhitePlayer = player1;
            BlackPlayer = player2;
            break;

         case ColorAssignment.Fair:
            (WhitePlayer, BlackPlayer) = AssignFairColors(player1, player2);
            break;

         case ColorAssignment.Random:
         default:
            (WhitePlayer, BlackPlayer) = AssignRandomColors(player1, player2);
            break;
      }
   }

   public List<Player> Players => new() { WhitePlayer, BlackPlayer };

   public double WhiteResult =>
      Result == GameResult.WhiteWin ? 1 :
      Result == GameResult.BlackWin ? 0 :
      Result == GameResult.Draw ? 0.5 : 0;

   public double BlackResult =>
      Result == GameResult.WhiteWin ? 0 :
      Result == GameResult.BlackWin ? 1 :
      Result == GameResult.Draw ? 0.5 : 0;

   public List<DayOfWeek> DayOptions { get; } = new();

   public double RatingDifference()
   {
      double diff;
      if (WhiteRating == null || BlackRating == null)
      {
         diff = WhitePlayer.Rating.Value - BlackPlayer.Rating.Value;
      }
      else
      {
         diff = WhiteRating.Value - BlackRating.Value;
      }

      return diff;
   }

   public Player Opponent(Player player) => player == WhitePlayer ? BlackPlayer : WhitePlayer;

   public bool Involves(Player player) => WhitePlayer == player || BlackPlayer == player;

   public bool Involves(Player player1, Player player2) => Involves(player1) && Involves(player2);

   public PieceColor Color(Player player) => player == WhitePlayer ? PieceColor.White : PieceColor.Black;

   public double PlayerResult(Player player) => player == WhitePlayer ? WhiteResult : BlackResult;

   public (Player, Player) AssignFairColors(Player one, Player two)
   {
      var oneAsWhite = one.CountGamesAsWhiteAgainst(two, DateOnly.MaxValue);
      var oneAsBlack = one.CountGamesAgainst(two, DateOnly.MaxValue) - oneAsWhite;

      if (oneAsWhite > oneAsBlack)
      {
         return (two, one);
      }
      else if (oneAsWhite < oneAsBlack)
      {
         return (one, two);
      }
      else
      {
         return AssignRandomColors(one, two);
      }
   }

   private Random _RandomSource = new();
   public (Player, Player) AssignRandomColors(Player one, Player two)
   {
      var random = _RandomSource.Next(0, 100);
      var white = random < 50 ? one : two;
      var black = random < 50 ? two : one;
      return (white, black);
   }

   public void UpdateLastPlayed()
   {
      DateOnly lastPlayed = DateOnly.MinValue;
      foreach (var pastGame in WhitePlayer.Games)
      {
         if (pastGame.Involves(BlackPlayer) && pastGame.Date > lastPlayed)
         {
            lastPlayed = pastGame.Date;
         }
      }
      LastPlayed = lastPlayed;
      LastPlayedWeeksAgo = DateOnly.FromDateTime(DateTime.Now).CountWeeksAfter(lastPlayed);
   }

   public void UpdateRating()
   {
      Log.Rating("Calculate rating update for {0}", ToString());

      if (Result == GameResult.None)
      {
         Log.Rating($"Game {ToString()} does not have a ratable result");
         return;
      }

      WhiteRating = WhitePlayer.Rating;
      WhiteRating.CalculateNew(this);

      BlackRating = BlackPlayer.Rating;
      BlackRating.CalculateNew(this);

      WhitePlayer.AssignNewRating(WhiteRating.New);
      BlackPlayer.AssignNewRating(BlackRating.New);
   }

   #region Presentation helper methods

   public string DayOptionsString()
   {
      var dayOptions = WhitePlayer.Availability.Days.ToWeekDaysString() + "   " +
         BlackPlayer.Availability.Days.ToWeekDaysString() + "   " +
         DayOptions.ToWeekDaysString();
      return dayOptions;
   }

   public override string ToString() => $"{Date:yyyy.MM.dd} {WhitePlayer} vs. {BlackPlayer}";

   public string PlayerResultString(Player player)
   {
      var result = player == WhitePlayer ?
         (Result == GameResult.WhiteWin ? "Win" : "Loss") :
         (Result == GameResult.BlackWin ? "Win" : "Loss");

      if (Result == GameResult.Draw)
      {
         result = "Draw";
      }

      if (Result == GameResult.None)
      {
         result = "----";
      }

      return result;
   }

   public static StringBuilder GetInfoForPlayerHeader()
   {
      var str = new StringBuilder()
         .Append("       Date".PadRight(19))
         .Append("Rating".PadRight(7))
         .Append("Color".PadRight(8))
         .Append("Opponent".PadRight(19))
         .Append("Rating".PadRight(32))
         .Append("Last Played".PadRight(12))
         .Append("Wks Ago".PadRight(10))
         .Append("Net Score".PadRight(10));

      return str;
   }

   public StringBuilder InfoFor(Player player)
   {
      var rating = WhiteRating;
      var opponentRating = BlackRating;
      if (Color(player) == PieceColor.Black)
      {
         rating = BlackRating;
         opponentRating = WhiteRating;
      }

      var str = new StringBuilder()
         .Append($"{Date:yyyy.MM.dd}  ")
         .Append($"{rating?.Value,6:f0} ")
         .Append($"{Color(player)} - ")
         .Append($"{Opponent(player).Name.PadRight(20).Substring(0, 20),-20} ")
         .Append($"{opponentRating?.Value,4:f0}   ")
         .Append($"{PlayerResultString(player)}".PadRight(7))
         .Append($"{rating?.New?.Value - rating?.Value,4:+0;-0;0} -> ")
         .Append($"{rating?.New?.Value,4:f0}    ")
         .Append($"{LastPlayed.ToAppString()}   ")
         .Append($"{LastPlayedWeeksAgoString,6}  ");

      if (player == WhitePlayer)
      {
         str
         .Append($"{WhitePlayer.ScoreAgainst(BlackPlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1} - ")
         .Append($"{BlackPlayer.ScoreAgainst(WhitePlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1}");
      }
      else
      {
         str
         .Append($"{BlackPlayer.ScoreAgainst(WhitePlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1} - ")
         .Append($"{WhitePlayer.ScoreAgainst(BlackPlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1}");
      }

      return str;
   }

   public StringBuilder VerboseInfo()
   {
      var eloEstablishedWhite = WhiteRating?.EloEstablished ?? false ? "Elo is established" : "Elo is provisional";
      var eloEstablishedBlack = BlackRating?.EloEstablished ?? false ? "Elo is established" : "Elo is provisional";

      var str = new StringBuilder(120)
         .Append($"{Date:yyyy.MM.dd} {Event} {Stage} Round {Round}\n\n")
         .Append($"           White {WhitePlayer.Name,-20} ")
         .Append($"{PlayerResultString(WhitePlayer).PadRight(6)} ")
         .Append($"{WhiteRating} --> {WhiteRating?.New}\n")
         .Append($"                 Expected result             ")
         .Append($"Elo {WhiteRating?.EloExpectedResult,4:f2} ")
         .Append($"Glicko {WhiteRating?.GlickoExpectedResult,4:f2}\n")
         .Append($"                 K Factor                    ")
         .Append($"Elo {WhiteRating?.EloKFactor,4:f0} ")
         .Append($"Glicko {WhiteRating?.GlickoKFactor,4:f0}\n")
         .Append($"                 {eloEstablishedWhite}\n\n")
         .Append($"           Black {BlackPlayer.Name,-20} ")
         .Append($"{PlayerResultString(BlackPlayer).PadRight(6)} ")
         .Append($"{BlackRating} --> {BlackRating?.New}\n")
         .Append($"                 Expected result             ")
         .Append($"Elo {BlackRating?.EloExpectedResult,4:f2} ")
         .Append($"Glicko {BlackRating?.GlickoExpectedResult,4:f2}\n")
         .Append($"                 K Factor                    ")
         .Append($"Elo {BlackRating?.EloKFactor,4:f0} ")
         .Append($"Glicko {BlackRating?.GlickoKFactor,4:f0}\n")
         .Append($"                 {eloEstablishedBlack}\n");

      return str;
   }

   public static StringBuilder GameTableHeaderStringBuilder() => new StringBuilder(120)
      .Append("Date".PadRight(13))
      .Append(BasicGameTableHeaderStringBuilder());

   public StringBuilder GameTableStringBuilder() => new StringBuilder(120)
      .Append($"{Date,-13:yyyy.MM.dd}")
      .Append(BasicGameTableStringBuilder());

   public static StringBuilder BasicGameTableHeaderStringBuilder()
   {
      var str = new StringBuilder(120)
         .Append("Event".PadRight(16))
         .Append("White".PadRight(19))
         .Append("Rating   ")
         .Append("Black".PadRight(19))
         .Append("Rating   ")
         .Append("Diff.   ")
         .Append("Result   ")
         .Append("LastPlayed  ")
         .Append("WkAgo  ")
         .Append("Net Score");
      
      return str;
   }

   public StringBuilder BasicGameTableStringBuilder()
   {
      var str = new StringBuilder(120)
         .Append($"{Event.PadRight(13).Substring(0,13),-16}")
         .Append($"{WhitePlayer.Name,-20} {WhitePlayer.Rating.Value,4:f0}   ")
         .Append($"{BlackPlayer.Name,-20} {BlackPlayer.Rating.Value,4:f0}   ")
         .Append($"{RatingDifference(),5:+#;-#;0}   ")
         .Append($"{Result.ToAppString().PadLeft(5).PadRight(9)}")
         .Append($"{LastPlayed.ToAppString()}   ")
         .Append($"{LastPlayedWeeksAgoString,4} ")
         .Append($"{WhitePlayer.ScoreAgainst(BlackPlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1} - ")
         .Append($"{BlackPlayer.ScoreAgainst(WhitePlayer, DateOnly.FromDateTime(DateTime.Now)),4:f1}");

      return str;
   }

   public string MatchString() => $"{WhitePlayer} vs. {BlackPlayer}";

   public static StringBuilder MatchTableHeaderStringBuilder()
   {
      var str = BasicGameTableHeaderStringBuilder()
         .Append("    Day Options");
      return str;
   }

   public StringBuilder MatchTableString()
   {
      var str = BasicGameTableStringBuilder()
         .Append($"   {DayOptionsString()}");

      return str;
   }

   public static StringBuilder ScheduleTableHeaderStringBuilder()
   {
      var str = BasicGameTableHeaderStringBuilder()
         .Append("    Date");
      return str;
   }

   public StringBuilder ScheduleTableStringBuilder()
   {
      var str = BasicGameTableStringBuilder()
         .Append($"   {Date:yyyy.MM.dd dddd}");

      return str;
   }

   #endregion
}
