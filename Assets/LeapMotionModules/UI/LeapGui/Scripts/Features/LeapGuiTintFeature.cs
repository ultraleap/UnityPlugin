using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiTintFeature : LeapGuiFeature {

  public override ScriptableObject CreateSettingsObject() {
    return ScriptableObject.CreateInstance<TintSettings>();
  }

  public class TintSettings : ElementSettings {

    [SerializeField]
    private Color _tint;
  }
}
