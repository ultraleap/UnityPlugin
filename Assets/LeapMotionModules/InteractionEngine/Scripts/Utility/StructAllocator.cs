using LeapInternal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Leap.Unity.Interaction {

  public static class StructAllocator {
    [ThreadStatic]
    private static List<IntPtr> _allocated;

    public static void CleanupAllocations() {
      if (_allocated == null) {
        _allocated = new List<IntPtr>();
      }

      for (int i = 0; i < _allocated.Count; i++) {
        Marshal.FreeHGlobal(_allocated[i]);
      }
      _allocated.Clear();
    }

    public static IntPtr AllocateStruct<T>(ref T t) where T : struct {
      if (_allocated == null) {
        _allocated = new List<IntPtr>();
      }

      IntPtr ptr = Marshal.AllocHGlobal(StructMarshal<T>.Size);
      StructMarshal<T>.CopyIntoDestination(ptr, ref t);
      _allocated.Add(ptr);
      return ptr;
    }

    public static IntPtr AllocateArray<T>(int count) where T : struct {
      if (_allocated == null) {
        _allocated = new List<IntPtr>();
      }

      IntPtr ptr = Marshal.AllocHGlobal(StructMarshal<T>.Size * count);
      _allocated.Add(ptr);
      return ptr;
    }

    public static IntPtr AllocateArray<T>(T[] data) where T : struct {
      if (_allocated == null) {
        _allocated = new List<IntPtr>();
      }

      IntPtr ptr = Marshal.AllocHGlobal(StructMarshal<T>.Size * data.Length);
      _allocated.Add(ptr);
      for (int i = 0; i < data.Length; i++) {
        T value = data[i];
        StructMarshal<T>.CopyIntoArray(ptr, ref value, i);
      }
      return ptr;
    }
  }
}
