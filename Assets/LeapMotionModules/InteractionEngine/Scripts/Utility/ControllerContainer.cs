/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public class ControllerContainer {

    private struct DefinableController<T> where T : IControllerBase {
      public T defaultController;
      public T registeredController;

      public DefinableController(T defaultController) {
        this.defaultController = defaultController;
        registeredController = null;
      }

      public void RegisterCustomController(T customController) {
        if (registeredController != null) {
          throw new InvalidOperationException("Cannot register a custom controller because one is already registered.");
        }
        registeredController = customController;
      }

      public void UnregisterCustomController() {
        if (registeredController == null) {
          throw new InvalidOperationException("Cannot unregister a custom controller because no custom controller is registered.");
        }
        registeredController = null;
      }

      public static implicit operator T(DefinableController<T> definableController) {
        if (definableController.registeredController != null) {
          return definableController.registeredController;
        } else {
          return definableController.defaultController;
        }
      }
    }

    private InteractionMaterial _material;

    private DefinableController<IHoldingPoseController> _holdingPoseController;
    private DefinableController<ILayerController> _layerController;
    private DefinableController<IMoveToController> _moveToController;
    private DefinableController<ISuspensionController> _suspensionController;
    private DefinableController<IThrowingController> _throwingController;

    public ControllerContainer(InteractionBehaviour obj, InteractionMaterial material) {
      _material = material;

      _holdingPoseController = new DefinableController<IHoldingPoseController>(_material.CreateHoldingPoseController(obj));
      _layerController = new DefinableController<ILayerController>(_material.CreateLayerController(obj));
      _moveToController = new DefinableController<IMoveToController>(_material.CreateMoveToController(obj));
      _suspensionController = new DefinableController<ISuspensionController>(_material.CreateSuspensionController(obj));
      _throwingController = new DefinableController<IThrowingController>(_material.CreateThrowingController(obj));
    }

    public IHoldingPoseController HoldingPoseController {
      get {
        return _holdingPoseController;
      }
    }

    public void RegisterCustomHoldingPoseController(IHoldingPoseController holdingPoseController) {
      _holdingPoseController.RegisterCustomController(holdingPoseController);
    }

    public void UnregisterCustomHoldingPoseController() {
      _holdingPoseController.UnregisterCustomController();
    }

    public ILayerController LayerController {
      get {
        return _layerController;
      }
    }

    public void RegisterCustomLayerController(ILayerController layerController) {
      _layerController.RegisterCustomController(layerController);
    }

    public void UnregisterCustomLayerController() {
      _layerController.UnregisterCustomController();
    }

    public IMoveToController MoveToController {
      get {
        return _moveToController;
      }
    }

    public void RegisterCustomMoveToController(IMoveToController moveToController) {
      _moveToController.RegisterCustomController(moveToController);
    }

    public void UnregisterCustomMoveToController() {
      _moveToController.UnregisterCustomController();
    }

    public ISuspensionController SuspensionController {
      get {
        return _suspensionController;
      }
    }

    public void RegisterCustomSuspensionController(ISuspensionController suspensionController) {
      _suspensionController.RegisterCustomController(suspensionController);
    }

    public void UnregisterCustomSuspensionController() {
      _suspensionController.UnregisterCustomController();
    }

    public IThrowingController ThrowingController {
      get {
        return _throwingController;
      }
    }

    public void RegisterCustomThrowingController(IThrowingController throwingController) {
      _throwingController.RegisterCustomController(throwingController);
    }

    public void UnregisterCustomThrowingController() {
      _throwingController.UnregisterCustomController();
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void Validate() {
      if (HoldingPoseController != null) HoldingPoseController.Validate();
      if (LayerController != null) LayerController.Validate();
      if (MoveToController != null) MoveToController.Validate();
      if (SuspensionController != null) SuspensionController.Validate();
      if (ThrowingController != null) ThrowingController.Validate();
    }
  }
}
