using System.Net.NetworkInformation;

namespace Ant.Weekly;

public static class ExtensionMethods
{
   public static string ToLetter(this DayOfWeek day)
   {
      return day switch
      {
         DayOfWeek.Monday => "M",
         DayOfWeek.Tuesday => "T",
         DayOfWeek.Wednesday => "W",
         DayOfWeek.Thursday => "R",
         DayOfWeek.Friday => "F",
         DayOfWeek.Saturday => "Sat",
         DayOfWeek.Sunday => "Su",
         _ => string.Empty
      };
   }

   public static string ToWeekDaysString(this Dictionary<DayOfWeek, bool> map)
   {
      return string.Join("", map.Select(kvp => kvp.Value ? kvp.Key.ToLetter() : "-"));
   }

   public static string ToWeekDaysString(this List<DayOfWeek> days)
   {
      Dictionary<DayOfWeek, bool> map = new()
      {
         { DayOfWeek.Monday, false },
         { DayOfWeek.Tuesday, false },
         { DayOfWeek.Wednesday, false },
         { DayOfWeek.Thursday, false },
         { DayOfWeek.Friday, false }
      };

      foreach (var day in days)
      {
         map[day] = true;
      }

      return string.Join("", map.Select(kvp => kvp.Value ? kvp.Key.ToLetter() : "-"));
   }

   public static string RemoveSpaces(this string str)
   {
      var s = str.Trim();
      while (s.Contains(" "))
      {
         s = s.Replace(" ", "");
      }
      return s;
   }

   public static string ToAppString(this GameResult result)
   {
      switch (result)
      {
         case GameResult.WhiteWin:
            return "1-0";
         case GameResult.BlackWin:
            return "0-1";
         case GameResult.Draw:
            return "1/2-1/2";
         case GameResult.None:
            return "---";
         default:
            throw new InvalidOperationException($"Unexpected result enum value");      
      }
   }
}
