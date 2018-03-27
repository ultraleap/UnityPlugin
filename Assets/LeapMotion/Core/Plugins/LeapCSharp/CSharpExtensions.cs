/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
namespace Leap
{
  /**
   * Various C# extensions used by the Leap C# classes.
   *
   * @since 3.0
   */
  public static class CSharpExtensions
  {
    /**
     * Compares whether two floating point numbers are within an epsilon value of each other.
     * @since 3.0
     */
    public static bool NearlyEquals(this float a, float b, float epsilon = Constants.EPSILON)
    {
      float absA = Math.Abs(a);
      float absB = Math.Abs(b);
      float diff = Math.Abs(a - b);

      if (a == b)
      { // shortcut, handles infinities
        return true;
      }
      else if (a == 0 || b == 0 || diff < float.MinValue)
      {
        // a or b is zero or both are extremely close to it
        // relative error is less meaningful here
        return diff < (epsilon * float.MinValue);
      }
      else { // use relative error
        return diff / (absA + absB) < epsilon;
      }
    }

    /**
     * Reports whether this object has the specified method.
     * @since 3.0
     */
    public static bool HasMethod(this object objectToCheck, string methodName)
    {
      var type = objectToCheck.GetType();
      return type.GetMethod(methodName) != null;
    }

    /**
     * Returns the ordinal index of this enumeration item.
     * @since 3.0
     */
    public static int indexOf(this Enum enumItem)
    {
      return Array.IndexOf(Enum.GetValues(enumItem.GetType()), enumItem);
    }

    /**
     * Gets the item at the ordinal position in this enumeration.
     * @since 3.0
     */
    public static T itemFor<T>(this int ordinal)
    {
      T[] values = (T[])Enum.GetValues(typeof(T));
      return values[ordinal];
    }

    /**
     * Convenience function to consolidate event dispatching boilerplate code.
     * @since 3.0
     */
    public static void Dispatch<T>(this EventHandler<T> handler,
                                object sender, T eventArgs) where T : EventArgs
    {
      if (handler != null) handler(sender, eventArgs);
    }

    /**
     * Convenience function to consolidate event dispatching boilerplate code.
     * Events are dispatched on the message queue of a threads' synchronization
     * context, if possible.
     * @since 3.0
     */
    public static void DispatchOnContext<T>(this EventHandler<T> handler, object sender,
                                System.Threading.SynchronizationContext context,
                                                 T eventArgs) where T : EventArgs
    {
      if (handler != null)
      {
        if (context != null)
        {
          System.Threading.SendOrPostCallback evt = (spc_args) => { handler(sender, spc_args as T); };
          context.Post(evt, eventArgs);
        }
        else
          handler(sender, eventArgs);
      }
    }
  }
}

