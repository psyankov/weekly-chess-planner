namespace Ant.Weekly;

public static class DateTimeExtensions
{
   public static DateOnly StartOfWeek(this DateOnly d, DayOfWeek startOfWeek)
   {
      int diff = (7 + (d.DayOfWeek - startOfWeek)) % 7;
      return d.AddDays(-diff);
   }

   public static DateTime StartOfWeek(this DateTime d, DayOfWeek startOfWeek)
   {
      return d.Date.StartOfWeek(startOfWeek);
   }

   public static DateOnly StartOfWeek(this DateOnly d)
   {
      return d.StartOfWeek(DayOfWeek.Monday);
   }

   public static DateOnly DateFromWeekOf(this DayOfWeek day, DateOnly startOfWeek)
   {
      var diff = (7 + (day - startOfWeek.DayOfWeek)) % 7;
      return startOfWeek.AddDays(diff);
   }

   public static int CountWeeksAfter(this DateOnly date, DateOnly older)
   {
      var day1 = older.StartOfWeek().DayNumber;
      var day2 = date.StartOfWeek().DayNumber;
      return (day2 - day1) / 7;
   }

   public static string ToAppString(this DateOnly date)
   {
      var str = date != DateOnly.MinValue ? string.Format("{0:yyyy.MM.dd}", date) : "----------";
      return str;
   }
}