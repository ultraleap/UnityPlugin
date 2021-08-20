/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Threading;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  public class ProduceConsumeBufferTest {

    private ProduceConsumeBuffer<TestStruct> buffer;

    [SetUp]
    public void Setup() {
      buffer = new ProduceConsumeBuffer<TestStruct>(16);
    }

    [TearDown]
    public void Teardown() {
      buffer = null;
    }

    [Test]
    [Timeout(1000)]
    public void Test() {
      Thread consumer = new Thread(new ThreadStart(consumerThread));
      Thread producer = new Thread(new ThreadStart(producerThread));

      consumer.Start();
      producer.Start();

      consumer.Join();
      producer.Join();
    }

    private void consumerThread() {
      try {
        for (int i = 0; i < buffer.Capacity; i++) {
          TestStruct s;
          s.index = i;
          s.name = i.ToString();
          while (!buffer.TryEnqueue(ref s)) { }
        }
      } catch (Exception e) {
        Assert.Fail(e.Message);
      }
    }

    private void producerThread() {
      try {
        for (int i = 0; i < buffer.Capacity; i++) {
          TestStruct s;
          while (!buffer.TryDequeue(out s)) { }

          Assert.That(s.index, Is.EqualTo(i));
          Assert.That(s.name, Is.EqualTo(i.ToString()));
        }
      } catch (Exception e) {
        Assert.Fail(e.Message);
      }
    }

    private struct TestStruct {
      public int index;
      public string name;
    }
  }
}
