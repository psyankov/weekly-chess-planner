namespace Ant.Weekly.Test;

[TestClass]
public class RatingTest
{
   public Data Data { get; private set; } = new Data();

   [TestInitialize]
   public void MyTestInitiailze()
   {
      App.S.Rating.MinimumRating = 100;
      App.S.Rating.MaximumRating = 9000;
      App.S.Rating.EloKFactor = 40;
      App.S.Rating.InitialGlicko = 400;
      App.S.Rating.TypicalDeviation = 50;
      App.S.Rating.InitialDeviation = 300;
      App.S.Rating.UncertaintyPeriodDays = 100;

      var testData = new TestData();
      Data = testData.Data;
   }

   [TestMethod]
   public void Constants()
   {
      Assert.AreEqual(0.00575646273, Rating.Q, 1e-6);

      var unrated = App.S.Rating.InitialDeviation;
      var typical = App.S.Rating.TypicalDeviation;
      var uncertaintyDays = App.S.Rating.UncertaintyPeriodDays;
      Assert.AreEqual((unrated * unrated - typical * typical) / uncertaintyDays, Rating.C2);
   }

   [TestMethod]
   public void GlickoFunctions()
   {
      Assert.AreEqual(0.98764240, Rating.G(50), 1e-6);
      Assert.AreEqual(0.95314897, Rating.G(100), 1e-6);
      Assert.AreEqual(0.90290777, Rating.G(150), 1e-6);
      Assert.AreEqual(0.84428149, Rating.G(200), 1e-6);
      Assert.AreEqual(0.72423546, Rating.G(300), 1e-6);

      Assert.AreEqual(365757.336650273, Rating.D2(1000, 600, 50), 1e-6);
      Assert.AreEqual(143123.430894585, Rating.D2(1000, 900, 100), 1e-6);
      Assert.AreEqual(158295.364144879, Rating.D2(1000, 1100, 150), 1e-6);
      Assert.AreEqual(386530.490318476, Rating.D2(1000, 1400, 200), 1e-6);
      Assert.AreEqual(821696.492088840, Rating.D2(1000, 1600, 300), 1e-6);

      Assert.AreEqual(0.906711770, Rating.ExpectedResultGlicko(1000, 600, 50), 1e-6);
      Assert.AreEqual(0.633828510, Rating.ExpectedResultGlicko(1000, 900, 100), 1e-6);
      Assert.AreEqual(0.372909409, Rating.ExpectedResultGlicko(1000, 1100, 150), 1e-6);
      Assert.AreEqual(0.125205788, Rating.ExpectedResultGlicko(1000, 1400, 200), 1e-6);
      Assert.AreEqual(0.075758645, Rating.ExpectedResultGlicko(1000, 1600, 300), 1e-6);

      Assert.AreEqual(410.636274368, Rating.GetGlickoKFactor(1000, 300.000000000, 600, 50), 1e-6);
      Assert.AreEqual(263.379875322, Rating.GetGlickoKFactor(1000, 268.751528913, 900, 100), 1e-6);
      Assert.AreEqual(191.442402454, Rating.GetGlickoKFactor(1000, 219.095295729, 1100, 150), 1e-6);
      Assert.AreEqual(163.437683841, Rating.GetGlickoKFactor(1000, 191.919685553, 1400, 200), 1e-6);
      Assert.AreEqual(134.686764017, Rating.GetGlickoKFactor(1000, 183.381117845, 1600, 300), 1e-6);

      var rating = new Rating(new Player("X"), false, 1200, 1200, 50, 1200);
      Assert.AreEqual(App.S.Rating.TypicalDeviation, rating.CurrentDeviation(0), 1e-6);
      Assert.AreEqual(App.S.Rating.InitialDeviation, rating.CurrentDeviation(App.S.Rating.UncertaintyPeriodDays), 1e-6);
   }

   [TestMethod]
   public void RatingUpdates()
   {
      var alice = Data.Players.GetPlayer("Alice");
      var bob = Data.Players.GetPlayer("Bob");
      var charlie = Data.Players.GetPlayer("Charlie");
      var daniel = Data.Players.GetPlayer("Daniel");
      var edward = Data.Players.GetPlayer("Edward");
      var fiona = Data.Players.GetPlayer("Fiona");

      Assert.IsNotNull(alice);
      Assert.IsNotNull(bob);
      Assert.IsNotNull(charlie);
      Assert.IsNotNull(daniel);
      Assert.IsNotNull(edward);
      Assert.IsNotNull(fiona);

      Game game;

      game = alice.Games.Where(g => g.Involves(alice, bob)).First();
      CheckRatingBeforeAndAfter(game, alice, 1000.00000000, 300.00000000, 1038.30753118, 268.75152891);
      CheckRatingBeforeAndAfter(game, bob, 600.00000000, 50.00000000, 598.35503705, 49.85555974);

      game = alice.Games.Where(g => g.Involves(alice, charlie)).First();
      CheckRatingBeforeAndAfter(game, alice, 1038.30753118, 268.75152891, 989.55882672, 221.49114677);
      CheckRatingBeforeAndAfter(game, charlie, 900.00000000, 100.00000000, 906.16671365, 97.87887303);

      game = alice.Games.Where(g => g.Involves(alice, daniel)).First();
      CheckRatingBeforeAndAfter(game, alice, 989.55882672, 221.49114677, 919.18312803, 193.85382446);
      CheckRatingBeforeAndAfter(game, daniel, 1100.00000000, 150.00000000, 1135.37709660, 141.94717561);

      game = alice.Games.Where(g => g.Involves(alice, edward)).First();
      CheckRatingBeforeAndAfter(game, alice, 919.18312803, 193.85382446, 1074.63918601, 187.28955195);
      CheckRatingBeforeAndAfter(game, edward, 1400.00000000, 200.00000000, 1233.49366676, 192.81085314);

      game = alice.Games.Where(g => g.Involves(alice, fiona)).First();
      CheckRatingBeforeAndAfter(game, alice, 1074.63918601, 187.28955195, 1060.69275031, 182.32690878);
      CheckRatingBeforeAndAfter(game, fiona, 1600.00000000, 300.00000000, 1626.97396030, 280.74549468);

      game = bob.Games.Where(g => g.Involves(bob, charlie)).First();
      CheckRatingBeforeAndAfter(game, bob, 598.35503705, 82.82859915, 628.75810667, 81.73803043);
      CheckRatingBeforeAndAfter(game, charlie, 906.16671365, 114.36902459, 847.53869216, 111.48270272);

      game = daniel.Games.Where(g => g.Involves(daniel, edward)).First();
      CheckRatingBeforeAndAfter(game, daniel, 1135.37709660, 156.60140697, 1095.27132139, 146.91173908);
      CheckRatingBeforeAndAfter(game, edward, 1233.49366676, 201.68298166, 1296.41261014, 180.12796300);

      // After the last game in the test data set
      Assert.AreEqual(new DateOnly(2025, 4, 5), alice.Rating.Date);
   }

   // Method arguments are expected values calculated independently in Excel
   public void CheckRatingBeforeAndAfter(Game game, Player player, double r0, double rd0, double r1, double rd1)
   {
      Assert.IsNotNull(game);
      Assert.IsNotNull(player);

      Rating? r;
      Rating rating;

      if (game.WhitePlayer == player)
      {
         r = game.WhiteRating;
      }
      else
      {
         r = game.BlackRating;
      }

      Assert.IsNotNull(r);
      rating = r;

      if (rating.Previous != null)
      {
         Assert.IsNotNull(rating.Game);
         Assert.AreEqual(rating.Game.Date, rating.Date);
      }

      Assert.IsNotNull(rating.New);
      Assert.AreEqual(game.Date, rating.New.Date);
      Assert.AreSame(game, rating.New.Game);

      Assert.AreEqual(r0, rating.Glicko, 1e-6);
      Assert.AreEqual(rd0, rating.CurrentDeviation(game.Date), 1e-6);
      Assert.AreEqual(r1, rating.New.Glicko, 1e-6);
      Assert.AreEqual(rd1, rating.New.Deviation, 1e-6);
   }
}
