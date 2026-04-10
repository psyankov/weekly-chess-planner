namespace Ant.Weekly;

public static class StatusLine
{
   private static long _MatchingIteration;
   private static long _MatchingIterationTotal;
   private static long _PlannedGameSetNumber;
   private static long _SchedulingIteration;
   private static long _SchedulingIterationTotal;

   public static void UpdateMatchingIteration(long gameSet, long iteration, long totalIteration)
   {
      _PlannedGameSetNumber = gameSet;
      _MatchingIteration = iteration;
      _MatchingIterationTotal = totalIteration;

      UpdateStatusLine();
   }

   public static void UpdateSchedulingIteration(long gameSet, long iteration, long totalIteration)
   {
      _PlannedGameSetNumber = gameSet;
      _SchedulingIteration = iteration;
      _SchedulingIterationTotal = totalIteration;

      UpdateStatusLine();
   }

   private static void UpdateStatusLine()
   {
      var status = $"Matching set {_PlannedGameSetNumber,-6} iteration {_MatchingIteration,-12} total {_MatchingIterationTotal,-12} " +
         $"scheduling iteration  {_SchedulingIteration,-12} total {_SchedulingIterationTotal,-12}";
      Console.Write($"\r{status}");
   }
}
