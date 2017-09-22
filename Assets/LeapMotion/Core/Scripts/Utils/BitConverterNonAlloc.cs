using System.Runtime.InteropServices;

public static class BitConverterNonAlloc {
  private static ConverterHelper _c;

  public static ushort ToUInt16(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    return _c._ushort;
  }

  public static ushort ToUInt16(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    return _c._ushort;
  }

  public static short ToInt16(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    return _c._short;
  }

  public static short ToInt16(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    return _c._short;
  }

  public static uint ToUInt32(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._uint;
  }

  public static uint ToUInt32(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._uint;
  }

  public static int ToInt32(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._int;
  }

  public static int ToInt32(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._int;
  }

  public static ulong ToUInt64(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._ulong;
  }

  public static ulong ToUInt64(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._ulong;
  }

  public static long ToInt64(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._long;
  }

  public static long ToInt64(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._long;
  }

  public static float ToSingle(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._float;
  }

  public static float ToSingle(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    return _c._float;
  }

  public static double ToDouble(byte[] bytes, ref int offset) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._double;
  }

  public static double ToDouble(byte[] bytes, int offset = 0) {
    _c._byte0 = bytes[offset++];
    _c._byte1 = bytes[offset++];
    _c._byte2 = bytes[offset++];
    _c._byte3 = bytes[offset++];
    _c._byte4 = bytes[offset++];
    _c._byte5 = bytes[offset++];
    _c._byte6 = bytes[offset++];
    _c._byte7 = bytes[offset++];
    return _c._double;
  }

  public static void GetBytes(ushort value, byte[] bytes, ref int offset) {
    _c._ushort = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
  }

  public static void GetBytes(ushort value, byte[] bytes, int offset = 0) {
    _c._ushort = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
  }

  public static void GetBytes(short value, byte[] bytes, ref int offset) {
    _c._short = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
  }

  public static void GetBytes(short value, byte[] bytes, int offset = 0) {
    _c._short = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
  }

  public static void GetBytes(uint value, byte[] bytes, ref int offset) {
    _c._uint = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(uint value, byte[] bytes, int offset = 0) {
    _c._uint = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(int value, byte[] bytes, ref int offset) {
    _c._int = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(int value, byte[] bytes, int offset = 0) {
    _c._int = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(ulong value, byte[] bytes, ref int offset) {
    _c._ulong = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  public static void GetBytes(ulong value, byte[] bytes, int offset = 0) {
    _c._ulong = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  public static void GetBytes(long value, byte[] bytes, ref int offset) {
    _c._long = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  public static void GetBytes(long value, byte[] bytes, int offset = 0) {
    _c._long = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  public static void GetBytes(float value, byte[] bytes, ref int offset) {
    _c._float = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(float value, byte[] bytes, int offset = 0) {
    _c._float = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
  }

  public static void GetBytes(double value, byte[] bytes, ref int offset) {
    _c._double = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  public static void GetBytes(double value, byte[] bytes, int offset = 0) {
    _c._double = value;
    bytes[offset++] = _c._byte0;
    bytes[offset++] = _c._byte1;
    bytes[offset++] = _c._byte2;
    bytes[offset++] = _c._byte3;
    bytes[offset++] = _c._byte4;
    bytes[offset++] = _c._byte5;
    bytes[offset++] = _c._byte6;
    bytes[offset++] = _c._byte7;
  }

  [StructLayout(LayoutKind.Explicit)]
  private struct ConverterHelper {
    [FieldOffset(0)]
    public byte _byte0;

    [FieldOffset(1)]
    public byte _byte1;

    [FieldOffset(2)]
    public byte _byte2;

    [FieldOffset(3)]
    public byte _byte3;

    [FieldOffset(4)]
    public byte _byte4;

    [FieldOffset(5)]
    public byte _byte5;

    [FieldOffset(6)]
    public byte _byte6;

    [FieldOffset(7)]
    public byte _byte7;

    [FieldOffset(0)]
    public ushort _ushort;

    [FieldOffset(0)]
    public short _short;

    [FieldOffset(0)]
    public uint _uint;

    [FieldOffset(0)]
    public int _int;

    [FieldOffset(0)]
    public ulong _ulong;

    [FieldOffset(0)]
    public long _long;

    [FieldOffset(0)]
    public float _float;

    [FieldOffset(0)]
    public double _double;
  }
}
