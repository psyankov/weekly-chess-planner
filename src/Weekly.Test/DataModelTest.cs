namespace Ant.Weekly.Test;

[TestClass]
public class DataModelTest
{
   public Data Data { get; private set; } = new Data();

   [TestInitialize]
   public void MyTestInitialize()
   {
      var testData = new TestData();
      Data = testData.Data;
   }

   [TestMethod]
   public void DataModelPopulatedAndUpdated()
   {
      Assert.AreEqual(7, Data.Games.All.Count);
      Assert.AreEqual(6, Data.Players.All.Count);

      var alice = Data.Players.GetPlayer("Alice");
      Assert.IsNotNull(alice);
      Assert.AreEqual(5, alice.Games.Count);
      Assert.AreEqual(3, alice.WhiteCount);
      Assert.AreEqual(2, alice.BlackCount);

      var bob = Data.Players.GetPlayer("Bob");
      Assert.IsNotNull(bob);
      Assert.AreEqual(2, bob.Games.Count);
      Assert.AreEqual(1, bob.WhiteCount);
      Assert.AreEqual(1, bob.BlackCount);

      var charlie = Data.Players.GetPlayer("Charlie");
      Assert.IsNotNull(charlie);
      Assert.AreEqual(2, charlie.Games.Count);
      Assert.AreEqual(1, charlie.WhiteCount);
      Assert.AreEqual(1, charlie.BlackCount);

      var daniel = Data.Players.GetPlayer("Daniel");
      Assert.IsNotNull(daniel);
      Assert.AreEqual(2, daniel.Games.Count);
      Assert.AreEqual(1, daniel.WhiteCount);
      Assert.AreEqual(1, daniel.BlackCount);

      var edward = Data.Players.GetPlayer("Edward");
      Assert.IsNotNull(edward);
      Assert.AreEqual(2, edward.Games.Count);
      Assert.AreEqual(1, edward.WhiteCount);
      Assert.AreEqual(1, edward.BlackCount);

      var fiona = Data.Players.GetPlayer("Fiona");
      Assert.IsNotNull(fiona);
      Assert.AreEqual(1, fiona.Games.Count);
      Assert.AreEqual(0, fiona.WhiteCount);
      Assert.AreEqual(1, fiona.BlackCount);
   }
}
