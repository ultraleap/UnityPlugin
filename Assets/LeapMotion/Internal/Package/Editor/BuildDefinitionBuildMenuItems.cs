using UnityEditor;

namespace Leap.Unity.Packaging {

  public class BuildDefinitionBuildMenuItems { 

    // Text Experiment
    [MenuItem("Build/Text Experiment", priority = 20)]
    public static void Build_541d1627636de0e4f8c7b8f8bedeea93() {
      BuildDefinition.BuildFromGUID("541d1627636de0e4f8c7b8f8bedeea93");
    }
  }
}

