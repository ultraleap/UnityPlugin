/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Globalization;

namespace Leap.Unity.Query {

  /// <summary>
  /// Class for extension methods that operate on query wrapper objects.  These 
  /// methods are located in an extension method because they impose additional 
  /// constrains on the general parameters of the query, and so cannot live inside
  /// of the partial class of QueryWrapper.
  /// </summary>
  public static class DirectQueryExtensions {

    public static QueryType Min<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryType : IComparable<QueryType>
      where QueryOp : IQueryOp<QueryType> {
      return wrapper.Fold((a, b) => a.CompareTo(b) < 0 ? a : b);
    }

    public static T Min<QueryType, QueryOp, T>(this QueryWrapper<QueryType, QueryOp> wrapper, Func<QueryType, T> selector)
      where T : IComparable<T>
      where QueryOp : IQueryOp<QueryType> {
      return wrapper.Select(selector).Fold((a, b) => a.CompareTo(b) < 0 ? a : b);
    }

    public static QueryType Max<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryType : IComparable<QueryType>
      where QueryOp : IQueryOp<QueryType> {
      return wrapper.Fold((a, b) => a.CompareTo(b) > 0 ? a : b);
    }

    public static T Max<QueryType, QueryOp, T>(this QueryWrapper<QueryType, QueryOp> wrapper, Func<QueryType, T> selector)
      where T : IComparable<T>
      where QueryOp : IQueryOp<QueryType> {
      return wrapper.Select(selector).Fold((a, b) => a.CompareTo(b) > 0 ? a : b);
    }

    public static QueryWrapper<byte, SelectOp<QueryType, byte, QueryOp>> ToBytes<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toByte);
    }

    public static QueryWrapper<ushort, SelectOp<QueryType, ushort, QueryOp>> ToUShorts<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toUShort);
    }

    public static QueryWrapper<short, SelectOp<QueryType, short, QueryOp>> ToShorts<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toShort);
    }

    public static QueryWrapper<uint, SelectOp<QueryType, uint, QueryOp>> ToUInts<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toUInt);
    }

    public static QueryWrapper<int, SelectOp<QueryType, int, QueryOp>> ToInts<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toInt);
    }

    public static QueryWrapper<ulong, SelectOp<QueryType, ulong, QueryOp>> ToULong<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toULong);
    }

    public static QueryWrapper<long, SelectOp<QueryType, long, QueryOp>> ToLongs<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toLong);
    }

    public static QueryWrapper<float, SelectOp<QueryType, float, QueryOp>> ToFloats<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toFloat);
    }

    public static QueryWrapper<double, SelectOp<QueryType, double, QueryOp>> ToDoubles<QueryType, QueryOp>(this QueryWrapper<QueryType, QueryOp> wrapper)
      where QueryOp : IQueryOp<QueryType>
      where QueryType : IConvertible {
      return wrapper.Select(FormatHelper<QueryType>.toDouble);
    }

    private static class FormatHelper<T> where T : IConvertible {
      private static NumberFormatInfo _numberFormatInfo = NumberFormatInfo.CurrentInfo;

      public static Func<T, byte> toByte;
      public static Func<T, ushort> toUShort;
      public static Func<T, short> toShort;
      public static Func<T, uint> toUInt;
      public static Func<T, int> toInt;
      public static Func<T, ulong> toULong;
      public static Func<T, long> toLong;
      public static Func<T, float> toFloat;
      public static Func<T, double> toDouble;

      static FormatHelper() {
        toByte = t => t.ToByte(_numberFormatInfo);
        toUShort = t => t.ToUInt16(_numberFormatInfo);
        toShort = t => t.ToInt16(_numberFormatInfo);
        toUInt = t => t.ToUInt32(_numberFormatInfo);
        toInt = t => t.ToInt32(_numberFormatInfo);
        toULong = t => t.ToUInt64(_numberFormatInfo);
        toLong = t => t.ToInt64(_numberFormatInfo);
        toFloat = t => t.ToSingle(_numberFormatInfo);
        toDouble = t => t.ToDouble(_numberFormatInfo);
      }
    }
  }
}
