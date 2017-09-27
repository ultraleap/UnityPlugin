using System;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Generation {

  public abstract class GeneratorBase : ScriptableObject {
    public abstract void Generate();
  }
}
