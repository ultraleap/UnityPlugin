using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PropertyRecorder : MonoBehaviour {

  [SerializeField]
  public List<ComponentProperties> serializedComponents;

  [Serializable]
  public class ComponentProperties {
    public bool expanded;
    public Component component;
    public List<string> bindings;
  }
}
