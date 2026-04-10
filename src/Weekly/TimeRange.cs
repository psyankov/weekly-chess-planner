namespace Ant.Weekly;

public class TimeRange
{
   public TimeOnly Start { get; }
   public TimeOnly End { get; }

   public TimeRange(TimeOnly start, TimeOnly end)
   {
      if (start > end)
         throw new ArgumentException("Start time must be earlier than end date.");

      Start = start;
      End = end;
   }

   public TimeSpan Duration { get => End - Start; }
}
