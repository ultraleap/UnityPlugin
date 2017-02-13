using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public interface IRadialTransformer : ITransformer {
  Vector4 GetVectorRepresentation(LeapGuiElement element);
}

public abstract class LeapGuiRadialSpaceBase : LeapGuiSpace {
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "RadialSpace_Radius";

  [SerializeField]
  public float radius = 1;
}

public abstract class LeapGuiRadialSpace<TType> : LeapGuiRadialSpaceBase, ISupportsAddRemove
  where TType : IRadialTransformer {

  protected Dictionary<Transform, TType> _transformerData = new Dictionary<Transform, TType>();

  public virtual void OnAddElements(List<LeapGuiElement> element, List<int> indexes) {
    BuildElementData(transform); //TODO, optimize
  }

  public virtual void OnRemoveElements(List<int> toRemove) {
    BuildElementData(transform); //TODO, optimize
  }

  public override void BuildElementData(Transform root) {
    _transformerData.Clear();

    _transformerData[transform] = GetRootTransformer();
    foreach (var anchor in gui.anchors) {
      _transformerData[anchor.transform] = GetRootTransformer();
    }

    RefreshElementData(root, 0, gui.anchors.Count);
  }

  public override ITransformer GetTransformer(Transform anchor) {
    return _transformerData[anchor];
  }

  public override void RefreshElementData(Transform root, int index, int count) {
    for (int i = index; i < count; i++) {
      var anchor = gui.anchors[i];
      var parent = gui.anchorParents[i];

      Assert.IsNotNull(anchor, "Cannot destroy anchors at runtime.");
      Assert.IsNotNull(parent, "Cannot destroy anchors at runtime.");
      Assert.IsTrue(anchor.enabled, "Cannot disable anchors at runtime.");

      Vector3 anchorGuiPosition = transform.InverseTransformPoint(anchor.transform.position);
      Vector3 parentGuiPosition = transform.InverseTransformPoint(parent.position);
      Vector3 delta = anchorGuiPosition - parentGuiPosition;

      TType parentTransformer = _transformerData[parent];
      TType curr = _transformerData[anchor.transform];
      SetTransformerRelativeTo(curr, parentTransformer, delta);
    }
  }

  protected abstract TType GetRootTransformer();
  protected abstract void SetTransformerRelativeTo(TType tartet, TType parent, Vector3 guiSpaceDelta);
}
