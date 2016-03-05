using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LeapInternal {

  /**
   * A helper class to marshal from unmanaged memory into structs without creating garbage.
   */
  public static class StructMarshal<T> where T : struct {
    [StructLayout(LayoutKind.Sequential)]
    private class StructContainer {
      public T value;
    }

    private static StructContainer _container;
    private static int _sizeofT;

    static StructMarshal() {
      _container = new StructContainer();
      _sizeofT = Marshal.SizeOf(typeof(T));
    }

    public static int Size {
      get {
        return _sizeofT;
      }
    }

    public static void CopyIntoDestination(IntPtr dstPtr, T t) {
      CopyIntoArray(dstPtr, t, 0);
    }

    public static void CopyIntoArray(IntPtr arrayPtr, T t, int index) {
      _container.value = t;
      Marshal.StructureToPtr(_container, new IntPtr(arrayPtr.ToInt64() + _sizeofT * index), false);
    }

    /**
     * Converts an IntPtr to a struct of type T.
     */
    public static T PtrToStruct(IntPtr ptr) {
      try {
        Marshal.PtrToStructure(ptr, _container);
        return _container.value;
      } catch (Exception e) {
        Logger.Log("Problem converting structure " + typeof(T) + " from ptr " + ptr + " : " + e.Message);
        return new T();
      }
    }

    /**
     * Converts a single element in an array pointed to by ptr to a struct
     * of type T.  This method does not and cannot do any bounds checking!
     * This method does not create any garbage.
     */
    public static T ArrayElementToStruct(IntPtr ptr, int arrayIndex) {
      return PtrToStruct(new IntPtr(ptr.ToInt64() + _sizeofT * arrayIndex));
    }
  }
}
