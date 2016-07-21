using UnityEngine;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public abstract class IControllerBase : ScriptableObject {
    protected InteractionBehaviour _obj;

    public static T CreateInstance<T>(InteractionBehaviour obj) where T : IControllerBase {
      T controller = CreateInstance<T>();
      controller.Init(obj);
      return controller;
    }

    public static T CreateInstance<T>(InteractionBehaviour obj, T template) where T : IControllerBase {
      if (template == null) {
        return null;
      }

      T controller = Instantiate(template);
      controller.Init(obj);
      return controller;
    }

    [Conditional("UNITY_ASSERTIONS")]
    public virtual void Validate() { }

    protected virtual void Init(InteractionBehaviour obj) {
      _obj = obj;
    }
  }
}
