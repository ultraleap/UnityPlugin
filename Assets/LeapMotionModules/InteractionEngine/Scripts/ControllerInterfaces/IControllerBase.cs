using UnityEngine;

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

    protected virtual void Init(InteractionBehaviour obj) {
      _obj = obj;
    }
  }
}
