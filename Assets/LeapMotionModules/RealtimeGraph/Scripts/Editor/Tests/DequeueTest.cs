using System;
using NUnit.Framework;

namespace Leap.Unity.Graphing.Tests {
  public class DequeueTest {

    private RingBuffer<int> _dequeue;

    [SetUp]
    public void Setup() {
      _dequeue = new RingBuffer<int>();
    }

    [TearDown]
    public void Teardown() {
      _dequeue.Clear();
      _dequeue = null;
    }

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void InvalidCapacity([Values(int.MinValue, -1, 0)] int minCapacity) {
      new RingBuffer<int>(minCapacity);
    }

    [Test]
    public void Clear() {
      _dequeue.PushBack(1);
      _dequeue.PushFront(1);
      Assert.That(_dequeue.Count, Is.EqualTo(2));
      _dequeue.Clear();
      Assert.That(_dequeue.Count, Is.EqualTo(0));
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AccessEmptyBack() {
      int value = _dequeue.Front;
      Assert.NotNull(value);  //Just to remove unused value warning
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AccessEmptyFront() {
      int value = _dequeue.Front;
      Assert.NotNull(value);  //Just to remove unused value warning
    }

    [Test]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void InvalidIndex([Values(int.MinValue, -1, 5, int.MaxValue)] int index) {
      for (int i = 0; i < 5; i++) {
        _dequeue.PushBack(0);
      }

      int value = _dequeue[index];
      Assert.NotNull(value); //Just to remove unused value warning
    }

    [Test]
    public void PushFront() {
      for (int i = 0; i < 100; i++) {
        _dequeue.PushBack(i);
        Assert.That(_dequeue.Back, Is.EqualTo(i));
        Assert.That(_dequeue.Count, Is.EqualTo(i + 1));
        for (int j = 0; j <= i; j++) {
          Assert.That(j, Is.EqualTo(_dequeue[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        Assert.That(_dequeue.Front, Is.EqualTo(i));
        _dequeue.PopFront(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }

    [Test]
    public void PushBack() {
      for (int i = 0; i < 100; i++) {
        _dequeue.PushFront(i);
        Assert.That(_dequeue.Front, Is.EqualTo(i));
        Assert.That(_dequeue.Count, Is.EqualTo(i + 1));
        for (int j = 0; j <= i; j++) {
          Assert.That(i - j, Is.EqualTo(_dequeue[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        Assert.That(_dequeue.Back, Is.EqualTo(i));
        _dequeue.PopBack(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }


  }
}
