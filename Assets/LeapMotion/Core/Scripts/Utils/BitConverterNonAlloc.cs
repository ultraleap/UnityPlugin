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

namespace Leap.Unity {

  public static class BitConverterNonAlloc {
    private static ConversionStruct _c = new ConversionStruct();


    public static UInt16 ToUInt16(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.UInt16;
    }

    public static Int16 ToInt16(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.Int16;
    }

    public static UInt32 ToUInt32(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.UInt32;
    }

    public static Int32 ToInt32(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Int32;
    }

    public static UInt64 ToUInt64(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.UInt64;
    }

    public static Int64 ToInt64(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Int64;
    }

    public static Single ToSingle(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Single;
    }

    public static Double ToDouble(byte[] bytes, int offset = 0) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Double;
    }

    public static UInt16 ToUInt16(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.UInt16;
    }

    public static Int16 ToInt16(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      return _c.Int16;
    }

    public static UInt32 ToUInt32(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.UInt32;
    }

    public static Int32 ToInt32(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Int32;
    }

    public static UInt64 ToUInt64(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.UInt64;
    }

    public static Int64 ToInt64(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Int64;
    }

    public static Single ToSingle(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      return _c.Single;
    }

    public static Double ToDouble(byte[] bytes, ref int offset) {
      _c.Byte0 = bytes[offset++];
      _c.Byte1 = bytes[offset++];
      _c.Byte2 = bytes[offset++];
      _c.Byte3 = bytes[offset++];
      _c.Byte4 = bytes[offset++];
      _c.Byte5 = bytes[offset++];
      _c.Byte6 = bytes[offset++];
      _c.Byte7 = bytes[offset++];
      return _c.Double;
    }

    public static void GetBytes(UInt16 value, byte[] bytes, int offset = 0) {
      _c.UInt16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
    }

    public static void GetBytes(Int16 value, byte[] bytes, int offset = 0) {
      _c.Int16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
    }

    public static void GetBytes(UInt32 value, byte[] bytes, int offset = 0) {
      _c.UInt32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(Int32 value, byte[] bytes, int offset = 0) {
      _c.Int32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(UInt64 value, byte[] bytes, int offset = 0) {
      _c.UInt64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

    public static void GetBytes(Int64 value, byte[] bytes, int offset = 0) {
      _c.Int64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

    public static void GetBytes(Single value, byte[] bytes, int offset = 0) {
      _c.Single = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(Double value, byte[] bytes, int offset = 0) {
      _c.Double = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

    public static void GetBytes(UInt16 value, byte[] bytes, ref int offset) {
      _c.UInt16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
    }

    public static void GetBytes(Int16 value, byte[] bytes, ref int offset) {
      _c.Int16 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
    }

    public static void GetBytes(UInt32 value, byte[] bytes, ref int offset) {
      _c.UInt32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(Int32 value, byte[] bytes, ref int offset) {
      _c.Int32 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(UInt64 value, byte[] bytes, ref int offset) {
      _c.UInt64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

    public static void GetBytes(Int64 value, byte[] bytes, ref int offset) {
      _c.Int64 = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

    public static void GetBytes(Single value, byte[] bytes, ref int offset) {
      _c.Single = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
    }

    public static void GetBytes(Double value, byte[] bytes, ref int offset) {
      _c.Double = value;
      bytes[offset++] = _c.Byte0;
      bytes[offset++] = _c.Byte1;
      bytes[offset++] = _c.Byte2;
      bytes[offset++] = _c.Byte3;
      bytes[offset++] = _c.Byte4;
      bytes[offset++] = _c.Byte5;
      bytes[offset++] = _c.Byte6;
      bytes[offset++] = _c.Byte7;
    }

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
  }
}
