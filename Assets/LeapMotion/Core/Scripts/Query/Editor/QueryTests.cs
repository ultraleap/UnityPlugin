/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Leap.Unity.Query.Test {

  public class QueryTests {
    public int[] LIST_0 = { 1, 2, 3, 4, 5 };
    public int[] LIST_1 = { 6, 7, 8, 9, 10 };

    [Test]
    public void AllTest() {
      Assert.AreEqual(LIST_0.All(i => i < 5),
                      LIST_0.Query().All(i => i < 5));

      Assert.AreEqual(LIST_0.All(i => i != 8),
                      LIST_0.Query().All(i => i != 8));
    }

    [Test]
    public void AnyTest() {
      Assert.AreEqual(LIST_0.Any(i => i == 4),
                      LIST_0.Query().Any(i => i == 4));
    }

    [Test]
    public void CastTest() {
      object[] objs = new object[] { "Hello", "World", "These", "Are", "All", "Strings" };

      Assert.That(objs.Cast<string>().SequenceEqual(
                  objs.Query().Cast<string>().ToList()));
    }

    [Test]
    public void ConcatTest() {
      Assert.That(LIST_0.Concat(LIST_1).SequenceEqual(
                  LIST_0.Query().Concat(LIST_1.Query()).ToList()));
    }

    [Test]
    public void ContainsTest() {
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
    public void ElemenAtTest() {
      Assert.AreEqual(LIST_0.ElementAt(3),
                      LIST_0.Query().ElementAt(3));

      Assert.AreEqual(LIST_0.ElementAtOrDefault(100),
                      LIST_0.Query().ElementAtOrDefault(100));
    }

    [Test]
    public void EnumeratorTest() {
      Assert.AreEqual(new TestEnumerator().Query().IndexOf(3), 3);
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

    [Test]
    public void FoldTest() {
      Assert.AreEqual(LIST_0.Query().Fold((a, b) => a + b),
                      LIST_0.Sum());
    }

    [Test]
    public void ForeachTest() {
      List<int> found = new List<int>();
      foreach (var item in LIST_0.Query().Concat(LIST_1.Query())) {
        found.Add(item);
      }
      Assert.That(LIST_0.Concat(LIST_1).SequenceEqual(found));
    }

    [Test]
    public void IndexOfTests() {
      Assert.AreEqual(LIST_0.Query().IndexOf(3), 2);
      Assert.AreEqual(LIST_0.Query().IndexOf(100), -1);
    }

    [Test]
    public void LastTests() {
      Assert.That(LIST_0.Query().Last(), Is.EqualTo(LIST_0.Last()));

      var empty = new int[] { };

      Assert.That(() => {
        empty.Query().Last();
      }, Throws.InvalidOperationException);

      Assert.That(empty.Query().LastOrDefault(), Is.EqualTo(empty.LastOrDefault()));
    }

    [Test]
    public void MultiFirstTest() {
      var q = LIST_0.Query();
      var a = q.First();
      var b = q.First();

      Assert.That(a, Is.EqualTo(LIST_0[0]));
      Assert.That(b, Is.EqualTo(LIST_0[0]));
    }

    [Test]
    public void MultiForeachTest() {
      List<int> a = new List<int>();
      List<int> b = new List<int>();

      var q = LIST_0.Query();
      foreach (var item in q) {
        a.Add(item);
      }

      foreach (var item in q) {
        b.Add(item);
      }

      Assert.That(a, Is.EquivalentTo(LIST_0));
      Assert.That(b, Is.EquivalentTo(LIST_0));
    }

    [Test]
    public void OfTypeTest() {
      object[] objs = new object[] { 0, 0.4f, "Hello", 7u, 0.4, "World", null };

      Assert.That(objs.OfType<string>().SequenceEqual(
                  objs.Query().OfType<string>().ToList()));

      Assert.That(objs.OfType<string>().SequenceEqual(
                  objs.Query().OfType(typeof(string)).Cast<string>().ToList()));
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
    public void SelectManyEmptyTest() {
      new int[] { }.Query().SelectMany(i => new int[] { }.Query()).ToList();
    }

    [Test]
    public void SingleTest() {
      var array = new int[] { 5 };
      Assert.That(array.Single(), Is.EqualTo(array.Query().Single()));

      Assert.That(() => {
        new int[] { }.Query().Single();
      }, Throws.InvalidOperationException);

      Assert.That(() => {
        new int[] { 0, 1 }.Query().Single();
      }, Throws.InvalidOperationException);
    }

    [Test]
    public void SkipTest() {
      Assert.That(LIST_0.Skip(3).SequenceEqual(
                  LIST_0.Query().Skip(3).ToList()));
    }

    [Test]
    public void SkipWhileTest() {
      Assert.That(LIST_0.SkipWhile(i => i < 4).SequenceEqual(
                  LIST_0.Query().SkipWhile(i => i < 4).ToList()));
    }

    [Test]
    public void TakeTest() {
      Assert.That(LIST_0.Take(4).SequenceEqual(
                  LIST_0.Query().Take(4).ToList()));
    }

    [Test]
    public void TakeWhileTest() {
      Assert.That(LIST_0.TakeWhile(i => i < 4).SequenceEqual(
                  LIST_0.Query().TakeWhile(i => i < 4).ToList()));
    }

    [Test]
    public void WithPreviousTest() {
      Assert.That(LIST_0.Query().WithPrevious().Count(p => p.hasPrev), Is.EqualTo(LIST_0.Length - 1));
      Assert.That(LIST_0.Query().WithPrevious(includeStart: true).Count(p => !p.hasPrev), Is.EqualTo(1));
      Assert.That(LIST_0.Query().WithPrevious(includeStart: true).Count(p => p.hasPrev), Is.EqualTo(LIST_0.Length - 1));

      foreach (var pair in LIST_0.Query().WithPrevious()) {
        Assert.That(pair.value, Is.EqualTo(pair.prev + 1));
      }
    }

    [Test]
    public void WithPreviousOffsetTest() {
      Assert.That(LIST_0.Query().WithPrevious(offset: 4).Count(), Is.EqualTo(1));
      Assert.That(LIST_0.Query().WithPrevious(offset: 5).Count(), Is.EqualTo(0));
      Assert.That(LIST_0.Query().WithPrevious(offset: int.MaxValue).Count(), Is.EqualTo(0));

      var item = LIST_0.Query().WithPrevious(offset: 4).First();
      Assert.That(item.value, Is.EqualTo(5));
      Assert.That(item.prev, Is.EqualTo(1));

      Assert.That(LIST_0.Query().WithPrevious(offset: 2).All(i => i.value - i.prev == 2));
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

    public class TestEnumerator : IEnumerator<int> {
      private int _curr = -1;

      public int Current {
        get {
          return _curr;
        }
      }

      public bool MoveNext() {
        _curr++;
        return (_curr != 10);
      }

      object IEnumerator.Current { get { return null; } }
      public void Dispose() { }
      public void Reset() { }
    }
  }
}
