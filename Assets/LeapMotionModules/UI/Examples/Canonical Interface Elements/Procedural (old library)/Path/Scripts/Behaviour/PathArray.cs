using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Procedural.DynamicPath {

  [ExecuteInEditMode]
  public class PathArray : MonoBehaviour {

    public enum RelativeTo {
      Start,
      End,
    }

    //[SerializeField]
    //private RelativeTo _relativeTo = RelativeTo.Start;

    [SerializeField]
    private float _offset = 0;

    [MinValue(0)]
    [SerializeField]
    private float _spacing = 0;

    [SerializeField]
    private bool _forceExpand = true;

    private List<LayoutElement> _layout = new List<LayoutElement>();

    void Update() {
      IPath path = GetComponent<PathBehaviourBase>().Path;

      _layout.Clear();

      int emptyCount = 0;

      bool isFirst = true;
      foreach (Transform child in transform) {
        if (!isFirst) {
          _layout.Add(new LayoutElement(null, _spacing));
          emptyCount++;
        }
        _layout.Add(new LayoutElement(child, 0));
        isFirst = false;
      }

      if (_forceExpand) {
        float totalLength = path.Length;

        float extraSpace = totalLength;
        for (int i = 0; i < _layout.Count; i++) {
          extraSpace -= _layout[i].length;
        }

        float perEmptyExtra = Mathf.Max(0, extraSpace / emptyCount);

        for (int i = 0; i < _layout.Count; i++) {
          var element = _layout[i];
          if (element.transform == null) {
            element.length += perEmptyExtra;
            _layout[i] = element;
          }
        }
      }

      float position = _offset;
      for (int i = 0; i < _layout.Count; i++) {
        var element = _layout[i];
        if (element.transform != null) {
          element.transform.position = path.GetPosition(position);
        }
        position += element.length;
      }
    }

    private float getLength(Transform child) {
      return 0;
    }

    private struct LayoutElement {
      public Transform transform;
      public float length;

      public LayoutElement(Transform transform, float length) {
        this.transform = transform;
        this.length = length;
      }
    }
  }
}
