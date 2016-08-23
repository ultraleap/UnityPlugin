using UnityEngine;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;

namespace Leap.Unity.Interaction.Tests {

  public class StructAllocatorTest {

    [TearDown]
    public void Teardown() {
      StructAllocator.CleanupAllocations();
    }

    [Test]
    public void AllocateInt([Values(0, 10, Int32.MaxValue, Int32.MinValue)] Int32 writeValue) {
      IntPtr valuePtr = StructAllocator.AllocateStruct(ref writeValue);
      Int32 readValue = Marshal.ReadInt32(valuePtr);
      Assert.That(writeValue, Is.EqualTo(readValue));
    }

    [Test]
    public void AllocateFloat([Values(0, 10, Single.MinValue, Single.MaxValue, Single.NegativeInfinity, Single.PositiveInfinity, Single.NaN)] Single writeValue) {
      IntPtr valuePtr = StructAllocator.AllocateStruct(ref writeValue);
      Single readValue = (Single)Marshal.PtrToStructure(valuePtr, typeof(Single));
      Assert.That(writeValue, Is.EqualTo(readValue));
    }

    [Test]
    public void MultipleAllocations([Values(0, 1)] Int32 first, [Values(2, 3)] Int32 second) {
      IntPtr firstPtr = StructAllocator.AllocateStruct(ref first);
      IntPtr secondPtr = StructAllocator.AllocateStruct(ref second);
      Int32 readFirst = Marshal.ReadInt32(firstPtr);
      Int32 readSecond = Marshal.ReadInt32(secondPtr);
      Assert.That(first, Is.EqualTo(readFirst));
      Assert.That(second, Is.EqualTo(readSecond));
    }

    [Test]
    public void StructAllocation([Values(0, 1)] Int32 a, [Values(2, 3)] Single b) {
      TestStruct writeStruct = new TestStruct();
      writeStruct.a = a;
      writeStruct.b = b;
      IntPtr structPtr = StructAllocator.AllocateStruct(ref writeStruct);
      TestStruct readStruct = (TestStruct)Marshal.PtrToStructure(structPtr, typeof(TestStruct));
      Assert.That(writeStruct, Is.EqualTo(readStruct));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestStruct {
      public Int32 a;
      public Single b;
    }
  }
}
