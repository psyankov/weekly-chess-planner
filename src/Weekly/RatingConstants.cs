namespace Ant.Weekly;

public sealed class RatingConstants
{
   // Values are provided though the AppSettings.json configuratiton file
   // If the file is missing, default values below are used

   // Which rating system to use in output lists and sort criteria
   public bool PreferElo { get; set; } = true;
   public bool PreferGlicko { get; set; } = false;
   public bool PreferClubRating { get; set; } = false;

   // Both Glicko and Elo rating values cannot drop below this value
   public double MinimumRating { get; set; } = 100;
   // Greater than all the expected real rating values
   public double MaximumRating { get; set; } = 9000;

   // First N games for new players are evaluated using increased K factor
   // Their opponent rating change (if the opponent is an established player)
   // is calculated using proportionately decreased K factor.
   // For example 4x for the new players and 1/4x for the established rating.
   public double EloK1Factor { get; set; } = 400;
   public double EloK1GamesCount { get; set; } = 5;

   // Next N games for new players are evaluated using another increased K factor
   // ... after this, the rating is considered to be established and
   // and standard K factor applies
   public double EloK2Factor { get; set; } = 200;
   public double EloK2GamesCount { get; set; } = 5;

   // K factor in Elo system for established rating
   public double EloKFactor { get; set; } = 100;

   // Elo performance rating - performance rating over the last N games
   // based on the opponent's Elo rating
   public double EloPerfGamesCount { get; set; } = 10;

   // Count of games for performance rating based on the Glicko rating over N games
   public double GlickoPerfGamesCount { get; set; } = 10;

   // Club rating is a continuously and iteratively updated performance rating
   // based on other player's performance rating calculated from last N games
   public double ClubRatingGamesCount { get; set; } = 20;

   // Count of games for performance rating based on the club rating over N games
   public double ClubPerfGamesCount { get; set; } = 10;

   // New unrated player in Elo system
   public double InitialElo { get; set; } = 1000;
   // New unrated player in Glicko system
   public double InitialGlicko { get; set; } = 1000;
   // New unrated player's Club Rating
   public double InitialClubRating { get; set; } = 1000;

   // Deviation in Glicko rating system
   // New unrated player has much greater uncertainty of the initial rating estimate
   // Typical deviation for an active player with established rating
   // results in 95% confidence interval of +/- 100 points
   public double InitialDeviation { get; set; } = 300;
   public double TypicalDeviation { get; set; } = 50;
   // Inactive player's deviation grows to the maximum, unrated value over this time
   public int UncertaintyPeriodDays { get; set; } = 180;
}
