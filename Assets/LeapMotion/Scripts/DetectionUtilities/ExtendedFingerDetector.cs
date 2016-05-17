using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

  /**
   * Detects when specified fingers are in an extended or non-extended state.
   * 
   * You can specify whether each finger is extended, not extended, or in either state.
   * This detector activates when every finger on the observed hand meets these conditions.
   * 
   * If added to a IHandModel instance or one of its children, this detector checks the
   * finger state at the interval specified by the Period variable. You can also specify
   * which hand model to observe explicitly by setting handModel in the Unity editor or 
   * in code.
   * 
   * @since 4.1.2
   */
  public class ExtendedFingerDetector : Detector {
    /**
     * The interval at which to check finger state.
     * @since 4.1.2
     */
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds

    /**
     * The IHandModel instance to observe. 
     * Set automatically if not explicitly set in the editor.
     * @since 4.1.2
     */
    [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
    public IHandModel HandModel = null;
  
    /** The required thumb state. */
    public PointingState Thumb = PointingState.Either;
    /** The required index finger state. */
    public PointingState Index = PointingState.Either;
    /** The required middle finger state. */
    public PointingState Middle = PointingState.Either;
    /** The required ring finger state. */
    public PointingState Ring = PointingState.Either;
    /** The required pinky finger state. */
    public PointingState Pinky = PointingState.Either;
  
    void Start () {
      if(HandModel == null){
        HandModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }
  
    void OnEnable () {
      StartCoroutine(extendedFingerWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(extendedFingerWatcher());
      Deactivate();
    }
  
    IEnumerator extendedFingerWatcher() {
      Hand hand;
      while(true){
        bool fingerState = false;
        if(HandModel != null && HandModel.IsTracked){
          hand = HandModel.GetLeapHand();
          if(hand != null){
            fingerState = matchFingerState(hand.Fingers[0], 0)
              && matchFingerState(hand.Fingers[1], 1)
              && matchFingerState(hand.Fingers[2], 2)
              && matchFingerState(hand.Fingers[3], 3)
              && matchFingerState(hand.Fingers[4], 4);
            if(HandModel.IsTracked && fingerState){
              Activate();
            } else if(!HandModel.IsTracked || !fingerState) {
              Deactivate();
            }
          }
        } else if(IsActive){
          Deactivate();
        }
        yield return new WaitForSeconds(Period);
      }
    }
  
    private bool matchFingerState(Finger finger, int ordinal){
      switch(ordinal){
        case 0:
          if(Thumb == PointingState.Either) return true;
          if(Thumb == PointingState.Extended && finger.IsExtended) return true;
          if(Thumb == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 1:
          if(Index == PointingState.Either) return true;
          if(Index == PointingState.Extended && finger.IsExtended) return true;
          if (Index == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 2:
          if(Middle == PointingState.Either) return true;
          if(Middle == PointingState.Extended && finger.IsExtended) return true;
          if (Middle == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 3:
          if(Ring == PointingState.Either) return true;
          if(Ring == PointingState.Extended && finger.IsExtended) return true;
          if (Ring == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 4:
          if(Pinky == PointingState.Either) return true;
          if(Pinky == PointingState.Extended && finger.IsExtended) return true;
          if (Pinky == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        default:
          return false;
      }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos && HandModel != null) {
        Hand hand = HandModel.GetLeapHand();
        for (int f = 0; f < 5; f++) {
          Finger finger = hand.Fingers[f];
          if (matchFingerState(finger, f)) {
            Gizmos.color = Color.green;
          } else {
            Gizmos.color = Color.red;
          }
          Gizmos.DrawWireSphere(finger.TipPosition.ToVector3(), finger.Width);
        }
      }
    }
    #endif
  }
  
  /** Defines the settings for comparing extended finger states */
  public enum PointingState{Extended, NotExtended, Either}
}
