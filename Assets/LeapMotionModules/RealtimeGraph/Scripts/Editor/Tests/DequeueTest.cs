using UnityEngine;
using NUnit.Framework;

namespace Leap.Unity.Graphing.Tests {

  public class DequeueTest {

    private Dequeue<int> _dequeue;

    [SetUp]
    public void Setup() {
      _dequeue = new Dequeue<int>();
    }

    [TearDown]
    public void Teardown() {
      _dequeue.Clear();
      _dequeue = null;
    }

    [Test]
    public void PushFront() {
      for (int i = 0; i < 100; i++) {
        _dequeue.PushFront(i);
        for (int j = 0; j <= i; j++) {
          Assert.That(j, Is.EqualTo(_dequeue[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        _dequeue.PopBack(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }
  }
}
