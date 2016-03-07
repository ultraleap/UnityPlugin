using UnityEngine;
using LeapInternal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class StructAllocator {
  private static List<IntPtr> _allocated = new List<IntPtr>();

  public static void BeginAllocationBlock() {
    for (int i = 0; i < _allocated.Count; i++) {
      Marshal.FreeHGlobal(_allocated[i]);
    }
    _allocated.Clear();
  }

  public static IntPtr AllocateStruct<T>(T t) where T : struct {
    IntPtr ptr = Marshal.AllocHGlobal(StructMarshal<T>.Size);
    StructMarshal<T>.CopyIntoDestination(ptr, t);
    _allocated.Add(ptr);
    return ptr;
  }

  public static IntPtr AllocateArray<T>(int count) where T : struct {
    IntPtr ptr = Marshal.AllocHGlobal(StructMarshal<T>.Size * count);
    _allocated.Add(ptr);
    return ptr;
  }

}
