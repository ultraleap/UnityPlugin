using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class LeapGuiElement : MonoBehaviour {

#if UNITY_EDITOR
  public readonly EditorApi editor;

  public class EditorApi {
    private readonly LeapGuiElement _element;

    public Mesh pickingMesh;

    public EditorApi(LeapGuiElement element) {
      _element = element;
    }

    public virtual void OnValidate() {
      for (int i = _element._data.Count; i-- != 0;) {
        var component = _element._data[i];
        if (component.gameObject != _element.gameObject) {
          LeapGuiElementData movedData;
          if (InternalUtility.TryMoveComponent(component, _element.gameObject, out movedData)) {
            _element._data[i] = movedData;
          } else {
            Debug.LogWarning("Could not move component " + component + "!");
            InternalUtility.Destroy(component);
            _element._data.RemoveAt(i);
          }
        }
      }

      if (!Application.isPlaying) {
        if (_element._attachedGroup != null) {
          _element._attachedGroup.gui.editor.ScheduleEditorUpdate();
          _element._preferredRendererType = _element._attachedGroup.renderer.GetType();
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

    public virtual void OnAttachedToGui(LeapGuiGroup group, Transform anchor) {
      if (!Application.isPlaying) {
        _element._preferredRendererType = group.renderer.GetType();
      }
    }
  }
#endif
}
