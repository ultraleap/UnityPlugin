using JetBrains.Annotations;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace Ultraleap.Tracking.OpenXR.Interop
{
    delegate XrResult GetInstanceProcAddrDelegate(XrInstance instance, in string name, out IntPtr function);

    delegate XrResult WaitFrameDelegate(XrSession session, in XrFrameWaitInfo frameWaitInfo, XrFrameState frameState);

    delegate XrResult GetSystemPropertiesDelegate(XrInstance instance, in XrSystemId systemId,
        XrSystemProperties systemProperties);

    delegate XrResult CreateHandTrackerExtDelegate(XrSession session, in XrHandTrackerCreateInfoExt createInfo,
        out XrHandTrackerExt handTracker);

    delegate XrResult DestroyHandTrackerExtDelegate(in XrHandTrackerExt handTracker);

    delegate XrResult LocateHandJointsExtDelegate(XrHandTrackerExt handTracker, in XrHandJointsLocateInfoExt locateInfo,
        XrHandJointLocationsExt jointLocations);

    delegate XrResult SetHandTrackingHintsUltraleapDelegate(in string[] hints, uint hintsLength);
    

    public static class XrResultExtensions
    {
        public static bool Succeeded(this XrResult result) => result >= 0;
        public static bool IsUnqualifiedSuccess(this XrResult result) => result == XrResult.Success;
        public static bool Failed(this XrResult result) => result < 0;
    }

    public struct XrInstance : IEquatable<ulong>
    {
        private readonly ulong _raw;

        public bool IsNull => _raw == 0;

        public XrInstance(ulong value) => _raw = value;
        public static implicit operator ulong(XrInstance equatable) => equatable._raw;
        public static implicit operator XrInstance(ulong value) => new(value);

        public bool Equals(XrInstance other) => _raw == other._raw;
        public bool Equals(ulong other) => _raw == other;
        public override bool Equals(object obj) => obj is XrInstance instance && Equals(instance);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrInstance a, XrInstance b) => a.Equals(b);
        public static bool operator !=(XrInstance a, XrInstance b) => !a.Equals(b);
    }

    public struct XrSession : IEquatable<ulong>
    {
        private readonly ulong _raw;

        public XrSession(ulong value) => _raw = value;
        public static implicit operator ulong(XrSession equatable) => equatable._raw;
        public static implicit operator XrSession(ulong value) => new(value);

        public bool Equals(XrSession other) => _raw == other._raw;
        public bool Equals(ulong other) => _raw == other;
        public override bool Equals(object obj) => obj is XrSession session && Equals(session);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrSession a, XrSession b) => a.Equals(b);
        public static bool operator !=(XrSession a, XrSession b) => !a.Equals(b);
    }

    public struct XrSystemId : IEquatable<ulong>
    {
        private readonly ulong _raw;

        public XrSystemId(ulong value) => _raw = value;
        public static implicit operator ulong(XrSystemId equatable) => equatable._raw;
        public static implicit operator XrSystemId(ulong value) => new(value);

        public bool Equals(XrSystemId other) => _raw == other._raw;
        public bool Equals(ulong other) => _raw == other;
        public override bool Equals(object obj) => obj is XrSystemId systemId && Equals(systemId);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrSystemId a, XrSystemId b) => a.Equals(b);
        public static bool operator !=(XrSystemId a, XrSystemId b) => !a.Equals(b);
    }


    public struct XrSpace : IEquatable<ulong>
    {
        private readonly ulong _raw;

        public XrSpace(ulong value) => _raw = value;
        public static implicit operator ulong(XrSpace equatable) => equatable._raw;
        public static implicit operator XrSpace(ulong value) => new(value);

        public bool Equals(XrSpace other) => _raw == other._raw;
        public bool Equals(ulong other) => _raw == other;
        public override bool Equals(object obj) => obj is XrSpace space && Equals(space);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrSpace a, XrSpace b) => a.Equals(b);
        public static bool operator !=(XrSpace a, XrSpace b) => !a.Equals(b);
    }
    
    public struct XrHandTrackerExt : IEquatable<ulong>
    {
        private readonly ulong _raw;

        public XrHandTrackerExt(ulong value) => _raw = value;
        public static implicit operator ulong(XrHandTrackerExt equatable) => equatable._raw;
        public static implicit operator XrHandTrackerExt(ulong value) => new(value);

        public bool Equals(XrHandTrackerExt other) => _raw == other._raw;
        public bool Equals(ulong other) => _raw == other;
        public override bool Equals(object obj) => obj is XrHandTrackerExt handTracker && Equals(handTracker);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrHandTrackerExt a, XrHandTrackerExt b) => a.Equals(b);
        public static bool operator !=(XrHandTrackerExt a, XrHandTrackerExt b) => !a.Equals(b);
    }

    public struct XrTime : IEquatable<long>
    {
        internal readonly long _raw;

        public XrTime(long value) => _raw = value;
        public static implicit operator long(XrTime equatable) => equatable._raw;
        public static implicit operator XrTime(long value) => new(value);

        public bool Equals(XrTime other) => _raw == other._raw;
        public bool Equals(long other) => _raw == other;
        public override bool Equals(object obj) => obj is XrTime time && Equals(time);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrTime a, XrTime b) => a.Equals(b);
        public static bool operator !=(XrTime a, XrTime b) => !a.Equals(b);
        public static bool operator >=(XrTime a, XrTime b) => a._raw >= b._raw;
        public static bool operator <=(XrTime a, XrTime b) => a._raw <= b._raw;
        public static bool operator >(XrTime a, XrTime b) => a._raw > b._raw;
        public static bool operator <(XrTime a, XrTime b) => a._raw < b._raw;
        public static XrTime operator +(XrTime a, XrDuration b) => a._raw + b._raw;
        public static XrDuration operator -(XrTime a, XrTime b) => a._raw - b._raw;
        public static XrTime operator *(XrTime a, float b) => (XrTime)(a._raw * b);
        public static float operator /(XrTime a, XrDuration b) => (float)a._raw / b._raw;
    }

    public struct XrDuration : IEquatable<long>
    {
        internal readonly long _raw;

        public XrDuration(long value) => _raw = value;
        public static implicit operator long(XrDuration equatable) => equatable._raw;
        public static implicit operator XrDuration(long value) => new(value);

        public bool Equals(XrDuration other) => _raw == other._raw;
        public bool Equals(long other) => _raw == other;
        public override bool Equals(object obj) => obj is XrDuration time && Equals(time);
        public override int GetHashCode() => _raw.GetHashCode();
        public override string ToString() => _raw.ToString();
        public static bool operator ==(XrDuration a, XrDuration b) => a.Equals(b);
        public static bool operator !=(XrDuration a, XrDuration b) => !a.Equals(b);
        public static bool operator >=(XrDuration a, XrDuration b) => a._raw >= b._raw;
        public static bool operator <=(XrDuration a, XrDuration b) => a._raw <= b._raw;
        public static bool operator >(XrDuration a, XrDuration b) => a._raw > b._raw;
        public static bool operator <(XrDuration a, XrDuration b) => a._raw < b._raw;
        public static XrDuration operator +(XrDuration a, XrDuration b) => a._raw + b._raw;
        public static XrDuration operator -(XrDuration a, XrDuration b) => a._raw - b._raw;
        public static XrDuration operator *(XrDuration a, XrDuration b) => a._raw * b._raw;
        public static XrDuration operator /(XrDuration a, XrDuration b) => a._raw / b._raw;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum XrStructureType
    {
        Unknown = 0,
        SystemProperties = 5,
        SystemHandTrackingProperties = 1000051000,
        HandTrackerCreateInfoExt = 1000051001,
        HandJointsLocateInfoExt = 1000051002,
        HandJointLocationsExt = 1000051003,
        HandJointVelocitiesExt = 1000051004,
    }
    
    public interface IXrExtendable
    {
        public XrStructureType Type { get; }
        public IntPtr Next { get; set; }
    }
    
    public struct XrSystemGraphicsProperties
    {
        public uint MaxSwapchainImageHeight;
        public uint MaxSwapchainImageWidth;
        public uint MaxLayerCount;
    }
  
    
    public struct XrSystemTrackingProperties
    {
        public bool OrientationTracking;
        public bool PositionTracking;
    }

    public struct XrSystemProperties : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public XrSystemId SystemId;
        [UsedImplicitly] public uint VendorId;

        [UsedImplicitly, MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string SystemName;

        [UsedImplicitly] public XrSystemGraphicsProperties GraphicsProperties;
        [UsedImplicitly] public XrSystemTrackingProperties TrackingProperties;
        
        public XrSystemProperties(IntPtr next) : this()
        {
            Type = XrStructureType.SystemProperties;
            Next = next;
        }
    }

    public struct XrSystemHandTrackingPropertiesExt : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public bool SupportsHandTracking;

        public XrSystemHandTrackingPropertiesExt(IntPtr next) : this()
        {
            Type = XrStructureType.SystemHandTrackingProperties;
            Next = next;
        }
    }
    
    public struct XrHandTrackerCreateInfoExt : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public XrHandExt Hand;
        [UsedImplicitly]public XrHandJointSetExt HandJointSet;

        public XrHandTrackerCreateInfoExt(XrHandExt hand, XrHandJointSetExt handJointSet) : this()
        {
            Type = XrStructureType.HandTrackerCreateInfoExt;
            Next = IntPtr.Zero;
            Hand = hand;
            HandJointSet = handJointSet;
        }
    }
    
    public struct XrFrameWaitInfo : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
    }

    public struct XrFrameState : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public XrTime PredictedDisplayTime;
        [UsedImplicitly] public XrDuration PredictedDisplayPeriod;
        [UsedImplicitly] public bool ShouldRender;
    }
    
    public enum XrHandExt
    {
        Left = 1,
        Right = 2,
    }
    
    public enum XrHandJointSetExt
    {
        Default = 0,
        HandWithForearm = 1000149000,
    }

    public struct XrHandJointsLocateInfoExt : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public XrSpace BaseSpace;
        [UsedImplicitly] public XrTime Time;
        
        public XrHandJointsLocateInfoExt(IntPtr next) : this()
        {
            Type = XrStructureType.HandTrackerCreateInfoExt;
            Next = next;
        }
    }

    public struct XrHandJointLocationsExt : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }

        [UsedImplicitly, MarshalAs(UnmanagedType.U8)]
        public bool IsActive;

        [UsedImplicitly] public uint JointCount;
        [UsedImplicitly] public IntPtr JointLocationsPtr;

        public XrHandJointLocationsExt(IntPtr next) : this()
        {
            Type = XrStructureType.HandJointLocationsExt;
            Next = next;
        }
    }

    public struct XrHandJointLocationExt
    {
        [UsedImplicitly] public XrSpaceLocationFlags LocationFlags;
        [UsedImplicitly] public XrPosef Pose;
        [UsedImplicitly] public float Radius;
        
        [UsedImplicitly] public bool IsValid => PositionValid && OrientationValid;
        [UsedImplicitly] public bool IsTracked => OrientationTracked && PositionTracked;

        private bool PositionValid => LocationFlags.HasFlag(XrSpaceLocationFlags.PositionValid);
        private bool OrientationValid => LocationFlags.HasFlag(XrSpaceLocationFlags.OrientationValid);
        private bool PositionTracked => LocationFlags.HasFlag(XrSpaceLocationFlags.PositionTracked);
        private bool OrientationTracked => LocationFlags.HasFlag(XrSpaceLocationFlags.OrientationTracked);
    }
    
    public struct XrHandJointVelocitiesExt : IXrExtendable
    {
        [UsedImplicitly] public XrStructureType Type { get; set; }
        [UsedImplicitly] public IntPtr Next { get; set; }
        [UsedImplicitly] public uint JointCount;
        [UsedImplicitly] public IntPtr JointVelocitiesPtr;
        
        public XrHandJointVelocitiesExt(IntPtr next) : this()
        {
            Type = XrStructureType.HandJointVelocitiesExt;
            Next = next;
        }
    }

    public struct XrHandJointVelocityExt
    {
        [UsedImplicitly] public XrSpaceVelocityFlags VelocityFlags;
        [UsedImplicitly] public XrVector3f LinearVelocity;
        [UsedImplicitly] public XrVector3f AngularVelocity;
        
        [UsedImplicitly] public bool IsLinearValid => VelocityFlags.HasFlag(XrSpaceVelocityFlags.LinearValid);
        [UsedImplicitly] public bool IsAngularValid => VelocityFlags.HasFlag(XrSpaceVelocityFlags.AngularValid);
    }
    
    public enum XrSpaceLocationFlags : ulong
    {
        OrientationValid = 0x1,
        PositionValid = 0x2,
        OrientationTracked = 0x4,
        PositionTracked = 0x8,
    }
    
    public enum XrSpaceVelocityFlags : ulong
    {
        LinearValid = 0x1,
        AngularValid = 0x2,
    }

    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct XrVector3f
    {
        [UsedImplicitly] public float X;
        [UsedImplicitly] public float Y;
        [UsedImplicitly] public float Z;

        public XrVector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = -z;
        }
        
        public static implicit operator Vector3(XrVector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public XrVector3f(Vector3 value)
        {
            X = value.x;
            Y = value.y;
            Z = -value.z;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct XrQuaternionf
    {
        [UsedImplicitly] public float X;
        [UsedImplicitly] public float Y;
        [UsedImplicitly] public float Z;
        [UsedImplicitly] public float W;

        public XrQuaternionf(float x, float y, float z, float w)
        {
            X = -x;
            Y = -y;
            Z = z;
            W = w;
        }
        
        public static implicit operator Quaternion(XrQuaternionf value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }

        public XrQuaternionf(Quaternion quaternion)
        {
            X = -quaternion.x;
            Y = -quaternion.y;
            Z = quaternion.z;
            W = quaternion.w;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct XrPosef
    {
        [UsedImplicitly] public XrQuaternionf Orientation;
        [UsedImplicitly] public XrVector3f Position;

        public XrPosef(Vector3 vec3, Quaternion quaternion)
        {
            Position = new XrVector3f(vec3);
            Orientation = new XrQuaternionf(quaternion);
        }
    }
}