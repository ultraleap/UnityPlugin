using UnityEngine;

[RequireComponent(typeof(LeapGui))]
[DisallowMultipleComponent]
public abstract class LeapGuiSpace : LeapGuiComponentBase<LeapGui> {

  [HideInInspector]
  public LeapGui gui;

  /// <summary>
  /// Called at runtime when the user wants to add a new element to 
  /// the gui.  Return false if the attempt should fail, or true if
  /// it could be completed.
  /// </summary>
  public abstract bool TryAddElement(LeapGuiElement element, int index);

  /// <summary>
  /// Called at runtime when the user wants to remove an element from
  /// the gui.  Return false if the attempt should fail, or true if
  /// it could be completed.
  /// </summary>
  public abstract bool TryRemoveElement(LeapGuiElement element, int index);

  /// <summary>
  /// Builds internal data for all existing gui elements in the gui from scratch.
  /// Will only ever be called with the gui transform as the root.
  /// </summary>
  public abstract void BuildElementData(Transform root);

  /// <summary>
  /// Rebuilds a subset of the internal data representation.  The root transform
  /// is the highest element in the hierarchy that could have changed.  This means
  /// that the root and ALL of it's children could potentially have changed.
  /// </summary>
  public abstract void RefreshElementData(Transform root);

  public abstract Vector3 TransformPoint(Vector3 worldRectPos);
  public abstract Vector3 InverseTransformPoint(Vector3 worldGuiPos);

  /// <summary>
  /// Transform a point that a local position relative to an element into gui world space.
  /// </summary>
  public abstract Vector3 TransformPoint(LeapGuiElement element, Vector3 localRectPos);

  /// <summary>
  /// Transforms a point that is in world gui space into a local position relative to a 
  /// chosen gui element.
  /// </summary>
  public abstract Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos);
}
