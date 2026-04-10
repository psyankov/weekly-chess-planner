namespace Ant.Weekly;

public class DataConfiguration
{
   public string GamesCsvFileName { get; set; } = "games";
   public string MatchCsvFileName { get; set; } = "match";
   public string PlayersCsvFileName { get; set; } = "players";
   public string RatingsCsvFileName { get; set; } = "ratings";
   public string CrossRefCsvFileName { get; set; } = "crossref";
   public string CrossTableCsvFileName { get; set; } = "crosstable";
   public string PlannedGamesFileName { get; set; } = "week";
   public string PlannedGamesPartialFileName { get; set; } = "week_partial";
}
