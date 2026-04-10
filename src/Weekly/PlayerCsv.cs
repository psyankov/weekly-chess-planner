namespace Ant.Weekly;

public class PlayerCsv
{
   public string Name { get; set; } = string.Empty;
   public bool Active { get; set; }

   public bool LeaguePlayer { get; set; }
   public string Section { get; set; } = string.Empty;
   public string SectionLimit { get; set; } = string.Empty;

   public int GamesPerWeek { get; set; }

   public bool Monday { get; set; }
   public bool Tuesday { get; set; }
   public bool Wednesday { get; set; }
   public bool Thursday { get; set; }
   public bool Friday { get; set; }

   public bool InitialEloEstablished { get; set; }
   public int InitialElo { get; set; }
   public int InitialGlicko { get; set; }
   public int InitialDeviation { get; set; }
   public int InitialClubRating { get; set; }
   
   public int Elo { get; set; }
   public int Glicko { get; set; }
   public int Deviation { get; set; }
   public int ClubRating { get; set; }
   public int ClubRatingAvgOpp { get; set; }
   public int EloPerf { get; set; }
   public int EloPerfAvgOpp { get; set; }
   public int GlickoPerf { get; set; }
   public int GlickoPerfAvgOpp { get; set; }
   public int ClubPerf { get; set; }
   public int ClubPerfAvgOpp { get; set; }
   
   public double NetScore { get; set; }
   public int GamesCount { get; set; }
   public double WinRatio { get; set; }

   public Section GetSection() => GetSection(Section);
   public Section GetOpponentSectionLimit() => GetSection(SectionLimit);

   private Section GetSection(string section)
   {
      switch (section.Trim().ToUpper())
      {
         case "A":
            return Weekly.Section.A;
         case "B":
            return Weekly.Section.B;
         case "C":
            return Weekly.Section.C;
         case "D":
            return Weekly.Section.D;
         case "E":
            return Weekly.Section.E;
         case "N":
         default:
            return Weekly.Section.N;
      }
   }

   public string GetFirstName() => ExtractFirstName(Name);

   public static string ExtractFirstName(string name)
   {
      ArgumentNullException.ThrowIfNull(name);

      var firstName = name.Split(' ')[0];
      if (!name.Contains(' '))
      {
         Log.Warning($"Player name '{name}' is not separated into First and Last names.");
         firstName = name;
      }
      return firstName;
   }
}
