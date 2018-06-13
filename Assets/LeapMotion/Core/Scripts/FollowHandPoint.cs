using System;
using Leap.Unity.Attributes;
using Leap.Unity.Query;
using UnityEngine;

namespace Leap.Unity.Attachments {
  
  [ExecuteInEditMode]
  public class FollowHandPoint : MonoBehaviour, IStream<Pose> {

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

    public enum FollowMode { Update, FixedUpdate }
    [SerializeField, OnEditorChange("followMode")]
    private FollowMode _followMode = FollowMode.Update;
    public FollowMode followMode {
      get {
        return _followMode;
      }
      set {
        if (value != _followMode) {
          if (Application.isPlaying) {
            unsubscribeFrameCallback();
          }

          _followMode = value;

          if (Application.isPlaying) {
            subscribeFrameCallback();
          }
        }
      }
    }

    [SerializeField]
    [Disable]
    private bool _isHandTracked = false;
    public bool isHandTracked { get { return _isHandTracked; } }

    [Header("Advanced")]

    public bool showAdvancedSettings = false;

    public bool usePalmRotationForWrist = false;

    [Header("Pose Stream")]

    [Tooltip("Follow Hand Point implements IStream<Pose>; It will stream data as long as "
          + "the component is enabled, the hand is tracked, and this option is enabled.")]
    [QuickButton("Send Stream Now", "sendStreamNow",
      tooltip: "Open, send pose, and close the stream immediately. Edit-time only.")]
    public bool doPoseStream = true;

    [DisableIf("doPoseStream", isEqualTo: false)]
    public bool usePoseStreamOffset = false;

    [DisableIfAny("usePoseStreamOffset", "doPoseStream", areEqualTo: false)]
    public Transform poseStreamOffsetSource = null;

    [Disable]
    public Pose poseStreamOffset = Pose.identity;

    private bool _isStreamOpen = false;

    #endregion

    #region Unity Events

    private void OnValidate() {
      if (!usePoseStreamOffset) {
        poseStreamOffset = Pose.identity;
      }
      else if (poseStreamOffsetSource != null) {
        poseStreamOffset = transform.ToWorldPose().inverse
                           * poseStreamOffsetSource.ToWorldPose();
      }
    }

    void OnEnable() {
      unsubscribeFrameCallback();
      subscribeFrameCallback();
    }

    void OnDisable() {
      if (_isStreamOpen) {
        OnClose();
        _isStreamOpen = false;
      }

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
      Pose streamPose = Pose.identity;
      
      if (hand != null) {
        _isHandTracked = true;

        if (enabled && gameObject.activeInHierarchy
            && attachmentPoint.IsSinglePoint()) {
          Vector3 pointPosition; Quaternion pointRotation;
          AttachmentPointBehaviour.GetLeapHandPointData(hand, attachmentPoint,
                                                        out pointPosition,
                                                        out pointRotation);

          // Replace wrist rotation data with that from the palm for now.
          if (attachmentPoint == AttachmentPointFlags.Wrist
              && usePalmRotationForWrist) {
            Vector3 unusedPos;
            AttachmentPointBehaviour.GetLeapHandPointData(hand, AttachmentPointFlags.Palm,
                                                          out unusedPos,
                                                          out pointRotation);
          }
          if (attachmentPoint == AttachmentPointFlags.Wrist) {
            // TODO: Fix wrist position for edit-time hands.
            // Artificially shift the wrist position down because for some reason it's
            // not so great.
            if (!Application.isPlaying) {
              pointPosition -= pointRotation * Vector3.forward * 0.03f;
            }
          }

          this.transform.position = pointPosition;
          this.transform.rotation = pointRotation;

          streamPose = new Pose(pointPosition, pointRotation);
          var streamOffset = Pose.identity;
          if (usePoseStreamOffset && poseStreamOffsetSource != null) {
            streamOffset = streamPose.inverse
                           * poseStreamOffsetSource.transform.ToWorldPose();
          }
          streamPose = streamPose * streamOffset;
          shouldStream = true;
        }
      }
      else {
        _isHandTracked = false;
      }

      // Pose Stream data.
      shouldStream &= doPoseStream;
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
        OnSend(streamPose);
      }
    }

    #endregion

    #region Frame Subscription

    private void unsubscribeFrameCallback() {
      if (_provider != null) {
        switch (_followMode) {
          case FollowMode.Update:
            Hands.Provider.OnUpdateFrame -= onUpdateFrame;
            break;
          case FollowMode.FixedUpdate:
            Hands.Provider.OnFixedFrame -= onUpdateFrame;
            break;
        }
      }
    }

    private void subscribeFrameCallback() {
      if (_provider != null) {
        switch (_followMode) {
          case FollowMode.Update:
            Hands.Provider.OnUpdateFrame += onUpdateFrame;
            break;
          case FollowMode.FixedUpdate:
            Hands.Provider.OnFixedFrame += onUpdateFrame;
            break;
        }
      }
    }

    #endregion

    #region IStream<Pose>
    
    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    #endregion

    #region Editor Methods

    private void moveToAttachmentPointNow() {
      onUpdateFrame(provider.CurrentFrame);
    }

    private void sendStreamNow() {
      if (!Application.isPlaying) {
        if (_isStreamOpen) {
          OnClose();
          _isStreamOpen = false;
        }
        OnOpen();
        OnSend(this.transform.ToPose());
        OnClose();
      }
    }

    #endregion

  }

}
