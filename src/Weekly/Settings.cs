namespace Ant.Weekly;

public sealed class Settings
{
   // Values are provided though the AppSettings.json configuratiton file
   // If the file is missing, default values below are used
   public UIConfiguration UI { get; set; } = new();
   public PlannerConfiguration Planner { get; set; } = new();
   public DataConfiguration Data { get; set; } = new();
   public LoggingConfiguration Logging { get; set; } = new();
   public RatingConstants Rating { get; set; } = new();
}
