using System.Linq;
using NUnit.Framework;

namespace Leap.Unity.Query.Test {

  public class QueryTests {
    public int[] LIST_0 = { 1, 2, 3, 4, 5 };
    public int[] LIST_1 = { 6, 7, 8, 9, 10 };

    [Test]
    public void ConcatTest() {
      Assert.That(LIST_0.Concat(LIST_1).SequenceEqual(
                  LIST_0.Query().Concat(LIST_1.Query()).ToList()));
    }

    [Test]
    public void OfTypeTest() {
      object[] objs = new object[] { 0, 0.4f, "Hello", 7u, 0.4, "World", null };

      Assert.That(objs.OfType<string>().SequenceEqual(
                  objs.Query().OfType<string>().ToList()));
    }

    [Test]
    public void SelectTest() {
      Assert.That(LIST_0.Select(i => i * 23).SequenceEqual(
                  LIST_0.Query().Select(i => i * 23).ToList()));
    }

    [Test]
    public void SelectManyTest() {
      Assert.That(LIST_0.SelectMany(i => LIST_1.Select(j => j * i)).SequenceEqual(
                  LIST_0.Query().SelectMany(i => LIST_1.Query().Select(j => j * i)).ToList()));
    }

    [Test]
    public void TakeWhileTest() {
      Assert.That(LIST_0.TakeWhile(i => i < 4).SequenceEqual(
                  LIST_0.Query().TakeWhile(i => i < 4).ToList()));
    }

    [Test]
    public void WhereTest() {
      Assert.That(LIST_0.Where(i => i % 2 == 0).SequenceEqual(
                  LIST_0.Query().Where(i => i % 2 == 0).ToList()));
    }

    [Test]
    public void ZipTest() {
      Assert.That(LIST_0.Query().Zip(LIST_1.Query(), (a, b) => a.ToString() + b.ToString()).ToList().SequenceEqual(
                  new string[] { "16", "27", "38", "49", "510" }));
    }

    [Test]
    public void AnyTest() {
      Assert.AreEqual(LIST_0.Any(i => i == 4),
                      LIST_0.Query().Any(i => i == 4));
    }

    [Test]
    public void AllTest() {
      Assert.AreEqual(LIST_0.All(i => i < 5),
                      LIST_0.Query().All(i => i < 5));

      Assert.AreEqual(LIST_0.All(i => i != 8),
                      LIST_0.Query().All(i => i != 8));
    }

    [Test]
    public void ConstainsTest() {
      Assert.AreEqual(LIST_0.Contains(3),
                      LIST_0.Query().Contains(3));

      Assert.AreEqual(LIST_0.Contains(9),
                      LIST_0.Query().Contains(9));
    }

    [Test]
    public void CountTests() {
      Assert.AreEqual(LIST_0.Count(),
                      LIST_0.Query().Count());

      Assert.AreEqual(LIST_0.Count(i => i % 2 == 0),
                      LIST_0.Query().Count(i => i % 2 == 0));
    }

    [Test]
    public void FirstTests() {
      Assert.AreEqual(LIST_0.First(),
                      LIST_0.Query().First());

      Assert.AreEqual(LIST_0.First(i => i % 2 == 0),
                      LIST_0.Query().First(i => i % 2 == 0));
    }

    [Test]
    public void FirstOrDefaultTests() {
      Assert.AreEqual(LIST_0.FirstOrDefault(),
                      LIST_0.Query().FirstOrDefault());

      Assert.AreEqual(LIST_0.FirstOrDefault(i => i % 2 == 0),
                      LIST_0.Query().FirstOrDefault(i => i % 2 == 0));

      Assert.AreEqual(LIST_0.FirstOrDefault(i => i > 10),
                      LIST_0.Query().FirstOrDefault(i => i > 10));
    }
  }
}
