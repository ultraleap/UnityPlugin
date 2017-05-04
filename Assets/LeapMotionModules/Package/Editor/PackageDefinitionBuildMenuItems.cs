/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Packaging {

  public class PackageDefinitionBuildMenuItems {

    // Android
    [MenuItem("Build/Android")]
    public static void Build_f1acba439a0394b43a768a06a42c039f() {
      PackageDefinition.BuildPackage("f1acba439a0394b43a768a06a42c039f");
    }

    // Attachments
    [MenuItem("Build/Attachments")]
    public static void Build_75ae930456fc07049858fdc6fc70393b() {
      PackageDefinition.BuildPackage("75ae930456fc07049858fdc6fc70393b");
    }

    // Core
    [MenuItem("Build/Core")]
    public static void Build_39b6898d05b13f54082394c350c88ed1() {
      PackageDefinition.BuildPackage("39b6898d05b13f54082394c350c88ed1");
    }

    // Detection Examples
    [MenuItem("Build/Detection Examples")]
    public static void Build_904a61d077ec8a6408978b1184f66599() {
      PackageDefinition.BuildPackage("904a61d077ec8a6408978b1184f66599");
    }

    // Hands
    [MenuItem("Build/Hands")]
    public static void Build_0270504144afc6248ba5d4114c5feddf() {
      PackageDefinition.BuildPackage("0270504144afc6248ba5d4114c5feddf");
    }

    // Interaction Engine
    [MenuItem("Build/Interaction Engine")]
    public static void Build_bffac24abeb9a8e48b10867fa36b5dfc() {
      PackageDefinition.BuildPackage("bffac24abeb9a8e48b10867fa36b5dfc");
    }

    // RealtimeGraph
    [MenuItem("Build/RealtimeGraph")]
    public static void Build_b48faf9dc13e83b429b454fe72d8690a() {
      PackageDefinition.BuildPackage("b48faf9dc13e83b429b454fe72d8690a");
    }

    // UIInput
    [MenuItem("Build/UIInput")]
    public static void Build_ee63a291f059d0c4a86b5b232ab19fae() {
      PackageDefinition.BuildPackage("ee63a291f059d0c4a86b5b232ab19fae");
    }
  }
}

