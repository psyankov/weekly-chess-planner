namespace Ant.Weekly;

public static class Out
{
   private static bool _WarningOff;
   private static int _WarningCount;

   public static ConsoleColor StandardColor = ConsoleColor.Gray;
   public static ConsoleColor ErrorColor = ConsoleColor.DarkRed;
   public static ConsoleColor WarningColor = ConsoleColor.DarkYellow;

   public static void Error(string format, params object[] data)
   {
      Log.Error(format, data);
      Write(ErrorColor, $"ERROR: {format}", data);
   }

   public static void Warning(string format, params object[] data)
   {
      _WarningCount++;
      Log.Warning(format, data);

      if (_WarningCount <= App.S.UI.ConsoleOutWarningMaxCount)
      {
         Write(WarningColor, $"WARNING: {format}", data);
      }
      else if (!_WarningOff)
      {
         _WarningOff = true;
         Error("Maximum number of warnings has been reached - all subsequent warnings will be suppressed");
      }
   }

   public static void Write()
   {
      Write("");
   }

   public static void Write(string format, params object[] data)
   {
      Write(StandardColor, format, data);
   }

   public static void Write(ConsoleColor color, string format, params object[] data)
   {
      var currentColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(format, data);
      Console.ForegroundColor = currentColor;
   }
}
