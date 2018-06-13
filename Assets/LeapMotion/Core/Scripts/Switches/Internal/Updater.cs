using System;
using UnityEngine;

namespace Leap.Unity {

  public class Updater : MonoBehaviour {

    private static Updater _singleton = null;
    public static Updater instance {
      get {
        if (_singleton == null) {
          GameObject updaterObj = new GameObject("__Updater Singleton__");
          _singleton = updaterObj.AddComponent<Updater>();
        }
        return _singleton;
      }
    }

    public event Action OnUpdate = () => { };

    void Update() {
      OnUpdate();
    }

  }

}