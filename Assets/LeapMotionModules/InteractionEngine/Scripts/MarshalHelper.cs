using UnityEngine;
using System;
using System.Runtime.InteropServices;

public static class MarshalHelper<T> where T : struct {

  [StructLayout(LayoutKind.Sequential)]
  private class ContainerClass {
    public T value;
  }

  private static int _sizeofT = Marshal.SizeOf(typeof(T));
  private static ContainerClass _container = new ContainerClass();

  public static T GetStruct(IntPtr pointer) {
    Marshal.PtrToStructure(pointer, _container);
    return _container.value;
  }

  public static void GetStructArray(IntPtr pointer, T[] toFill, int count) {
    for (int i = 0; i < count; i++) {
      toFill[i] = GetStruct(pointer);
      pointer = (IntPtr)((int)pointer + _sizeofT);
    }
  }

}
