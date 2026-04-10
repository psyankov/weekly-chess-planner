using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Ant.Weekly;

public class BoolConverter : DefaultTypeConverter
{
   public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
   {
      if (string.IsNullOrWhiteSpace(text))
      {
         return false;
      }
      else if (text.ToUpper() == "X" || text.ToUpper() == "Y" || text.ToUpper() == "YES" || text.ToUpper() == "TRUE")
      {
         return true;
      }
      else
      {
         return false;
      }
   }

   public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
   {
      var boolean = value ?? false;
      return (bool)boolean ? "X" : "";
   }
}
