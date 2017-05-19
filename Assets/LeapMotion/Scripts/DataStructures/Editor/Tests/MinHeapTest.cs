/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  public class MinHeapTest {

    private class HeapElement : IMinHeapNode, IComparable<HeapElement> {
      public int heapIndex { get; set; }
      public float value;

      public HeapElement(float value) {
        this.value = value;
      }

      public int CompareTo(HeapElement other) {
        return value.CompareTo(other.value);
      }
    }

    private MinHeap<HeapElement> _heap;

    [SetUp]
    public void Setup() {
      _heap = new MinHeap<HeapElement>();
    }

    [TearDown]
    public void Teardown() {
      _heap.Clear();
      _heap = null;
    }

    [Test]
    public void HeapTest() {
      _heap.Insert(new HeapElement(0));
      _heap.Insert(new HeapElement(2));
      _heap.Insert(new HeapElement(1));
      _heap.Insert(new HeapElement(-5));
      _heap.Insert(new HeapElement(10));

      Assert.That(_heap.Validate(), Is.EqualTo(true));
      Assert.That(_heap.PeekMin().value, Is.EqualTo(-5));

      _heap.RemoveMin();

      Assert.That(_heap.Validate(), Is.EqualTo(true));
      Assert.That(_heap.PeekMin().value, Is.EqualTo(0));

      var element4 = new HeapElement(4);
      _heap.Insert(element4);

      Assert.That(_heap.Validate(), Is.EqualTo(true));

      _heap.Remove(element4);

      Assert.That(_heap.Validate(), Is.EqualTo(true));
      Assert.That(_heap.Count, Is.EqualTo(4));
    }
  }
}
