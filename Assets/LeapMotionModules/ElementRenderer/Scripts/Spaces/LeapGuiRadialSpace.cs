using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Attributes;

public interface IRadialTransformer : ITransformer {
  Vector4 GetVectorRepresentation(LeapGuiElement element);
}

public abstract class LeapGuiRadialSpaceBase : LeapGuiSpace {
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "RadialSpace_Radius";

  [MinValue(0.001f)]
  [EditTimeOnly, SerializeField]
  public float radius = 1;
}

public abstract class LeapGuiRadialSpace<TType> : LeapGuiRadialSpaceBase, ISupportsAddRemove
  where TType : IRadialTransformer {

  protected Dictionary<Transform, TType> _transformerData = new Dictionary<Transform, TType>();

  public virtual void OnAddElement() {
    BuildElementData(transform); //TODO, optimize
  }

  public virtual void OnRemoveElement() {
    BuildElementData(transform); //TODO, optimize
  }

  public override void BuildElementData(Transform root) {
    _transformerData.Clear();

    _transformerData[transform] = ConstructTransformer(root);
    foreach (var anchor in gui.anchors) {
      _transformerData[anchor.transform] = ConstructTransformer(anchor.transform);
    }

    RefreshElementData(root, 0, gui.anchors.Count);
  }

  public override ITransformer GetTransformer(Transform anchor) {
    TType transformer;
    if (!_transformerData.TryGetValue(anchor, out transformer)) {
      string message;
      if (Application.isPlaying) {
        message = "Could not find an anchor reference for " + anchor +
                  ".  Remember that is is not legal to add or enabled anchors " +
                  "at runtime, or otherwise change the hierarchy structure.";
      } else {
        message = "Could not find an anchor reference for " + anchor +
                  ".  Make sure to pass in a transform with an enabled AnchorOfConstantSize " +
                  "or the LeapGui component itself.";
      }

      throw new InvalidOperationException(message);
    }

    return transformer;
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

  protected abstract TType ConstructTransformer(Transform anchor);
  protected abstract void SetTransformerRelativeTo(TType tartet, TType parent, Vector3 guiSpaceDelta);
}
