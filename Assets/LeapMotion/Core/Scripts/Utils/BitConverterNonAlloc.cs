/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity {

  public class BitConverterNonAlloc {

    public static UInt16 ToUInt16(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt16* primitivePtr = (UInt16*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static UInt16 ToUInt16(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt16);
          UInt16* primitivePtr = (UInt16*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(UInt16 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt16* primitivePtr = (UInt16*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(UInt16 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt16);
          UInt16* primitivePtr = (UInt16*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static Int16 ToInt16(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int16* primitivePtr = (Int16*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static Int16 ToInt16(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int16);
          Int16* primitivePtr = (Int16*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(Int16 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int16* primitivePtr = (Int16*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(Int16 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int16);
          Int16* primitivePtr = (Int16*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static UInt32 ToUInt32(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt32* primitivePtr = (UInt32*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static UInt32 ToUInt32(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt32);
          UInt32* primitivePtr = (UInt32*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(UInt32 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt32* primitivePtr = (UInt32*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(UInt32 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt32);
          UInt32* primitivePtr = (UInt32*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static Int32 ToInt32(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int32* primitivePtr = (Int32*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static Int32 ToInt32(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int32);
          Int32* primitivePtr = (Int32*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(Int32 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int32* primitivePtr = (Int32*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(Int32 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int32);
          Int32* primitivePtr = (Int32*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static UInt64 ToUInt64(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt64* primitivePtr = (UInt64*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static UInt64 ToUInt64(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt64);
          UInt64* primitivePtr = (UInt64*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(UInt64 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          UInt64* primitivePtr = (UInt64*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(UInt64 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(UInt64);
          UInt64* primitivePtr = (UInt64*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static Int64 ToInt64(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int64* primitivePtr = (Int64*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static Int64 ToInt64(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int64);
          Int64* primitivePtr = (Int64*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(Int64 primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Int64* primitivePtr = (Int64*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(Int64 primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Int64);
          Int64* primitivePtr = (Int64*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static Single ToSingle(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Single* primitivePtr = (Single*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static Single ToSingle(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          Single* primitivePtr = (Single*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(Single primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Single* primitivePtr = (Single*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(Single primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Single);
          Single* primitivePtr = (Single*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static Double ToDouble(byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Double* primitivePtr = (Double*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static Double ToDouble(byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Double);
          Double* primitivePtr = (Double*)ptr;
          return *primitivePtr;
        }
      }
    }

    public static void GetBytes(Double primitive, byte[] bytes, int offset = 0) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          Double* primitivePtr = (Double*)ptr;
          *primitivePtr = primitive;
        }
      }
    }

    public static void GetBytes(Double primitive, byte[] bytes, ref int offset) {
      unsafe {
        fixed (byte* ptr = &bytes[offset]) {
          offset += sizeof(Double);
          Double* primitivePtr = (Double*)ptr;
          *primitivePtr = primitive;
        }
      }
    }
  }
}
