namespace Ant.Weekly;

public class Availability
{
   private Player _Player;

   public Dictionary<DayOfWeek, bool> Days { get; }

   public Availability(Player player)
      : this(player, false, false, false, false, false) { }

   public Availability(Player player, bool monday, bool tuesday, bool wednesday, bool thursday, bool friday)
   {
      _Player = player;
      Days = new Dictionary<DayOfWeek, bool>
      {
         { DayOfWeek.Monday, monday },
         { DayOfWeek.Tuesday, tuesday },
         { DayOfWeek.Wednesday, wednesday },
         { DayOfWeek.Thursday, thursday },
         { DayOfWeek.Friday, friday }
      };
   }

   public override string ToString()
   {
      var days = string.Empty;
      days += Days[DayOfWeek.Monday] ? "M" : "-";
      days += Days[DayOfWeek.Tuesday] ? "T" : "-";
      days += Days[DayOfWeek.Wednesday] ? "W" : "-";
      days += Days[DayOfWeek.Thursday] ? "R" : "-";
      days += Days[DayOfWeek.Friday] ? "F" : "-";

      return days;
   }

   public int DaysCount() => Days.Aggregate(0, (i, kv) => i + (kv.Value ? 1 : 0));

   public List<DayOfWeek> Against(Player player)
   {
      var overlap = Days
         .Where(d => d.Value && player.Availability.Days[d.Key])
         .Select(d => d.Key)
         .ToList();
      return overlap;
   }

   public bool Overlap(Availability other)
   {
      var overlap = Days.Where(d => d.Value && other.Days[d.Key]).Any();
      return overlap;
   }

   public static string ConvertToString(List<DayOfWeek> days)
   {
      var dayString = string.Empty;
      dayString += days.Contains(DayOfWeek.Monday) ? "M" : "-";
      dayString += days.Contains(DayOfWeek.Tuesday) ? "T" : "-";
      dayString += days.Contains(DayOfWeek.Wednesday) ? "W" : "-";
      dayString += days.Contains(DayOfWeek.Thursday) ? "R" : "-";
      dayString += days.Contains(DayOfWeek.Friday) ? "F" : "-";
      dayString += days.Contains(DayOfWeek.Saturday) ? "S" : "-";
      dayString += days.Contains(DayOfWeek.Sunday) ? "S" : "-";

      return dayString;
   }
}
