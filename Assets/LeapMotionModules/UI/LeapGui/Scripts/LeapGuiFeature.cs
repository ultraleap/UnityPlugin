using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiFeature : ScriptableObject {
  public abstract ScriptableObject CreateSettingsObject();

  public abstract class ElementSettings : ScriptableObject {

  }
}

