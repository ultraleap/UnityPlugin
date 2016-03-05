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

    private static Stack<IntPtr> _tempPtrPool = new Stack<IntPtr>();
    private static List<IntPtr> _allocatedPtrs = new List<IntPtr>();

    private static IntPtr _tempArray;
    private static int _tempArrayCount;

    /**
     * Converts a single element in an array pointed to by ptr to a struct
     * of type T.  This method does not and cannot do any bounds checking!
     * This method does not create any garbage.
     */
    public static T ArrayElementToStruct(IntPtr ptr, int arrayIndex) {
      return PtrToStruct(new IntPtr(ptr.ToInt64() + _sizeofT * arrayIndex));
    }

    public static IntPtr GetTempArray(int count) {
      if (count > _tempArrayCount) {
        if (_tempArrayCount != 0) {
          Marshal.FreeHGlobal(_tempArray);
        }

        _tempArray = Marshal.AllocHGlobal(_sizeofT * count);
        _tempArrayCount = count;
      }

      return _tempArray;
    }

    public static IntPtr AllocNewTemp(T t) {
      IntPtr ptr;
      if (_tempPtrPool.Count != 0) {
        ptr = _tempPtrPool.Pop();
      } else {
        ptr = Marshal.AllocHGlobal(_sizeofT);
      }

      _container.value = t;
      Marshal.StructureToPtr(_container, ptr, false);

      _allocatedPtrs.Add(ptr);
      return ptr;
    }

    public static void ReleaseAllTemp() {
      for (int i = 0; i < _allocatedPtrs.Count; i++) {
        _tempPtrPool.Push(_allocatedPtrs[i]);
      }
      _allocatedPtrs.Clear();
    }
  }
}
