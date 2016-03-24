using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Leap;

namespace Leap.Unity {
  /**LeapServiceProvider creates a Controller and supplies Leap Hands and images */
  public class LeapServiceProvider : LeapProvider {
    /** Conversion factor for nanoseconds to seconds. */
    protected const float NS_TO_S = 1e-6f;
    /** Conversion factor for seconds to nanoseconds. */
    protected const float S_TO_NS = 1e6f;
    /** How much smoothing to use when calculating the FixedUpdate offset. */
    protected const float FIXED_UPDATE_OFFSET_SMOOTHING_DELAY = 0.1f;

    [Tooltip("Set true if the Leap Motion hardware is mounted on an HMD; otherwise, leave false.")]
    [SerializeField]
    protected bool _isHeadMounted = false;

    [SerializeField]
    protected bool overrideDeviceType = false;

    [Tooltip("If overrideDeviceType is enabled, the hand controller will return a device of this type.")]
    [SerializeField]
    protected LeapDeviceType _overrideDeviceTypeWith = LeapDeviceType.Peripheral;

    protected Controller leap_controller_;

    protected Frame _currentFrame;
    protected Image _currentImage;
    protected int _currentUpdateCount = -1;

    protected Frame _currentFixedFrame;
    protected float _currentFixedTime = -1;
    protected float _perFrameFixedUpdateOffset = -1;
    /* The smoothed offset between the FixedUpdate timeline and the Leap timeline.  
     * Used to provide temporally correct frames within FixedUpdate */
    protected SmoothedFloat _smoothedFixedUpdateOffset = new SmoothedFloat();
    protected float _initialFrameOffset = 0f;

    private ClockCorrelator clockCorrelator;

    public override Frame CurrentFrame {
      get {
        ensureCurrentFrameAndImageUpToDate();
        return _currentFrame;
      }
    }

    public override Image CurrentImage {
      get {
        ensureCurrentFrameAndImageUpToDate();
        return _currentImage;
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        ensureCurrentFixedFrameUpToDate();
        return _currentFixedFrame;
      }
    }

    /** Returns the Leap Controller instance. */
    public Controller GetLeapController() {
#if UNITY_EDITOR
      //Null check to deal with hot reloading
      if (leap_controller_ == null) {
        createController();
        Debug.Log("GetLeapController() calling createController()");
      }
#endif
      return leap_controller_;
    }

    /** True, if the Leap Motion hardware is plugged in and this application is connected to the Leap Motion service. */
    public bool IsConnected() {
      return GetLeapController().IsConnected;
    }

    /** Returns information describing the device hardware. */
    public LeapDeviceInfo GetDeviceInfo() {
      if (overrideDeviceType) {
        return new LeapDeviceInfo(_overrideDeviceTypeWith);
      }

      DeviceList devices = GetLeapController().Devices;
      if (devices.Count == 1) {
        LeapDeviceInfo info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
        // TODO: DeviceList does not tell us the device type. Dragonfly serial starts with "LE" and peripheral starts with "LP"
        if (devices[0].SerialNumber.Length >= 2) {
          switch (devices[0].SerialNumber.Substring(0, 2)) {
            case ("LP"):
              info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
              break;
            case ("LE"):
              info = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
              break;
            default:
              break;
          }
        }

        // TODO: Add baseline & offset when included in API
        // NOTE: Alternative is to use device type since all parameters are invariant
        info.isEmbedded = devices[0].IsEmbedded;
        info.horizontalViewAngle = devices[0].HorizontalViewAngle * Mathf.Rad2Deg;
        info.verticalViewAngle = devices[0].VerticalViewAngle * Mathf.Rad2Deg;
        info.trackingRange = devices[0].Range / 1000f;
        info.serialID = devices[0].SerialNumber;
        return info;
      } else if (devices.Count > 1) {
        return new LeapDeviceInfo(LeapDeviceType.Peripheral);
      }
      return new LeapDeviceInfo(LeapDeviceType.Invalid);
    }

    protected virtual void Awake() {
      clockCorrelator = new ClockCorrelator();
      _smoothedFixedUpdateOffset.delay = FIXED_UPDATE_OFFSET_SMOOTHING_DELAY;

    }

    protected virtual void Start() {
      createController();
    }

    protected virtual void Update() {
#if UNITY_EDITOR
      if (EditorApplication.isCompiling) {
        EditorApplication.isPlaying = false;
        Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
      }
#endif
      
      Int64 unityTime = (Int64)(Time.time);
      clockCorrelator.UpdateRebaseEstimate(unityTime);

      Int64 unityFrameTime = (Int64)(CurrentFrame.Timestamp - _initialFrameOffset);
      Int64 LeapFrameTime = clockCorrelator.ExternalClockToLeapTime(unityFrameTime);
 
      if (_perFrameFixedUpdateOffset > 0) {
        _smoothedFixedUpdateOffset.Update(_perFrameFixedUpdateOffset, Time.deltaTime);
        _perFrameFixedUpdateOffset = -1;
      }
    }

    protected virtual void FixedUpdate() {
      _perFrameFixedUpdateOffset = 
        Mathf.Max(
          _perFrameFixedUpdateOffset,
          GetLeapController().Frame().Timestamp * NS_TO_S - Time.fixedTime
        );
    }

    protected virtual void OnDestroy() {
      destroyController();
    }

    protected virtual void OnApplicationPause(bool isPaused) {
      if (leap_controller_ != null) {
        if (isPaused) {
          leap_controller_.StopConnection();
        } else {
          leap_controller_.StartConnection();
        }
      }
    }

    protected virtual void OnApplicationQuit() {
      destroyController();
    }

    /*
     * Initializes the Leap Motion policy flags.
     * The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
     */
    protected void initializeFlags() {
      //Optimize for top-down tracking if on head mounted display.
      if (_isHeadMounted) {
        leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      } else {
        leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      }
    }
    /** Create an instance of a Controller, initialize its policy flags
     * and subscribe to connection event */
    protected void createController() {
      if (leap_controller_ != null) {
        Debug.Log("Did destroy?");
        destroyController();
      }

      leap_controller_ = new Controller();
      if (leap_controller_.IsConnected) {
        initializeFlags();
      }
      leap_controller_.Device += onHandControllerConnect;
    }

    /** Calling this method stop the connection for the existing instance of a Controller, 
     * clears old policy flags and resets to null */
    protected void destroyController() {
      if (leap_controller_ != null) {
        if (leap_controller_.IsConnected) {
          leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
        }
        leap_controller_.StopConnection();
        leap_controller_ = null;
      }
    }


    protected void onHandControllerConnect(object sender, LeapEventArgs args) {
      initializeFlags();
      leap_controller_.Device -= onHandControllerConnect;
    }

    /* Calling this method updates _currentFrame and _currentImage if and only if this
     * is the first time the method has been called for this Update cycle.
     */
    protected void ensureCurrentFrameAndImageUpToDate() {
      if (Time.frameCount == _currentUpdateCount) {
        return;
      }
      _currentUpdateCount = Time.frameCount;

      var leapMat = UnityMatrixExtension.GetLeapMatrix(transform);
      _currentFrame = GetLeapController().GetTransformedFrame(leapMat, 0);
    }

    /* Calling this method updates _currentFixedFrame if and only if this is the first time
     * the method has been called for this FixedUpdate burst.
     */
    protected void ensureCurrentFixedFrameUpToDate() {
      if (Time.fixedTime == _currentFixedTime) {
        return;
      }
      _currentFixedTime = Time.fixedTime;

      //Aproximate the correct timestamp given the current fixed time
      long correctedTimestamp = (long)((Time.fixedTime + _smoothedFixedUpdateOffset.value) * S_TO_NS);
  
      //Search the leap history for a frame with a timestamp closest to the corrected timestamp
      Frame closestFrame = GetLeapController().Frame();
      if ((correctedTimestamp < closestFrame.Timestamp)) {
        _smoothedFixedUpdateOffset.reset = true;
      }

      for (int searchHistoryIndex = 1; searchHistoryIndex < 60; searchHistoryIndex++) {
        Frame historyFrame = GetLeapController().Frame(searchHistoryIndex);
        //If we reach an invalid frame, terminate the search
        if (historyFrame.Id < 0) {
          Debug.LogWarning("historyFrame.Id was less than 0!");
          break;
        }

        /** If the history frame is closer, replace closestFrame with the historyFrame */
        if (Math.Abs(historyFrame.Timestamp - correctedTimestamp) < Math.Abs(closestFrame.Timestamp - correctedTimestamp)) {
          closestFrame = historyFrame;
        } else {
          //Since frames are always reported in order, we can terminate the search once we stop finding a closer frame
          break;
        }
      }
      var leapMat = UnityMatrixExtension.GetLeapMatrix(transform);
      _currentFixedFrame = closestFrame.TransformedCopy(leapMat);
    }
  }
}
