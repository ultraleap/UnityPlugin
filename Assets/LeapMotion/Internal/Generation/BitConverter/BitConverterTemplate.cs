/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Leap.Unity.Generation {

  public static class BitConverterNonAlloc_Template_ {

#if !IL2CPP_ENABLED
    [ThreadStatic]
    private static ConversionStruct _c;
#endif

    //BEGIN TO

    public static Single ToSingle(byte[] bytes, int offset = 0) {
#if IL2CPP_ENABLED
#if UNITY_2018_1_OR_NEWER
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Single*)ptr;
        }
      }
#else
#error BitConverterNoAlloc only supports IL2CPP on versions of Unity 2018.1 or greater.
#endif
#else
      //FILL BYTES
      return _c.Single;
#endif
    }
    //END
    //BEGIN TO

    public static Single ToSingle(byte[] bytes, ref int offset) {
#if IL2CPP_ENABLED
#if UNITY_2018_1_OR_NEWER
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          return *(Single*)ptr;
        }
      }
#else
#error BitConverterNoAlloc only supports IL2CPP on versions of Unity 2018.1 or greater.
#endif
#else
      //FILL BYTES
      return _c.Single;
#endif
    }
    //END
    //BEGIN GET

    public static void GetBytes(Single value, byte[] bytes, int offset = 0) {
#if IL2CPP_ENABLED
#if UNITY_2018_1_OR_NEWER
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Single*)ptr = value;
        }
      }
#else
#error BitConverterNoAlloc only supports IL2CPP on versions of Unity 2018.1 or greater.
#endif
#else
      _c.Single = value;
      //FILL BYTES
#endif
    }
    //END
    //BEGIN GET

    public static void GetBytes(Single value, byte[] bytes, ref int offset) {
#if IL2CPP_ENABLED
#if UNITY_2018_1_OR_NEWER
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          *(Single*)ptr = value;
        }
      }
#else
#error BitConverterNoAlloc only supports IL2CPP on versions of Unity 2018.1 or greater.
#endif
#else
      _c.Single = value;
      //FILL BYTES
#endif
    }
    //END

#if !IL2CPP_ENABLED
    [StructLayout(LayoutKind.Explicit)]
    private struct ConversionStruct {
      [FieldOffset(0)]
      public byte Byte0;
      [FieldOffset(1)]
      public byte Byte1;
      [FieldOffset(2)]
      public byte Byte2;
      [FieldOffset(3)]
      public byte Byte3;
      [FieldOffset(4)]
      public byte Byte4;
      [FieldOffset(5)]
      public byte Byte5;
      [FieldOffset(6)]
      public byte Byte6;
      [FieldOffset(7)]
      public byte Byte7;

      [FieldOffset(0)]
      public UInt16 UInt16;
      [FieldOffset(0)]
      public Int16 Int16;
      [FieldOffset(0)]
      public UInt32 UInt32;
      [FieldOffset(0)]
      public Int32 Int32;
      [FieldOffset(0)]
      public UInt64 UInt64;
      [FieldOffset(0)]
      public Int64 Int64;
      [FieldOffset(0)]
      public Single Single;
      [FieldOffset(0)]
      public Double Double;
    }
#endif
  }
}
