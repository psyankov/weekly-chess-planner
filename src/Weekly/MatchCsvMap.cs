using CsvHelper.Configuration;

namespace Ant.Weekly;
public class MatchCsvMap : ClassMap<MatchCsv>
{
   public MatchCsvMap()
   {
      Map(m => m.White).Name("White");
      Map(m => m.Black).Name("Black");
      Map(m => m.Event).Name("Event");
      Map(m => m.AutoColor).Name("Auto Color").TypeConverter<BoolConverter>();
   }
}
