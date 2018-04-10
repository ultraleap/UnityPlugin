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
    private static ConversionStruct _c = new ConversionStruct();

    //BEGIN TO

    public static Single ToSingle(byte[] bytes, int offset = 0) {
      //FILL BYTES
      return _c.Single;
    }
    //END
    //BEGIN TO

    public static Single ToSingle(byte[] bytes, ref int offset) {
      //FILL BYTES
      return _c.Single;
    }
    //END
    //BEGIN GET

    public static void GetBytes(Single value, byte[] bytes, int offset = 0) {
      _c.Single = value;
      //FILL BYTES
    }
    //END
    //BEGIN GET

    public static void GetBytes(Single value, byte[] bytes, ref int offset) {
      _c.Single = value;
      //FILL BYTES
    }
    //END

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
