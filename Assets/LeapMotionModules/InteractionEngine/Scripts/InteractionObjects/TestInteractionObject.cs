using UnityEngine;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class TestInteractionObject : InteractionBehaviour {
    private Renderer _renderer;

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
      _renderer.material.color = Color.green;
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();
      _renderer.material.color = Color.white;
    }

    void Awake() {
      _renderer = GetComponent<Renderer>();
    }
  }
}
