using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gestures {

  /// <summary>
  /// Abstraction class for a two-handed gesture. Two-handed gestures require two
  /// tracked hands to be active; if a hand loses tracking mid-gesture, the gesture will
  /// fire its deactivation method as a cancellation.
  /// 
  /// For gestures that only require one hand (and possibly, for example, an object),
  /// see OneHandedGesture.
  /// </summary>
  [ExecuteInEditMode]
  public abstract class TwoHandedGesture : Gesture {

    /// <summary>
    /// What LeapProvider should be used to get hand data?
    /// </summary>
    public LeapProvider provider = null;

    #region Public API

    /// <summary>
    /// Whether this gesture is currently active.
    /// </summary>
    public override bool isActive { get { return _isActive; } }

    /// <summary>
    /// The left hand performing the gesture. If the hand is null, it has lost tracking,
    /// and the gesture will be deactivated (reason: cancellation).
    /// </summary>
    public Hand leftHand { get { return _lHand; } }

    /// <summary>
    /// The right hand performing the gesture. If the hand is null, it has lost tracking,
    /// and the gesture will be deactivated (reason: cancellation).
    /// </summary>
    public Hand rightHand { get { return _rHand; } }

    /// <summary>
    /// Whether this gesture was activated this frame.
    /// </summary>
    public override bool wasActivated { get { return _wasActivated; } }

    /// <summary>
    /// Whether this gesture was deactivated this frame. For some gestures, it may be
    /// necessary to differentiate between a successfully completed gesture and a
    /// cancelled gesture -- for this distinction, refer to wasCancelled and wasFinished.
    /// </summary>
    public override bool wasDeactivated { get { return _wasDeactivated; } }

    /// <summary>
    /// Whether this gesture was deactivated via cancellation this frame.
    /// 
    /// 'Cancelled' implies the gesture was not finished successfully, or was
    /// deliberately halted so as to not enact the result of the gesture. For example,
    /// a currently active gesture is cancelled when a hand performing the gesture
    /// loses tracking. This distinction is important when a gesture is a trigger of some
    /// kind.
    /// </summary>
    public override bool wasCancelled { get { return _wasCancelled; } }

    /// <summary>
    /// Whether this gesture was deactivated by being successfully completed this frame.
    /// 
    /// 'Finished' implies the gesture was successful, as opposed to cancellation. This
    /// distinction is important when a gesture is a trigger for some behavior.
    /// </summary>
    public override bool wasFinished { get { return _wasFinished; } }

    #region TODO: Seriously consider removing Actions from the system.

    public Action<Hand, Hand> OnTwoHandedGestureActivated
                              = (leftHand, rightHand) => { };

    public Action<Hand, Hand> OnTwoHandedGestureDeactivated
                              = (maybeNullLeftHand, maybeNullRightHand) => { };

    #endregion

    #endregion

    #region Implementer's API

    /// <summary>
    /// Returns whether the gesture should activate this frame.
    /// This method is called once per Update frame if the gesture is currently inactive.
    /// The hands are guaranteed to be non-null.
    /// </summary>
    protected abstract bool ShouldGestureActivate(Hand leftHand, Hand rightHand);

    /// <summary>
    /// A two-handed gesture will deactivate if this method returns true OR if one of
    /// the hands loses tracking mid-gesture. The method is called once per Update frame
    /// if the gesture is currently active.
    /// 
    /// The deactivationReason will be provided to WhenGestureDeactivated if this method
    /// returns true on a given frame. If a hand lost tracking mid-gesture, the reason
    /// for deactivation will be CancelledGesture.
    /// 
    /// The hands provided to this method are guaranteed to be non-null.
    /// </summary>
    protected abstract bool ShouldGestureDeactivate(Hand leftHand, Hand rightHand,
                                                    out DeactivationReason? deactivationReason);

    /* Gesture / Hand State
     * 
     * The overrideable methods provided below are thorough, but optional.
     * Depending on the nature of the gesture to be implemented, it is likely that
     * only a subset of the methods provided will be necessary to override.
     * 
     * Gesture State contains methods called when the activation state of the
     * gesture changes and methods called while the gesture is active or inactive
     * on a given frame.
     * 
     * Hand State contains methods called per-hand when the tracked state of the
     * hand changes or while the hand is tracked or untracked.
     */

    #region Gesture State

    /// <summary>
    /// Called when the gesture has just been activated. Hands are guaranteed to be
    /// non-null.
    /// </summary>
    protected virtual void WhenGestureActivated(Hand leftHand, Hand rightHand) { }

    /// <summary>
    /// Called when the gesture has just been deactivated. Hands might be null; this
    /// will be true if a hand loses tracking while the gesture is active.
    /// </summary>
    protected virtual void WhenGestureDeactivated(Hand maybeNullLeftHand, Hand maybeNullRightHand, DeactivationReason reason) { }

    /// <summary>
    /// Called every Update frame while the gesture is active. Hands are guaranteed to
    /// be non-null.
    /// </summary>
    protected virtual void WhileGestureActive(Hand leftHand, Hand rightHand) { }

    /// <summary>
    /// Called every Update frame while the gesture is inactive. The hands might be null;
    /// this will be the case if a hand loses tracking while the gesture is active.
    /// </summary>
    protected virtual void WhileGestureInactive(Hand maybeNullLeftHand, Hand maybeNullRightHand) { }

    #endregion

    #region Hand State

    /// <summary>
    /// Called for the left hand if the left hand has started tracking,
    /// or for the right hand if the right hand has started tracking.
    /// 
    /// Refer to hand.IsLeft to know whether the hand is the left hand
    /// or the right hand.
    /// </summary>
    protected virtual void WhenHandBecomesTracked(Hand hand) { }

    /// <summary>
    /// Called for the left hand if the left hand has lost tracking,
    /// or for the right hand if the right hand has lost tracking.
    /// </summary>
    protected virtual void WhenHandLosesTracking(bool wasLeftHand) { }

    /// <summary>
    /// Called every Update frame while both hands are tracked.
    /// </summary>
    protected virtual void WhileBothHandsTracked(Hand leftHand, Hand rightHand) { }

    /// <summary>
    /// Called every Update frame while a hand is tracked.
    /// 
    /// Refer to hand.IsLeft to know whether the hand is the left hand
    /// or the right hand.
    /// </summary>
    protected virtual void WhileHandTracked(Hand hand) { }

    /// <summary>
    /// Called every Update frame while the hand is not tracked;
    /// called twice if both hands are not tracked, once for the left,
    /// and once for the right (with isLeftHand set accordingly).
    /// </summary>
    protected virtual void WhileHandUntracked(bool wasLeftHand) { }

    #endregion

    #endregion

    #region Base Implementation (Unity Callbacks)

    private Hand _lHand;
    private Hand _rHand;

    private bool _isActive = false;

    private bool _isLeftHandTracked = false;
    private bool _isRightHandTracked = false;
    protected bool isLeftHandTracked { get { return _isLeftHandTracked; } }
    protected bool isRightHandTracked { get { return _isRightHandTracked; } }

    private bool _wasLeftTracked = false;
    private bool _wasRightTracked = false;

    protected bool _wasActivated = false;
    protected bool _wasDeactivated = false;
    protected bool _wasCancelled = false;
    protected bool _wasFinished = false;

    public override bool isEligible {
      get { return isLeftHandTracked && isRightHandTracked; }
    }

    protected virtual void OnDisable() {
      if (_isActive) {
        WhenGestureDeactivated(_lHand, _rHand,
          DeactivationReason.CancelledGesture);
        _isActive = false;
      }
    }

    protected virtual void Start() {
      if (Application.isPlaying) {
        initUnityEvents();

        if (provider == null) {
          provider = Hands.Provider;
        }
        if (provider != null) {
          provider.OnUpdateFrame += onUpdateFrame;
        }
      }
      else {
        refreshEditorHands();
      }
    }

    protected virtual void Update() {
      if (!Application.isPlaying) {
        refreshEditorHands();
      }
    }

    private void refreshEditorHands() {
      if (provider != null) {
        var frame = provider.CurrentFrame;

        if (frame.Hands != null) {
          _lHand = frame.Hands.Query().FirstOrDefault(h =>  h.IsLeft);
          _rHand = frame.Hands.Query().FirstOrDefault(h => !h.IsLeft);
        }
      }
      else {
        _lHand = null;
        _rHand = null;
      }
    }

    protected virtual void onUpdateFrame(Frame frame) {

      // Initialize frame state.
      _wasActivated = false;
      _wasDeactivated = false;
      _wasCancelled = false;
      _wasFinished = false;

      // Get hand data.
      _lHand = frame.Hands.Query().FirstOrDefault(h =>  h.IsLeft);
      _rHand = frame.Hands.Query().FirstOrDefault(h => !h.IsLeft);

      // Determine the tracked state of hands and fire appropriate methods.
      _isLeftHandTracked  = _lHand != null;
      _isRightHandTracked = _rHand != null;

      if (isLeftHandTracked != _wasLeftTracked) {
        if (isLeftHandTracked) {
          WhenHandBecomesTracked(_lHand);
        }
        else {
          WhenHandLosesTracking(true);
        }
      }
      if (isRightHandTracked != _wasRightTracked) {
        if (isRightHandTracked) {
          WhenHandBecomesTracked(_rHand);
        }
        else {
          WhenHandLosesTracking(false);
        }
      }

      if (isLeftHandTracked) {
        WhileHandTracked(_lHand);
      }
      else {
        WhileHandUntracked(true);
      }
      if (isRightHandTracked) {
        WhileHandTracked(_rHand);
      }
      else {
        WhileHandUntracked(false);
      }

      if (isLeftHandTracked && isRightHandTracked) {
        WhileBothHandsTracked(_lHand, _rHand);
      }
      _wasLeftTracked  = isLeftHandTracked;
      _wasRightTracked = isRightHandTracked;

      // Determine whether or not the gesture should be active or inactive.
      bool shouldGestureBeActive;
      DeactivationReason? deactivationReason = null;
      if (_lHand == null || _rHand == null) {
        shouldGestureBeActive = false;
      }
      else {
        if (!_isActive) {
          if (ShouldGestureActivate(_lHand, _rHand)) {
            shouldGestureBeActive = true;
          }
          else {
            shouldGestureBeActive = false;
          }
        }
        else {
          if (ShouldGestureDeactivate(_lHand, _rHand, out deactivationReason)) {
            shouldGestureBeActive = false;
          }
          else {
            shouldGestureBeActive = true;
          }
        }
      }

      bool wasGestureSuccessful = deactivationReason.GetValueOrDefault()
                                  == DeactivationReason.FinishedGesture;

      // Fire gesture state change events.
      if (shouldGestureBeActive != _isActive) {
        if (shouldGestureBeActive) {
          _isActive = true;
          _wasActivated = true;

          WhenGestureActivated(_lHand, _rHand);
          OnGestureActivated();
          OnTwoHandedGestureActivated(_lHand, _rHand);
        }
        else {
          _isActive = false;
          _wasDeactivated = true;
          if (wasGestureSuccessful) {
            _wasFinished = true;
          }
          else {
            _wasCancelled = true;
          }

          WhenGestureDeactivated(_lHand, _rHand, deactivationReason.GetValueOrDefault());
          OnGestureDeactivated();
          OnTwoHandedGestureDeactivated(_lHand, _rHand);
        }
      }

      // Fire per-update events.
      if (_isActive) {
        WhileGestureActive(_lHand, _rHand);
      }
      else {
        WhileGestureInactive(_lHand, _rHand);
      }

    }

    #region Unity Events

    [SerializeField]
    private EnumEventTable _eventTable = new EnumEventTable();

    public enum EventType {
      GestureActivated = 100,
      GestureDeactivated = 110,
    }

    private void initUnityEvents() {
      setupCallback(ref OnGestureActivated, EventType.GestureActivated);
      setupCallback(ref OnGestureDeactivated, EventType.GestureDeactivated);
    }

    private void setupCallback(ref Action action, EventType type) {
      if (_eventTable.HasUnityEvent((int)type)) {
        action += () => _eventTable.Invoke((int)type);
      }
      else {
        action += () => { };
      }
    }


    private void setupCallback<T, U>(ref Action<T, U> action, EventType type) {
      if (_eventTable.HasUnityEvent((int)type)) {
        action += (lh, rh) => _eventTable.Invoke((int)type);
      }
      else {
        action += (lh, rh) => { };
      }
    }

    #endregion

    #endregion

  }

}
