using UnityEngine;
using System;

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

    private InteractionMaterial2 _material;

    private DefinableController<IGraspController> _graspController;
    private DefinableController<IHoldingController> _holdingController;
    private DefinableController<ILayerController> _layerController;
    private DefinableController<IPhysicsController> _physicsController;
    private DefinableController<ISuspensionController> _suspensionController;
    private DefinableController<IThrowingController> _throwingController;

    public ControllerContainer(InteractionBehaviour obj, InteractionMaterial2 material) {
      _material = material;

      _graspController = new DefinableController<IGraspController>(_material.CreateGraspController(obj));
      _holdingController = new DefinableController<IHoldingController>(_material.CreateHoldingController(obj));
      _layerController = new DefinableController<ILayerController>(_material.CreateLayerController(obj));
      _physicsController = new DefinableController<IPhysicsController>(_material.CreatePhysicsController(obj));
      _suspensionController = new DefinableController<ISuspensionController>(_material.CreateSuspensionController(obj));
      _throwingController = new DefinableController<IThrowingController>(_material.CreateThrowingController(obj));
    }

    public IGraspController GraspController {
      get {
        return _graspController;
      }
    }

    public void RegisterCustomGraspController(IGraspController graspController) {
      _graspController.RegisterCustomController(graspController);
    }

    public void UnregisterCustomGraspController() {
      _graspController.UnregisterCustomController();
    }

    public IHoldingController HoldingController {
      get {
        return _holdingController;
      }
    }

    public void RegisterCustomHoldingController(IHoldingController holdingController) {
      _holdingController.RegisterCustomController(holdingController);
    }

    public void UnregisterCustomHoldingController() {
      _holdingController.UnregisterCustomController();
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

    public IPhysicsController PhysicsController {
      get {
        return _physicsController;
      }
    }

    public void RegisterCustomPhysicsController(IPhysicsController physicsController) {
      _physicsController.RegisterCustomController(physicsController);
    }

    public void UnregisterCustomPhysicsController() {
      _physicsController.UnregisterCustomController();
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
  }
}
