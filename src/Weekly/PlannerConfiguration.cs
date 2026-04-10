namespace Ant.Weekly;
public class PlannerConfiguration
{
   public int DefaultBoardCount { get; set; } = 3;
   public int MaximumBoardCount { get; set; } = 4;
   public int MaximumGamesPerWeek { get; set; } = 2;
   public int RejectRecentOpponents { get; set; } = 3;
   public int KeepMinimumPotentialOpponents { get; set; } = 5;
   public int OpponentVarietyBasedOnMaxWeeks { get; set; } = 10;
   public int OpponentVarietyBasedOnGamesCount { get; set; } = 6;
}
