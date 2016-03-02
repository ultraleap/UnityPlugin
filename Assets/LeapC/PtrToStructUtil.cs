using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace LeapInternal {

  public class StructUtil<T> where T : struct {
    [StructLayout(LayoutKind.Sequential)]
    private class StructContainer {
      public T value;
    }

    private static StructContainer _container = new StructContainer();
    private static int _sizeofT = Marshal.SizeOf(typeof(T));

    public static T PtrToStruct(IntPtr ptr) {
      try {
        Marshal.PtrToStructure(ptr, _container);
        return _container.value;
      } catch (Exception e) {
        Logger.Log("Problem converting structure " + typeof(T) + " from ptr " + ptr + " : " + e.Message);
        return new T();
      }
    }

    public static T ArrayElementToStruct(IntPtr ptr, int arrayIndex) {
      return PtrToStruct(new IntPtr(ptr.ToInt64() + _sizeofT * arrayIndex));
    }

  }
}
