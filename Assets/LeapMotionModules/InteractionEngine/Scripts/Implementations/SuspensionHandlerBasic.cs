using UnityEngine;
using System;

namespace Leap.Unity.Interaction {

  public class SuspensionHandlerBasic : ISuspensionController {
    [SerializeField]
    private float _maxSuspensionTime = 4;

    private Renderer[] _renderers;

    public override void Init(InteractionBehaviour obj) {
      base.Init(obj);

      _renderers = obj.GetComponentsInChildren<Renderer>();
    }

    public override float MaxSuspensionTime {
      get {
        return _maxSuspensionTime;
      }
    }

    public override void Resume() {
      setRendererState(true);
    }

    public override void Suspend() {
      _obj.Rigidbody.isKinematic = true;
      setRendererState(false);
    }

    public override void Timeout() {
      setRendererState(true);
    }

    private void setRendererState(bool visible) {
      for (int i = 0; i < _renderers.Length; i++) {
        _renderers[i].enabled = visible;
      }
    }
  }
}
