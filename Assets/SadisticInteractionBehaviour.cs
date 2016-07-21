using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticInteractionBehaviour : InteractionBehaviour {

    public enum SadisticAction {
      DisableComponent,
      DestroyComponent,
      DestroyComponentImmediately,
      DisableGameObject,
      DestroyGameObject,
      DestroyGameObjectImmediately,
      ForceGrab,
      ForceRelease
    }

    public enum Callback {
      OnRegister,
      OnUnregister,
      OnCreateInstance,
      OnDestroyInstance,
      OnGrasp,
      OnRelease,
      OnSuspend,
      OnResume,
    }

    public class SadisticDef {
      public Callback callback;
      public SadisticAction action;

      public SadisticDef(Callback callback, SadisticAction action) {
        this.callback = callback;
        this.action = action;
      }
    }

    public static List<SadisticDef> definitions;
    public static SadisticDef definition;

    static SadisticInteractionBehaviour() {
      definitions = new List<SadisticDef>();

      definitions.AddRange(combine(
        allUnregisterOpterations(Callback.OnRegister),
        allUnregisterOpterations(Callback.OnGrasp),
        allUnregisterOpterations(Callback.OnRelease),
        allUnregisterOpterations(Callback.OnSuspend),
        allUnregisterOpterations(Callback.OnResume),
        allUnregisterOpterations(Callback.OnUnregister),

        new SadisticDef(Callback.OnGrasp, SadisticAction.ForceRelease),
        new SadisticDef(Callback.OnRelease, SadisticAction.ForceGrab),
        new SadisticDef(Callback.OnSuspend, SadisticAction.ForceRelease),
        new SadisticDef(Callback.OnResume, SadisticAction.ForceGrab),
        ));
    }

    private static IEnumerable<SadisticDef> combine(params object[] objs) {
      foreach (var obj in objs) {
        IEnumerable<SadisticDef> ien = obj as IEnumerable<SadisticDef>;
        if (ien != null) {
          foreach (var def in ien) {
            yield return def;
          }
        } else {
          yield return (obj as SadisticDef);
        }
      }
    }

    private static IEnumerable<SadisticDef> allUnregisterOpterations(Callback callback) {
      yield return new SadisticDef(callback, SadisticAction.DisableComponent);
      yield return new SadisticDef(callback, SadisticAction.DestroyComponent);
      yield return new SadisticDef(callback, SadisticAction.DestroyComponentImmediately);
      yield return new SadisticDef(callback, SadisticAction.DisableGameObject);
      yield return new SadisticDef(callback, SadisticAction.DestroyGameObject);
      yield return new SadisticDef(callback, SadisticAction.DestroyGameObjectImmediately);
    }

    protected override void OnRegistered() {
      base.OnRegistered();

      if (definition.callback == Callback.OnRegister) {

      }
    }

    private void executeSadisticAction(SadisticAction action) {
      switch (action) {
        case SadisticAction.DisableComponent:
          enabled = false;
          break;
        case SadisticAction.DestroyComponent:
          Destroy(this);
          break;
        case SadisticAction.DestroyComponentImmediately:
          DestroyImmediate(this);
          break;
        case SadisticAction.DisableGameObject:
          gameObject.SetActive(false);
          break;
        case SadisticAction.DestroyGameObject:
          Destroy(gameObject);
          break;
        case SadisticAction.DestroyGameObjectImmediately:
          DestroyImmediate(gameObject);
          break;
        case SadisticAction.ForceGrab:
          //TODO
          break;
        case SadisticAction.ForceRelease:
          //TODO
          break;
        default:
          break;
      }
    }


  }
}
