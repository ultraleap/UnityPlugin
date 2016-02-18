/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;

namespace Leap {
    interface IController :IDisposable{
         Frame Frame(int history = 0);
         void SetPolicy(Controller.PolicyFlag policy);
         void ClearPolicy(Controller.PolicyFlag policy);
         bool IsPolicySet(Controller.PolicyFlag policy);
         long Now();
         bool IsConnected {get;}
         bool IsServiceConnected {get;}
         bool HasFocus {get;}
         Config Config {get;}
//         ImageList Images {get;}
         DeviceList Devices {get;}
         TrackedQuad TrackedQuad {get;}

         event EventHandler<LeapEventArgs> Init;
         event EventHandler<ConnectionEventArgs> Connect;
         event EventHandler<ConnectionLostEventArgs> Disconnect;
         event EventHandler<LeapEventArgs> Exit;
         event EventHandler<FrameEventArgs> FrameReady;
         event EventHandler<LeapEventArgs> FocusGained;
         event EventHandler<LeapEventArgs> FocusLost;
         event EventHandler<LeapEventArgs> ServiceConnect;
         event EventHandler<LeapEventArgs> ServiceDisconnect;
         event EventHandler<DeviceEventArgs> Device;
         event EventHandler<DeviceEventArgs> DeviceLost;
         event EventHandler<ImageEventArgs> ImageReady;
         event EventHandler<LeapEventArgs> ServiceChange;
         event EventHandler<DeviceFailureEventArgs> DeviceFailure;
         event EventHandler<LogEventArgs> LogMessage;
        
        //new
         event EventHandler<PolicyEventArgs> PolicyChange;
         event EventHandler<ConfigChangeEventArgs> ConfigChange;
         event EventHandler<DistortionEventArgs> DistortionChange;
         event EventHandler<TrackedQuadEventArgs> TrackedQuadReady;
    }
}

