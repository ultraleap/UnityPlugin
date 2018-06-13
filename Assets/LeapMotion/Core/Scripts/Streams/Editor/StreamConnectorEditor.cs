

using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Streams {

  [CustomEditor(typeof(StreamConnector<>), editorForChildClasses: true)]
  public class StreamConnectorEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_stream", drawStreamProperty);
      specifyCustomDrawer("_receiver", drawReceiverProperty);

      // Only show wire settings if it makes sense to draw a wire (connector must connect
      // two different objects instead of being an 'internal' connection).
      specifyConditionalDrawing(() => getStreamBehaviour() == null
                                      || getReceiverBehaviour() == null
                                      || (getStreamBehaviour().gameObject
                                          != getReceiverBehaviour().gameObject),
                                "drawWire",
                                "debugWire");
    }

    private MonoBehaviour getStreamBehaviour() {
      return (serializedObject.FindProperty("_stream").objectReferenceValue
              as MonoBehaviour);
    }
    private MonoBehaviour getReceiverBehaviour() {
      return (serializedObject.FindProperty("_receiver").objectReferenceValue
              as MonoBehaviour);
    }

    private void drawStreamProperty(SerializedProperty property) {
      var rect = EditorGUILayout.GetControlRect(true);
      var label = new GUIContent("Stream ("
                                 + (target as StreamConnector).streamDataType.Name + ")");

      var streamType = (target as StreamConnector).streamType;

      var spoofedImplementsInterfaceAttribute
        = new ImplementsInterfaceAttribute(streamType);
      spoofedImplementsInterfaceAttribute.Init(property);

      var attributes = Pool<List<CombinablePropertyAttribute>>.Spawn();
      attributes.Clear();
      try {
        attributes.Add(spoofedImplementsInterfaceAttribute);
        CombinablePropertyDrawer.OnGUI(attributes, null, rect, property, label);
      }
      finally {
        attributes.Clear();
        Pool<List<CombinablePropertyAttribute>>.Recycle(attributes);
      }
    }

    private void drawReceiverProperty(SerializedProperty property) {
      var rect = EditorGUILayout.GetControlRect(true);
      var label = new GUIContent("Stream Receiver ("
                                 + (target as StreamConnector).streamDataType.Name + ")");

      var streamReceiverType = (target as StreamConnector).streamReceiverType;

      var spoofedImplementsInterfaceAttribute
        = new ImplementsInterfaceAttribute(streamReceiverType);
      spoofedImplementsInterfaceAttribute.Init(property);

      var attributes = Pool<List<CombinablePropertyAttribute>>.Spawn();
      attributes.Clear();
      try {
        attributes.Add(spoofedImplementsInterfaceAttribute);
        CombinablePropertyDrawer.OnGUI(attributes, null, rect, property, label);
      }
      finally {
        attributes.Clear();
        Pool<List<CombinablePropertyAttribute>>.Recycle(attributes);
      }
    }

  }

}
