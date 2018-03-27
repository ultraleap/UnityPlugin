/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{

  using System;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;
  using LeapInternal;

  /**
   * The ClockCorrelator class correlates time between the Leap Motion clock and an application clock.
   *
   * Create a ClockCorrelator for each independent application clock.
   * @since 3.x.y
   */

  public class ClockCorrelator : IDisposable
  {
    private IntPtr _rebaserHandle = IntPtr.Zero;
    private bool _disposed = false;

    /**
     * Creates a new Clock Correlation object for maintaining a latency-adjusted relationship between the
     * Leap Motion system clock and an external clock.
     *
     * @since 3.x.y
     */
    public ClockCorrelator()
    {
      eLeapRS result = LeapC.CreateClockRebaser(out _rebaserHandle);
      if(result != eLeapRS.eLeapRS_Success)
        throw new Exception (result.ToString());
    }

    /**
    * Updates the estimate of latency between render time
    * and the Leap Motion device time.
    *
    * Call this function when a frame is rendered. Uses the leap clock time
    * at the moment this function is called.
    *
    * @param externalClockTime The time in milliseconds when the graphics frame is rendered.
    * @since 3.x.z
    */
    public void UpdateRebaseEstimate(Int64 applicationClock){
      LeapC.UpdateRebase(_rebaserHandle, applicationClock, LeapC.GetNow());
    }

    /**
    * Updates the estimate of latency between render time
    * and the Leap Motion device time.
    *
    * Call this function when a frame is rendered.
    *
    * @param externalClockTime The time in milliseconds when the graphics frame is rendered.
    * @param leapClock the time in milliseconds obtained by calling Controller.Now().
    * @since 3.x.z
    */
    public void UpdateRebaseEstimate(Int64 applicationClock, Int64 leapClock){
      LeapC.UpdateRebase(_rebaserHandle, applicationClock, leapClock);
    }

    /**
    * Returns the Leap Motion device time corresponding to an external time.
    *
    * For this function to return meaningful results, the UpdateRebaseEstimate() function must be
    * called for each graphics frame rendered.
    *
    * @param externalClockTime The time in milliseconds.
    * @returns Int64 The latency-corrected Leap Motion device time in milliseconds corresponding to
    * the specified external time.
    * @since 3.x.z
    */
    public Int64 ExternalClockToLeapTime(Int64 applicationClock){
      Int64 leapTime;
      LeapC.RebaseClock(_rebaserHandle, applicationClock, out leapTime);
      return leapTime;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      LeapC.DestroyClockRebaser(_rebaserHandle);
      _rebaserHandle = IntPtr.Zero;
      _disposed = true;
    }

    ~ClockCorrelator(){
      Dispose(false);
    }
  }
}
