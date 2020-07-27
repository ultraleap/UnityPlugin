/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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

