using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ant.Weekly.Test;

[TestClass]
public class DateTimeExtensionsTest
{
   [TestMethod]
   public void StartOfWeekTest()
   {
      var date = new DateOnly(2025, 4, 10); // Thursday
      var startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 10); // Thursday
      startOfWeek = date.StartOfWeek();
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 10); // Thursday
      startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
      Assert.AreEqual(new DateOnly(2025, 4, 6), startOfWeek);

      date = new DateOnly(2025, 4, 12); // Saturday
      startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 12); // Saturday
      startOfWeek = date.StartOfWeek();
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 12); // Saturday
      startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
      Assert.AreEqual(new DateOnly(2025, 4, 6), startOfWeek);

      date = new DateOnly(2025, 4, 13); // Sunday
      startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 13); // Sunday
      startOfWeek = date.StartOfWeek();
      Assert.AreEqual(new DateOnly(2025, 4, 7), startOfWeek);

      date = new DateOnly(2025, 4, 13); // Sunday
      startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
      Assert.AreEqual(new DateOnly(2025, 4, 13), startOfWeek);

      date = new DateOnly(2025, 4, 14); // Monday
      startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
      Assert.AreEqual(new DateOnly(2025, 4, 13), startOfWeek);

      date = new DateOnly(2025, 4, 14); // Monday
      startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
      Assert.AreEqual(new DateOnly(2025, 4, 14), startOfWeek);

      date = new DateOnly(2025, 4, 14); // Monday
      startOfWeek = date.StartOfWeek();
      Assert.AreEqual(new DateOnly(2025, 4, 14), startOfWeek);
   }

   [TestMethod]
   public void DateTest()
   {
      var startOfWeek = new DateOnly(2025, 4, 6); // Sunday

      var date = DayOfWeek.Sunday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 6), date);

      date = DayOfWeek.Monday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 7), date);

      date = DayOfWeek.Tuesday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 8), date);

      date = DayOfWeek.Wednesday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 9), date);

      startOfWeek = new DateOnly(2025, 4, 7); // Monday

      date = DayOfWeek.Monday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 7), date);

      date = DayOfWeek.Tuesday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 8), date);

      date = DayOfWeek.Wednesday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 9), date);

      date = DayOfWeek.Sunday.DateFromWeekOf(startOfWeek);
      Assert.AreEqual(new DateOnly(2025, 4, 13), date);
   }

   [TestMethod]
   public void CountWeeksAfter()
   {
      var date1 = new DateOnly(2025, 4, 8);
      var date2 = new DateOnly(2025, 4, 17);
      Assert.AreEqual(1, date2.CountWeeksAfter(date1));

      date1 = new DateOnly(2025, 4, 8);
      date2 = new DateOnly(2025, 4, 9);
      Assert.AreEqual(0, date2.CountWeeksAfter(date1));

      date1 = new DateOnly(2025, 3, 11);
      date2 = new DateOnly(2025, 4, 17);
      Assert.AreEqual(5, date2.CountWeeksAfter(date1));

   }
}