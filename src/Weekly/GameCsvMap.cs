using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Ant.Weekly;

public class GameCsvMap : ClassMap<GameCsv>
{
   public GameCsvMap()
   {
      Map(m => m.Date).Name("Date");
      Map(m => m.WhiteName).Name("White Name");
      Map(m => m.BlackName).Name("Black Name");
      Map(m => m.Result).Name("Result");
      Map(m => m.TimeControl).Name("Time Control").Optional();
      Map(m => m.Site).Name("Site").Optional();
      Map(m => m.Event).Name("Event").Optional();
      Map(m => m.Section).Name("Section").Optional();
      Map(m => m.Stage).Name("Stage").Optional();
      Map(m => m.Round).Name("Round").TypeConverter<IntConverter>().Optional();
   }
}

public class PlannedGameCsvMap : ClassMap<Game>
{
   public PlannedGameCsvMap()
   {
      Map(m => m.Date).Name("Date").TypeConverter<DateConverter>();
      Map(m => m.WhitePlayer.Name).Name("White Name");
      Map(m => m.BlackPlayer.Name).Name("Black Name");
      Map(m => m.Event).Name("Event");
      Map(m => m.LastPlayedWeeksAgoString).Name("Last Played Wks Ago");
   }
}

public class PlayerExportGameCsvMap : ClassMap<GameCsv>
{
   public PlayerExportGameCsvMap()
   {
      Map(m => m.Date).Name("Date").TypeConverter<DateConverter>();
      Map(m => m.WhiteName).Name("White Name");
      Map(m => m.WhiteRating).Name("White Rating");
      Map(m => m.BlackName).Name("Black Name");
      Map(m => m.BlackRating).Name("Black Rating");
      Map(m => m.Result).Name("Result");

      Map(m => m.TimeControl).Name("Time Control");
      Map(m => m.Site).Name("Site");
      Map(m => m.Event).Name("Event");
      Map(m => m.Section).Name("Section");
      Map(m => m.Stage).Name("Stage");
      Map(m => m.Round).Name("Round").TypeConverter<IntConverter>();

      Map(m => m.WhiteNewRating).Name("White New Rating");
      Map(m => m.WhitePerf).Name("White Perf");
      Map(m => m.WhitePerfAvgOpp).Name("White Perf Avg Opp");
      
      Map(m => m.BlackNewRating).Name("Black New Rating");
      Map(m => m.BlackPerf).Name("Black Perf");
      Map(m => m.BlackPerfAvgOpp).Name("Black Perf Avg Opp");
      
      Map(m => m.PlayerName).Name("Player Name");
      Map(m => m.OpponentName).Name("Opponent Name");
      Map(m => m.PlayerColor).Name("Player Color");
      Map(m => m.PlayerResult).Name("Player Result");

      Map(m => m.PlayerElo).Name("Player Elo");
      Map(m => m.PlayerGlicko).Name("Player Glicko");
      Map(m => m.PlayerDeviation).Name("Player Deviation");
      Map(m => m.PlayerClubRating).Name("Player Club Rating");
      Map(m => m.PlayerClubRatingAvgOpp).Name("Player Club Rating Avg Opp");
      Map(m => m.PlayerEloPerf).Name("Player Elo Perf");
      Map(m => m.PlayerEloPerfAvgOpp).Name("Player Elo Perf Avg Opp");
      Map(m => m.PlayerGlickoPerf).Name("Player Glicko Perf");
      Map(m => m.PlayerGlickoPerfAvgOpp).Name("Player Glicko Perf Avg Opp");
      Map(m => m.PlayerClubPerf).Name("Player Club Perf");
      Map(m => m.PlayerClubPerfAvgOpp).Name("Player Club Perf Avg Opp");

   }
}

public class DateConverter : DefaultTypeConverter
{
   public override string ConvertToString(object? data, IWriterRow row, MemberMapData memberMapData)
   {
      if (data is DateOnly date)
      {
         return date.ToString("yyyy-MM-dd");
      }
      else
      {
         return string.Empty;
      }
   }
}

public class IntConverter : DefaultTypeConverter
{
   public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
   {
      if (text == null || text == string.Empty || text == " ")
      {
         return 0;
      }
      else
      {
         try
         {
            return int.Parse(text);
         }
         catch
         {
            return 0;
         }
      }
   }

   public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
   {
      int myValue;
      try
      {
         myValue = (int)(value ?? 0);
         if (myValue == 0)
         {
            return string.Empty;
         }
         else
         {
            return myValue.ToString();
         }
      }
      catch
      {
         return string.Empty;
      }
   }
}