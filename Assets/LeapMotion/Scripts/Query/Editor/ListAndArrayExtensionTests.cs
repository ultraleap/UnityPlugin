using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

namespace Leap.Unity.Query.Test {

  public class ListAndArrayExtensionTests {

    [Test]
    public void RemoveAtMany_Random() {
      List<int> toRemove = new List<int>().FillEach(100, i => i);
      while (toRemove.Count != 20) {
        toRemove.RemoveAt(Random.Range(0, toRemove.Count));
      }

      doRemoveAtManyTest(toRemove);
    }

    [Test]
    public void RemoveAtMany_First() {
      List<int> toRemove = new List<int>();
      toRemove.Add(0);

      doRemoveAtManyTest(toRemove);
    }

    [Test]
    public void RemoveAtMany_Last() {
      List<int> toRemove = new List<int>();
      toRemove.Add(99);

      doRemoveAtManyTest(toRemove);
    }

    private void doRemoveAtManyTest(List<int> toRemove) {
      List<int> listA = new List<int>().FillEach(100, i => i);
      List<int> listB = new List<int>(listA);

      for (int i = toRemove.Count; i-- != 0;) {
        listA.RemoveAt(toRemove[i]);
      }

      listB.RemoveAtMany(toRemove);

      Assert.AreEqual(listA, listB);
    }
  }
}
