/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using NUnit.Framework;

namespace Leap.LeapCSharp.Tests
{
    class TestObjectType
    {
        public int id = 0;
        public TestObjectType() { id = -1; }
        public TestObjectType(int t) { id = t; }
    }

    public class CircularObjectBufferTests
    {
        [Test]
        public void CreateTest()
        {
            CircularObjectBuffer<TestObjectType> ciq = new CircularObjectBuffer<TestObjectType>(100);
            Assert.AreEqual(100, ciq.Capacity, "Capacity is the same as initialized value");
            Assert.AreEqual(0, ciq.Count, "Buffer starts with no items");
            Assert.IsTrue(ciq.IsEmpty, "Buffer starts out empty");
        }

        [Test]
        public void PutGetTest()
        {
            CircularObjectBuffer<TestObjectType> ciq = new CircularObjectBuffer<TestObjectType>(100);
            TestObjectType bar = new TestObjectType(1);
            ciq.Put(ref bar);
            Assert.IsFalse(ciq.IsEmpty, "Not empty.");

            for (int t = 0; t <= 12345; ++t)
            {
                TestObjectType foo = new TestObjectType(t);
                TestObjectType mu = new TestObjectType(t);
                ciq.Put(ref foo);
                ciq.Get(out mu);
                Assert.AreEqual(t, mu.id, "Got the same value that we put.");
            }
            ciq.Get(out bar);
            int currentId = bar.id;
            for (int t = 0; t < ciq.Capacity; t++)
            {
                //Console.WriteLine(t + ", " + ciq.Get (t).id + ", " + currentId);
                TestObjectType chew = new TestObjectType(t);
                ciq.Get(out chew, t);
                Assert.AreEqual(chew.id, currentId, "Older objects are in order: " + chew.id + ", " + currentId);
                currentId--;
            }
        }

        [Test]
        public void OutOfBoundsTests()
        {
            CircularObjectBuffer<TestObjectType> ciq = new CircularObjectBuffer<TestObjectType>(100);
            TestObjectType foo = new TestObjectType(1);
            for (int t = 0; t <= 12345; ++t)
            {
                ciq.Get(out foo, t);
                Assert.AreEqual(-1, foo.id, "Get default object from empty buffer");
            }
            TestObjectType bar = new TestObjectType(0);
            ciq.Put(ref bar);
            for (int t = 1; t <= 12345; ++t)
            {
                ciq.Get(out foo, t);
                Assert.AreEqual(-1, foo.id, "Get default object past last item in mostly empty buffer");
            }
            for (int t = 0; t <= 122; ++t)
            {
                TestObjectType mu = new TestObjectType(t);
                ciq.Put(ref mu);
            }
            for (int t = ciq.Capacity; t <= 12345; ++t)
            {
                ciq.Get(out foo, t);
                Assert.AreEqual(-1, foo.id, "Get default object past last item in full buffer");
            }
        }

        [Test]
        public void OrderTests()
        {
            CircularObjectBuffer<TestObjectType> ciq = new CircularObjectBuffer<TestObjectType>(10);
            Assert.AreEqual(10, ciq.Capacity, "Capacity is the same as initialized value");
            Assert.AreEqual(0, ciq.Count, "Buffer starts with no items");
            for (int t = 0; t < 5; ++t)
            {
                TestObjectType foo = new TestObjectType(t);
                ciq.Put(ref foo);
            }

            TestObjectType bar = new TestObjectType(0);
            Assert.AreEqual(5, ciq.Count, "Buffer has 5 items");
            ciq.Get(out bar, 0);
            Assert.AreEqual(bar.id, 4, "Objects are still in order: " + bar.id + ", " + 4);
            ciq.Get(out bar, 1);
            Assert.AreEqual(bar.id, 3, "Objects are still in order: " + bar.id + ", " + 3);
            ciq.Get(out bar, 2);
            Assert.AreEqual(bar.id, 2, "Objects are still in order: " + bar.id + ", " + 2);
            ciq.Get(out bar, 3);
            Assert.AreEqual(bar.id, 1, "Objects are still in order: " + bar.id + ", " + 1);
            ciq.Get(out bar, 4);
            Assert.AreEqual(bar.id, 0, "Objects are still in order: " + bar.id + ", " + 0);
        }

        [Test]
        public void ResizeTests()
        {
            CircularObjectBuffer<TestObjectType> ciq = new CircularObjectBuffer<TestObjectType>(10);
            Assert.AreEqual(10, ciq.Capacity, "Capacity is the same as initialized value");
            Assert.AreEqual(0, ciq.Count, "Buffer starts with no items");
            for (int t = 0; t < 5; ++t)
            {
                TestObjectType foo = new TestObjectType(t);
                ciq.Put(ref foo);
            }
            Assert.AreEqual(5, ciq.Count, "Buffer has 5 items");
            ciq.Resize(15);
            Assert.AreEqual(15, ciq.Capacity, "Capacity now is 15");
            Assert.AreEqual(5, ciq.Count, "Buffer still has 5 items");
            TestObjectType bar = new TestObjectType(0);
            ciq.Get(out bar, 0);
            Assert.AreEqual(4, bar.id, "Objects are still in order: " + bar.id + ", " + 4);
            ciq.Get(out bar, 1);
            Assert.AreEqual(3, bar.id, "Objects are still in order: " + bar.id + ", " + 3);
            ciq.Get(out bar, 2);
            Assert.AreEqual(2, bar.id, "Objects are still in order: " + bar.id + ", " + 2);
            ciq.Get(out bar, 3);
            Assert.AreEqual(1, bar.id, "Objects are still in order: " + bar.id + ", " + 1);
            ciq.Get(out bar, 4);
            Assert.AreEqual(0, bar.id, "Objects are still in order: " + bar.id + ", " + 0);

            for (int t = 0; t <= 12345; ++t)
            {
                TestObjectType foo = new TestObjectType(t);
                ciq.Put(ref foo);
                ciq.Get(out foo);
                Assert.AreEqual(t, foo.id, "Got the same value that we put.");
            }
            TestObjectType mu = new TestObjectType();
            ciq.Get(out mu);
            int currentId = mu.id;
            for (int t = 0; t < ciq.Capacity; t++)
            {
                //Console.WriteLine(t + ", " + ciq.Get (t).id + ", " + currentId);
                ciq.Get(out mu, t);
                Assert.AreEqual(mu.id, currentId, "Older objects are in order: " + mu.id + ", " + currentId);
                currentId--;
            }
        }
    }
}