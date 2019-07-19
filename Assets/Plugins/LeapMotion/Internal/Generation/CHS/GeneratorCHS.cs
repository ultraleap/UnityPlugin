/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using UnityEngine;
using Leap.Unity.Generation;

namespace Leap.Unity.Animation.Generation {

  public struct __CHS_T__ {
    public static __CHS_T__ operator +(__CHS_T__ a, __CHS_T__ b) {
      return default(__CHS_T__);
    }
    public static __CHS_T__ operator *(float a, __CHS_T__ b) {
      return default(__CHS_T__);
    }
  }

  [CreateAssetMenu(menuName = "Generator/CHS", order = 900)]
  public class GeneratorCHS : GeneratorBase {
    public const string TEMPLATE_NAME = "__CHS__";
    public const string TEMPLATE_TYPE = "__CHS_T__";
    public const string TEMPLATE_NAMESPACE = "Leap.Unity.Animation.Generation";
    public const string TARGET_NAMESPACE = "Leap.Unity.Animation";

    public TextAsset templateAsset;
    public AssetFolder destFolder;

    public Definition[] definitions;

    public override void Generate() {
      var template = templateAsset.text;

      foreach (var def in definitions) {
        File.WriteAllText(Path.Combine(destFolder.Path, def.name + ".cs"),
                          template.Replace(TEMPLATE_NAMESPACE, TARGET_NAMESPACE).
                                   Replace(TEMPLATE_NAME, def.name).
                                   Replace(TEMPLATE_TYPE, def.type));
      }
    }

    [Serializable]
    public struct Definition {
      public string name;
      public string type;
    }
  }
}
