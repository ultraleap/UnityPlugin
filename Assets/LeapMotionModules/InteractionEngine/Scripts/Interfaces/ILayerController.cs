using UnityEngine;
using System.Collections;

namespace Leap.Unity.Interaction {

  public abstract class ILayerController : IControllerBase {
    public abstract SingleLayer InteractionLayer { get; }
    public abstract SingleLayer InteractionNoClipLayer { get; }
  }
}
