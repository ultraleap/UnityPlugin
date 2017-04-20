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

        //Delete any null references
        for (int i = _graphic._featureData.Count; i-- != 0;) {
          if (_graphic._featureData[i] == null) {
            _graphic._featureData.RemoveAt(i);
          }
        }

        //Validation is also triggered by undo operations, and undo operations
        //can trigger validation :(  This can result in weird loops, so we delay
        //this particular call one frame to prevent weirdness.
        UnityEditor.EditorApplication.delayCall += () => {
          if (_graphic != null) {
            AttachedObjectHandler.Validate(_graphic, _graphic._featureData);
          }
        };

        if (!Application.isPlaying) {
          if (_graphic._attachedGroup != null) {
            _graphic._attachedGroup.renderer.editor.ScheduleEditorUpdate();
            _graphic._preferredRendererType = _graphic._attachedGroup.renderingMethod.GetType();
          }
        }

        //Destroy any components that are not referenced by me
        var allComponents = _graphic.GetComponents<LeapFeatureData>();
        foreach (var component in allComponents) {
          if (!_graphic._featureData.Contains(component)) {
            InternalUtility.Destroy(component);
          }
        }

        foreach (var dataObj in _graphic._featureData) {
          dataObj.graphic = _graphic;
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
