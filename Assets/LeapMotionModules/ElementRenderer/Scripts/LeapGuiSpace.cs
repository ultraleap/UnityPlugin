using System;
using UnityEngine;

public interface ITransformer {
  /// <summary>
  /// Transform a point from gui-local rect space to gui-local gui space.
  /// </summary>
  Vector3 TransformPoint(Vector3 localRectPos);

  /// <summary>
  /// Transform a point from gui-local gui space to gui-local rect space.
  /// </summary>
  Vector3 InverseTransformPoint(Vector3 localGuiPos);

  /// <summary>
  /// Transform a rotation from gui-local rect space to gui-local gui space.
  /// </summary>
  Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot);

  /// <summary>
  /// Transform a rotation from gui-local gui space to gui-local rect space.
  /// </summary>
  Quaternion InverseTransformRotation(Vector3 localGuiPos, Quaternion localGuiRot);

  /// <summary>
  /// Transform a direction from gui-local rect space to gui-local gui space.
  /// </summary>
  Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection);

  /// <summary>
  /// Transform a direction from gui-local gui space to gui-local rect space.
  /// </summary>
  Vector3 InverseTransformDirection(Vector3 localGuiPos, Vector3 localGuiDirection);

  /// <summary>
  /// Get a transformation matrix that maps a position in local rect space 
  /// to a position in local gui space.
  /// </summary>
  Matrix4x4 GetTransformationMatrix(Vector3 localRectPos);
}

public abstract class LeapGuiSpace : LeapGuiComponentBase<LeapGui> {

  [HideInInspector]
  public LeapGui gui;

  protected override void OnValidate() {
    base.OnValidate();

#if UNITY_EDITOR
    if (!Application.isPlaying) {
      if (gui != null) {
        gui.editor.ScheduleEditorUpdate();
      }
    }
#endif
  }

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
  public abstract void RefreshElementData(Transform root, int index, int count);

  /// <summary>
  /// 
  /// </summary>
  public abstract ITransformer GetTransformer(Transform anchor);
}
