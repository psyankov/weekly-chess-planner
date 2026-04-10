namespace Ant.Weekly;

public class UIConfiguration
{
   // Limit the number of warnings written to console with Out.Warning()
   // Has no effect on the behaviur of Log.Warning()
   public int ConsoleOutWarningMaxCount { get; set; } = 10;
}
