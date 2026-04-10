namespace Ant.Weekly;

public class MatchMakerV1
{
   private bool _Random;
   private int _GameRequestsCount;

   private List<Player> _Players;
   private List<Game> _PlannedGames;
   Func<long, List<Game>, string, bool> _ScheduleGamesDelegate;

   public MatchMakerV1(List<Player> players, List<Game> plannedGames, int gameRequestsCount, bool random, Func<long, List<Game>, string, bool> scheduleGamesDelegate)
   {
      _Random = random;
      _Players = players;
      _PlannedGames = plannedGames;
      _GameRequestsCount = gameRequestsCount;
      _ScheduleGamesDelegate = scheduleGamesDelegate;

      _IterationCountTotal = 1;

      _MatchStack = new();
   }

   private Stack<Player> _MatchStack;

   private int _CurrentGameIndex;
   private int _MaximumGameIndexReached;

   private bool _IterationFailed;
   private long _IterationCount;
   private long _IterationCountTotal;
   private long _PlannedGameSetNumber;

   public void MatchAndSchedule()
   {
      Out.Write($"\nExecuting match maker version 1\n");
      Log.Info($"Executing match maker version 1");

      PrepareMatchStackOfPlayers();

      _CurrentGameIndex = 0;
      _IterationCount = 1;
      _IterationFailed = false;

      _PlannedGameSetNumber = 1;
      var player = _MatchStack.Pop();
      var success = TryMatchNextPlayer(player);

      if (!success)
      {
         Out.Warning("Failed to automatically match and schedule all game requests");

         if (_PlannedGames.Count > 0)
         {
            Out.Warning($"Attempting to schedule pre-defined match games only");
            _PlannedGameSetNumber++;
            success = _ScheduleGamesDelegate.Invoke(_PlannedGameSetNumber, _PlannedGames, "1");
            if (!success)
            {
               Out.Error("Failed to schedule pre-defined match games");
            }
         }
      }
   }

   private void PrepareMatchStackOfPlayers()
   {
      Log.Planning("Preparing match stack of players (listed from the bottom up)");

      var fiveGamePlayers = _Players
         .Where(p => p.GamesPerWeek - p.PlannedGamesCount == 5)
         .OrderByDescending(p => p.OpponentVariety)
         .ThenByDescending(p => p.OpponentOptions.Count)
         .ToList();

      var fourGamePlayers = _Players
         .Where(p => p.GamesPerWeek - p.PlannedGamesCount == 4)
         .OrderByDescending(p => p.OpponentVariety)
         .ThenByDescending(p => p.OpponentOptions.Count)
         .ToList();

      var threeGamePlayers = _Players
         .Where(p => p.GamesPerWeek - p.PlannedGamesCount == 3)
         .OrderByDescending(p => p.OpponentVariety)
         .ThenByDescending(p => p.OpponentOptions.Count)
         .ToList();

      var twoPlusGamePlayers = _Players
         .Where(p => p.GamesPerWeek - p.PlannedGamesCount >= 2)
         .OrderByDescending(p => p.OpponentVariety)
         .ThenByDescending(p => p.OpponentOptions.Count)
         .ToList();

      var onePlusGamePlayers = _Players
         .Where(p => p.GamesPerWeek - p.PlannedGamesCount >= 1)
         .OrderByDescending(p => p.OpponentVariety)
         .ThenByDescending(p => p.OpponentOptions.Count)
         .ToList();

      // Players at the beginning of each list will be pushed into the stack first
      // and will be considered for matching last, resulting in relatively more
      // opponent options considered for these players during match iterations.

      // Player's opponent options are sorted by last played date, with oldest
      // opponents being preferred (at the beginning of the list). Considering
      // more opponent options results in increased likelihood of matching against
      // a relatively recent opponent.

      // Therefore players at the bottom of the match stack will more likely
      // be paired against a relatively recent opponent.

      // Players requesting multiple games will appear in this list multiple times but at different locations!
      var fullList = new List<Player>();
      fullList.AddRange(fiveGamePlayers);
      fullList.AddRange(fourGamePlayers);
      fullList.AddRange(threeGamePlayers);
      fullList.AddRange(twoPlusGamePlayers);
      fullList.AddRange(onePlusGamePlayers);

      if (_Random)
      {
         Out.Write(ConsoleColor.Magenta, $"\nRandomizing the match making stack!\n");

         var random = new Random();
         fullList.Sort((a, b) => random.NextDouble().CompareTo(random.NextDouble()));

         foreach(var p in fullList)
         {
            p.OpponentOptions.Sort((a, b) => random.NextDouble().CompareTo(random.NextDouble()));
         }
      }

      foreach (var player in fullList)
      {
         _MatchStack.Push(player);
      }

      Out.Write($"Prepared stack of {_MatchStack.Count} players ({twoPlusGamePlayers.Count + threeGamePlayers.Count} players have multiple entries) for matching {_GameRequestsCount} planned games");
      Out.Write($"{_PlannedGames.Count} games have already been planned with match override\n");
   }

   private bool TryMatchNextPlayer(Player? player)
   {
      if (player == null)
      {
         // Bottom of stack has been reached
         // All players have been matched, proceeed to schedule the planned games
         Log.Matching($"Iteration {_IterationCount} (total iterations {_IterationCountTotal}) - all players are matched");
         
         var scheduleSuccess = _ScheduleGamesDelegate.Invoke(_PlannedGameSetNumber, _PlannedGames, "1");

         Log.Matching($"Last planned game set generation required {_IterationCount} iterations ({_IterationCountTotal} iterations total)");
 
         if (scheduleSuccess)
         {
            Out.Write($"Last planned game set generation required {_IterationCount} iterations ({_IterationCountTotal} iterations total)");
         }
         else
         {
            // Reset the iteration count for the next game set generation if scheduling was not successful
            _IterationCount = 1;
            _PlannedGameSetNumber++;
         }

         return scheduleSuccess;
      }

      _CurrentGameIndex++;
      Log.Matching($"Iteration {_IterationCount} game index {_CurrentGameIndex} for {player.Name}");

      if (_CurrentGameIndex > _MaximumGameIndexReached)
      {
         _MaximumGameIndexReached = _CurrentGameIndex;
         Log.Matching($"Iteration {_IterationCount} reached new maximum game index {_MaximumGameIndexReached}");
      }

      var success = false;
      if (player.PlannedGamesCount < player.GamesPerWeek)
      {
         foreach (var opponent in player.OpponentOptions)
         {
            Log.Matching("Considering {0} as a opponent for {1}", opponent.Name, player.Name);
            if (opponent.PlannedGamesCount >= opponent.GamesPerWeek)
            {
               Log.Matching("{0} already has {1} games planned, skip to the next opponent", opponent.Name, opponent.PlannedGamesCount);
               continue;
            }

            if (_PlannedGames.Where(g => g.Involves(player, opponent)).Any())
            {
               Log.Matching("{0} and {1} already have a game planned, skip to the next opponent", player.Name, opponent.Name);
               continue;
            }

            var game = new Game(player, opponent, ColorAssignment.Fair);

            if (Scheduler.IsMatchFeasible(game, _PlannedGames))
            {
               _PlannedGames.Add(game);
               player.PlannedGamesCount++;
               opponent.PlannedGamesCount++;
               Log.Matching("Matched {0} with {1}", player.Name, opponent.Name);

               var nextPlayer = _MatchStack.Count > 0 ? _MatchStack.Pop() : null;
               // Count new iteration every time we start moving forward after
               // a failed iteration resulted in one or more steps back
               if (_IterationFailed)
               {
                  _IterationCount++;
                  _IterationCountTotal++;
                  _IterationFailed = false;
                  StatusLine.UpdateMatchingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
               }
               success = TryMatchNextPlayer(nextPlayer);
               _IterationFailed = !success;

               if (success)
               {
                  break;
               }
               else
               {
                  if (nextPlayer == null)
                  {
                     throw new ArgumentNullException("By design, nextPlayer is not expected to be null here");
                  }

                  Log.Matching($"Iteration {_IterationCount} push next player {nextPlayer.Name} back to the stack");
                  _MatchStack.Push(nextPlayer);

                  _PlannedGames.Remove(game);
                  player.PlannedGamesCount--;
                  opponent.PlannedGamesCount--;
                  continue;
               }
            }
            else
            {
               continue;
            }
         }
      }
      else
      {
         // This can and will happen: when we match a player with an opponent, the opponent
         // remains in the match stack. Removing the opponent is not practical since we may
         // need to backtrack, requiring us to replace the opponent back in its original position.
         // It is easier to skip the players that no longer need a game planned.

         Log.Matching("{0} already has {1} game(s) planned, skip to the next player", player.Name, player.PlannedGamesCount);

         var nextPlayer = _MatchStack.Count > 0 ? _MatchStack.Pop() : null;
         if (_IterationFailed && nextPlayer != null)
         {
            _IterationCount++;
            _IterationCountTotal++;
            _IterationFailed = false;
            StatusLine.UpdateMatchingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
         }
         success = TryMatchNextPlayer(nextPlayer);
         _IterationFailed = !success;

         if (!success)
         {
            // nextPlayer may be null here if we reached the bottom of the matching stack
            // and executing TryMatchNextPlayer(null) resulted in an attempt to schedule
            // but the schedule generation failed
            if (nextPlayer != null)
            {
               Log.Matching($"Iteration {_IterationCount} push next player {nextPlayer.Name} back to the stack");
               _MatchStack.Push(nextPlayer);
            }
         }
      }

      if (success)
      {
         Log.Matching($"Iteration {_IterationCount} game index {_CurrentGameIndex} successfully matched {player.Name}");
      }
      else
      {
         Log.Matching($"Iteration {_IterationCount} game index {_CurrentGameIndex} failed to match {player.Name}");
      }

      _CurrentGameIndex--;
      return success;
   }
}
