using UnityEngine;
using System.Collections;

namespace Leap.Unity.Interaction {

  public abstract class IControllerBase : ScriptableObject {
    protected InteractionBehaviour _obj;

    public static T CreateController<T>(InteractionBehaviour obj) where T : IControllerBase {
      T controller = CreateInstance<T>();
      controller.Init(obj);
      return controller;
    }

    protected virtual void Init(InteractionBehaviour obj) {
      _obj = obj;
    }
  }
}
