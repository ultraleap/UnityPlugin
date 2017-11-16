using UnityEditor;

namespace Leap.Unity.Packaging {

  public class BuildDefinitionBuildMenuItems { 

    // My Super Build
    [MenuItem("Build/My Super Build", priority = 20)]
    public static void Build_a568da3389816734e866c3c97cfce958() {
      BuildDefinition.Build("a568da3389816734e866c3c97cfce958");
    }
  }
}

