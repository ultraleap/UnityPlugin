/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Packaging {

  public class PackageDefinitionBuildMenuItems { 

    // Attachments
    [MenuItem("Build/Attachments", priority = 50)]
    public static void Build_75ae930456fc07049858fdc6fc70393b() {
      PackageDefinition.BuildPackage("75ae930456fc07049858fdc6fc70393b");
    }

    // Core
    [MenuItem("Build/Core", priority = 50)]
    public static void Build_39b6898d05b13f54082394c350c88ed1() {
      PackageDefinition.BuildPackage("39b6898d05b13f54082394c350c88ed1");
    }

    // Detection Examples
    [MenuItem("Build/Detection Examples", priority = 50)]
    public static void Build_904a61d077ec8a6408978b1184f66599() {
      PackageDefinition.BuildPackage("904a61d077ec8a6408978b1184f66599");
    }

    // Graphic Renderer
    [MenuItem("Build/Graphic Renderer", priority = 50)]
    public static void Build_467ca7c53d8bceb4191d08a77cb1848b() {
      PackageDefinition.BuildPackage("467ca7c53d8bceb4191d08a77cb1848b");
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

    // UIInput
    [MenuItem("Build/UIInput", priority = 50)]
    public static void Build_ee63a291f059d0c4a86b5b232ab19fae() {
      PackageDefinition.BuildPackage("ee63a291f059d0c4a86b5b232ab19fae");
    }
  }
}

