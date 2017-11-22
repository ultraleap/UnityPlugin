/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.Generation {

  public class BitConverterNonAlloc_Template_ {
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
