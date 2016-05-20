using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Leap.Unity {

  /**
   * Controls activation and deactivation of child game objects, optionally using a transition.
   * 
   * Call Activate() to enable all child objects. If an InTransition is specified, it is applied
   * after enabling the children.
   * 
   * Call Deactivate() to disable all child objects. If an OutTransition is specified, it is applied
   * before the children are deactivated.
   * 
   * You can override ChangeChildState() for more sophisticated behavior.
   * 
   * Use with Detectors and a DetectorAndGate to turn on and off hand attachments based
   * on hand pose or other factors for which a detector class exists.
   * 
   * @since 4.1.1
   */
  public class AttachmentController : MonoBehaviour {

    /**
     * Reports whether this attachment is in an activated state or not.
     *  @since 4.1.1
     */
    public bool IsActive = false;
    /**
     * A Transition played when the attachment is activated.
     *  @since 4.1.1
     */
    public Transition InTransition;
    /**
     * A Transition played when the attachment is deactivated.
     *  @since 4.1.1
     */
    public Transition OutTransition;

    /**
     * Activates the attachment's child object.
     * Plays the InTransition, if one is specified.
     *  @since 4.1.1
     */
    public virtual void Activate(bool doTransition = true){
      IsActive = true;
      ChangeChildState();
      if(InTransition != null && doTransition){
        InTransition.OnComplete.AddListener(ChangeChildState);
        InTransition.TransitionIn();
      }
    }

    /**
     * Deactivates the attachment's child object.
     * Plays the OutTransition, if one is specified.
     *  @since 4.1.1
     */
    public virtual void Deactivate(bool doTransition = true) {
      IsActive = false;
      if(OutTransition != null && doTransition){
        OutTransition.OnComplete.AddListener(ChangeChildState);
        OutTransition.TransitionOut();
      } else {
        ChangeChildState();
      }
    }

    /**
     * Toggles child state.
     *  @since 4.1.1
     */
    protected virtual void ChangeChildState(){
      if(InTransition != null){
        InTransition.OnComplete.RemoveListener(ChangeChildState);
      }
      if(OutTransition != null){
        OutTransition.OnComplete.RemoveListener(ChangeChildState);
      }
      Transform[] children = GetComponentsInChildren<Transform>(true);
      for(int g = 0; g < children.Length; g++){
        if ( children[g].gameObject.GetInstanceID() != gameObject.GetInstanceID() ) {
          children[g].gameObject.SetActive(IsActive);
        }
      }
    }

    private void OnDisable(){
      Deactivate(false);
    }
  }
}