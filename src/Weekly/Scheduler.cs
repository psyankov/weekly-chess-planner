namespace Ant.Weekly;
public class Scheduler
{
   private DateOnly _StartDate;
   private int _BoardCount;
   private string _MatchMakerVersion = string.Empty;

   public Scheduler(DateOnly startDate, int boardCount)
   {
      _StartDate = startDate;
      _BoardCount = boardCount;

      _IterationCountTotal = 1;
      _MaximumGameIndexReachedAccrossAllSets = 0;
   }

   private Dictionary<DayOfWeek, int> _DayGamesCount = new()
   {
      { DayOfWeek.Monday, 0 },
      { DayOfWeek.Tuesday, 0 },
      { DayOfWeek.Wednesday, 0 },
      { DayOfWeek.Thursday, 0 },
      { DayOfWeek.Friday, 0 }
   };

   private long _PlannedGameSetNumber;

   private long _CurrentGameIndex;
   private long _MaximumGameIndexReached;
   private long _MaximumGameIndexReachedAccrossAllSets;

   private bool _IterationFailed;
   private long _IterationCount;
   private long _IterationCountTotal;

   private List<Game> _PlannedGames = new();
   private Stack<Game> _ScheduleStack = new();

   public bool TryScheduleGames(long plannedGameSetNumber, List<Game> games, string matchMakerVersion = "")
   {
      _PlannedGames = games;
      _MatchMakerVersion = matchMakerVersion;

      _IterationFailed = false;
      _IterationCount = 1;
      _PlannedGameSetNumber = plannedGameSetNumber;
      StatusLine.UpdateSchedulingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);

      Log.Scheduling($"Begin to schedule {_PlannedGames.Count} planned games, set {_PlannedGameSetNumber}");
      
      _ScheduleStack.Clear();
      // Games with the most options are placed at the bottom of the stack to be scheduled last
      Log.Scheduling(Game.MatchTableHeaderStringBuilder().ToString());
      foreach (var game in _PlannedGames.OrderByDescending(g => g.DayOptions.Count()))
      {
         // to support calculation of last played number of weeks ago
         game.Date = DayOfWeek.Sunday.DateFromWeekOf(_StartDate);
         game.UpdateLastPlayed();
         _ScheduleStack.Push(game);
         Log.Scheduling(game.MatchTableString().ToString());
      }

      while (_BoardCount <= App.S.Planner.MaximumBoardCount)
      {
         Log.Scheduling($"Attempt to schedule with {_BoardCount} boards");

         _CurrentGameIndex = 0;
         _MaximumGameIndexReached = 0;

         var nextGame = _ScheduleStack.Pop();
         // Count new iteration every time we start moving forward after
         // a failed iteration resulted in one or more steps back
         if (_IterationFailed)
         {
            _IterationCount++;
            _IterationCountTotal++;
            _IterationFailed = false;
            StatusLine.UpdateSchedulingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
         }
         var success = TryScheduleNextGame(nextGame);
         _IterationFailed = !success;

         if (success)
         {
            OnSuccess();
            return true;
         }
         else
         {
            Log.Scheduling($"Failed to schedule (partial success for at most {_MaximumGameIndexReached} games of {_PlannedGames.Count})");
            _ScheduleStack.Push(nextGame);
            _BoardCount++;
            continue;
         }
      }
      Log.Scheduling($"Failed to schedule planned games set {_PlannedGameSetNumber} over {_IterationCount} iterations ({_IterationCountTotal} total iterations)");
      return false;
   }

   public void OnSuccess()
   {
      Out.Write();
      Out.Write();
      Out.Write($"Considered {_PlannedGameSetNumber} planned game set(s) over the total of {_IterationCountTotal} iterations");
      Out.Write($"Successfully scheduled {_PlannedGames.Count} games in the last planned set over {_IterationCount} iterations");

      var errors = ValidateSchedule();
      if (errors > 0)
      {
         Out.Write();
         Out.Error("There are {0} errors in the schedule", errors);
         Out.Write();
         throw new InvalidOperationException("Schedule validation failed");
      }

      Out.Write($"\n{_PlannedGames.Count} games are scheduled for the week of {_StartDate:MMMM d, yyyy}\n");

      _PlannedGames.Sort((a, b) => a.Date.CompareTo(b.Date));
      Out.Write(Game.ScheduleTableHeaderStringBuilder().ToString());
      Out.Write();
      foreach (var game in _PlannedGames)
      {
         game.UpdateLastPlayed();
         Out.Write(game.ScheduleTableStringBuilder().ToString());
      }

      Games.ExportPlannedGames(_PlannedGames, final: true);

      Out.Write($"\nFinish time:           {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
   }

   private bool TryScheduleNextGame(Game? game)
   {
      if (game == null)
      {
         // End of stack has been reached
         Log.Scheduling($"All games have been scheduled");
         return true;
      }

      _CurrentGameIndex++;

      Log.Scheduling($"Set {_PlannedGameSetNumber} iteration {_IterationCount} game {_CurrentGameIndex}");

      if (_CurrentGameIndex > _MaximumGameIndexReached)
      {
         _MaximumGameIndexReached = _CurrentGameIndex;
         Log.Scheduling($"Set {_PlannedGameSetNumber} - reached new maximum game index {_MaximumGameIndexReached}");
         if (_MaximumGameIndexReached > _MaximumGameIndexReachedAccrossAllSets)
         {
            _MaximumGameIndexReachedAccrossAllSets = _MaximumGameIndexReached;
            // Export the games successfully schedule so far. Note that _CurrentGameIndex is not scheduled yet!
            Games.ExportPlannedGames(_PlannedGames, final: false);
         }
      }

      var success = false;
      // randomize the order in which day options are tried to have a more even distribution of games through the week
      var random = new Random();
      var gameDayOptions = game.DayOptions.ToList();
      gameDayOptions.Sort((a, b) => random.NextDouble().CompareTo(random.NextDouble()));

      foreach (var day in gameDayOptions)
      {
         if (!(game.WhitePlayer.PlannedDays[day] || game.BlackPlayer.PlannedDays[day]))
         {
            if (_DayGamesCount[day] >= _BoardCount) continue;

            _DayGamesCount[day]++;
            game.Date = day.DateFromWeekOf(_StartDate);
            game.WhitePlayer.PlannedDays[day] = true;
            game.BlackPlayer.PlannedDays[day] = true;

            var nextGame = _ScheduleStack.Count > 0 ? _ScheduleStack.Pop() : null;
            if (_IterationFailed && nextGame != null)
            {
               _IterationCount++;
               _IterationCountTotal++;
               _IterationFailed = false;
               StatusLine.UpdateSchedulingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
            }
            success = TryScheduleNextGame(nextGame);
            _IterationFailed = !success;

            if (success)
            {
               break;
            }
            else
            {
               if (nextGame == null) throw new ArgumentNullException("By design, nextGame is not expected to be null here");

               Log.Scheduling("Push {0} back to the stack", nextGame.MatchString());
               _ScheduleStack.Push(nextGame);

               _DayGamesCount[day]--;
               game.Date = DateOnly.MinValue;
               game.WhitePlayer.PlannedDays[day] = false;
               game.BlackPlayer.PlannedDays[day] = false;
               _IterationCount++;
               _IterationCountTotal++;
               StatusLine.UpdateSchedulingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
            }
         }
      }

      if (success)
      {
         Log.Scheduling($"Scheduled game {_CurrentGameIndex} ({game.MatchString()}) on {game.Date.DayOfWeek}");
      }
      else
      {
         _IterationCount++;
         _IterationCountTotal++;
         StatusLine.UpdateSchedulingIteration(_PlannedGameSetNumber, _IterationCount, _IterationCountTotal);
         Log.Scheduling($"Failed to schedule game {_CurrentGameIndex} ({game.MatchString()})");
      }

      _CurrentGameIndex--;
      return success;
   }

   public int ValidateSchedule()
   {
      Log.Info("Validating schedule for {0} games", _PlannedGames.Count);
      int errors = 0;
      List<Player> players = new List<Player>();
      foreach (var game in _PlannedGames)
      {
         var whitePlayer = game.WhitePlayer;
         var blackPlayer = game.BlackPlayer;

         if (!players.Contains(whitePlayer))
         {
            players.Add(whitePlayer);
            whitePlayer.ResetPlanningData();
         }
         if (!players.Contains(blackPlayer))
         {
            players.Add(blackPlayer);
            blackPlayer.ResetPlanningData();
         }

         whitePlayer.PlannedGamesCount++;
         blackPlayer.PlannedGamesCount++;

         if (whitePlayer.PlannedGamesCount > whitePlayer.GamesPerWeek)
         {
            errors++;
            Out.Error("Game {0} on {1:yyyy.MM.dd} results in the {2} planned games for {3} above maximum {4}", game.MatchString(), game.Date, whitePlayer.PlannedGamesCount, whitePlayer.GamesPerWeek);
         }
         if (blackPlayer.PlannedGamesCount > blackPlayer.GamesPerWeek)
         {
            errors++;
            Out.Error("Game {0} on {1:yyyy.MM.dd} results in the {2} planned games for {3} above maximum {4}", game.MatchString(), game.Date, blackPlayer.PlannedGamesCount, blackPlayer.GamesPerWeek);
         }

         if (game.Date < _StartDate || game.Date >= _StartDate.AddDays(7))
         {
            errors++;
            Out.Error("Game {0} is scheduled for {1} - outside of the target week", game.MatchString());
         }

         var day = game.Date.DayOfWeek;
         if (game.Day != day)
         {
            errors++;
            Out.Error("Game {0} on {1:yyyy.MM.dd} was scheduled for {2} but the date is {3}", game.MatchString(), game.Date, game.Day, day);
         }
         if (whitePlayer.PlannedDays[day])
         {
            errors++;
            Out.Error("{0} already has a game on {1} {2:yyyy.MM.dd}", whitePlayer.Name, day, game.Date);
         }
         if (blackPlayer.PlannedDays[day])
         {
            errors++;
            Out.Error("{0} already has a game on {1} {2:yyyy.MM.dd}", blackPlayer.Name, day, game.Date);
         }
         if (whitePlayer.Availability.Days[day] == false)
         {
            errors++;
            Out.Error("{0} is not available on {1} {2:yyyy.MM.dd}", whitePlayer.Name, day, game.Date);
         }
         if (blackPlayer.Availability.Days[day] == false)
         {
            errors++;
            Out.Error("{0} is not available on {1} {2:yyyy.MM.dd}", blackPlayer.Name, day, game.Date);
         }

         whitePlayer.PlannedDays[game.Date.DayOfWeek] = true;
         blackPlayer.PlannedDays[game.Date.DayOfWeek] = true;

         var matchCount = _PlannedGames.Where(g => g.Involves(whitePlayer, blackPlayer)).Count();

         if (matchCount > 1)
         {
            errors++;
            Out.Error("{0} and {1} are scheduled for {2} games", whitePlayer.Name, blackPlayer.Name, matchCount);
         }
      }

      return errors;
   }

   public static bool IsMatchFeasible(Game game, List<Game> plannedGames)
   {
      return IsMatchFeasible(new Match(game.WhitePlayer, game.BlackPlayer), plannedGames);
   }

   public static bool IsMatchFeasible(Match match, List<Game> plannedGames)
   {
      bool feasible = true;
      if (match.DayOptions.Count == 1)
      {
         Log.Matching($"Match {match} can only happen on {match.DayOptions[0]}");

         feasible = plannedGames
            .Where(g => g.DayOptions.Count == 1 && g.DayOptions[0] == match.DayOptions[0])
            .Count() < App.S.Planner.MaximumBoardCount;

         feasible = feasible && !plannedGames
               .Where(g => g.Involves(match.Player1) && g.DayOptions.Count == 1)
               .Any(g => g.DayOptions.Contains(match.DayOptions[0]));

         feasible = feasible && !plannedGames
            .Where(g => g.Involves(match.Player2) && g.DayOptions.Count == 1)
            .Any(g => g.DayOptions.Contains(match.DayOptions[0]));

         var feasibleString = feasible ? "feasible" : "not feasible";
         Log.Matching($"Match {match} scheduling is {feasibleString}");
      }

      return feasible;
   }
}
