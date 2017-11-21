/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Generation;

namespace Leap.Unity.Swizzle.Generation {
  using Query;

  [CreateAssetMenu(menuName = "Generator/Swizzle", order = 900)]
  public class SwizzleGenerator : GeneratorBase {

    public const string TEMPLATE_CODE_KEY = "//__SWIZZLE__";
    public const string TEMPLATE_NAMESPACE = "Leap.Unity.Swizzle.Generation";
    public const string TARGET_NAMESPACE = "Leap.Unity.Swizzle";

    public TextAsset templateAsset;
    public AssetFolder destFolder;

    public override void Generate() {
      StringBuilder builder = new StringBuilder();

      for (int i = 2; i <= 4; i++) {
        string sourceType = "Vector" + i;

        for (int j = 2; j <= 4; j++) {
          string resultType = "Vector" + j;

          int[] components = new int[j];
          do {
            builder.AppendLine();

            builder.Append("    ");
            builder.Append("public static " + resultType + " ");
            for (int k = 0; k < components.Length; k++) {
              builder.Append("xyzw"[components[k]]);
            }
            builder.Append("(this " + sourceType + " vector) {");
            builder.AppendLine();

            builder.Append("      ");
            builder.Append("return new " + resultType + "(");
            for (int k = 0; k < components.Length; k++) {
              if (k != 0) {
                builder.Append(", ");
              }
              builder.Append("vector." + "xyzw"[components[k]]);
            }
            builder.Append(");");
            builder.AppendLine();

            builder.Append("    ");
            builder.Append("}");
            builder.AppendLine();
          } while (Utils.NextTuple(components, i));
        }
      }

      File.WriteAllText(Path.Combine(destFolder.Path, "Swizzle.cs"),
                        templateAsset.text.Replace(TEMPLATE_NAMESPACE, TARGET_NAMESPACE).
                                           Replace(TEMPLATE_CODE_KEY, builder.ToString()));
    }
  }
}
