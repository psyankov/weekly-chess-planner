using CsvHelper.Configuration;

namespace Ant.Weekly;
public class PlayerCsvMap : ClassMap<PlayerCsv>
{
   public PlayerCsvMap()
   {
      Map(m => m.Name).Name("Name");

      Map(m => m.Section).Name("Section").Optional();
      Map(m => m.SectionLimit).Name("Section Limit").Optional();
      Map(m => m.Active).Name("Active").TypeConverter<BoolConverter>().Optional();
      Map(m => m.LeaguePlayer).Name("League").TypeConverter<BoolConverter>().Optional();

      Map(m => m.GamesPerWeek).Name("Games Per Week");

      Map(m => m.Monday).Name("Monday").TypeConverter<BoolConverter>();
      Map(m => m.Tuesday).Name("Tuesday").TypeConverter<BoolConverter>();
      Map(m => m.Wednesday).Name("Wednesday").TypeConverter<BoolConverter>();
      Map(m => m.Thursday).Name("Thursday").TypeConverter<BoolConverter>();
      Map(m => m.Friday).Name("Friday").TypeConverter<BoolConverter>();
   }
}

public class PlayerRatingCsvMap : ClassMap<PlayerCsv>
{
   public PlayerRatingCsvMap()
   {
      Map(m => m.Name).Name("Name");
      Map(m => m.InitialEloEstablished).Name("Initial Elo Established").TypeConverter<BoolConverter>();
      Map(m => m.InitialElo).Name("Initial Elo");
      Map(m => m.InitialGlicko).Name("Initial Glicko");
      Map(m => m.InitialDeviation).Name("Initial Deviation");
      Map(m => m.InitialClubRating).Name("Initial Club Rating");
   }
}

public class PlayerRatingExportCsvMap : ClassMap<PlayerCsv>
{
   public PlayerRatingExportCsvMap()
   {
      Map(m => m.Name).Name("Name");
      Map(m => m.Active).Name("Active").TypeConverter<BoolConverter>();
      Map(m => m.NetScore).Name("Net Score");
      Map(m => m.GamesCount).Name("Games Count");
      Map(m => m.WinRatio).Name("Win Ratio");
      Map(m => m.Elo).Name("Elo");
      Map(m => m.Glicko).Name("Glicko");
      Map(m => m.Deviation).Name("Deviation");
      Map(m => m.ClubRating).Name("Club Rating");
      Map(m => m.ClubRatingAvgOpp).Name("Club Rating Avg Opp");
      Map(m => m.EloPerf).Name("Elo Perf");
      Map(m => m.EloPerfAvgOpp).Name("Elo Perf Avg Opp");
      Map(m => m.GlickoPerf).Name("Glicko Perf");
      Map(m => m.GlickoPerfAvgOpp).Name("Glicko Perf Avg Opp");
      Map(m => m.ClubPerf).Name("Club Perf");
      Map(m => m.ClubPerfAvgOpp).Name("Club Perf Avg Opp");
      Map(m => m.InitialEloEstablished).Name("Initial Elo Established").TypeConverter<BoolConverter>();
      Map(m => m.InitialElo).Name("Initial Elo");
      Map(m => m.InitialGlicko).Name("Initial Glicko");
      Map(m => m.InitialDeviation).Name("Initial Deviation");
      Map(m => m.InitialClubRating).Name("Initial Club Rating");
   }
}
