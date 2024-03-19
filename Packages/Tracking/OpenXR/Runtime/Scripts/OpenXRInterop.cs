using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace Ultraleap.Tracking.OpenXR.Interop
{
    delegate XrResult GetInstanceProcAddrDelegate(XrInstance instance, in string name, out IntPtr function);
    delegate XrResult WaitFrameDelegate(XrSession session, in XrFrameWaitInfo frameWaitInfo, out XrFrameState frameState);
    delegate XrResult GetSystemPropertiesDelegate(XrInstance instance, in XrSystemId systemId, XrSystemProperties systemProperties);
    delegate XrResult CreateHandTrackerExtDelegate(XrSession session, in XrHandTrackerCreateInfoExt createInfo, out XrHandTrackerExt handTracker);
    delegate XrResult DestroyHandTrackerExtDelegate(in XrHandTrackerExt handTracker);
    

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
        public static float operator /(XrTime a, XrDuration b) => (float)a._raw / (float)b._raw;
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
    
    public struct XrSystemProperties
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrSystemId SystemId;
        public uint VendorId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string SystemName;
        public XrSystemGraphicsProperties GraphicsProperties;
        public XrSystemTrackingProperties TrackingProperties;
    }
    
    public struct XrSystemHandTrackingPropertiesExt
    {
        public XrStructureType Type;
        public IntPtr Next;
        public bool SupportsHandTracking;
    }
    
    public struct XrHandTrackerCreateInfoExt
    {
        public readonly XrStructureType Type;
        public IntPtr Next;
        public XrHandExt Hand;
        public XrHandJointSetExt HandJointSet;

        public XrHandTrackerCreateInfoExt(XrHandExt hand, XrHandJointSetExt handJointSet) : this()
        {
            Type = XrStructureType.HandTrackerCreateInfoExt;
            Next = IntPtr.Zero;
            Hand = hand;
            HandJointSet = handJointSet;
        }
    }
    
    public struct XrFrameWaitInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
    }

    public struct XrFrameState
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrTime PredictedDisplayTime;
        public XrDuration PredictedDisplayPeriod;
        public bool ShouldRender;
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
}