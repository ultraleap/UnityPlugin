using System;

namespace Leap.Unity.Generation {

  public static class BitConverterNonAlloc {
    //BEGIN

    public static _Primitive_ To_Primitive_(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          _Primitive_* primitivePtr = (_Primitive_*)ptr;
          return *primitivePtr;
        }
      }
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
