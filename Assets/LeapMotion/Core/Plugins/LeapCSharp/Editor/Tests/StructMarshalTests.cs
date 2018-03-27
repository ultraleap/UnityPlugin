using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using LeapInternal;

namespace Leap.Tests {

  [StructLayout(LayoutKind.Sequential)]
  struct TestMarshaledStruct {
    public int id;
    public TestMarshaledStruct(int t) { id = t; }
  }

  [TestFixture()]
  public class StructMarshalTests {
    public const int ARRAY_SIZE = 5;
    public const int ARRAY_TEST_INDEX = 3;
    public const int TEST_ID = 23;

    private int _size;
    private IntPtr _ptr;
    private TestMarshaledStruct _testStruct;

    [SetUp]
    public void Setup() {
      _size = Marshal.SizeOf(typeof(TestMarshaledStruct));
      //For each test, allocate a chunk of memory large enough for [ARRAY_SIZE] structs
      _ptr = Marshal.AllocHGlobal(_size * ARRAY_SIZE);
      _testStruct = new TestMarshaledStruct(TEST_ID);
    }

    [TearDown]
    public void Teardown() {
      _size = 0;
      Marshal.FreeHGlobal(_ptr);
      _ptr = IntPtr.Zero;
      _testStruct = new TestMarshaledStruct();
    }

    [Test]
    public void SizeTest() {
      int reportedSize = StructMarshal<TestMarshaledStruct>.Size;
      Assert.That(_size, Is.EqualTo(reportedSize), "Size must match Marshal.SizeOf.");
    }

    [Test]
    public void PtrToStructTest() {
      Marshal.StructureToPtr(_testStruct, _ptr, false);

      var output = StructMarshal<TestMarshaledStruct>.PtrToStruct(_ptr);
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }

    [Test]
    public void ArrayElementToStructTest() {
      Marshal.StructureToPtr(_testStruct, (IntPtr)((long)_ptr + _size * ARRAY_TEST_INDEX), false);

      var output = StructMarshal<TestMarshaledStruct>.ArrayElementToStruct(_ptr, ARRAY_TEST_INDEX);
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }

    [Test]
    public void CopyIntoDestination() {
      StructMarshal<TestMarshaledStruct>.CopyIntoDestination(_ptr, _testStruct);

      var output = (TestMarshaledStruct)Marshal.PtrToStructure(_ptr, typeof(TestMarshaledStruct));
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }

    [Test]
    public void CopyIntoArray() {
      StructMarshal<TestMarshaledStruct>.CopyIntoArray(_ptr, _testStruct, ARRAY_TEST_INDEX);

      var output = (TestMarshaledStruct)Marshal.PtrToStructure((IntPtr)((long)_ptr + _size * ARRAY_TEST_INDEX), typeof(TestMarshaledStruct));
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }
  }
}
