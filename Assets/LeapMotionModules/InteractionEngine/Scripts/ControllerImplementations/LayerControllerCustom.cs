using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  /**
  * An ILayerController implementation that allow you to specify explicit layer masks to
  * use for the parent InteractionMaterial.
  *
  * @since 4.1.4
  */
  public class LayerControllerCustom : ILayerController {

    [Tooltip("The normal layer for interactable objects.")]
    [SerializeField]
    private SingleLayer _interactionLayer;

    [Tooltip("The layer used for interactable objects when they cannot collide with hands.")]
    [SerializeField]
    private SingleLayer _interactionNoClipLayer;

    /**
    * The layer mask to use when an object is in an interatable state.
    * @since 4.1.4
    */
    public override SingleLayer InteractionLayer {
      get {
        return _interactionLayer;
      }
    }

    /**
    * The layer mask to use when an object is not in an interatable state.
    * @since 4.1.4
    */
    public override SingleLayer InteractionNoClipLayer {
      get {
        return _interactionNoClipLayer;
      }
    }
  }
}
