/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Leap.Unity.Tests {
  using Query;

  public class QueryTests {
    public int[] LIST_0 = { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 9, 1, 900, int.MinValue, int.MaxValue };
    public int[] LIST_1 = { 6, 7, 8, 9, 10, 1, 1, 9, 300, 6, 900, int.MaxValue };

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
    public void Array2DTest() {
      const int WIDTH = 23;
      const int HEIGHT = 17;

      int[,] array = new int[WIDTH, HEIGHT];
      int counter = 0;
      for (int i = 0; i < WIDTH; i++) {
        for (int j = 0; j < HEIGHT; j++) {
          array[i, j] = counter++;
        }
      }

      Assert.That(array.Query().Count(), Is.EqualTo(WIDTH * HEIGHT));
      foreach (var value in Enumerable.Range(0, WIDTH * HEIGHT)) {
        Assert.That(array.Query().Contains(value));
      }
    }

    [Test]
    public void CastTest() {
      object[] objs = new object[] { "Hello", "World", "These", "Are", "All", "Strings" };

      Assert.That(objs.Cast<string>().SequenceEqual(
                  objs.Query().Cast<string>().ToList()));
    }

    [Test]
    public void ConcatTest() {
      Assert.That(LIST_0.Concat(LIST_1).ToList(), Is.EquivalentTo(
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
    public void DistinctTest() {
      Assert.That(LIST_0.Query().Distinct().OrderBy(t => t).ToList(), Is.EquivalentTo(
                  LIST_0.Distinct().OrderBy(t => t).ToList()));
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
      Assert.That(LIST_0.Concat(LIST_1).ToList(), Is.EquivalentTo(found));
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

      q.First();

      Assert.That(() => q.First(), Throws.InvalidOperationException);
    }

    [Test]
    public void MultiForeachTest() {
      List<int> a = new List<int>();
      List<int> b = new List<int>();

      var q = LIST_0.Query();
      foreach (var item in q) {
        a.Add(item);
      }

      Assert.That(() => {
        foreach (var item in q) {
          b.Add(item);
        }
      }, Throws.InvalidOperationException);
    }

    [Test]
    public void OfTypeTest() {
      object[] objs = new object[] { 0, 0.4f, "Hello", 7u, 0.4, "World", null };

      Assert.That(objs.OfType<string>().ToList(), Is.EquivalentTo(
                  objs.Query().OfType<string>().ToList()));

      Assert.That(objs.OfType<string>(), Is.EquivalentTo(
                  objs.Query().OfType(typeof(string)).Cast<string>().ToList()));
    }

    [Test]
    public void OrderByTest() {
      Assert.That(LIST_0.Query().OrderBy(i => i).ToList(), Is.EquivalentTo(
                  LIST_0.OrderBy(i => i).ToList()));
    }

    [Test]
    public void OrderByDescendingTest() {
      Assert.That(LIST_0.Query().OrderByDescending(i => i).ToList(), Is.EquivalentTo(
                  LIST_0.OrderByDescending(i => i).ToList()));
    }

    [Test]
    [Pairwise]
    public void RangeFromTo([Values(0, 1, 100, -1, -100)] int startValue,
                            [Values(0, 1, 100, -1, -100)] int endValue,
                            [Values(1, 2, 10)] int step,
                            [Values(true, false)] bool endIsExclusive) {
      List<int> expected = new List<int>();
      int value = startValue;
      int signStep = endValue > startValue ? step : -step;
      for (int i = 0; i < Mathf.Abs(startValue - endValue) + 1; i++) {
        expected.Add(value);
        value += signStep;
      }

      if (endIsExclusive) {
        expected.Remove(endValue);
      }

      expected = expected.Where(i => i >= Mathf.Min(startValue, endValue)).
                          Where(i => i <= Mathf.Max(startValue, endValue)).
                          ToList();

      Assert.That(Values.Range(startValue, endValue, step, endIsExclusive).ToList(), Is.EquivalentTo(expected));
    }

    [Test]
    public void Repeat([Values(0, 1, 2, 3, 100)] int repetitions) {
      List<int> list = new List<int>();
      for (int i = 0; i < repetitions; i++) {
        list.AddRange(LIST_0);
      }

      Assert.That(list, Is.EquivalentTo(
                  LIST_0.Query().Repeat(repetitions).ToList()));
    }

    [Test]
    public void ReverseTest() {
      Assert.That(LIST_0.Query().Reverse().ToList(), Is.EquivalentTo(
                  LIST_0.Select(i => i).Reverse().ToList()));
    }

    [Test]
    public void SelectTest() {
      Assert.That(LIST_0.Select(i => i * 23).ToList(), Is.EquivalentTo(
                  LIST_0.Query().Select(i => i * 23).ToList()));
    }

    [Test]
    public void SelectManyTest() {
      Assert.That(LIST_0.SelectMany(i => LIST_1.Select(j => j * i)).ToList(), Is.EquivalentTo(
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
      Assert.That(LIST_0.Skip(3).ToList(), Is.EquivalentTo(
                  LIST_0.Query().Skip(3).ToList()));
    }

    [Test]
    public void SkipWhileTest() {
      Assert.That(LIST_0.SkipWhile(i => i < 4).ToList(), Is.EquivalentTo(
                  LIST_0.Query().SkipWhile(i => i < 4).ToList()));
    }

    [Test]
    public void SortTest() {
      var expected = new List<int>(LIST_0);
      expected.Sort();

      Assert.That(LIST_0.Query().Sort().ToList(), Is.EquivalentTo(
                  expected));
    }

    [Test]
    public void SortDescendingTests() {
      var expected = new List<int>(LIST_0);
      expected.Sort();
      expected.Reverse();

      Assert.That(LIST_0.Query().SortDescending().ToList(), Is.EquivalentTo(
                  expected));
    }

    [Test]
    public void TakeTest() {
      Assert.That(LIST_0.Take(4).ToList(), Is.EquivalentTo(
                  LIST_0.Query().Take(4).ToList()));
    }

    [Test]
    public void TakeWhileTest() {
      Assert.That(LIST_0.TakeWhile(i => i < 4).ToList(), Is.EquivalentTo(
                  LIST_0.Query().TakeWhile(i => i < 4).ToList()));
    }

    [Test]
    public void WithPreviousTest() {
      Assert.That(LIST_0.Query().WithPrevious().Count(p => p.hasPrev), Is.EqualTo(LIST_0.Length - 1));
      Assert.That(LIST_0.Query().WithPrevious(includeStart: true).Count(p => !p.hasPrev), Is.EqualTo(1));
      Assert.That(LIST_0.Query().WithPrevious(includeStart: true).Count(p => p.hasPrev), Is.EqualTo(LIST_0.Length - 1));

      int index = 0;
      foreach (var pair in LIST_0.Query().WithPrevious()) {
        Assert.That(pair.prev, Is.EqualTo(LIST_0[index]));
        index++;
      }
    }

    [Test]
    public void WithPreviousOffsetTest() {
      Assert.That(LIST_0.Query().WithPrevious(offset: 4).Count(), Is.EqualTo(LIST_0.Length - 4));
      Assert.That(LIST_0.Query().WithPrevious(offset: LIST_0.Length + 1).Count(), Is.EqualTo(0));
      Assert.That(LIST_0.Query().WithPrevious(offset: int.MaxValue).Count(), Is.EqualTo(0));

      var item = LIST_0.Query().WithPrevious(offset: 4).First();
      Assert.That(item.value, Is.EqualTo(5));
      Assert.That(item.prev, Is.EqualTo(1));

      Assert.That(Values.Range(0, 10).WithPrevious(offset: 2).All(i => i.value - i.prev == 2));
    }

    [Test]
    public void WhereTest() {
      Assert.That(LIST_0.Where(i => i % 2 == 0).ToList(), Is.EquivalentTo(
                  LIST_0.Query().Where(i => i % 2 == 0).ToList()));
    }

    [Test]
    public void WithIndicesTest() {
      int index = 0;
      foreach (var item in LIST_0.Query().WithIndices()) {
        Assert.That(item.index, Is.EqualTo(index));
        Assert.That(item.value, Is.EqualTo(LIST_0[index]));
        index++;
      }
    }

    [Test]
    public void ZipTest() {
      List<string> expected = new List<string>();
      for (int i = 0; i < Mathf.Min(LIST_0.Length, LIST_1.Length); i++) {
        expected.Add(LIST_0[i].ToString() + LIST_1[i].ToString());
      }

      Assert.That(LIST_0.Query().Zip(LIST_1.Query(), (a, b) => a.ToString() + b.ToString()).ToList(), Is.EquivalentTo(
                  expected));
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
