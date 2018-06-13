using Leap.Unity.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Gestures {

  public class GestureUnityEvent : MonoBehaviour {

    [ImplementsInterface(typeof(IGesture))]
    public MonoBehaviour _gesture;
    public IGesture gesture {
      get { return _gesture as IGesture; }
      set { _gesture = value as MonoBehaviour; }
    }

    public UnityEvent OnGestureFinished;

    void Update() {
      if (gesture.wasFinished) {
        OnGestureFinished.Invoke();
      }
    }

  }

}
