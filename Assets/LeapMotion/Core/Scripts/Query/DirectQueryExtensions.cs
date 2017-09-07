using System;

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
  }
}
