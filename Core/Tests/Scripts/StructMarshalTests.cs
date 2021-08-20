/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using LeapInternal;

namespace Leap.LeapCSharp.Tests {

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

      TestMarshaledStruct output;
      StructMarshal<TestMarshaledStruct>.PtrToStruct(_ptr, out output);
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }

    [Test]
    public void ArrayElementToStructTest() {
      Marshal.StructureToPtr(_testStruct, (IntPtr)((long)_ptr + _size * ARRAY_TEST_INDEX), false);

      TestMarshaledStruct output;
      StructMarshal<TestMarshaledStruct>.ArrayElementToStruct(_ptr, ARRAY_TEST_INDEX, out output);
      Assert.That(_testStruct.id, Is.EqualTo(output.id), "Input must match output.");
    }
  }
}
