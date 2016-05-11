using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  /**A Component to be attached to an IHandModel to handle starting and ending of tracking */
  public abstract class HandTransitionBehavior : MonoBehaviour {

    protected abstract void HandReset();
    protected abstract void HandFinish();
    protected IHandModel iHandModel;
    protected virtual void Awake(){
      iHandModel = GetComponent<IHandModel>();
      if (iHandModel == null) {
        Debug.LogWarning("HandTransitionBehavior components require an IHandModel component attached to the same GameObject");
        return;
      }
      iHandModel.OnBegin += HandReset;
      iHandModel.OnFinish += HandFinish;
    }
    protected virtual void OnDestroy() {
      IHandModel iHandModel = GetComponent<IHandModel>();
      if (iHandModel == null) {
        Debug.LogWarning("HandTransitionBehavior components require an IHandModel component attached to the same GameObject");
        return;
      }
      iHandModel.OnBegin -= HandReset;
      iHandModel.OnFinish -= HandFinish;
    }
  } 
}
