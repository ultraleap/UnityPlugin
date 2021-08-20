/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
namespace Leap {
  /// <summary>
  /// Various C# extensions used by the Leap C# classes.
  ///
  /// @since 3.0
  /// </summary>
  public static class CSharpExtensions {
    /// <summary>
    /// Compares whether two floating point numbers are within an epsilon value of each other.
    /// @since 3.0
    /// </summary>
    public static bool NearlyEquals(this float a, float b, float epsilon = Constants.EPSILON) {
      float absA = Math.Abs(a);
      float absB = Math.Abs(b);
      float diff = Math.Abs(a - b);

      if (a == b) { // shortcut, handles infinities
        return true;
      } else if (a == 0 || b == 0 || diff < float.MinValue) {
        // a or b is zero or both are extremely close to it
        // relative error is less meaningful here
        return diff < (epsilon * float.MinValue);
      } else { // use relative error
        return diff / (absA + absB) < epsilon;
      }
    }

    /// <summary>
    /// Reports whether this object has the specified method.
    /// @since 3.0
    /// </summary>
    public static bool HasMethod(this object objectToCheck, string methodName) {
      var type = objectToCheck.GetType();
      return type.GetMethod(methodName) != null;
    }

    /// <summary>
    /// Returns the ordinal index of this enumeration item.
    /// @since 3.0
    /// </summary>
    public static int indexOf(this Enum enumItem) {
      return Array.IndexOf(Enum.GetValues(enumItem.GetType()), enumItem);
    }

    /// <summary>
    /// Gets the item at the ordinal position in this enumeration.
    /// @since 3.0
    /// </summary>
    public static T itemFor<T>(this int ordinal) {
      T[] values = (T[])Enum.GetValues(typeof(T));
      return values[ordinal];
    }

    /// <summary>
    /// Convenience function to consolidate event dispatching boilerplate code.
    /// @since 3.0
    /// </summary>
    public static void Dispatch<T>(this EventHandler<T> handler,
                                object sender, T eventArgs) where T : EventArgs {
      if (handler != null) handler(sender, eventArgs);
    }

    /// <summary>
    /// Convenience function to consolidate event dispatching boilerplate code.
    /// Events are dispatched on the message queue of a threads' synchronization
    /// context, if possible.
    /// @since 3.0
    /// </summary>
    public static void DispatchOnContext<T>(this EventHandler<T> handler, object sender,
                                System.Threading.SynchronizationContext context,
                                                 T eventArgs) where T : EventArgs {
      if (handler != null) {
        if (context != null) {
          System.Threading.SendOrPostCallback evt = (spc_args) => { handler(sender, spc_args as T); };
          context.Post(evt, eventArgs);
        } else
          handler(sender, eventArgs);
      }
    }
  }
}

