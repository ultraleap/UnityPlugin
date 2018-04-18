/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public abstract partial class LeapGraphic : MonoBehaviour {

#if UNITY_EDITOR
    public EditorApi editor { get; protected set; }

    public class EditorApi {
      private readonly LeapGraphic _graphic;

      public Mesh pickingMesh;

      public EditorApi(LeapGraphic graphic) {
        _graphic = graphic;
      }

      public virtual void OnValidate() {
        _graphic.isRepresentationDirty = true;

        foreach (var data in _graphic._featureData) {
          data.MarkFeatureDirty();
        }

        if (!Application.isPlaying) {
          if (_graphic.isAttachedToGroup && !_graphic.transform.IsChildOf(_graphic._attachedRenderer.transform)) {
            _graphic.OnDetachedFromGroup();
          }

          if (_graphic.isAttachedToGroup) {
            _graphic._attachedRenderer.editor.ScheduleRebuild();
            _graphic._preferredRendererType = _graphic.attachedGroup.renderingMethod.GetType();
          }
        } else {
          var group = _graphic.attachedGroup;
          if (group != null) {
            if (!group.graphics.Contains(_graphic)) {
              _graphic.OnDetachedFromGroup();
              group.TryAddGraphic(_graphic);
            }
          }
        }
      }

      public virtual void OnDrawGizmos() {
        if (pickingMesh != null && pickingMesh.vertexCount != 0) {
          Gizmos.color = new Color(1, 0, 0, 0);
          Gizmos.DrawMesh(pickingMesh);
        }
      }

      /// <summary>
      /// Called whenever this graphic needs to rebuild its editor picking mesh.
      /// This mesh is a fully warped representation of the graphic, which allows
      /// it to be accurately picked when the user clicks in the scene view.
      /// </summary>
      public virtual void RebuildEditorPickingMesh() { }

      /// <summary>
      /// Called whenever this graphic is attached to a specific group.  This method
      /// is only called at edit time!
      /// </summary>
      public virtual void OnAttachedToGroup(LeapGraphicGroup group, LeapSpaceAnchor anchor) {
        if (!Application.isPlaying) {
          _graphic._preferredRendererType = group.renderingMethod.GetType();
        }
      }
    }
#endif
  }
}
