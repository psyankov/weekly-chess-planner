namespace Ant.Weekly;

public class DateRange
{
   public DateOnly Start { get; }
   public DateOnly End { get; }

   public DateRange(DateOnly start, DateOnly end)
   {
      if (start > end)
         throw new ArgumentException("Start date must be earlier than end date.");

      Start = start;
      End = end;
   }

   public override string ToString() => $"{Start:yyyy.MM.dd} - {End:yyyy.MM.dd}";

   public IEnumerable<DateOnly> GetDates()
   {
      for (DateOnly date = Start; date <= End; date = date.AddDays(1))
      {
         yield return date;
      }
   }
}
