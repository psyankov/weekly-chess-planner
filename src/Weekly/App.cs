using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;

namespace Ant.Weekly;

public class App
{
   public static Settings S { get; private set; } = new();

   static void Main(string[] args)
   {
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.ForegroundColor = ConsoleColor.Gray;

      var assembly = Assembly.GetExecutingAssembly();
      var assemblyVersion = assembly.GetName().Version;
      var productVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

      Out.Write("\nWeekly chess games plannning\n");
      Out.Write($"Application version:  {productVersion}");
      Out.Write($"Start time:           {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
      Out.Write("Run 'wk.exe --help' for command line options.\n");

      ApplyAppSettings();

      var rootCmd = new RootCommand("Weekly chess games plannning");
      rootCmd.SetHandler(() => CmdProcessor.AppInit());
      ConfigureCommands(rootCmd);
      
      try
      {
         rootCmd.Invoke(args);
      }
      catch
      {
         Out.Write($"\nPress any key to close this window");
         Console.ReadKey();
      }
   }

   private static void ConfigureCommands(Command rootCmd)
   {
      // *** Show ***

      var showCmd = new Command("show", "Show player's full info");
      rootCmd.AddCommand(showCmd);
      var playerInfoArg = new Argument<string>("name");
      var verboseOption = new Option<bool>("--verbose", "Verbose output");
      verboseOption.AddAlias("-v");
      playerInfoArg.Arity = ArgumentArity.ExactlyOne;
      showCmd.AddArgument(playerInfoArg);
      showCmd.AddOption(verboseOption);
      showCmd.SetHandler(CmdProcessor.ShowFullPlayerInfo, playerInfoArg, verboseOption);

      // *** List ***

      var listCmd = new Command("list", "List players or games");
      rootCmd.AddCommand(listCmd);

      // *** List Players ***

      var allOption = new Option<bool>("--all", "List all players");
      allOption.AddAlias("-a");

      var inactiveOption = new Option<bool>("--inactive", "List inactive players only");
      inactiveOption.AddAlias("-i");

      var sortByNameOption = new Option<bool>("--name", "Sort by name");
      sortByNameOption.AddAlias("-n");

      var listPlayersCmd = new Command("players", "List players sorted by rating");
      listPlayersCmd.AddAlias("p");
      listPlayersCmd.AddOption(allOption);
      listPlayersCmd.AddOption(inactiveOption);
      listPlayersCmd.AddOption(sortByNameOption);
      listPlayersCmd.SetHandler(CmdProcessor.ListPlayers, allOption, inactiveOption, sortByNameOption);
      listCmd.AddCommand(listPlayersCmd);

      // *** List Games ***

      var fromDateOption = new Option<DateOnly>("--from", () => DateOnly.MinValue, "From date");
      fromDateOption.AddAlias("-f");

      var toDateOption = new Option<DateOnly>("--to", () => DateOnly.MaxValue, "To date");
      toDateOption.AddAlias("-t");

      var playerOption = new Option<string>("--player", "Player name or nik");
      playerOption.AddAlias("-p");

      var listGamesCmd = new Command("games", "List games");
      listGamesCmd.AddAlias("g");
      listGamesCmd.AddOption(fromDateOption);
      listGamesCmd.AddOption(toDateOption);
      listGamesCmd.AddOption(playerOption);
      listGamesCmd.SetHandler(CmdProcessor.ListGames, fromDateOption, toDateOption, playerOption);
      listCmd.AddCommand(listGamesCmd);

      // *** Export ***

      var exportCmd = new Command("export", "Export data");
      rootCmd.AddCommand(exportCmd);

      var exportPlayerData = new Command("player", "Export player data");
      exportPlayerData.AddAlias("p");
      exportPlayerData.AddArgument(playerInfoArg);
      exportCmd.AddCommand(exportPlayerData);
      exportPlayerData.SetHandler(CmdProcessor.ExportPlayer, playerInfoArg);

      // *** Plan ***

      var version1Option = new Option<bool>("--version1", "Use match maker version 1 (iterate over opponents prioritized by opponent varity)");
      version1Option.AddAlias("-v1");

      var version2Option = new Option<bool>("--version2", "Use match maker version 2 (iterate over matches prioritized by opponent variery)");
      version2Option.AddAlias("-v2");

      var randomOption = new Option<bool>("--random", "Match maker will iterate over a randomized list");
      randomOption.AddAlias("-r");

      var currentWeekOption = new Option<bool>("--current", "Plan for the current week");
      currentWeekOption.AddAlias("-c");

      var planCmd = new Command("plan", "Plan games for next week (starting on Monday)");
      planCmd.AddOption(version1Option);
      planCmd.AddOption(version2Option);
      planCmd.AddOption(randomOption);
      planCmd.AddOption(currentWeekOption);
      planCmd.SetHandler(CmdProcessor.Plan, version1Option, version2Option, randomOption, currentWeekOption);
      rootCmd.AddCommand(planCmd);
   }

   public static void ApplyAppSettings()
   {
      // Build a config object, using env vars and JSON providers.
      IConfigurationRoot config = new ConfigurationBuilder()
          .AddJsonFile(@"AppSettings.json", optional: true)
          .Build();

      // Get values from the config given their key and their target type.
      Settings? settings;
      try
      {
         settings = config.GetRequiredSection("Settings").Get<Settings>();
      }
      catch
      {
         settings = null;
      }

      if (settings != null)
      {
         S = settings;
      }
      else
      {
         Out.Error("Settings file is missing or has invalid configuration, using the hard coded default values.");
      }

      Log.Configure(S.Logging);
      Log.ApplicationConfiguration(S);
   }
}
