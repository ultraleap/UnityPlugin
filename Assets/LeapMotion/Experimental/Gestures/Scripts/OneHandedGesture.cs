using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gestures {

  /// <summary>
  /// Abstraction class for a one-handed gesture. One-handed gestures require
  /// a tracked hand to be active; if the hand loses tracking mid-gesture, the
  /// gesture will fire its deactivation method as a cancellation.
  /// 
  /// If either hand can perform the implementing gesture, create a Gesture instance for
  /// each hand. For gestures that require both hands or interactions between
  /// hands to perform, use a TwoHandedGesture.
  /// </summary>
  public abstract class OneHandedGesture : Gesture {

    /// <summary>
    /// What LeapProvider should be used to get hand data?
    /// </summary>
    public LeapProvider provider = null;

    /// <summary>
    /// Which hand does the gesture apply to? If either hand can perform the
    /// gesture, create an instance for each hand.
    /// </summary>
    [EditTimeOnly]
    public Chirality whichHand;

    #if UNITY_EDITOR
    /// <summary>
    /// This editor-only value prevents chirality from being changed when the component
    /// is Reset back to its default values.
    /// </summary>
    [NonSerialized]
    private Chirality? _editorPreviousChirality = null;
    #endif

    #region Public API

    /// <summary>
    /// Whether this gesture is currently active.
    /// </summary>
    public override bool isActive { get { return _isActive; } }

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

    #region TODO: Really consider removing Actions from the system.

    public Action<Hand> OnOneHandedGestureActivated
                              = (hand) => { };

    public Action<Hand> OnOneHandedGestureDeactivated
                              = (maybeNullHand) => { };

    #endregion

    #endregion

    #region Implementer's API

    /// <summary>
    /// Returns whether the gesture should activate this frame.
    /// This method is called once per Update frame if the gesture is currently inactive.
    /// The hand is guaranteed to be non-null.
    /// </summary>
    protected abstract bool ShouldGestureActivate(Hand hand);

    /// <summary>
    /// The gesture will deactivate if this method returns true OR if the hand loses tracking
    /// mid-gesture. The method is called once per Update frame if the gesture is currently
    /// active and the hand is tracked.
    /// 
    /// The deactivationReason will be provided to WhenGestureDeactivated if this method
    /// returns true on a given frame. If a hand lost tracking mid-gesture, the reason for
    /// deactivation will be CancelledGesture.
    /// 
    /// The hand provided to this method is guaranteed to be non-null.
    /// </summary>
    protected abstract bool ShouldGestureDeactivate(Hand hand,
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
     * Hand State contains methods called when the tracked state of the
     * hand changes or while the hand is tracked or untracked.
     */

    #region Gesture State

    /// <summary>
    /// Called when the gesture has just been activated. The hand is guaranteed to
    /// be non-null.
    /// </summary>
    protected virtual void WhenGestureActivated(Hand hand) { }

    /// <summary>
    /// Called when the gesture has just been deactivated. The hand might be null; this
    /// will be true if the hand loses tracking while the gesture is active.
    /// </summary>
    protected virtual void WhenGestureDeactivated(Hand maybeNullHand, DeactivationReason reason) { }

    /// <summary>
    /// Called every Update frame while the gesture is active. The hand is guaranteed to
    /// be non-null.
    /// </summary>
    protected virtual void WhileGestureActive(Hand hand) { }

    /// <summary>
    /// Called every Update frame while the gesture is inactive. the hand might be null;
    /// this will be the case if a hand loses tracking while the gesture is active.
    /// </summary>
    protected virtual void WhileGestureInactive(Hand maybeNullHand) { }

    #endregion

    #region Hand State

    /// <summary>
    /// Called when the hand this gesture pays attention to begins tracking.
    /// </summary>
    protected virtual void WhenHandBecomesTracked(Hand hand) { }

    /// <summary>
    /// Called when the hand this gesture pays attention to loses tracking.
    /// </summary>
    protected virtual void WhenHandLosesTracking(bool wasLeftHand) { }

    /// <summary>
    /// Called every Update frame while the hand this gesture pays attention to
    /// is tracked.
    /// 
    /// Refer to hand.IsLeft to know whether the hand is the left or right hand.
    /// </summary>
    protected virtual void WhileHandTracked(Hand hand) { }

    /// <summary>
    /// Called every Update frame while the hand this gesture pays attention to
    /// is NOT tracked.
    /// 
    /// The provided boolean indicates whether this gesture is paying attention to
    /// the left hand. (Otherwise, it is paying attention to the right hand.)
    /// </summary>
    protected virtual void WhileHandUntracked(bool isLeftHand) { }

    #endregion

    #endregion

    #region Base Implementation (Unity Callbacks)

    private bool _isActive = false;
    private bool _isHandTracked = false;
    private bool _wasHandTracked = false;
    
    protected bool _wasActivated = false;
    protected bool _wasDeactivated = false;
    protected bool _wasCancelled = false;
    protected bool _wasFinished = false;

    public bool isHandTracked { get { return _isHandTracked; } }
    
    public override bool isEligible {
      get { return _isHandTracked; }
    }

    protected virtual void Reset() {
      if (provider == null) provider = Hands.Provider;

      #if UNITY_EDITOR
      if (_editorPreviousChirality.HasValue) {
        whichHand = _editorPreviousChirality.Value;
      }
      #endif
    }

    protected virtual void OnValidate() {
      #if UNITY_EDITOR
      _editorPreviousChirality = whichHand;
      #endif
    }

    protected virtual void OnDisable() {
      if (_isActive) {
        var hand = Hands.Get(whichHand);
        WhenGestureDeactivated(hand, DeactivationReason.CancelledGesture);
        _isActive = false;
      }
    }

    protected virtual void Start() {
      if (provider == null) provider = Hands.Provider;
    }

    protected virtual void Update() {
      if (provider == null) {
        Debug.LogError("No provider set for " + this.GetType().Name
          + " in " + this.name + "; you should sure that there is a LeapProvider in "
          + "your scene and that this component has a LeapProvider reference.", this);
        this.enabled = false;
      }

      _wasActivated = false;
      _wasDeactivated = false;
      _wasCancelled = false;
      _wasFinished = false;

      var hand = provider.Get(whichHand);

      // Determine the tracked state of hands and fire appropriate methods.
      _isHandTracked = hand != null;

      if (isHandTracked != _wasHandTracked) {
        if (isHandTracked) {
          WhenHandBecomesTracked(hand);
        }
        else {
          WhenHandLosesTracking(whichHand == Chirality.Left);
        }
      }

      if (isHandTracked) {
        WhileHandTracked(hand);
      }
      else {
        WhileHandUntracked(whichHand == Chirality.Left);
      }

      _wasHandTracked = isHandTracked;

      // Determine whether or not the gesture should be active or inactive.
      bool shouldGestureBeActive;
      DeactivationReason? deactivationReason = null;
      if (!isHandTracked) {
        shouldGestureBeActive = false;
      }
      else {
        if (!_isActive) {
          if (ShouldGestureActivate(hand)) {
            shouldGestureBeActive = true;
          }
          else {
            shouldGestureBeActive = false;
          }
        }
        else {
          if (ShouldGestureDeactivate(hand, out deactivationReason)) {
            shouldGestureBeActive = false;
          }
          else {
            shouldGestureBeActive = true;
          }
        }
      }

      // If the deactivation reason is not set, we assume the gesture completed
      // successfully.
      bool wasGestureSuccessful = deactivationReason.GetValueOrDefault()
                                  == DeactivationReason.FinishedGesture;

      // Fire gesture state change events.
      if (shouldGestureBeActive != _isActive) {
        if (shouldGestureBeActive) {
          _wasActivated = true;
          _isActive = true;

          WhenGestureActivated(hand);
          OnGestureActivated();
          OnOneHandedGestureActivated(hand);
        }
        else {
          _wasDeactivated = true;
          if (wasGestureSuccessful) {
            _wasFinished = true;
          }
          else {
            _wasCancelled = true;
          }
          _isActive = false;

          WhenGestureDeactivated(hand, deactivationReason.GetValueOrDefault());
          OnGestureDeactivated();
          OnOneHandedGestureDeactivated(hand);
        }
      }

      // Fire per-update events.
      if (_isActive) {
        WhileGestureActive(hand);
      }
      else {
        WhileGestureInactive(hand);
      }
    }

    #endregion

  }

}