using System;
using Leap.Unity.Attributes;
using Leap.Unity.Query;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

namespace Leap.Unity.Attachments {

  [ExecuteInEditMode]
  public class HandRayStream : MonoBehaviour,
                               IStream<Ray>,
                               IRuntimeGizmoComponent {

    #region Inspector

    [SerializeField]
    private LeapProvider _provider;
    public LeapProvider provider {
      get {
        if (_provider == null) { _provider = Hands.Provider; }
        return _provider;
      }
    }

    public Chirality whichHand;

    public AttachmentPointFlags attachmentPoint = AttachmentPointFlags.Palm;

    public enum ProjectionMode { FromShoulder, FromElbow, FromHead }
    public ProjectionMode projectionMode = ProjectionMode.FromShoulder;

    private readonly Vector3 _shoulderOffset = new Vector3(0.196f, -0.101f, 0f);
    private readonly Vector3 _elbowOffset = new Vector3(0.196f, -0.269f, 0f);
    private readonly Vector3 _headOffset = new Vector3(0f, 0f, 0f);
    private Vector3 projectionOrigin {
      get {
        if (provider != null) {
          var rootTransform = provider.transform;
          var shouldMirrorX = whichHand == Chirality.Left;
          Vector3 offset;
          switch (projectionMode) {
            case ProjectionMode.FromShoulder:
              offset = _shoulderOffset;
              break;
            case ProjectionMode.FromElbow:
              offset = _elbowOffset;
              break;
            case ProjectionMode.FromHead: default:
              offset = _headOffset;
              break;
          }
          return rootTransform.TransformPoint(offset.MaybeMirroredX(shouldMirrorX));
        }
        else {
          return Vector3.zero;
        }
      }
    }

    public enum StreamMode { Update, FixedUpdate }
    private StreamMode _followMode = StreamMode.Update;
    public StreamMode followMode {
      get {
        return _followMode;
      }
      set {
        if (value != _followMode) {
          unsubscribeFrameCallback();

          _followMode = value;

          subscribeFrameCallback();
        }
      }
    }

    [SerializeField]
    [Disable]
    private bool _isHandTracked = false;
    public bool isHandTracked { get { return _isHandTracked; } }

    [Header("Debug")]
    public bool drawDebug = false;

    private bool _isStreamOpen = false;
    private Ray? _lastRay = null;

    #endregion

    #region Unity Events

    void OnEnable() {
      unsubscribeFrameCallback();
      subscribeFrameCallback();
    }

    void OnDisable() {
      unsubscribeFrameCallback();
    }

    private void Update() {
      if (!Application.isPlaying) {
        moveToAttachmentPointNow();
      }
    }

    #endregion

    #region On Frame Event

    private void onUpdateFrame(Frame frame) {
      if (frame == null) Debug.Log("Frame null");

      var hand = frame.Hands.Query()
                            .FirstOrDefault(h => h.IsLeft == (whichHand == Chirality.Left));

      bool shouldStream = false;
      Ray streamRay = default(Ray);

      if (hand != null) {
        _isHandTracked = true;

        if (enabled && gameObject.activeInHierarchy) {
          Vector3 pointPosition; Quaternion pointRotation;
          AttachmentPointBehaviour.GetLeapHandPointData(hand, attachmentPoint,
                                                        out pointPosition,
                                                        out pointRotation);

          // Replace wrist rotation data with that from the palm for now.
          if (attachmentPoint == AttachmentPointFlags.Wrist) {
            Vector3 unusedPos;
            AttachmentPointBehaviour.GetLeapHandPointData(hand, AttachmentPointFlags.Palm,
                                                          out unusedPos,
                                                          out pointRotation);
          }

          this.transform.position = pointPosition;
          this.transform.rotation = pointRotation;
          
          var origin = projectionOrigin;
          streamRay = new Ray(origin,
                              pointPosition - origin);
          _lastRay = streamRay;
          shouldStream = true;
        }
      }
      else {
        _isHandTracked = false;
      }

      // Ray Stream data.
      shouldStream &= Application.isPlaying;
      shouldStream &= this.enabled && gameObject.activeInHierarchy;
      if (!shouldStream && _isStreamOpen) {
        OnClose();
        _isStreamOpen = false;
      }
      if (shouldStream && !_isStreamOpen) {
        OnOpen();
        _isStreamOpen = true;
      }
      if (shouldStream) {
        OnSend(streamRay);
      }
    }

    #endregion

    #region Frame Subscription

    private void unsubscribeFrameCallback() {
      if (_provider != null) {
        switch (_followMode) {
          case StreamMode.Update:
            Hands.Provider.OnUpdateFrame -= onUpdateFrame;
            break;
          case StreamMode.FixedUpdate:
            Hands.Provider.OnFixedFrame -= onUpdateFrame;
            break;
        }
      }
    }

    private void subscribeFrameCallback() {
      if (_provider != null) {
        switch (_followMode) {
          case StreamMode.Update:
            Hands.Provider.OnUpdateFrame += onUpdateFrame;
            break;
          case StreamMode.FixedUpdate:
            Hands.Provider.OnFixedFrame += onUpdateFrame;
            break;
        }
      }
    }

    #endregion

    #region IStream<Pose>

    public event Action OnOpen = () => { };
    public event Action<Ray> OnSend = (ray) => { };
    public event Action OnClose = () => { };

    #endregion

    #region Editor Methods

    private void moveToAttachmentPointNow() {
      onUpdateFrame(provider.CurrentFrame);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (this.enabled && this.gameObject.activeInHierarchy && drawDebug
          && _lastRay.HasValue) {
        drawer.color = LeapColor.lime;
        drawer.DrawRay(_lastRay.Value.origin, _lastRay.Value.direction);
      }
    }

    #endregion

  }

  public static class HandRayStreamExtensions {

    public static Vector3 MirroredX(this Vector3 v) {
      return new Vector3(-v.x, v.y, v.z);
    }

    public static Vector3 MaybeMirroredX(this Vector3 v, bool shouldMirror) {
      return new Vector3(shouldMirror ? -v.x : v.x, v.y, v.z);
    }

  }

}
