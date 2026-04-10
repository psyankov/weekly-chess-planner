namespace Ant.Weekly.Test;

public class TestData
{
   public Data Data { get; }

   public TestData()
   {
      Data = new Data();
      var alice = new Player("Alice", Section.C, Section.A, false, 1000, 1000, 300, 1000);
      var bob = new Player("Bob", Section.D, Section.A, false, 600, 600, 50, 1000);
      var charlie = new Player("Charlie", Section.C, Section.A, false, 900, 900, 100, 1000);
      var daniel = new Player("Daniel", Section.C, Section.A, false, 1100, 1100, 150, 1000);
      var edward = new Player("Edward", Section.B, Section.A, false, 1400, 1400, 200, 1000);
      var fiona = new Player("Fiona", Section.A, Section.A, false, 1600, 1600, 300, 1000);

      Data.Players.Add(alice);
      Data.Players.Add(bob);
      Data.Players.Add(charlie);
      Data.Players.Add(daniel);
      Data.Players.Add(edward);
      Data.Players.Add(fiona);

      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 1), alice, bob, GameResult.WhiteWin));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 2), charlie, alice, GameResult.Draw));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 3), alice, daniel, GameResult.BlackWin));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 4), edward, alice, GameResult.BlackWin));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 5), alice, fiona, GameResult.BlackWin));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 7), bob, charlie, GameResult.WhiteWin));
      Data.Games.All.Add(new Game(new DateOnly(2025, 4, 9), daniel, edward, GameResult.BlackWin));

      Data.UpdateDataModel(exportRatings: false);
   }
}
