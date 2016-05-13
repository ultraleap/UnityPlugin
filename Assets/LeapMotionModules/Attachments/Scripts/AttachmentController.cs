using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Leap.Unity.Attachments{

  public class AttachmentController : MonoBehaviour {
    
    public bool IsActive = false;
    public Transition InTransition;
    public Transition OutTransition;
  
    public virtual void Activate(){
      IsActive = true;
      ChangeChildState();
      if(InTransition != null){
        InTransition.OnComplete.AddListener(ChangeChildState);
        InTransition.TransitionIn();
      }
    }
  
    public virtual void Deactivate(){
      IsActive = false;
      if(OutTransition != null){
        OutTransition.OnComplete.AddListener(ChangeChildState);
        OutTransition.TransitionOut();
      } else {
        ChangeChildState();
      }
    }
  
    public virtual void ChangeChildState(){
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
  }
}