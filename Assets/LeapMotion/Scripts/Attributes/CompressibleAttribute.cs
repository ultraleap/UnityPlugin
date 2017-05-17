using System;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.Attributes {

  public class CompressibleAttribute : CombinablePropertyAttribute, IFullPropertyDrawer {

#if UNITY_EDITOR
    private static TextureFormat[] _compressibleFormats;
    private static GUIContent[] _contents;

    private static void init() {
      if (_compressibleFormats == null || _contents == null) {
        _compressibleFormats = (Enum.GetValues(typeof(TextureFormat)) as int[]).
                                Query().
                                Select(v => (TextureFormat)v).
                                Where(f => Utils.IsCompressible(f)).
                                ToArray();

        _contents = _compressibleFormats.Query().
                                         Select(f => Enum.GetName(typeof(TextureFormat), f)).
                                         Select(n => new GUIContent(n)).
                                         ToArray();
      }
    }

    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      init();

      int index = Array.IndexOf(_compressibleFormats, property.intValue);
      if (index < 0) {
        index = 0;
      }

      index = EditorGUI.Popup(rect, label, index, _contents);

      property.intValue = (int)_compressibleFormats[index];
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Enum;
      }
    }
#endif
  }
}
