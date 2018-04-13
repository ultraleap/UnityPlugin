/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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
  using System;

  public class QueryTests {

    [Test]
    public void AllTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().All(i => i < 5), Is.EqualTo(
                  arg.ToList().All(i => i < 5)));

      Assert.That(arg.ToQuery().All(i => i != 8), Is.EqualTo(
                  arg.ToList().All(i => i != 8)));
    }

    [Test]
    public void AnyTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Any(i => i == 4), Is.EqualTo(
                  arg.ToList().Query().Any(i => i == 4)));
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
    public void AverageTest([ValueSource("list0")] QueryArg arg0) {
      if (arg0.ToList().Count == 0) {
        Assert.Ignore("Ignore empty queries for average test.");
        return;
      }

      Assert.That(arg0.ToQuery().Select(t => (double)t).Average(), Is.EqualTo(
                  arg0.ToList().Average()).Within(0.001).Percent);
    }

    [Test]
    public void CastTest() {
      object[] objs = new object[] { "Hello", "World", "These", "Are", "All", "Strings" };

      Assert.That(objs.Cast<string>().SequenceEqual(
                  objs.Query().Cast<string>().ToList()));
    }

    [Test]
    public void ConcatTest([ValueSource("list0")] QueryArg arg0, [ValueSource("list0")] QueryArg arg1) {
      Assert.That(arg0.ToQuery().Concat(arg1.ToList()).ToList(), Is.EquivalentTo(
                  arg0.ToList().Concat(arg1.ToList()).ToList()));

      Assert.That(arg0.ToQuery().Concat(arg1.ToQuery()).ToList(), Is.EquivalentTo(
                  arg0.ToList().Concat(arg1.ToList()).ToList()));
    }

    [Test]
    public void ContainsTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Contains(3), Is.EqualTo(
                  arg.ToList().Contains(3)));

      Assert.That(arg.ToQuery().Contains(9), Is.EqualTo(
                  arg.ToList().Contains(9)));
    }

    [Test]
    public void CountTests([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Count(), Is.EqualTo(
                  arg.ToList().Count()));

      Assert.That(arg.ToQuery().Count(i => i % 2 == 0), Is.EqualTo(
                  arg.ToList().Count(i => i % 2 == 0)));
    }

    [Test]
    public void DistinctTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Distinct().OrderBy(t => t).ToList(), Is.EquivalentTo(
                  arg.ToList().Distinct().OrderBy(t => t).ToList()));
    }

    [Test]
    public void ElementAtTest([ValueSource("list0")] QueryArg arg, [Values(0, 3, 100)] int index) {
      var list = arg.ToList();

      if (index >= list.Count) {
        Assert.That(() => arg.ToQuery().ElementAt(index), Throws.InstanceOf<IndexOutOfRangeException>());
      } else {
        Assert.That(arg.ToQuery().ElementAt(index), Is.EqualTo(
                    arg.ToList().ElementAt(index)));
      }
    }

    [Test]
    public void EnumeratorTest() {
      Assert.AreEqual(new TestEnumerator().Query().IndexOf(3), 3);
    }

    [Test]
    public void FirstTests([ValueSource("list0")] QueryArg arg) {
      var list = arg.ToList();

      if (list.Count == 0) {
        Assert.That(() => arg.ToQuery().First(), Throws.InvalidOperationException);
      } else {
        Assert.That(arg.ToQuery().First(), Is.EqualTo(
                    arg.ToList().First()));
      }

      if (list.Where(i => i % 2 == 0).Count() == 0) {
        Assert.That(() => arg.ToQuery().Where(i => i % 2 == 0).First(), Throws.InvalidOperationException);
      } else {
        Assert.That(arg.ToQuery().First(i => i % 2 == 0), Is.EqualTo(
                    arg.ToList().First(i => i % 2 == 0)));
      }
    }

    [Test]
    public void FirstOrDefaultTests([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().FirstOrDefault(), Is.EqualTo(
                  arg.ToList().FirstOrDefault()));

      Assert.That(arg.ToQuery().FirstOrDefault(i => i % 2 == 0), Is.EqualTo(
                  arg.ToList().FirstOrDefault(i => i % 2 == 0)));

      Assert.That(arg.ToQuery().FirstOrDefault(i => i > 10), Is.EqualTo(
                  arg.ToList().FirstOrDefault(i => i > 10)));
    }

    [Test]
    public void FoldTest([ValueSource("list0")] QueryArg arg) {
      var list = arg.ToList();

      if (list.Count == 0) {
        Assert.That(() => arg.ToQuery().Fold((a, b) => a + b), Throws.InvalidOperationException);
      } else {
        Assert.That(arg.ToQuery().Fold((a, b) => a + b), Is.EqualTo(
                    arg.ToList().Sum()));
      }
    }

    [Test]
    public void ForeachTest([ValueSource("list0")] QueryArg arg0, [ValueSource("list1")] QueryArg arg1) {
      List<int> actual = new List<int>();
      foreach (var item in arg0.ToQuery().Concat(arg1.ToQuery())) {
        actual.Add(item);
      }
      Assert.That(actual, Is.EquivalentTo(
                  arg0.ToList().Concat(arg1.ToList()).ToList()));
    }

    [Test]
    public void IndexOfTests([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().IndexOf(3), Is.EqualTo(
                  arg.ToList().IndexOf(3)));

      Assert.That(arg.ToQuery().IndexOf(100), Is.EqualTo(
                  arg.ToList().IndexOf(100)));
    }

    [Test]
    public void LastTests([ValueSource("list0")] QueryArg arg) {
      var list = arg.ToList();

      if (list.Count == 0) {
        Assert.That(() => arg.ToQuery().Last(), Throws.InvalidOperationException);
      } else {
        Assert.That(arg.ToQuery().Last(), Is.EqualTo(
                    arg.ToList().Last()));
      }

      Assert.That(arg.ToQuery().LastOrDefault(), Is.EqualTo(
                  arg.ToList().LastOrDefault()));
    }

    [Test]
    public void MaxTest([ValueSource("list0")] QueryArg arg) {
      if (arg.ToList().Count == 0) {
        Assert.Ignore("Ignore empty queries for max tests.");
        return;
      }

      Assert.That(arg.ToQuery().Max(), Is.EqualTo(
                  arg.ToList().Max()));
    }

    [Test]
    public void MinTest([ValueSource("list0")] QueryArg arg) {
      if (arg.ToList().Count == 0) {
        Assert.Ignore("Ignore empty queries for min tests.");
        return;
      }

      Assert.That(arg.ToQuery().Min(), Is.EqualTo(
                  arg.ToList().Min()));
    }

    [Test]
    public void MultiFirstTest([ValueSource("list0")] QueryArg arg) {
      var q = arg.ToQuery();

      q.FirstOrDefault();

      Assert.That(() => q.FirstOrDefault(), Throws.InvalidOperationException);
    }

    [Test]
    public void MultiForeachTest([ValueSource("list0")] QueryArg arg) {
      List<int> a = new List<int>();
      List<int> b = new List<int>();

      var q = arg.ToQuery();
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
    public void OrderByTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().OrderBy(i => i).ToList(), Is.EquivalentTo(
                  arg.ToList().OrderBy(i => i).ToList()));
    }

    [Test]
    public void OrderByDescendingTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().OrderByDescending(i => i).ToList(), Is.EquivalentTo(
                  arg.ToList().OrderByDescending(i => i).ToList()));
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
    public void Repeat([ValueSource("list0")] QueryArg arg, [Values(0, 1, 2, 3, 100)] int repetitions) {
      List<int> list = new List<int>();
      for (int i = 0; i < repetitions; i++) {
        list.AddRange(arg.ToList());
      }

      Assert.That(arg.ToQuery().Repeat(repetitions).ToList(), Is.EquivalentTo(
                  list));
    }

    [Test]
    public void ReverseTest([ValueSource("list0")] QueryArg arg) {
      var expected = arg.ToList();
      expected.Reverse();

      Assert.That(arg.ToQuery().Reverse().ToList(), Is.EquivalentTo(
                  expected));
    }

    [Test]
    public void SelectTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Select(i => i * 23).ToList(), Is.EquivalentTo(
                  arg.ToList().Select(i => i * 23).ToList()));
    }

    [Test]
    public void SelectManyTest([ValueSource("list0")] QueryArg arg0, [ValueSource("list0")] QueryArg arg1) {
      Assert.That(arg0.ToQuery().SelectMany(i => arg1.ToQuery().Select(j => j * i)).ToList(), Is.EquivalentTo(
                  arg0.ToList().SelectMany(i => arg1.ToList().Select(j => j * i)).ToList()));

      Assert.That(arg0.ToQuery().SelectMany(i => arg1.ToList().Select(j => j * i).ToList()).ToList(), Is.EquivalentTo(
                  arg0.ToList().SelectMany(i => arg1.ToList().Select(j => j * i)).ToList()));
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
    public void SkipTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Skip(3).ToList(), Is.EquivalentTo(
                  arg.ToList().Skip(3).ToList()));
    }

    [Test]
    public void SkipWhileTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().SkipWhile(i => i < 4).ToList(), Is.EquivalentTo(
                  arg.ToList().SkipWhile(i => i < 4).ToList()));
    }

    [Test]
    public void SortTest([ValueSource("list0")] QueryArg arg) {
      var expected = arg.ToList();
      expected.Sort();

      Assert.That(arg.ToQuery().Sort().ToList(), Is.EquivalentTo(
                  expected));
    }

    [Test]
    public void SortDescendingTests([ValueSource("list0")] QueryArg arg) {
      var expected = arg.ToList();
      expected.Sort();
      expected.Reverse();

      Assert.That(arg.ToQuery().SortDescending().ToList(), Is.EquivalentTo(
                  expected));
    }

    [Test]
    public void SumTests([ValueSource("list0")] QueryArg arg) {
      if (arg.ToList().Count == 0) {
        Assert.Ignore("Ignore empty queries for sum tests.");
        return;
      }

      Assert.That(arg.ToQuery().Sum(), Is.EqualTo(
                  arg.ToList().Sum()));
    }

    [Test]
    public void TakeTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Take(4).ToList(), Is.EquivalentTo(
                  arg.ToList().Take(4).ToList()));
    }

    [Test]
    public void TakeWhileTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().TakeWhile(i => i < 4).ToList(), Is.EquivalentTo(
                  arg.ToList().TakeWhile(i => i < 4).ToList()));
    }

    [Test]
    public void WithPreviousTest([ValueSource("list0")] QueryArg arg) {
      var list = arg.ToList();
      if (list.Count == 0) {
        Assert.That(arg.ToQuery().WithPrevious().Count(), Is.EqualTo(0));
        Assert.That(arg.ToQuery().WithPrevious(includeStart: true).Count(), Is.EqualTo(0));
      } else if (list.Count == 1) {
        Assert.That(arg.ToQuery().WithPrevious().Count(), Is.EqualTo(0));
        Assert.That(arg.ToQuery().WithPrevious(includeStart: true).Count(), Is.EqualTo(1));
      } else {
        Assert.That(arg.ToQuery().WithPrevious().Count(p => p.hasPrev), Is.EqualTo(list.Count - 1));
        Assert.That(arg.ToQuery().WithPrevious(includeStart: true).Count(p => !p.hasPrev), Is.EqualTo(1));
        Assert.That(arg.ToQuery().WithPrevious(includeStart: true).Count(p => p.hasPrev), Is.EqualTo(list.Count - 1));
      }

      int index = 0;
      foreach (var pair in arg.ToQuery().WithPrevious()) {
        Assert.That(pair.prev, Is.EqualTo(list[index]));
        index++;
      }
    }

    [Test]
    public void WithPreviousOffsetTest([ValueSource("list0")] QueryArg arg) {
      var list = arg.ToList();
      if (list.Count == 0) {
        Assert.That(arg.ToQuery().WithPrevious(offset: 4).Count(), Is.EqualTo(0));
        Assert.That(arg.ToQuery().WithPrevious(offset: 4, includeStart: true).Count(), Is.EqualTo(0));
      } else if (list.Count == 1) {
        Assert.That(arg.ToQuery().WithPrevious(offset: 4).Count(), Is.EqualTo(0));
        Assert.That(arg.ToQuery().WithPrevious(offset: 4, includeStart: true).Count(), Is.EqualTo(1));
      } else {
        Assert.That(arg.ToQuery().WithPrevious(offset: 4).Count(), Is.EqualTo(Mathf.Max(0, list.Count - 4)));
        Assert.That(arg.ToQuery().WithPrevious(offset: list.Count + 1).Count(), Is.EqualTo(0));
        Assert.That(arg.ToQuery().WithPrevious(offset: int.MaxValue).Count(), Is.EqualTo(0));
      }

      Assert.That(Values.Range(0, 10).WithPrevious(offset: 2).All(i => i.value - i.prev == 2));
    }

    [Test]
    public void WhereTest([ValueSource("list0")] QueryArg arg) {
      Assert.That(arg.ToQuery().Where(i => i % 2 == 0).ToList(), Is.EquivalentTo(
                  arg.ToList().Where(i => i % 2 == 0).ToList()));
    }

    [Test]
    public void WithIndicesTest([ValueSource("list0")] QueryArg arg) {
      int index = 0;
      foreach (var item in arg.ToQuery().WithIndices()) {
        Assert.That(item.index, Is.EqualTo(index));
        Assert.That(item.value, Is.EqualTo(arg.ToList()[index]));
        index++;
      }
    }

    [Test]
    public void ZipTest([ValueSource("list0")] QueryArg arg0, [ValueSource("list1")] QueryArg arg1) {
      var list0 = arg0.ToList();
      var list1 = arg1.ToList();

      List<string> expected = new List<string>();
      for (int i = 0; i < Mathf.Min(list0.Count, list1.Count); i++) {
        expected.Add(list0[i].ToString() + list1[i].ToString());
      }

      Assert.That(arg0.ToQuery().Zip(arg1.ToQuery(), (a, b) => a.ToString() + b.ToString()).ToList(), Is.EquivalentTo(
                  expected));

      Assert.That(arg0.ToQuery().Zip(arg1.ToList(), (a, b) => a.ToString() + b.ToString()).ToList(), Is.EquivalentTo(
                  expected));
    }

    private static IEnumerable<QueryArg> list0 {
      get {
        List<int> values = new List<int>() { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 9, 1, 900, int.MinValue, int.MaxValue };
        List<int> lengths = new List<int>() {
          0,
          1,
          2,
          int.MaxValue
        };

        foreach (var length in lengths) {
          var list = values.Take(length).ToList();
          yield return new QueryArg(list, list.Count);
          yield return new QueryArg(list, list.Count * 10 + 10);
        }
      }
    }

    private static IEnumerable<QueryArg> list1 {
      get {
        List<int> values = new List<int>() { 6, 7, 8, 9, 10, 1, 1, 9, 300, 6, 900, int.MaxValue };
        List<int> lengths = new List<int>() {
          0,
          1,
          2,
          int.MaxValue
        };

        foreach (var length in lengths) {
          var list = values.Take(length).ToList();
          yield return new QueryArg(list, list.Count);
          yield return new QueryArg(list, list.Count * 10 + 10);
        }
      }
    }

    public class QueryArg {
      private int[] _array;
      private int _count;

      public QueryArg(List<int> values, int capacity) {
        _array = new int[capacity];
        values.CopyTo(_array);
        _count = values.Count;
      }

      public Query<int> ToQuery() {
        int[] copy = new int[_array.Length];
        _array.CopyTo(copy, 0);
        return new Query<int>(copy, _count);
      }

      public List<int> ToList() {
        return new List<int>(_array.Take(_count));
      }

      public override string ToString() {
        return _array.Length + " : " + Utils.ToArrayString(_array.Take(_count));
      }
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
