namespace Ant.Weekly;

public class Planner
{
   private Data _Data;
   private List<Player> _Players;
   private List<MatchCsv> _PreDefinedMatchList;

   private DateOnly _StartDate;
   private int _BoardCount = App.S.Planner.DefaultBoardCount;

   public Planner(Data data, List<MatchCsv> preDefinedMatchList, DateOnly startDate)
   {
      _Data = data;
      _Players = data.Players.All.Where(p => p.PlayingThisWeek).ToList();
      _PreDefinedMatchList = preDefinedMatchList;
      _StartDate = startDate;
   }

   public List<Game> PlannedGames { get; } = new();

   private int _GameRequestsCount;

   public void PlanWeek(bool matchMakerVersion1, bool matchMakerVersion2, bool random)
   {
      Out.Write($"\nPlanning games for the week of {_StartDate:dddd yyyy.MM.dd}");

      CheckSufficientAvailabilityOfPlayers();
      // Check sufficient availability first - number of player game requests
      // may be adjusted if they don't have sufficient availability
      ReviewGameRequests();

      ApplyPreDefinedMatchList();

      IdentifyOpponentOptions();
      RemoveRecentOpponents();
      CheckFeasibilityOfRemainingGameRequests();
      ListOpponentOptions();

      var scheduler = new Scheduler(_StartDate, _BoardCount);

      if (_GameRequestsCount == 0)
      {
         Out.Write($"Proceeding directly to schedule {PlannedGames.Count} planned games\n");
         var success = scheduler.TryScheduleGames(1, PlannedGames);

         if (!success)
         {
            Out.Write("\n");
            Out.Error("Failed to schedule planned games");
         }
         else
         {
            Out.Write($"Scheduled {PlannedGames.Count} planned games");
         }
      }
      else
      {
         Console.WriteLine();
         Console.WriteLine(); // new blank line to accept updating status line with iteration counter
         if (matchMakerVersion1)
         {
            var matchMaker = new MatchMakerV1(_Players, PlannedGames, _GameRequestsCount / 2, random, scheduler.TryScheduleGames);
            matchMaker.MatchAndSchedule();
         }
         else
         {
            var matchMaker = new MatchMakerV2(_Players, PlannedGames, _GameRequestsCount / 2, random, scheduler.TryScheduleGames);
            matchMaker.MatchAndSchedule();
         }
      }
   }

   private void CheckSufficientAvailabilityOfPlayers()
   {
      foreach (var player in _Players)
      {
         var availableDays = player.Availability.Days.Count(d => d.Value);
         if (availableDays < player.GamesPerWeek)
         {
            Out.Warning($"{player} requires {player.GamesPerWeek} game(s) but has {availableDays} available day(s) - adjusting the number of player's games.");
            player.GamesPerWeek = availableDays;
         }
      }
   }

   private void ReviewGameRequests()
   {
      var excessiveRequestPlayersCount = _Players.Where(p => p.GamesPerWeek > App.S.Planner.MaximumGamesPerWeek).Count();

      if (excessiveRequestPlayersCount != 0)
      {
         foreach (var player in _Players.Where(p => p.GamesPerWeek > App.S.Planner.MaximumGamesPerWeek))
         {
            Out.Warning("{0} required {1} games - adjusting to {2}!", player.Name, player.GamesPerWeek, App.S.Planner.MaximumGamesPerWeek);
            player.GamesPerWeek = App.S.Planner.MaximumGamesPerWeek;
         }
      }

      var activePlayersCount = _Players.Count;
      var oneGamePlayersCount = _Players.Where(p => p.GamesPerWeek == 1).Count();
      var twoGamePlayersCount = _Players.Where(p => p.GamesPerWeek == 2).Count();
      var threeGamePlayersCount = _Players.Where(p => p.GamesPerWeek == 3).Count();
      var fourGamePlayersCount = _Players.Where(p => p.GamesPerWeek == 4).Count();
      var fiveGamePlayersCount = _Players.Where(p => p.GamesPerWeek == 5).Count();
      var moreThanFiveGamePlayersCount = _Players.Where(p => p.GamesPerWeek > 5).Count();


      var totalGameRequests = oneGamePlayersCount + 2 * twoGamePlayersCount + 3 * threeGamePlayersCount + 4 * fourGamePlayersCount + 5 * fiveGamePlayersCount;

      if (totalGameRequests % 2 == 1)
      {
         Out.Warning("Odd number ({0}) of game requests", totalGameRequests);
         var players = _Players.Where(p => p.GamesPerWeek > 1);
         if (players.Count() > 0)
         {
            var player = players
               .OrderByDescending(p => p.GamesPerWeek)
               .First();
            player.GamesPerWeek--;
            totalGameRequests--;
            if (player.GamesPerWeek == 4)
            {
               fiveGamePlayersCount--;
               fourGamePlayersCount++;
            }
            if (player.GamesPerWeek == 3)
            {
               fourGamePlayersCount--;
               threeGamePlayersCount++;
            }
            if (player.GamesPerWeek == 2)
            {
               threeGamePlayersCount--;
               twoGamePlayersCount++;
            }
            if (player.GamesPerWeek == 1)
            {
               totalGameRequests--;
               twoGamePlayersCount--;
               oneGamePlayersCount++;
            }
            Out.Warning("Number of games has been reduced to {0} for {1}", player.GamesPerWeek, player.Name);
         }
         else
         {
            throw new InvalidOperationException("Odd number of game requests but all players require only 1 game each");
         }
      }

      _GameRequestsCount = totalGameRequests;
      var expectedGamesCount = _GameRequestsCount / 2;

      Out.Write("\nTotal active player count = {0}", activePlayersCount);
      Out.Write("   1 game  for {0} players", oneGamePlayersCount);
      Out.Write("   2 games for {0} players", twoGamePlayersCount);
      Out.Write("   3 games for {0} players", threeGamePlayersCount);
      Out.Write("   4 games for {0} players", fourGamePlayersCount);
      Out.Write("   5 games for {0} players", fiveGamePlayersCount);
      Out.Write("{0} game requests in total", _GameRequestsCount);
      Out.Write("\nPlanning {0} games", expectedGamesCount);

      var daysCount = 0;
      var weekDays = new List<DayOfWeek>()
      {
         DayOfWeek.Monday,
         DayOfWeek.Tuesday,
         DayOfWeek.Wednesday,
         DayOfWeek.Thursday,
         DayOfWeek.Friday
      };

      foreach (var day in weekDays)
      {
         var activeDay = _Players.Any(p => p.Availability.Days[day]);
         daysCount = activeDay ? daysCount + 1 : daysCount;
      }
      Out.Write($"This week has {daysCount} days open for planning");

      _BoardCount = expectedGamesCount / daysCount + (expectedGamesCount % daysCount == 0 ? 0 : 1);
      Out.Write($"Minimum {_BoardCount} boards are required\n");

      if (_BoardCount > App.S.Planner.MaximumBoardCount)
      {
         var msg = $"Required number of boards exceeds the maximum available number ({App.S.Planner.MaximumBoardCount})";
         Out.Error(msg);
         Out.Write("\n\n");
         throw new InvalidOperationException(msg);
      }
   }

   private void IdentifyOpponentOptions()
   {
      foreach (var player in _Players)
      {
         var opponents = _Players
            // Uncomment to stop players without an established Elo from playing each other
            // This however will reduced the number of potential opponents for players without an established Elo
            // .Where(o => o.Rating.EloEstablished || player.Rating.EloEstablished)
            .Where(o => o != player && o.Availability.Overlap(player.Availability))
            .Where(o => o.Section <= player.SectionLimit && player.Section <= o.SectionLimit)
            .OrderBy(o => o.LastDatePlayedAgainst(player))
            .ToList();
         Log.Planning($"{player} has {opponents.Count} opponent options");
         player.OpponentOptions.AddRange(opponents);
      }
   }

   private void RemoveRecentOpponents()
   {
      foreach (var player in _Players)
      {
         var recentOpponents = new HashSet<Player>();
         foreach (var game in player.Games.OrderByDescending(g => g.Date))
         {
            var recentOpponent = game.Opponent(player);
            recentOpponents.Add(recentOpponent);

            if (player.OpponentOptions.Contains(recentOpponent))
            {
               if (player.OpponentOptions.Count > App.S.Planner.KeepMinimumPotentialOpponents
                  && recentOpponent.OpponentOptions.Count > App.S.Planner.KeepMinimumPotentialOpponents)
               {
                  player.OpponentOptions.Remove(recentOpponent);
                  recentOpponent.OpponentOptions.Remove(player);

                  Log.Planning($"Removed {player.Name} and {recentOpponent.Name} from their respective potential opponent lists due to the game played on {game.Date:yyyy.MM.dd}");
               }
               else
               {
                  Log.Planning($"Kept {player.Name} and {recentOpponent.Name} on their respective potential opponent lists to satisfy the requirement for the minimum opponent count (last game was played on {game.Date:yyyy.MM.dd})");
               }
            }

            if (recentOpponents.Count == App.S.Planner.RejectRecentOpponents)
            {
               break;
            }
            if (player.OpponentOptions.Count == App.S.Planner.KeepMinimumPotentialOpponents)
            {
               break;
            }
         }
         Log.Planning($"{player} has {player.OpponentOptions.Count} potential opponents remaining");
      }
   }

   private void ListOpponentOptions()
   {     
      if (_Players.Count == 1)
      {
         Out.Error($"Something is wrong - only one player is remaining to match.");
         Out.Write("\n\n");
         throw new InvalidOperationException();
      }

      if (_Players.Count > 0)
      {
         Out.Write($"Name            Available  G/Wk  Remaining  Opp.Count  Variety\n");
         foreach (var player in _Players.OrderBy(p => p.Name))
         {
            Out.Write($"{player.Name,-20}{player.Availability,5}{player.GamesPerWeek,6}{player.GamesPerWeek - player.PlannedGamesCount,11}{player.OpponentOptions.Count,11}{player.OpponentVariety,9:f1}");
         }
         Out.Write();

         foreach (var player in _Players)
         {
            Log.Planning($"{player} has {player.OpponentOptions.Count} potential opponents");

            foreach (var opponent in player.OpponentOptions)
            {
               Log.Planning("   {0,-50} {1}", opponent, player.LastDatePlayedAgainst(opponent).ToAppString());
            }
         }
      }
   }

   public void ApplyPreDefinedMatchList()
   {
      foreach (var match in _PreDefinedMatchList)
      {
         Player? playerA, playerB;
         try
         {
            playerA = _Data.Players.ByName[match.White];
         }
         catch (KeyNotFoundException)
         {
            playerA = null;
         }
         if (playerA == null)
         {
            Out.Error($"Pre-defined match list contains unknown player '{match.White}' vs '{match.Black}' - the match is ignored");
            continue;
         }

         try
         {
            playerB = _Data.Players.ByName[match.Black];
         }
         catch (KeyNotFoundException)
         {
            playerB = null;
         }
         if (playerB == null)
         {
            Out.Error($"Pre-defined match list contains unknown player '{match.Black}' vs '{match.White}' - the match is ignored");
            continue;
         }

         if (playerA == playerB)
         {
            Out.Error("{0} cannot play against himself - match is ignored!", playerA.Name);
            continue;
         }

         var msg = $"Found pre-defined match {match.White} vs {match.Black} for '{match.Event}' event with automatic color assignment {match.AutoColor}";
         Log.Planning(msg);

         if (!playerA.Availability.Overlap(playerB.Availability))
         {
            Out.Error($"{playerA.Name} and {playerB.Name} do not have a mutually available day - match is ignored");
            continue;
         }

         if (playerA.PlannedGamesCount == playerA.GamesPerWeek)
         {
            Out.Error($"{playerA.Name} already reached their target number of {playerA.GamesPerWeek} games per week - match is ignored");
            continue;
         }

         if (playerB.PlannedGamesCount == playerB.GamesPerWeek)
         {
            Out.Error($"{playerB.Name} already reached their target number of {playerB.GamesPerWeek} games per week - match is ignored");
            continue;
         }

         Log.Planning($"Adding a planned game between {playerA.Name} and {playerB.Name}");
         var game = new Game(playerA, playerB, match.AutoColor ? ColorAssignment.Fair : ColorAssignment.Requested);
         game.Event = match.Event;
         PlannedGames.Add(game);

         playerA.PlannedGamesCount++;
         playerB.PlannedGamesCount++;
         _GameRequestsCount -= 2;

         playerA.OpponentOptions.Remove(playerB);
         playerB.OpponentOptions.Remove(playerA);

         if (playerA.PlannedGamesCount == playerA.GamesPerWeek)
         {
            Log.Planning($"{playerA.Name} with a pre-defined match now has {playerA.PlannedGamesCount} planned games - removing from the list of players to be matched and from the opponent lists of all other players");
            _Players.Remove(playerA);
            _Players
               .Where(p => p.OpponentOptions.Contains(playerA))
               .ToList()
               .ForEach(p => p.OpponentOptions.Remove(playerA));
         }

         if (playerB.PlannedGamesCount == playerB.GamesPerWeek)
         {
            Log.Planning($"{playerB.Name} with a pre-defined match now has {playerB.PlannedGamesCount} planned games - removing from the list of players to be matched and from the opponent lists of all other players");
            _Players.Remove(playerB);
            _Players
               .Where(p => p.OpponentOptions.Contains(playerB))
               .ToList()
               .ForEach(p => p.OpponentOptions.Remove(playerB));
         }
      }

      if (PlannedGames.Count > 0)
      {
         Out.Write($"Planned {PlannedGames.Count} of {_PreDefinedMatchList.Count} pre-defined matches:\n");
         foreach (var game in PlannedGames)
         {
            Out.Write($"{game.WhitePlayer,-30} - {game.BlackPlayer,-30}   {game.Event}");
         }
         Out.Write($"\n{_GameRequestsCount} game requests for {_Players.Count} players are remaining to match\n");
      }
   }

   private void CheckFeasibilityOfRemainingGameRequests()
   {
      var playersToRemove = new List<Player>();

      foreach (var player in _Players)
      {
         var remainingRequests = player.GamesPerWeek - player.PlannedGamesCount;
         if (player.OpponentOptions.Count < remainingRequests)
         {
            var countImpossible = remainingRequests - player.OpponentOptions.Count;
            player.GamesPerWeek -= countImpossible;
            _GameRequestsCount -= countImpossible;
            if (player.OpponentOptions.Count == 0)
            {
               playersToRemove.Add(player);
               Out.Error($"{player.Name} has no potential opponents left and will not be matched");
            }
            else
            {
               Out.Warning($"{player.Name} has {player.OpponentOptions.Count} potential opponents for {remainingRequests} remaining game requests");
               Out.Warning($"{countImpossible} game requests cannot be satisfied - reducing number of requests");
            }
         }
      }

      if (playersToRemove.Count > 0)
      {
         foreach (var player in playersToRemove)
         {
            _Players.Remove(player);
         }
      }
   }
}
