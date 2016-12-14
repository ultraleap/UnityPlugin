using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  [CustomPropertyDrawer(typeof(SingleLayer))]
  public class SingleLayerEditor : PropertyDrawer {
    private string[] _layerNames;
    private List<int> _layerValues;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      ensureLayersInitialized();

      SerializedProperty layerProperty = property.FindPropertyRelative("layer");

      int index = _layerValues.IndexOf(layerProperty.intValue);
      if (index < 0) {
        if (Application.isPlaying) {
          //If application is playing we dont want to change the layers on the fly
          //Instead, just display them as an int value
          layerProperty.intValue = EditorGUI.IntField(position, property.displayName, layerProperty.intValue);
          return;
        } else {
          //If the application is not running, reset the layer to the default layer
          layerProperty.intValue = 0;
          index = 0;
        }
      }

      index = EditorGUI.Popup(position, property.displayName, index, _layerNames);
      layerProperty.intValue = _layerValues[index];
    }

    private void ensureLayersInitialized() {
      if (_layerNames == null) {
        Dictionary<int, string> valueToLayer = new Dictionary<int, string>();
        for (int i = 0; i < 32; i++) {
          string layerName = LayerMask.LayerToName(i);
          if (!string.IsNullOrEmpty(layerName)) {
            valueToLayer[i] = layerName;
          }
        }

        _layerValues = valueToLayer.Keys.ToList();
        _layerNames = valueToLayer.Values.ToArray();
      }
    }
  }
}
