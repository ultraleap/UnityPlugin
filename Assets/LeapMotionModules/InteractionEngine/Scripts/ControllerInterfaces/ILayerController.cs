/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Interaction {

  /**
  * ILayerController defines the interface used by the Interaction Engine
  * to identify the Unity layers to use for a specific InteractionMaterial.
  *
  * The InteractionManager provides default layer handling for all InteractionMaterials.
  * An ILayerController implementation can provide customized behavior for specific categories
  * of interactable objects.
  * 
  * The Interaction Engine provides the LayerControllerCustom implementation for
  * this controller.
  * @since 4.1.4
  */
  public abstract class ILayerController : IControllerBase {
    /**
    * Identifies the layer to use when an object can interact with BrushHands.
    * @since 4.1.4
    */
    public abstract SingleLayer InteractionLayer { get; }
    /**
    * Identifies the layer to use when an object cannot interact with BrushHands.
    * @since 4.1.4
    */
    public abstract SingleLayer InteractionNoClipLayer { get; }
  }
}
