using System;
using NUnit.Framework;

namespace Leap.Unity.Graphing.Tests {
  public class RingBufferTest {

    private RingBuffer<int> _buffer;

    [SetUp]
    public void Setup() {
      _buffer = new RingBuffer<int>();
    }

    [TearDown]
    public void Teardown() {
      _buffer.Clear();
      _buffer = null;
    }

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void InvalidCapacity([Values(int.MinValue, -1, 0)] int minCapacity) {
      new RingBuffer<int>(minCapacity);
    }

    [Test]
    public void Clear() {
      _buffer.PushBack(1);
      _buffer.PushFront(1);
      Assert.That(_buffer.Count, Is.EqualTo(2));
      _buffer.Clear();
      Assert.That(_buffer.Count, Is.EqualTo(0));
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AccessEmptyBack() {
      int value = _buffer.Front;
      Assert.NotNull(value);  //Just to remove unused value warning
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AccessEmptyFront() {
      int value = _buffer.Front;
      Assert.NotNull(value);  //Just to remove unused value warning
    }

    [Test]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void InvalidIndex([Values(int.MinValue, -1, 5, int.MaxValue)] int index) {
      for (int i = 0; i < 5; i++) {
        _buffer.PushBack(0);
      }

      int value = _buffer[index];
      Assert.NotNull(value); //Just to remove unused value warning
    }

    [Test]
    public void PushFront() {
      for (int i = 0; i < 100; i++) {
        _buffer.PushBack(i);
        Assert.That(_buffer.Back, Is.EqualTo(i));
        Assert.That(_buffer.Count, Is.EqualTo(i + 1));
        for (int j = 0; j <= i; j++) {
          Assert.That(j, Is.EqualTo(_buffer[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        Assert.That(_buffer.Front, Is.EqualTo(i));
        _buffer.PopFront(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }

    [Test]
    public void PushBack() {
      for (int i = 0; i < 100; i++) {
        _buffer.PushFront(i);
        Assert.That(_buffer.Front, Is.EqualTo(i));
        Assert.That(_buffer.Count, Is.EqualTo(i + 1));
        for (int j = 0; j <= i; j++) {
          Assert.That(i - j, Is.EqualTo(_buffer[j]));
        }
      }

      for (int i = 0; i < 100; i++) {
        int value;
        Assert.That(_buffer.Back, Is.EqualTo(i));
        _buffer.PopBack(out value);
        Assert.That(i, Is.EqualTo(value));
      }
    }


  }
}
