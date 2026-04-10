namespace Ant.Weekly;

/// <summary>
/// Match is a potential game that has not been planned yet.
/// The two playes have overlapping availability and suitable section limits.
/// </summary>
public class Match : IEquatable<Match>
{
   public Player Player1 { get; set; }
   public Player Player2 { get; set; }
   public DateOnly LastPlayed { get; set; }
   public List<DayOfWeek> DayOptions { get; }

   public Match(Player player1, Player player2)
   {
      Player1 = player1;
      Player2 = player2;
      LastPlayed = Player1.LastDatePlayedAgainst(Player2);
      DayOptions = Player1.Availability.Against(Player2);

      if (DayOptions.Count == 0)
      {
         throw new InvalidOperationException($"Invalid match - there are no available days for the game between {Player1.Name} and {Player2.Name}");
      }

      if (Player1.SectionLimit < Player2.Section)
      {
         throw new InvalidOperationException($"Invalid match - {Player2.Name} section {Player2.Section} is above {Player1.Name} section limit {Player1.SectionLimit}");
      }

      if (Player2.SectionLimit < Player1.Section)
      {
         throw new InvalidOperationException($"Invalid match - {Player1.Name} section {Player1.Section} is above {Player2.Name} section limit {Player2.SectionLimit}");
      }
   }

   public override string ToString() => $"{Player1} vs {Player2} (last game {LastPlayed:yyyy.MM.dd})";

   public bool Involves(Player player)
   {
      return Player1 == player || Player2 == player;
   }

   public bool Involves(Player player1, Player player2)
   {
      return Involves(player1) && Involves(player2);
   }

   public bool Equals(Match? other)
   {
      return other == null ? false : Involves(other.Player1, other.Player2);
   }
}
