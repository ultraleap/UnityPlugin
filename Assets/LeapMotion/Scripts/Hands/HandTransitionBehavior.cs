using UnityEngine;
using System.Collections;

namespace Leap {
  public abstract class HandTransitionBehavior : MonoBehaviour {

    protected abstract void HandReset();
    protected abstract void HandFinish();
    protected virtual void Awake(){
      IHandModel iHandModel = GetComponent<IHandModel>();
      iHandModel.OnBegin += HandReset;
      iHandModel.OnFinish += HandFinish;
    }
    protected virtual void OnDestroy() {
      IHandModel iHandModel = GetComponent<IHandModel>();
      iHandModel.OnBegin -= HandReset;
      iHandModel.OnFinish -= HandFinish;
    }
  } 
}
