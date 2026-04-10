namespace Ant.Weekly;

public sealed class LoggingConfiguration
{
   // Application level logging
   public string Level { get; set; } = "off";

   // Verbose logging per applicaiton function
   public bool Rating { get; set; }
   public bool Planning { get; set; }
   public bool Matching { get; set; }
   public bool Scheduling { get; set; }

   // Log file name (in the current working directory)
   public string LogFileName { get; set; } = "ant.weekly.log";
   // Enable logging to file
   public bool LogToFile { get; set; }
   // Enable logging to console
   public bool LogToConsole { get; set; }
}
