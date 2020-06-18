/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Leap.Unity {

  public static class BitConverterNonAlloc {

#if !ENABLE_IL2CPP
    [ThreadStatic]
    private static ConversionStruct _c;
#endif

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt16 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static UInt16 ToUInt16(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(UInt16*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.UInt16;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int16 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static Int16 ToInt16(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Int16*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.Int16;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt32 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static UInt32 ToUInt32(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(UInt32*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.UInt32;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int32 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static Int32 ToInt32(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Int32*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Int32;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt64 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static UInt64 ToUInt64(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(UInt64*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.UInt64;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int64 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static Int64 ToInt64(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Int64*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Int64;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Single value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static Single ToSingle(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Single*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Single;
#endif
    }
    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Double value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// </summary>
    public static Double ToDouble(byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          return *(Double*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Double;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt16 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the UInt16 in bytes
    /// after this method is complete.
    /// </summary>
    public static UInt16 ToUInt16(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt16);
          return *(UInt16*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.UInt16;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int16 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the Int16 in bytes
    /// after this method is complete.
    /// </summary>
    public static Int16 ToInt16(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int16);
          return *(Int16*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.Int16;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt32 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the UInt32 in bytes
    /// after this method is complete.
    /// </summary>
    public static UInt32 ToUInt32(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt32);
          return *(UInt32*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.UInt32;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int32 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the Int32 in bytes
    /// after this method is complete.
    /// </summary>
    public static Int32 ToInt32(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int32);
          return *(Int32*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Int32;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the UInt64 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the UInt64 in bytes
    /// after this method is complete.
    /// </summary>
    public static UInt64 ToUInt64(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt64);
          return *(UInt64*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.UInt64;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Int64 value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the Int64 in bytes
    /// after this method is complete.
    /// </summary>
    public static Int64 ToInt64(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int64);
          return *(Int64*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Int64;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Single value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the Single in bytes
    /// after this method is complete.
    /// </summary>
    public static Single ToSingle(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          return *(Single*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Single;
#endif
    }

    /// <summary>
    /// Given an array of bytes, and an offset to start reading from, return
    /// the Double value that is represented by that byte pattern.  Undefined
    /// results if there are not enough bytes in the array.
    /// 
    /// The offset variable is incremented by the size of the Double in bytes
    /// after this method is complete.
    /// </summary>
    public static Double ToDouble(byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Double);
          return *(Double*)ptr;
        }
      }
#else
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Double;
#endif
    }

    /// <summary>
    /// Given a UInt16 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(UInt16 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(UInt16*)ptr = value;
        }
      }
#else
      _c.UInt16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
#endif
    }

    /// <summary>
    /// Given a Int16 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(Int16 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Int16*)ptr = value;
        }
      }
#else
      _c.Int16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
#endif
    }

    /// <summary>
    /// Given a UInt32 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(UInt32 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(UInt32*)ptr = value;
        }
      }
#else
      _c.UInt32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a Int32 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(Int32 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Int32*)ptr = value;
        }
      }
#else
      _c.Int32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a UInt64 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(UInt64 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(UInt64*)ptr = value;
        }
      }
#else
      _c.UInt64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

    /// <summary>
    /// Given a Int64 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(Int64 value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Int64*)ptr = value;
        }
      }
#else
      _c.Int64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

    /// <summary>
    /// Given a Single value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(Single value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Single*)ptr = value;
        }
      }
#else
      _c.Single = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a Double value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// </summary>
    public static void GetBytes(Double value, byte[] bytes, int offset = 0) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          *(Double*)ptr = value;
        }
      }
#else
      _c.Double = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

    /// <summary>
    /// Given a UInt16 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the UInt16 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(UInt16 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt16);
          *(UInt16*)ptr = value;
        }
      }
#else
      _c.UInt16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
#endif
    }

    /// <summary>
    /// Given a Int16 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the Int16 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(Int16 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int16);
          *(Int16*)ptr = value;
        }
      }
#else
      _c.Int16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
#endif
    }

    /// <summary>
    /// Given a UInt32 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the UInt32 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(UInt32 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt32);
          *(UInt32*)ptr = value;
        }
      }
#else
      _c.UInt32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a Int32 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the Int32 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(Int32 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int32);
          *(Int32*)ptr = value;
        }
      }
#else
      _c.Int32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a UInt64 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the UInt64 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(UInt64 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(UInt64);
          *(UInt64*)ptr = value;
        }
      }
#else
      _c.UInt64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

    /// <summary>
    /// Given a Int64 value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the Int64 in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(Int64 value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Int64);
          *(Int64*)ptr = value;
        }
      }
#else
      _c.Int64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

    /// <summary>
    /// Given a Single value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the Single in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(Single value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          *(Single*)ptr = value;
        }
      }
#else
      _c.Single = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
#endif
    }

    /// <summary>
    /// Given a Double value, copy its binary representation into the given array
    /// of bytes, at the given offset.  Undefined results if there are not enough
    /// bytes in the array to accept the value.
    /// 
    /// The offset variable is incremented by the size of the Double in bytes
    /// after this method is complete.
    /// </summary>
    public static void GetBytes(Double value, byte[] bytes, ref int offset) {
#if ENABLE_IL2CPP
      unsafe {
        fixed (void* ptr = &bytes[offset]) {
          offset += sizeof(Double);
          *(Double*)ptr = value;
        }
      }
#else
      _c.Double = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
#endif
    }

#if !ENABLE_IL2CPP
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
