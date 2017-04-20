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
        AttachedObjectHandler.Validate(_graphic, _graphic._featureData);

        if (!Application.isPlaying) {
          if (_graphic._attachedGroup != null) {
            _graphic._attachedGroup.renderer.editor.ScheduleEditorUpdate();
            _graphic._preferredRendererType = _graphic._attachedGroup.renderingMethod.GetType();
          }
        }
      }

      public virtual void OnDrawGizmos() {
        if (pickingMesh != null && pickingMesh.vertexCount != 0) {
          Gizmos.color = new Color(1, 0, 0, 0);
          Gizmos.DrawMesh(pickingMesh);
        }
      }

      public virtual void RebuildEditorPickingMesh() { }

      public virtual void OnAttachedToGroup(LeapGraphicGroup group, LeapSpaceAnchor anchor) {
        if (!Application.isPlaying) {
          _graphic._preferredRendererType = group.renderingMethod.GetType();
        }
      }
    }
#endif
  }
}
