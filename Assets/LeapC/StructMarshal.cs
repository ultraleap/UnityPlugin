using System;
using System.Runtime.InteropServices;

namespace LeapInternal {

  /**
   * A helper class to marshal from unmanaged memory into structs without creating garbage.
   */
  public class StructMarshal<T> where T : struct {
    [StructLayout(LayoutKind.Sequential)]
    private class StructContainer {
      public T value;
    }

    private static StructContainer _container = new StructContainer();
    private static int _sizeofT = Marshal.SizeOf(typeof(T));

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
