/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap {

using System;
using System.Runtime.InteropServices;

    public enum LeapEvent {
        EVENT_CONNECTION,        //!< A connection event has occurred
        EVENT_CONNECTION_LOST,   //!< The connection with the service has been lost
        EVENT_DEVICE,            //!<  A device event has occurred
        EVENT_DEVICE_FAILURE,    //!< A device failure event has occurred
        EVENT_DEVICE_LOST,       //!< Event asserted when the underlying device object has been lost
        EVENT_POLICY_CHANGE,     //!< A change in policy occurred
        EVENT_CONFIG_RESPONSE,   //!< Response to a Config value request
        EVENT_CONFIG_CHANGE,     //!< Success response to a Config value change
        EVENT_FRAME,             //!< A tracking frame has been received
        EVENT_IMAGE,             //!< A requested image is available
        EVENT_IMAGE_REQUEST_FAILED, //!< A requested image could not be provided
        EVENT_DISTORTION_CHANGE, //!< The distortion matrix used for image correction has changed
        EVENT_TRACKED_QUAD,      //!< A new tracked quad has been received
        EVENT_LOG_EVENT,         //!< A diagnostic event has occured
        EVENT_INIT,
    };

    public class LeapEventArgs : EventArgs{
        public LeapEventArgs(LeapEvent type){
            this.type = type;
        }
        public LeapEvent type {get; set;}
    }
    
    public class FrameEventArgs : LeapEventArgs
    {
        public FrameEventArgs (Frame frame) : base(LeapEvent.EVENT_FRAME)
        {
            this.frame = frame;
        }
        
        public Frame frame{ get; set; }
    }
    
    public class ImageEventArgs : LeapEventArgs
    {
        public ImageEventArgs (Image image) : base(LeapEvent.EVENT_IMAGE)
        {
            this.image = image;
        }
        
        public Image image{ get; set; }
    }
    public class ImageRequestFailedEventArgs : LeapEventArgs{
        public ImageRequestFailedEventArgs(Int64 frameId, Image.ImageType imageType) : base(LeapEvent.EVENT_IMAGE_REQUEST_FAILED)
        {
            this.frameId = frameId;
            this.imageType = imageType;
        }

        public ImageRequestFailedEventArgs(Int64 frameId, Image.ImageType imageType,
                                           Image.RequestFailureReason reason,
                                           string message, 
                                           Int64 requiredBufferSize
        ) : base(LeapEvent.EVENT_IMAGE_REQUEST_FAILED)
        {
            this.frameId = frameId;
            this.imageType = imageType;
            this.reason = reason;
            this.message = message;
            this.requiredBufferSize = requiredBufferSize;
        }

        public Int64 frameId{get; set;}
        public Image.ImageType imageType{get; set;}
        public Image.RequestFailureReason reason{get; set;}
        public string message{get; set;}
        public Int64 requiredBufferSize{get; set;}

    }

    public class LogEventArgs : LeapEventArgs
    {
        public LogEventArgs (MessageSeverity severity, Int64 timestamp, string message) : base(LeapEvent.EVENT_LOG_EVENT)
        {
            this.severity = severity;
            this.message = message;
            this.timestamp = timestamp;
        }
        
        public MessageSeverity severity{ get; set; }
        public Int64 timestamp{ get; set; }
        public string message{ get; set; }
    }
    
    public class PolicyEventArgs : LeapEventArgs
    {
        public PolicyEventArgs (UInt64 currentPolicies, UInt64 oldPolicies) : base(LeapEvent.EVENT_POLICY_CHANGE)
        {
            this.currentPolicies = currentPolicies;
            this.oldPolicies = oldPolicies;
        }
        
        public UInt64 currentPolicies{ get; set; }
        public UInt64 oldPolicies{ get; set; }
    }
    
    public class TrackedQuadEventArgs : LeapEventArgs
    {
        public TrackedQuadEventArgs (TrackedQuad quad) : base(LeapEvent.EVENT_TRACKED_QUAD)
        {
            trackedQuad = quad;
        }
        
        public TrackedQuad trackedQuad{ get; set; }
    }

    public class DistortionEventArgs : LeapEventArgs
    {
        public DistortionEventArgs(DistortionData distortion):base(LeapEvent.EVENT_DISTORTION_CHANGE){
            this.distortion = distortion;
        }
        public DistortionData distortion{get; set;}
    }

    public class ConfigChangeEventArgs : LeapEventArgs
    {
        public ConfigChangeEventArgs(string config_key, bool succeeded, uint requestId):base(LeapEvent.EVENT_CONFIG_CHANGE){
            this.ConfigKey = config_key;
            this.Succeeded = succeeded;
            this.RequestId = requestId;
        }
        public string ConfigKey{get; set;}
        public bool Succeeded{get; set;}
        public uint RequestId{get; set;}

    }

    public class SetConfigResponseEventArgs : LeapEventArgs
    {
        public SetConfigResponseEventArgs(string config_key, Config.ValueType dataType, object value, uint requestId):base(LeapEvent.EVENT_CONFIG_RESPONSE){
            this.ConfigKey = config_key;
            this.DataType = dataType;
            this.Value = value;
            this.RequestId = requestId;
        }
        public string ConfigKey{get; set;}
        public Config.ValueType DataType{get; set;}
        public object Value{get; set;}
        public uint RequestId{get; set;}
    }

    public class ConnectionEventArgs : LeapEventArgs
    {
        public ConnectionEventArgs():base(LeapEvent.EVENT_CONNECTION){}
    }

    public class ConnectionLostEventArgs : LeapEventArgs
    {
        public ConnectionLostEventArgs():base(LeapEvent.EVENT_CONNECTION_LOST){}
    }

    public class DeviceEventArgs : LeapEventArgs
    {
        public DeviceEventArgs(Device device):base(LeapEvent.EVENT_DEVICE){
            this.Device = device;
        }
        public Device Device{get; set;}
    }

    public class DeviceFailureEventArgs : LeapEventArgs
    {
        public DeviceFailureEventArgs(uint code, string message, string serial):base(LeapEvent.EVENT_DEVICE_FAILURE){
            ErrorCode = code;
            ErrorMessage = message;
            DeviceSerialNumber = serial;
        }

        public uint ErrorCode{get; set;}
        public string ErrorMessage{get; set;}
        public string DeviceSerialNumber{get; set;}
    }

}
