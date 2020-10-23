/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Packaging {

  public class PackageDefinitionBuildMenuItems { 

    // Core
    [MenuItem("Build/Core", priority = 50)]
    public static void Build_828092ac76618d349a96555173177a8f() {
      PackageDefinition.BuildPackage("828092ac76618d349a96555173177a8f");
    }

    // Hands
    [MenuItem("Build/Hands", priority = 50)]
    public static void Build_0270504144afc6248ba5d4114c5feddf() {
      PackageDefinition.BuildPackage("0270504144afc6248ba5d4114c5feddf");
    }

    // Interaction Engine
    [MenuItem("Build/Interaction Engine", priority = 50)]
    public static void Build_60936e78f540d804ba4f2f4b509b1994() {
      PackageDefinition.BuildPackage("60936e78f540d804ba4f2f4b509b1994");
    }

    // RealtimeGraph
    [MenuItem("Build/RealtimeGraph", priority = 50)]
    public static void Build_b48faf9dc13e83b429b454fe72d8690a() {
      PackageDefinition.BuildPackage("b48faf9dc13e83b429b454fe72d8690a");
    }
  }
}

