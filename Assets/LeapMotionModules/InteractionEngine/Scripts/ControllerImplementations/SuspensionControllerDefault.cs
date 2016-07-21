using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.Interaction {

  public class SuspensionControllerDefault : ISuspensionController {
    [SerializeField]
    private float _maxSuspensionTime = 4;

    private Renderer[] _renderers;

    protected override void Init(InteractionBehaviour obj) {
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
      _obj.rigidbody.isKinematic = true;
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

    public override void Validate() {
      base.Validate();

      if (_obj.UntrackedHandCount != 0) {
        Assert.IsTrue(_obj.rigidbody.isKinematic,
                      "Object must be kinematic when suspended.");
      }
    }
  }
}
