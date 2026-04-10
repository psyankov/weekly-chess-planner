# Weekly Chess Planner

Weekly casual games planning and rating tracking command line tool for a small chess club.

## Introduction

This planning tool is working and has been used in a practical application over many months. Nevertheless, the code is not _clean_ or _production-ready_ in the professional sense. It serves the purpose though - with many caveats.

Basic use case is as follows. There is a small to medium size group of players wishing to play some casual games against each other at a pre-determined time daily (such as lunch hour). Different people may be available on different days of the week, which changes from week to week, and be interested in playing a different number of games per week. Each week it is necessary to prepare a schedule for the following week taking into account any availability changes and avoiding pairing recent opponents.

## How to use

The following input files need to be present in the working directory:

`players.csv` contains information on players' availability and the number of game requests.

`games.csv` contains the history of games played.

Example files are included in the `data` folder.

Group coordinator updates these files at the end of each week (using a text editor or a spreadsheet) before preparing next week's schedule.

The history of games is used to avoid pairing recent opponents and to calculate rating changes. Only those games that have a valid result record are taken into account.  Ratings have no impact on the pairing and scheduling process in the current version of the application.

Example use:

```
wk --help
wk plan
wk plan --help
```

Each time the application is executed, it reads `players.csv` and `games.csv`. The application never modifies these files.

Game planning is done for the immediate next week.

If planning is successful, `week.csv` is prepared (in the working directory) in addition to the command line output.

If the application runs for longer than a few minutes, it is unlikely that it will complete successfully. Use Ctrl+C to quit.

If planning fails, you can review `week_partial.csv` for the most complete schedule that was achieved.

Make sure to stretch the command line windows wide enough to accommodate the output tables without lines wrapping.

Configurable application settings are in AppSettings.json but the existing values should work well for most scenarios.

## Development Notes

### Scheduling Algorithm

After preparing a list of viable pairings, scheduling is essentially recursive, brute-force iteration. It works sufficiently well as long as there is enough overlap in availability between most players.

Each game is given a metric of how many weeks ago the two players last met in a match. Opponent variety experienced by a player is measured by the average number of "last played number of weeks ago" over the last N games (N = "OpponentVarietyBasedOnGamesCount"). For this average, the maximum "number of weeks ago" is capped at some value K (K="OpponentVarietyBasedOnMaxWeeks"). This ensures that a single game played against someone after a very long break does not inflate the variety metric (average will never exceed K since the max contributing value is capped by K). The algorithm favors pairings with higher "number of weeks ago" and attempts to increase the opponent variety.

Matching and scheduling iterations count is incremented every time we start moving FORWARD in the list of all possible options AFTER a failed iteration resulted in one or more STEPS BACK.

### Rating

A new player's Elo rating change is calculated using an increased K Value (and if their opponent has an established rating, their K value is proportionally decreased) to account for a greater uncertainty and to allow new player's rating to settle near its true value quicker.

Maximum Elo rating change resulting from winning a game against a much higher rated player is 100; winning or losing against an equally rated player is 50; winning against a much lower rated player approaches 0. This K value is higher than used by USCF and FIDE but makes sense for a group that consists mostly of evolving players.

Glicko ratings (chess.com uses similar method with custom tweaks) are calculated starting from the initial rating (chess.com rating can be taken as a reference if it is available) assigned to the player or from the default starting value. Provisional games concept does not apply to Glicko since it already has a build-in mechanism accounting for greater uncertainty of a new player’s results.

Club rating is calculated iteratively as true performance based on itself (club performance) using Elo-based expected results such that net change over N games is zero. First all players are assigned the same rating intial default rating. Games are evaluated one after another and, after each game, performance rating of both players is calculated based on at most N latest games and the expected results derived using the previous club performance rating of both players. The result is recorded as the new club performance rating.

Performance ratings are calculated as true performance based on Elo, Glicko and Club ratings and using Elo-based expected results such that net change over N games is zero (practically, less than 1.0). To deal with players who won or lost all N games (and to add some stability), a mock drawn game against a player with an equal rating is always added to the list of N actual games that the performance is calculated for.
