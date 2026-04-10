using System.Text;

namespace Ant.Weekly;

public class Player : IComparable<Player>
{
   public string Name { get; set; }
   public bool Active { get; set; }
   public bool LeaguePlayer { get; set; }
   public Rating Rating { get; private set; }
   public Rating InitialRating { get; }
   public Section Section { get; set; }
   public Section SectionLimit { get; set; }
   public int GamesPerWeek { get; set; }
   public Availability Availability { get; set; }

   public Player(PlayerCsv playerCsv)
   {
      Name = playerCsv.Name;
      Active = playerCsv.Active;

      LeaguePlayer = playerCsv.LeaguePlayer;
      Section = playerCsv.GetSection();
      SectionLimit = playerCsv.GetOpponentSectionLimit();

      GamesPerWeek = playerCsv.GamesPerWeek;

      Availability = new Availability(this,
         monday: playerCsv.Monday,
         tuesday: playerCsv.Tuesday,
         wednesday: playerCsv.Wednesday,
         thursday: playerCsv.Thursday,
         friday: playerCsv.Friday
         );

      Rating = new Rating(this, playerCsv.InitialEloEstablished, playerCsv.InitialElo, playerCsv.InitialGlicko, playerCsv.InitialDeviation, playerCsv.InitialClubRating);

      InitialRating = Rating.Clone();
   }

   public Player(string name)
      : this(name, Section.E, Section.A, eloEstablished: false, App.S.Rating.InitialElo,
           App.S.Rating.InitialGlicko, App.S.Rating.InitialDeviation, App.S.Rating.InitialClubRating) { }

   public Player(string name, Section section, Section sectionLimit, bool eloEstablished, double elo, double glicko, double deviation, double clubRating)
   {
      ArgumentNullException.ThrowIfNull(name);

      Name = name.Trim();
      if (Name.Length == 0)
      {
         var msg = "Player name cannot be an empty string";
         Out.Error(msg);
         throw new ArgumentException(msg);
      }
      GamesPerWeek = 0;
      Section = section;
      SectionLimit = sectionLimit;
      Rating = new Rating(this, eloEstablished, elo, glicko, deviation, clubRating);
      InitialRating = Rating.Clone();
      Availability = new Availability(this);
   }

   public int CompareTo(Player? other) => Name.CompareTo(other?.Name);

   public bool PlayingThisWeek => GamesPerWeek > 0;

   public int WhiteCount => Games.Count(g => g.WhitePlayer == this && g.Result != GameResult.None);
   public int BlackCount => Games.Count(g => g.BlackPlayer == this && g.Result != GameResult.None);

   public List<Game> Games { get; private set; } = new();

   public void PrepareHistory(Games games)
   {
      Games = games.All
         .Where(g => g.Players.Contains(this))
         .OrderBy(g => g.Date)
         .ToList();
   }

   public double NetScoreAsOf(DateOnly date)
   {
      return Games.Where(g => g.Date <= date).Aggregate(0.0, (s, g) => s + g.PlayerResult(this));
   }

   public double NetScore()
   {
      return NetScoreAsOf(DateOnly.FromDateTime(DateTime.Now));
   }

   public int CountGamesAgainst(Player player, DateOnly asOfDate)
   {
      return Games.Count(g => g.Players.Contains(player) && g.Date <= asOfDate && g.Result != GameResult.None);
   }

   public int CountGamesAsWhiteAgainst(Player player, DateOnly asOfDate)
   {
      return Games.Count(g => g.BlackPlayer == player && g.Date <= asOfDate && g.Result != GameResult.None);
   }

   public DateOnly LastDatePlayed() => LastDatePlayedBefore(DateOnly.FromDateTime(DateTime.Now));

   public DateOnly LastDatePlayedBefore(DateOnly beforeDate)
   {
      var previousGames = Games
         .Where(g => g.Date < beforeDate && g.Result != GameResult.None);

      var lastPlayedDate = DateOnly.MinValue;
      if (previousGames.Any())
      {
         lastPlayedDate = previousGames.Last().Date;
      }
      else
      {
         lastPlayedDate = DateOnly.MinValue;
      }

      return lastPlayedDate;
   }

   public DateOnly LastDatePlayedAgainst(Player player)
   {
      var games = Games
         .Where(g => g.Players.Contains(player) && g.Result != GameResult.None)
         .OrderByDescending(g => g.Date);

      if (games.Any())
      {
         return games.First().Date;
      }
      else
      {
         return DateOnly.MinValue;
      }
   }

   public void AssignNewRating(Rating? newRating)
   {
      ArgumentNullException.ThrowIfNull(newRating);
      Rating = newRating;
   }

   public void ResetRating() => Rating = InitialRating.Clone();

   #region Game planning

   public double OpponentVariety { get; private set; }
   public List<Player> OpponentOptions { get; } = new();
   public int PlannedGamesCount { get; set; }
   public Dictionary<DayOfWeek, bool> PlannedDays { get; } = new()
   {
      { DayOfWeek.Monday, false },
      { DayOfWeek.Tuesday, false },
      { DayOfWeek.Wednesday, false },
      { DayOfWeek.Thursday, false },
      { DayOfWeek.Friday, false }
   };

   public void ResetPlanningData()
   {
      OpponentOptions.Clear();
      PlannedGamesCount = 0;
      foreach (var day in PlannedDays.Keys)
      {
         PlannedDays[day] = false;
      }
   }

   public void UpdateOpponentVariety()
   {
      List<Game> games = new(Games);
      List<Game> gamesCopy = new(Games);
      foreach (var game in gamesCopy)
      {
         // Keep only one game against each opponent per day
         games.RemoveAll(g => g.Date == game.Date && g.Players.Contains(game.Opponent(this)));
         games.Add(game);
      }

      var count = 0;
      int sumWeeks = 0;
      foreach (var game in games.OrderByDescending(g => g.Date))
      {
         count++;
         sumWeeks += game.LastPlayedWeeksAgo > App.S.Planner.OpponentVarietyBasedOnMaxWeeks ? App.S.Planner.OpponentVarietyBasedOnMaxWeeks : game.LastPlayedWeeksAgo;

         if (count >= App.S.Planner.OpponentVarietyBasedOnGamesCount)
         {
            break;
         }
      }

      OpponentVariety = (double)sumWeeks / count;
   }

   #endregion

   #region Presentation helper methods

   public override string ToString() => $"{Name} ({Rating.Value:f0})";

   public static string GetShortInfoHeader()
   {
      return
         "Name".PadRight(23) +
         "Section".PadLeft(7) +
         "Score".PadLeft(6) +
         "Games".PadLeft(6) +
         "Win".PadLeft(7) +
         "Elo".PadLeft(7) +
         "Glicko".PadLeft(7) +
         "Club".PadLeft(7) +
         "Days".PadLeft(8) +
         "LastPlayed".PadLeft(14) +
         "WkAgo".PadLeft(6) +
         "Variety".PadLeft(8);
   }

   public string GetShortInfo()
   {
      var inactive = Active ? "" : "(-)";
      var wks = DateOnly.FromDateTime(DateTime.Now).CountWeeksAfter(LastDatePlayed());
      var wksStr = LastDatePlayed() == DateOnly.MinValue ? "---" : wks.ToString();

      var str = new StringBuilder(120);
      str.AppendFormat("{0,-25}", $"{inactive}{Name}");
      str.AppendFormat("{0} / {1}", Section, SectionLimit);
      str.AppendFormat("{0,6:f1}", NetScore());
      str.AppendFormat("{0,6}", Games.Count());
      str.AppendFormat("{0,7:p0}", Games.Count() == 0 ? 0 : NetScore() / Games.Count());
      str.AppendFormat("{0,7:f0}", Rating.Elo);
      str.AppendFormat("{0,7:f0}", Rating.Glicko);
      str.AppendFormat("{0,7:f0}", Rating.ClubRating);
      str.AppendFormat("{0,8}", Availability);
      str.AppendFormat("{0,14}", LastDatePlayed().ToAppString());
      str.AppendFormat("{0,6}", wksStr);
      str.AppendFormat("{0,8:f1}", OpponentVariety);

      return str.ToString();
   }

   public void ListFullInfo(bool verbose)
   {
      var fullInfo = new StringBuilder(1024)
         .Append($"\n{Name,-20}\n")
         .Append($"\n".PadLeft(36, '-'))
         .Append($"Section                  {Section,10}\n")
         .Append($"Play up to section       {SectionLimit,10}\n")
         .Append($"Last played on           {LastDatePlayed().ToAppString(),10}\n")
         .Append($"Games played             {Games.Count(),10}\n")
         .Append($"Net score                {NetScore(),10}\n")
         .Append($"Win rate                 {(Games.Count() == 0 ? 0 : NetScore() / Games.Count()),10:p0}\n")
         .Append($"Elo rating               {Rating.Elo,10:f0}\n")
         .Append($"   Established           {Rating.EloEstablished,10}\n")
         .Append($"   Performance           {Rating.EloPerf,10:f0} against average {Rating.EloPerfAvgOpp,4:f0}\n")
         .Append($"Glicko rating            {Rating.Glicko,10:f0}\n")
         .Append($"   Deviation             {Rating.CurrentDeviation(DateOnly.FromDateTime(DateTime.Now)),10:f0}\n")
         .Append($"   Performance           {Rating.GlickoPerf,10:f0} against average {Rating.GlickoPerfAvgOpp,4:f0}\n")
         .Append($"Club rating              {Rating.ClubRating,10:f0}\n")
         .Append($"   Performance           {Rating.ClubPerf,10:f0} against average {Rating.ClubPerfAvgOpp,4:f0}\n")
         .Append($"Games per week           {GamesPerWeek,10}\n")
         .Append($"Availability             {Availability,10}\n")
         .Append($"Opponent variety (/{App.S.Planner.OpponentVarietyBasedOnGamesCount,2}){OpponentVariety,13:f1}\n");

      Out.Write(fullInfo.ToString());
      Out.Write($"{Games.Count} games history\n");
      if (!verbose)
      {
         Out.Write(Game.GetInfoForPlayerHeader().ToString());
         Out.Write();
      }

      var i = 0;

      foreach (var game in Games)
      {
         i++;
         
         var str = new StringBuilder(1024)
            .Append($"{i,4}.  ")
            .Append(game.InfoFor(this));

         if (verbose)
         {
            str = new StringBuilder(1024)
               .Append($"{i,4}.  ")
               .Append(game.VerboseInfo());
         }
         Out.Write(str.ToString());
      }
   }

   public double ScoreAgainst(Player player, DateOnly asOfDate)
   {
      var score = Games
         .Where(g => g.Opponent(this) == player && g.Date <= asOfDate && g.Result != GameResult.None)
         .Aggregate(0.0, (a, g) => a + g.PlayerResult(this));
      return score;
   }

   #endregion
}
