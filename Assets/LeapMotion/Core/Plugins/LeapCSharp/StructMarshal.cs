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

namespace LeapInternal {

  /**
   * A helper class to marshal between unmanaged memory and structs without creating garbage.
   */
  public static class StructMarshal<T> where T : struct {
#if !ENABLE_IL2CPP
    [StructLayout(LayoutKind.Sequential)]
    private class StructContainer {
      public T value;
    }

    [ThreadStatic]
    private static StructContainer _container;
#endif

    private static int _sizeofT;

    static StructMarshal() {
      _sizeofT = Marshal.SizeOf(typeof(T));
    }

    /**
     * Returns the size in bytes of the struct of type T.  This call is equivalent to
     * Marshal.Sizeof(typeof(T)) but caches the result for ease of access.
     */
    public static int Size {
      get {
        return _sizeofT;
      }
    }

    /**
     * Converts an IntPtr to a struct of type T.
     */
    public static void PtrToStruct(IntPtr ptr, out T t) {
#if ENABLE_IL2CPP
#if UNITY_2018_1_OR_NEWER
      unsafe {
        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyPtrToStructure((void*)ptr, out t);
      }
#else
#error UnityModules Only supports IL2CPP on versions of Unity 2018.1 or greater.
#endif
#else
      if (_container == null) {
        _container = new StructContainer();
      }

      try {
        Marshal.PtrToStructure(ptr, _container);
        t = _container.value;
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
        t = default(T);
      }
#endif
    }

    /**
     * Converts a single element in an array pointed to by ptr to a struct
     * of type T.  This method does not and cannot do any bounds checking!
     * This method does not create any garbage.
     */
    public static void ArrayElementToStruct(IntPtr ptr, int arrayIndex, out T t) {
      PtrToStruct(new IntPtr(ptr.ToInt64() + _sizeofT * arrayIndex), out t);
    }
  }
}
