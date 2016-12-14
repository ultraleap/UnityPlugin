using UnityEditor;

namespace Leap.Unity.Packaging {

  public class PackageDefinitionBuildMenuItems {

    // Interaction Engine
    [MenuItem("Build/Interaction Engine")]
    public static void Buildbffac24abeb9a8e48b10867fa36b5dfc() {
      PackageDefinition.BuildPackage("bffac24abeb9a8e48b10867fa36b5dfc");
    }

    // Core
    [MenuItem("Build/Core")]
    public static void Build39b6898d05b13f54082394c350c88ed1() {
      PackageDefinition.BuildPackage("39b6898d05b13f54082394c350c88ed1");
    }
  }
}

