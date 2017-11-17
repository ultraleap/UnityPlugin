using System;

public static class Enum<T> {
  public static readonly string[] names;
  public static readonly T[] values;

  static Enum() {
    names = (string[])Enum.GetNames(typeof(T));
    values = (T[])Enum.GetValues(typeof(T));
  }
}
