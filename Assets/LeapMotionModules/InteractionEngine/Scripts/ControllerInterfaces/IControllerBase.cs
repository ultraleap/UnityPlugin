using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IControllerBase : ScriptableObject {
    protected InteractionBehaviour _obj;
    protected InteractionManager _manager;

    public static T CreateInstance<T>(InteractionBehaviour obj) where T : IControllerBase {
      T controller = CreateInstance<T>();
      controller.Init(obj, obj.Manager);
      return controller;
    }

    public static T CreateInstance<T>(InteractionBehaviour obj, T template) where T : IControllerBase {
      T controller = Instantiate(template);
      controller.Init(obj, obj.Manager);
      return controller;
    }

    protected virtual void Init(InteractionBehaviour obj, InteractionManager manager) {
      _manager = obj.Manager;
    }
  }
}
