using System;
using NUnit.Framework;

namespace Leap.Unity.Generation {

  public class BitConverterNonAllocTests {
    //BEGIN

    public byte[] randomBytes;

    [SetUp]
    void Setup() {
      randomBytes = new byte[128];
      for (int i = 0; i < randomBytes.Length; i++) {
        randomBytes[i] = (byte)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
      }
    }

    [Test]
    public void TestToSingle() {
      Single expected = BitConverter.ToSingle(randomBytes, 0);
      Single actual = BitConverterNonAlloc.ToSingle(randomBytes, 0);
      Assert.That(actual, Is.EqualTo(expected));
    }

    public static _Primitive_ To_Primitive_(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(_Primitive_);
          _Primitive_* primitivePtr = (_Primitive_*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(_Primitive_ primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          _Primitive_* primitivePtr = (_Primitive_*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(_Primitive_ primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(_Primitive_);
          _Primitive_* primitivePtr = (_Primitive_*)ptr;
          *primitivePtr = primitive;
        }
      }
    }
    //END
  }
}
