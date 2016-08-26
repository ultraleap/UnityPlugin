using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;
using UnityEngine.Events;

namespace Leap.Unity.Interaction {
  public class InteractionGraspDetector : Detector {

    [AutoFind(AutoFindLocations.Scene)]
    [Tooltip("The Interaction Manager.")]
    public InteractionManager ieManager = null;
    /**
     * The IHandModel instance to observe. 
     * Set automatically if not explicitly set in the editor.
     * @since 4.1.2
     */
    [AutoFind(AutoFindLocations.Parents)]
    [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
    public IHandModel HandModel = null;

    /**
 * The interval at which to check palm direction.
 * @since 4.1.2
 */
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds
    /**
     * Dispatched when the proximity check succeeds.
     * The ProximityEvent object provides a reference to the proximate GameObject. 
     * @since 4.1.2
     */
    [Tooltip("Dispatched when a target object is grasped.")]
    public InteractiveBehaviorGraspEvent OnGrasp;

    [Tooltip("Activate when any interactable object is grasped.")]
    public bool AnyInteractionObject = false;
    /**
     * The list of IInteractionBehaviour objects which can activate the detector when held.
     * @since 4.1.2
     */
    [Tooltip("The list of target objects.")]
    public IInteractionBehaviour[] TargetObjects;

    /**
    * IInteractiveBehaviour objects with this tag name will activate the detector.
    * @since 4.1.3
    */
    [Tooltip("Activate if the held object has this tag name.")]
    public string TagName = "";

    /**
     * The object that is currently held.
     * 
     * @since 4.1.2
     */
    public IInteractionBehaviour CurrentObject { get { return _currentObj; } }

    private IEnumerator watcherCoroutine;
    private IInteractionBehaviour _currentObj = null;

    void Awake() {
      watcherCoroutine = graspWatcher();
    }

    void OnEnable() {
      StopCoroutine(watcherCoroutine);
      StartCoroutine(watcherCoroutine);
    }

    void OnDisable() {
      StopCoroutine(watcherCoroutine);
      Deactivate();
    }

    IEnumerator graspWatcher() {
      bool graspingState = false;
      IInteractionBehaviour graspedObject;
      int handId = 0;
      while (true) {
        if (ieManager != null) {
          Leap.Hand hand = HandModel.GetLeapHand();
          if(hand != null) {
            handId = hand.Id;
          } else {
            handId = 0;
          }
          ieManager.TryGetGraspedObject(handId, out graspedObject);
          graspingState = false;
          if (graspedObject != null) {
            if ((AnyInteractionObject ||
                (TagName != "" && graspedObject.gameObject.tag == TagName))) {
              graspingState = true;
            } else {
              for (int o = 0; o < TargetObjects.Length; o++) {
                if (TargetObjects[o] == graspedObject) {
                  graspingState = true;
                  break;
                }
              }
            }
          }
          if (graspingState) {
            if(_currentObj != graspedObject) {
              _currentObj = graspedObject;
              OnGrasp.Invoke(_currentObj);
            }
            Activate();
          } else {
            _currentObj = null;
            Deactivate();
          }
        }
        yield return new WaitForSeconds(Period);
      }
    }
  }

  /**
   * An event class that is dispatched by a InteractionGraspDetector when one of its target
   * IInteractiveBehavior objects is grasped.
   * The event parameters provide the IInteractionBehaviour object.
   * @since 4.1.2
   */
  [System.Serializable]
  public class InteractiveBehaviorGraspEvent : UnityEvent<IInteractionBehaviour> { }
}
