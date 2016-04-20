using System;
using System.Runtime.InteropServices;

namespace LeapInternal {

  /**
   * A helper class to marshal between unmanaged memory and structs without creating garbage.
   */
  public static class StructMarshal<T> where T : struct {
    [StructLayout(LayoutKind.Sequential)]
    private class StructContainer {
      public T value;
    }

    [ThreadStatic]
    private static StructContainer _container;
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
     * Copies a struct of type T into the memory pointed to by dstPtr.  This is an 
     * unsafe operation that assumes there is enough space allocated at the pointer
     * to accommodate the struct.
     */
    public static void CopyIntoDestination(IntPtr dstPtr, ref T t) {
      CopyIntoArray(dstPtr, ref t, 0);
    }

    /**
     * Copies a struct of type T into the array pointed to by arrayPtr at the 
     * offset index specified by indexx.  This is an unsafe operation that assumes
     * there is enough space allocated in the array to accommodate the struct.
     */
    public static void CopyIntoArray(IntPtr arrayPtr, ref T t, int index) {
      if (_container == null) {
        _container = new StructContainer();
      }

      _container.value = t;
      Marshal.StructureToPtr(_container, new IntPtr(arrayPtr.ToInt64() + _sizeofT * index), false);
    }

    /**
     * Converts an IntPtr to a struct of type T.
     */
    public static void PtrToStruct(IntPtr ptr, out T t) {
      if (_container == null) {
        _container = new StructContainer();
      }

      try {
        Marshal.PtrToStructure(ptr, _container);
        t = _container.value;
      } catch (Exception e) {
        Logger.Log("Problem converting structure " + typeof(T) + " from ptr " + ptr + " : " + e.Message);
        t = default(T);
      }
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
