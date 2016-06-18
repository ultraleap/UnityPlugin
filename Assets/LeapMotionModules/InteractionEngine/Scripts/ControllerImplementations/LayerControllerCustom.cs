using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  public class LayerControllerCustom : ILayerController {

    [SerializeField]
    private SingleLayer _interactionLayer;

    [SerializeField]
    private SingleLayer _interactionNoClipLayer;

    public override SingleLayer InteractionLayer {
      get {
        return _interactionLayer;
      }
    }

    public override SingleLayer InteractionNoClipLayer {
      get {
        return _interactionNoClipLayer;
      }
    }
  }
}
