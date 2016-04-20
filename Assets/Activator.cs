using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;
using Leap.Unity;

public class Activator : MonoBehaviour {
  public float Period = .1f; //seconds
  public bool IsActive = false;
  
  public UnityEvent OnActivate;
  public UnityEvent OnDeactivate;

  public virtual bool CombineBooleans(bool other){
    return other && IsActive;
  }

  public virtual void Activate(){
    IsActive = true;
    OnActivate.Invoke();
  }

  public virtual void Deactivate(){
    IsActive = false;
    OnDeactivate.Invoke();
  }

}
