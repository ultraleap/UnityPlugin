/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap
{
    public interface IController :
      IDisposable
    {
        Frame Frame(int history = 0);
        Frame GetTransformedFrame(LeapTransform trs, int history = 0);
        Frame GetInterpolatedFrame(Int64 time);

        void SetPolicy(Controller.PolicyFlag policy, Device device = null);
        void ClearPolicy(Controller.PolicyFlag policy, Device device = null);
        bool IsPolicySet(Controller.PolicyFlag policy, Device device = null);

        long Now();

        bool IsConnected { get; }
        Config Config { get; }
        DeviceList Devices { get; }

        event EventHandler<ConnectionEventArgs> Connect;
        event EventHandler<ConnectionLostEventArgs> Disconnect;
        event EventHandler<FrameEventArgs> FrameReady;
        event EventHandler<DeviceEventArgs> Device;
        event EventHandler<DeviceEventArgs> DeviceLost;
        event EventHandler<DeviceFailureEventArgs> DeviceFailure;
        event EventHandler<LogEventArgs> LogMessage;

        //new
        event EventHandler<PolicyEventArgs> PolicyChange;
        event EventHandler<ConfigChangeEventArgs> ConfigChange;
        event EventHandler<DistortionEventArgs> DistortionChange;
        event EventHandler<ImageEventArgs> ImageReady;
        event EventHandler<PointMappingChangeEventArgs> PointMappingChange;
        event EventHandler<HeadPoseEventArgs> HeadPoseChange;
    }
}