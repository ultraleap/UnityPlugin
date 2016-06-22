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
        Assert.That(_dequeue.Front, Is.EqualTo(i));
        for (int j = 0; j <= i; j++) {
          Assert.That(j, Is.EqualTo(_dequeue[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        Assert.That(_dequeue.Back, Is.EqualTo(i));
        _dequeue.PopBack(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }

    [Test]
    public void PushBack() {
      Debug.Log(_dequeue.ToDebugString());
      for (int i = 0; i < 30; i++) {
        _dequeue.PushBack(i);
        Assert.That(_dequeue.Back, Is.EqualTo(i));
        for (int j = 0; j <= i; j++) {
          //Debug.Log((i - j) + " : " + _dequeue[j]);
          //Assert.That(i - j, Is.EqualTo(_dequeue[j]));
        }
        Debug.Log(_dequeue.ToDebugString());
      }

      for (int i = 0; i < 30; i++) {
        int value;
        Assert.That(_dequeue.Front, Is.EqualTo(i));
        _dequeue.PopFront(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }
  }
}
