using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ActivatorWatcher : MonoBehaviour {
  public bool IsActive;
  public UnityEvent OnActivate;
  public UnityEvent OnDeactivate;

  private Activator[] _activators;

  void Start(){
    _activators = GetComponents<Activator>();
    foreach(Activator activator in _activators){
      activator.OnActivate.AddListener(CheckActivators);
      activator.OnDeactivate.AddListener(CheckActivators);
    }
  }

  void CheckActivators(){
    bool state = true;
    for(int a = 0; a < _activators.Length; a++){
      state = _activators[a].CombineBooleans(state); 
    }
    if(state && !IsActive){
      Activate();
    } else if(!state && IsActive){
      Deactivate();
    }
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
