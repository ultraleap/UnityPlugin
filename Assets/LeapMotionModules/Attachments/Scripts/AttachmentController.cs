/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Attachments {

  /**
   * Controls activation and deactivation of child game objects, optionally using a transition.
   * 
   * Call Activate() to enable all child objects. If a Transition is specified, it is applied
   * after enabling the children.
   * 
   * Call Deactivate() to disable all child objects. If a Transition is specified, it is applied
   * before the children are deactivated.
   * 
   * You can override ChangeChildState() for more sophisticated behavior.
   * 
   * Use with Detectors and a DetectorAndGate to turn on and off hand attachments based
   * on hand pose or other factors for which a detector class exists.
   * 
   * Note: if your attached objects should always be visible, you can remove the AttachmentController
   * or set both ActivateOnEnable and DeactivateOnDisable to true.
   * @since 4.1.1
   */
  public class AttachmentController : MonoBehaviour {

    /**
     * Reports whether this attachment is in an activated state or not.
     *  @since 4.1.1
     */
    public bool IsActive {
      get {
        return _isActive;
      }
      set {
        _isActive = value;
      }
    }
    private bool _isActive = false;

    /**
    * Deactivate child objects when the attachment is disabled.
    * When false, any currently active attached objects will remain active when the hand reappears.
    * @since 4.1.3
    */
    [Tooltip("Deactivate child objects automatically without playing a transition")]
    public bool DeactivateOnDisable = true;

    /**
     * A Transition played when the attachment is activated or deactivated.
     *  @since 4.1.1
     */
    [Tooltip("The transition to play when this attachment controller activates or deactivates")]
    public ITransition Transition;

    /**
     * Activates the attachment's child object.
     * Plays the Transition, if one is specified.
     *  @since 4.1.1
     */
    public virtual void Activate(bool doTransition = true){
      IsActive = true;
      ChangeChildState();
      if (Transition != null && doTransition) {
        Transition.OnComplete.AddListener(FinishInTransition);
        Transition.TransitionIn();
      }
    }

    /**
     * Deactivates the attachment's child object.
     * Plays the Transition, if one is specified.
     *  @since 4.1.1
     */
    public virtual void Deactivate(bool doTransition = true) {
      IsActive = false;
      if(Transition != null && doTransition) {
          Transition.OnComplete.AddListener(FinishOutTransition);
          Transition.TransitionOut();
      } else {
        ChangeChildState();
      }
    }

    /**
    * Performs post-transition tasks after an "in" transition.
    * @since 4.1.4
    */
    protected virtual void FinishInTransition() {
      if (Transition != null) {
        Transition.OnComplete.RemoveListener(FinishInTransition);
      }
    }

    /**
    * Performs post-transition tasks after an "out" transition.
    * @since 4.1.4
    */
    protected virtual void FinishOutTransition() {
      if (Transition != null) {
        Transition.OnComplete.RemoveListener(FinishOutTransition);
      }
      ChangeChildState();
    }

    /**
     * Toggles child state.
     *  @since 4.1.1
     */
    protected virtual void ChangeChildState(){
      Transform[] children = GetComponentsInChildren<Transform>(true);
      for(int g = 0; g < children.Length; g++){
        if ( children[g].gameObject.GetInstanceID() != gameObject.GetInstanceID() ) {
          children[g].gameObject.SetActive(IsActive);
        }
      }
    }

    private void OnDisable(){
      if(DeactivateOnDisable)
        Deactivate(false);
    }

  }
}
