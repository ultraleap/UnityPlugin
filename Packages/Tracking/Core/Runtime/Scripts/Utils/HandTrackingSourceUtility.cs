/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    public static class HandTrackingSourceUtility
    {
        public static bool LeapOpenXRTrackingAvailable;
        public static bool LeapOpenXRHintingAvailable;

        public static bool NonLeapOpenXRTrackingAvailable;

        private static bool leapCTrackingAvailable;
        public static bool LeapCTrackingAvailable
        {
            get
            {
                return leapCTrackingAvailable = IsLeapCConnectionAvailable();
            }
        }

        private static bool leapCConnectionChecked = false;
        private static bool IsLeapCConnectionAvailable()
        {
            if (leapCConnectionChecked)
            {
                return leapCTrackingAvailable;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if(AndroidServiceBinder.Bind())
            {
                return true;
            }
#endif
            if (LeapInternal.Connection.IsConnectionAvailable())
            {
                return true;
            }


            leapCConnectionChecked = true;
            return false;
        }
    }
}