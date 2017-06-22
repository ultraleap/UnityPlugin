using Leap.Unity.Interaction;

namespace Leap.Unity.Interaction {

  public interface IInternalInteractionManager {

    // Layers

    void NotifyIntObjAddedInteractionLayer(IInteractionBehaviour intObj, int layer, bool refreshImmediately = true);
    void NotifyIntObjRemovedInteractionLayer(IInteractionBehaviour intObj, int layer, bool refreshImmediately = true);

    void NotifyIntObjAddedNoContactLayer(IInteractionBehaviour intObj, int layer, bool refreshImmediately = true);
    void NotifyIntObjRemovedNoContactLayer(IInteractionBehaviour intObj, int layer, bool refreshImmediately = true);

    void RefreshLayersNow();

  }

  public static class IInternalInteractionManagerExtensions {

    public static void NotifyIntObjHasNewInteractionLayer(this IInternalInteractionManager manager,
                                                          IInteractionBehaviour intObj,
                                                          int oldInteractionLayer,
                                                          int newInteractionLayer) {
      manager.NotifyIntObjRemovedInteractionLayer(intObj, oldInteractionLayer, false);
      manager.NotifyIntObjAddedInteractionLayer(intObj, newInteractionLayer, false);
      manager.RefreshLayersNow();
    }

    public static void NotifyIntObjHasNewNoContactLayer(this IInternalInteractionManager manager,
                                                        IInteractionBehaviour intObj,
                                                        int oldNoContactLayer,
                                                        int newNoContactLayer) {
      manager.NotifyIntObjRemovedNoContactLayer(intObj, oldNoContactLayer, false);
      manager.NotifyIntObjAddedNoContactLayer(intObj, newNoContactLayer, false);
      manager.RefreshLayersNow();
    }

  }

}