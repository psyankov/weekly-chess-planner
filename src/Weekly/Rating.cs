using System.Runtime.InteropServices.Marshalling;

namespace Ant.Weekly;

public class Rating
{
   public Player Player { get; }
   public DateOnly Date { get; }
   public Game? Game { get; private set; }
   public Rating? New { get; private set; }
   public Rating? Previous { get; private set; }

   private Queue<(double oppRating, double result)> _ClubRatingGames;
   private Queue<(double oppRating, double result)> _EloPerfGames;
   private Queue<(double oppRating, double result)> _GlickoPerfGames;
   private Queue<(double oppRating, double result)> _ClubPerfGames;

   public Rating(Player player)
      : this(player,
           eloEstablished: false,
           elo: App.S.Rating.InitialElo,
           glicko: App.S.Rating.InitialGlicko,
           deviation: App.S.Rating.InitialDeviation,
           clubRating: App.S.Rating.InitialClubRating
           ) { }

   public Rating(Player player, bool eloEstablished, double elo, double glicko, double deviation, double clubRating)
      : this(player,
           game: null,
           previousRating: null,
           eloEstablished: eloEstablished,
           elo: elo, glicko: glicko, deviation: deviation, clubRating: clubRating,
           eloPerf: 0, glickoPerf: 0, clubPerf: 0,
           clubRatingGames: new Queue<(double oppRating, double result)>(),
           eloPerfGames: new Queue<(double oppRating, double result)>(),
           glickoPerfGames: new Queue<(double oppRating, double result)>(),
           clubPerfGames: new Queue<(double oppRating, double result)>()
           ) { }

   public Rating(Player player, Game? game, Rating? previousRating, bool eloEstablished,
      double elo, double glicko, double deviation, double clubRating,
      double eloPerf, double glickoPerf, double clubPerf,
      Queue<(double oppRating, double result)> clubRatingGames,
      Queue<(double oppRating, double result)> eloPerfGames,
      Queue<(double oppRating, double result)> glickoPerfGames,
      Queue<(double oppRating, double result)> clubPerfGames)
   {
      Player = player;
      Game = game;
      Date = game?.Date ?? DateOnly.MinValue;
      Previous = previousRating;
      EloEstablished = eloEstablished;
      
      New = null;

      Elo = elo;
      Glicko = glicko;
      Deviation = deviation;
      ClubRating = clubRating;

      EloPerf = eloPerf;
      GlickoPerf = glickoPerf;
      ClubPerf = clubPerf;

      _ClubRatingGames = clubRatingGames;
      _EloPerfGames = eloPerfGames;
      _GlickoPerfGames = glickoPerfGames;
      _ClubPerfGames = clubPerfGames;

      ClubRatingAvgOpp = AverageOpponentRating(_ClubRatingGames);
      EloPerfAvgOpp = AverageOpponentRating(_EloPerfGames);
      GlickoPerfAvgOpp = AverageOpponentRating(_GlickoPerfGames);
      ClubPerfAvgOpp = AverageOpponentRating(_ClubPerfGames);
   }

   public Rating Clone()
   {
      return new Rating(Player, Game, Previous, EloEstablished,
         Elo, Glicko, Deviation, ClubRating,
         EloPerf, GlickoPerf, ClubPerf,
         new Queue<(double oppRating, double result)>(_ClubRatingGames),
         new Queue<(double oppRating, double result)>(_EloPerfGames),
         new Queue<(double oppRating, double result)>(_GlickoPerfGames),
         new Queue<(double oppRating, double result)>(_ClubPerfGames));
   }

   public double Value
   {
      get
      {
         if (App.S.Rating.PreferElo)
         {
            return Elo;
         }
         else if (App.S.Rating.PreferGlicko)
         {
            return Glicko;
         }
         else
         {
            return ClubRating;
         }
      }
   }

   public double Perf
   {
      get
      {
         if (App.S.Rating.PreferElo)
         {
            return EloPerf;
         }
         else if (App.S.Rating.PreferGlicko)
         {
            return GlickoPerf;
         }
         else
         {
            return ClubPerf;
         }
      }
   }

   public double PerfAvgOpp
   {
      get
      {
         if (App.S.Rating.PreferElo)
         {
            return EloPerfAvgOpp;
         }
         else if (App.S.Rating.PreferGlicko)
         {
            return GlickoPerfAvgOpp;
         }
         else
         {
            return ClubPerfAvgOpp;
         }
      }
   }


   public bool EloEstablished { get; private set; }
   public double Elo { get; private set; }
   public double Glicko { get; private set; }

   // Deviation remains at the last calculated value after a game
   // Deviation increase over time is only calculated when necessary to evaluate
   // a new game and is not recorded - get value using CurrentDeviation(date)
   public double Deviation { get; private set; }

   public double EloExpectedResult { get; private set; }
   public double GlickoExpectedResult { get; private set; }

   public double EloKFactor { get; private set; }
   public double GlickoKFactor { get; private set; }

   public double ClubRating { get; private set; }
   public double ClubRatingAvgOpp { get; private set; }
   public double EloPerf { get; private set; }
   public double EloPerfAvgOpp { get; private set; }
   public double GlickoPerf { get; private set; }
   public double GlickoPerfAvgOpp { get; private set; }
   public double ClubPerf { get; private set; }
   public double ClubPerfAvgOpp { get; private set; }

   public override string ToString() => $"Elo {Elo,4:f0} Glicko {Glicko,4:f0} Club {ClubRating,4:f0}";

   public void CalculateNew(Game game)
   {
      ArgumentNullException.ThrowIfNull(game);

      var actualResult = game.PlayerResult(Player);

      // Elo

      var newElo = Elo;
      var opponentElo = game.Opponent(Player).Rating.Elo;
      EloExpectedResult = ExpectedResultElo(Elo, opponentElo);
      
      // If the game is between two players with established rating
      EloKFactor = App.S.Rating.EloKFactor;

      if (!EloEstablished)
      {
         // If the player does not have an established rating, we apply an increased K-factor
         var gamesCount = Player.Games.Where(g => g.Date <= Date).Count();

         if (gamesCount <= App.S.Rating.EloK1GamesCount)
         {
            EloKFactor = App.S.Rating.EloK1Factor;
         }
         else if (gamesCount <= App.S.Rating.EloK1GamesCount + App.S.Rating.EloK2GamesCount)
         {
            EloKFactor = App.S.Rating.EloK2Factor;
         }
      }

      if (EloEstablished && !game.Opponent(Player).Rating.EloEstablished)
      {
         // If the opponent does not have an established rating, we apply
         // a reduced K-factor to calculate a change of the established rating
         // reducing impact of the new player's rating uncertainty
         var opponentGamesCount = game.Opponent(Player).Games.Where(g => g.Date <= Date).Count();

         if (opponentGamesCount <= App.S.Rating.EloK1GamesCount)
         {
            EloKFactor = App.S.Rating.EloKFactor * App.S.Rating.EloKFactor / App.S.Rating.EloK1Factor;
         }
         else if (opponentGamesCount <= App.S.Rating.EloK1GamesCount + App.S.Rating.EloK2GamesCount)
         {
            EloKFactor = App.S.Rating.EloKFactor * App.S.Rating.EloKFactor / App.S.Rating.EloK2Factor;
         }
      }

      newElo = Math.Min(App.S.Rating.MaximumRating, Math.Max(App.S.Rating.MinimumRating,
         Elo + EloKFactor * (actualResult - EloExpectedResult)));

      var newEloEstablished = EloEstablished ? true : Player.Games.Where(g => g.Date <= Date).Count() >= App.S.Rating.EloK1GamesCount + App.S.Rating.EloK2GamesCount;

      // Glicko

      var opponentGlicko = game.Opponent(Player).Rating.Glicko;
      var opponentDeviation = game.Opponent(Player).Rating.CurrentDeviation(game.Date);

      var deviation = CurrentDeviation(game.Date);
      GlickoKFactor = GetGlickoKFactor(Glicko, deviation, opponentGlicko, opponentDeviation);
      GlickoExpectedResult = ExpectedResultGlicko(Glicko, opponentGlicko, opponentDeviation);

      var newGlicko = Math.Min(App.S.Rating.MaximumRating, Math.Max(App.S.Rating.MinimumRating,
         Glicko + GlickoKFactor * (actualResult - GlickoExpectedResult)));
      var newDeviation = Math.Sqrt(1 / (1 / (deviation * deviation) + 1 / D2(Player.Rating.Glicko, opponentGlicko, opponentDeviation)));

      // Club rating (iterative performance)

      var opponentClubRating = game.Opponent(Player).Rating.ClubRating;
      _ClubRatingGames.Enqueue((opponentClubRating, actualResult)); // add this (newest) game
      if (_ClubRatingGames.Count > App.S.Rating.ClubRatingGamesCount)
      {
         _ClubRatingGames.Dequeue(); // discard oldest game
      }
      
      var newClubRating = TruePerformanceRating(ClubRating, _ClubRatingGames);

      // Elo performance

      var newEloPerf = EloPerf;
      if (game.Opponent(Player).Rating.EloEstablished)
      {
         // Update only the opponent has an established rating
         _EloPerfGames.Enqueue((opponentElo, actualResult)); // add this (newest) game
         if (_EloPerfGames.Count > App.S.Rating.EloPerfGamesCount)
         {
            _EloPerfGames.Dequeue(); // discard oldest game
         }
         newEloPerf = TruePerformanceRating(Elo, _EloPerfGames);
      }

      // Glicko performance

      _GlickoPerfGames.Enqueue((opponentGlicko, actualResult));
      if (_GlickoPerfGames.Count > App.S.Rating.GlickoPerfGamesCount)
      {
         _GlickoPerfGames.Dequeue();
      }
      var newGlickoPerf = TruePerformanceRating(Glicko, _GlickoPerfGames);

      // Club performance

      _ClubPerfGames.Enqueue((opponentClubRating, actualResult));
      if (_ClubPerfGames.Count > App.S.Rating.ClubPerfGamesCount)
      {
         _ClubPerfGames.Dequeue();
      }
      var newClubPerf = TruePerformanceRating(ClubRating, _ClubPerfGames);

      // Apply new rating

      New = new Rating(Player, game, this, newEloEstablished,
         elo: newElo,
         glicko: newGlicko,
         deviation: newDeviation,
         clubRating: newClubRating,
         eloPerf: newEloPerf,
         glickoPerf: newGlickoPerf,
         clubPerf: newClubPerf,
         clubRatingGames: new Queue<(double oppRating, double result)>(_ClubRatingGames),
         eloPerfGames: new Queue<(double oppRating, double result)>(_EloPerfGames),
         glickoPerfGames: new Queue<(double oppRating, double result)>(_GlickoPerfGames),
         clubPerfGames: new Queue<(double oppRating, double result)>(_ClubPerfGames)
         );

      Log.Rating($"New rating for {Player.Name} after game against {game.Opponent(Player)} on {Date}: Elo {newElo:f1} (established {newEloEstablished}) Glicko {newGlicko:f1} Deviation {newDeviation:f1} Club {newClubRating:f1} Elo Perf {newEloPerf:f1} Glicko Perf {newGlickoPerf:f1} Club Perf {newClubPerf:f1}");
   }

   public double CurrentDeviation(DateOnly date)
   {
      var currentDeviation = Deviation;
      var lastPlayedDate = Player.LastDatePlayedBefore(date);
      var inactiveDaysCount = date.DayNumber - lastPlayedDate.DayNumber - 1;
      if (lastPlayedDate == DateOnly.MinValue)
      {
         inactiveDaysCount = 0;
      }
      currentDeviation = CurrentDeviation(inactiveDaysCount);

      return currentDeviation;
   }

   public double CurrentDeviation(int inactiveDays)
   {
      var rd = Math.Sqrt(Deviation * Deviation + C2 * inactiveDays);
      rd = Math.Min(App.S.Rating.InitialDeviation, rd);

      return rd;
   }

   public static double Q = Math.Log(10) / 400;

   /// <summary>
   /// Estimate 'c' squared - square of deviation growth rate over period of inactivity
   /// Current deviation RD = SQRT( oldRD^2 + C^2 * N ) where N = period of inactivity
   /// RD is never greater than the UnratedDeviation value
   /// Assuming that a typical established rating will grows to its maximum unrated value
   /// over a period of UncertaintyPeriodDays (count of days), we get
   /// UnratedDeviation^2 =  TypicalDeviation^2 + C^2 * UncertaintyPeriodDays
   /// </summary>
   // In principle, we can only calculate this once however it break the unit tests that may
   // set values of calculation parameters to something other than the current production values. 
   public static double C2 => (App.S.Rating.InitialDeviation * App.S.Rating.InitialDeviation - App.S.Rating.TypicalDeviation * App.S.Rating.TypicalDeviation) / App.S.Rating.UncertaintyPeriodDays;

   /// <summary>
   /// </summary>
   /// <param name="RDi">Opponent's rating deviation</param>
   /// <returns></returns>
   public static double G(double RDi)
   {
      var g = 1 / Math.Sqrt(1 + 3 * Q * Q * RDi * RDi / (Math.PI * Math.PI));
      return g;
   }

   /// <summary>
   /// Expected score for the player rated R (Elo)
   /// </summary>
   /// <param name="R">Player's rating</param>
   /// <param name="Ri">Opponent's rating</param>
   /// <param name="result">Result from the Player's perspective</param>
   /// <returns></returns>
   public static double RatingChangeElo(double R, double Ri, double result)
   {
      var change = App.S.Rating.EloKFactor * (result - ExpectedResultElo(R, Ri));
      return change;
   }

   /// <summary>
   /// Expected score for the player rated R (Elo)
   /// </summary>
   /// <param name="R">Player's rating</param>
   /// <param name="Ri">Opponent's rating</param>
   /// <returns></returns>
   public static double ExpectedResultElo(double R, double Ri)
   {
      // Using logistic curve with base 10
      // Expected result for a player rated R0 against player rated Ri
      // Elo scaling results in a difference of 200 rating points meaning that
      // the stronger player has an expected score of approximately 0.75
      var e = 1 / (1 + Math.Pow(10, -(R - Ri) / 400));
      return e;
   }

   /// <summary>
   /// Expected score for the player rated R (Glicko)
   /// </summary>
   /// <param name="R">Player's rating</param>
   /// <param name="Ri">Opponent's rating</param>
   /// <param name="RDi">Opponent's rating deviation</param>
   /// <returns></returns>
   public static double ExpectedResultGlicko(double R, double Ri, double RDi)
   {
      var e = 1 / (1 + Math.Pow(10, -G(RDi) * (R - Ri) / 400));
      return e;
   }

   /// <summary>
   /// </summary>
   /// <param name="R">Player's rating</param>
   /// <param name="Ri">Opponent's rating</param>
   /// <param name="RDi">Opponent's rating deviation</param>
   /// <returns></returns>
   public static double D2(double R, double Ri, double RDi)
   {
      // d^2 in Glicko calculations
      var Ei = ExpectedResultGlicko(R, Ri, RDi);
      var d2 = 1 / (Q * Q * G(RDi) * G(RDi) * Ei * (1 - Ei));
      return d2;
   }

   /// <summary>
   /// K factor in Glicko rating system
   /// </summary>
   /// <param name="R">Player's rating</param>
   /// <param name="RD">Player's rating deviation</param>
   /// <param name="Ri">Opponent's rating</param>
   /// <param name="RDi">Opponent's rating deviation</param>
   /// <returns></returns>
   public static double GetGlickoKFactor(double R, double RD, double Ri, double RDi)
   {
      var d2 = D2(R, Ri, RDi);
      var K = Q * G(RDi) / (1 / (RD * RD) + 1 / d2);
      return K;
   }

   public static double LinearPerformanceRating(IEnumerable<(double oppRating, double result)> games)
   {
      var linearPerfRating = 0.0;
      foreach (var g in games)
      {
         linearPerfRating += g.oppRating + 800 * g.result - 400;
      }
      linearPerfRating = linearPerfRating / games.Count();

      if (linearPerfRating < App.S.Rating.MinimumRating)
      {
         linearPerfRating = App.S.Rating.MinimumRating;
      }

      if (linearPerfRating > App.S.Rating.MaximumRating)
      {
         linearPerfRating = App.S.Rating.MaximumRating;
      }

      return linearPerfRating;
   }

   public static double AverageOpponentRating(IEnumerable<(double oppRating, double result)> games)
   {
      var count = games.Count();
      var sumRating = 0.0;

      if (count > 0)
      {
         foreach (var g in games)
         {
            sumRating += g.oppRating;
         }
         return sumRating / count;
      }
      else
      {
         return 0.0;
      }
   }

   public static double TruePerformanceRating(double playerRating, IEnumerable<(double oppRating, double result)> games)
   {
      // Add a mock draw against a player with equal rating to avoid abs low or abs high results if all games are lost or won
      var gamesForCalc = games.Append((oppRating: playerRating, result: 0.5));

      // The alternative would be to use a linear calculation in this scenario:
      //if (games.All(g => g.result == 0) || games.All(g => g.result == 1.0))
      //{
      //   return LinearPerformanceRating(games);
      //}

      var rating = 0.0;
      var lowerBound = App.S.Rating.MinimumRating;
      var upperBound = App.S.Rating.MaximumRating;
      var netChange = 0.0;
     
      netChange = NetChange(lowerBound, games);
      if (netChange <= 0.0)
      {
         return lowerBound;
      }

      netChange = NetChange(upperBound, games);
      if (netChange >= 0.0)
      {
         return upperBound;
      }

      do
      {
         rating = 0.5 * (upperBound + lowerBound);
         netChange = NetChange(rating, games);

         if (netChange > 0.0)
         {
            lowerBound = rating;
         }
         else
         {
            upperBound = rating;
         }
      }
      while (Math.Abs(netChange) > 1.0);

      return rating;
   }

   public static double NetChange(double rating, IEnumerable<(double oppRating, double result)> games)
   {
      var netChange = 0.0;
      var change = 0.0;

      foreach (var game in games)
      {
         change = RatingChangeElo(rating, game.oppRating, game.result);
         netChange += change;
      }

      return netChange;
   }
}
