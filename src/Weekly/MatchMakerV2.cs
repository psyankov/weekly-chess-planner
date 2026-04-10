using System;

namespace Ant.Weekly;

public class MatchMakerV2
{
   private bool _Random;
   private int _GameRequestsCount;
   private List<Player> _Players;
   private List<Game> _PlannedGames;
   Func<long, List<Game>, string, bool> _ScheduleGamesDelegate;

   public MatchMakerV2(List<Player> players, List<Game> plannedGames, int gameRequestsCount, bool random, Func<long, List<Game>, string, bool> scheduleGamesDelegate)
   {
      _Random = random;
      _Players = players;
      _PlannedGames = plannedGames;
      _GameRequestsCount = gameRequestsCount;
      _ScheduleGamesDelegate = scheduleGamesDelegate;

      _IterationCountTotal = 1;

      _MatchOptions = new();
   }

   private List<Match> _MatchOptions;

   private int _CurrentGameIndex;
   private int _MaximumGameIndexReached;

   private bool _IterationFailed;
   private long _IterationCount;
   private long _IterationCountTotal;
   private long _PlannedGameSetNumber;

   public void MatchAndSchedule()
   {
      Out.Write($"\nExecuting match maker version 2");
      Log.Info($"Executing match maker version 2");

      PrepareMatchOptions();

      _CurrentGameIndex = 0;
      _MaximumGameIndexReached = 0;
      
      _IterationCount = 1;
      _PlannedGameSetNumber = 0;
      _IterationFailed = false;

      var success = TryPickNextMatch(0);

      if (!success)
      {
         Out.Warning("Failed to automatically match and schedule all game requests");

         if (_PlannedGames.Count > 0)
         {
            Out.Warning($"Attempting to schedule pre-defined match games only");
            _PlannedGameSetNumber++;
            success = _ScheduleGamesDelegate.Invoke(_PlannedGameSetNumber, _PlannedGames, "2");
            if (!success)
            {
               Out.Error("Failed to schedule pre-defined match games");
            }
         }
      }
   }

   private void PrepareMatchOptions()
   {
      var matchOptions = new List<Match>();
      foreach (var player in _Players)
      {
         foreach (var opponent in player.OpponentOptions)
         {
            var match = matchOptions.Find(m => m.Involves(player, opponent));
            if (match == null)
            {
               match = new Match(player, opponent);
               matchOptions.Add(match);
            }
         }
      }

      if (_Random)
      {
         Out.Write(ConsoleColor.Magenta, $"\nRandomizing the match making stack!\n");

         var random = new Random();
         _MatchOptions = matchOptions
            .OrderBy(m => random.NextDouble())
            .ToList();
      }
      else
      {
         _MatchOptions = matchOptions
            .OrderBy(m => m.LastPlayed)
            .ThenBy(m => Math.Min(m.Player1.OpponentVariety, m.Player2.OpponentVariety))
            .ThenByDescending(m => m.DayOptions.Count)
            .ToList();
      }

      Out.Write($"Prepared {_MatchOptions.Count} match options for {_GameRequestsCount} planned games");
      Out.Write($"{_PlannedGames.Count} games have already been planned with match override\n");
   }

   private bool TryPickNextMatch(int nextCandidateIndex)
   {
      _CurrentGameIndex++;

      Log.Matching($"Iteration {_IterationCount} game index {_CurrentGameIndex}");
      
      if (_CurrentGameIndex > _MaximumGameIndexReached)
      {
         _MaximumGameIndexReached = _CurrentGameIndex;
         Log.Matching($"Iteration {_IterationCount} reached new maximum game index {_MaximumGameIndexReached}");
      }

      for (var i = nextCandidateIndex; i < _MatchOptions.Count; i++)
      {
         var match = _MatchOptions[i];

         if (IsMatchSuitable(match))
         {
            Log.Matching($"Iteration {_IterationCount} game index {_CurrentGameIndex,2} considering match option {i} ({match})");

            var game = new Game(match.Player1, match.Player2, ColorAssignment.Fair);
            _PlannedGames.Add(game);
            match.Player1.PlannedGamesCount++;
            match.Player2.PlannedGamesCount++;
            var success = false;

            if (_CurrentGameIndex == _GameRequestsCount)
            {
               Log.Matching($"Iteration {_IterationCount} (total iterations {_IterationCountTotal}) - all players are matched");
               
               _PlannedGameSetNumber++;
               success = _ScheduleGamesDelegate.Invoke(_PlannedGameSetNumber, _PlannedGames, "2");

               if (success)
               {
                  Out.Write($"Last planned game set generation required {_IterationCount} iterations ({_IterationCountTotal} iterations total)");
               }

               Log.Matching($"Last planned game set generation required {_IterationCount} iterations ({_IterationCountTotal} iterations total)");

               // Reset the iteration count for the next game set generation if scheduling was not successful
               _IterationCount = 1;
            }
            else
            {
               // Count new iteration every time we start moving forward after
               // a failed iteration resulted in one or more steps back
               if (_IterationFailed)
               {
                  _IterationCount++;
                  _IterationCountTotal++;
                  _IterationFailed = false;
                  StatusLine.UpdateMatchingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
               }
               success = TryPickNextMatch(i + 1);
               _IterationFailed = !success;
            }

            if (success)
            {
               Log.Matching($"Iteration {_IterationCount} game {_CurrentGameIndex} confirmed match option {i} ({match})");
               return true;
            }
            else
            {
               Log.Matching($"Iteration {_IterationCount} game {_CurrentGameIndex} rejected match option {i} ({match})");
               _PlannedGames.Remove(game);
               match.Player1.PlannedGamesCount--;
               match.Player2.PlannedGamesCount--;
            }
         }
      }

      Log.Matching($"Iteration {_IterationCount} game {_CurrentGameIndex} exhausted available match option");
      _CurrentGameIndex--;
      return false;
   }

   private bool IsMatchSuitable(Match match)
   {
      Log.Matching($"Considering {match}");

      if (match.Player1.PlannedGamesCount == match.Player1.GamesPerWeek)
      {
         Log.Matching($"Rejected - {match.Player1} already has {match.Player1.PlannedGamesCount} planned games");
         return false;
      }

      if (match.Player2.PlannedGamesCount == match.Player2.GamesPerWeek)
      {
         Log.Matching($"Rejected - {match.Player2} already has {match.Player2.PlannedGamesCount} planned games");
         return false;
      }

      var ok = Scheduler.IsMatchFeasible(match, _PlannedGames);
      return ok;
   }
}
