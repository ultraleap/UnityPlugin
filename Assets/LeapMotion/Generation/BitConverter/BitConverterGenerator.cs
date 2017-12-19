/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Generation {
  using Query;

  [CreateAssetMenu(menuName = "Generator/BitConverter", order = 900)]
  public class BitConverterGenerator : GeneratorBase {

    public const string BEGIN_KEY = "//BEGIN";
    public const string END_KEY = "//END";
    public const string TEMPLATE_NAMESPACE = "Leap.Unity.Generation";
    public const string TARGET_NAMESPACE = "Leap.Unity";

    public TextAsset codeTemplate;
    public TextAsset testTemplate;

    public AssetFolder targetFolder;
    public AssetFolder testFolder;

    public string[] primitiveTypes;

    public override void Generate() {
      replaceCenterCode(codeTemplate, targetFolder, "_Primitive_", "BitConverterNonAlloc.cs");
      replaceCenterCode(testTemplate, testFolder, "Single", "BitConverterNonAllocTests.cs");
    }

    private void replaceCenterCode(TextAsset template, AssetFolder folder, string toReplace, string filename) {
      List<string> lines = new List<string>();
      using (var reader = new StringReader(template.text)) {
        while (true) {
          string line = reader.ReadLine();
          if (line == null) {
            break;
          }

          lines.Add(line.Replace(TEMPLATE_NAMESPACE, TARGET_NAMESPACE).
                         Replace("_Template_", "").
                         Replace("_BitConverterTestMock_", "BitConverterNonAlloc"));
        }
      }

      string codeTemplate = lines.Query().
                                  SkipWhile(l => !l.Contains(BEGIN_KEY)).
                                  Skip(1).
                                  TakeWhile(l => !l.Contains(END_KEY)).
                                  Select(s => s + "\n").
                                  Fold((a, b) => a + b);

      string beforeCode = lines.Query().
                                TakeWhile(l => !l.Contains(BEGIN_KEY)).
                                Select(s => s + "\n").
                                Fold((a, b) => a + b);

      string afterCode = lines.Query().
                               SkipWhile(l => !l.Contains(END_KEY)).
                               Skip(1).
                               Select(s => s + "\n").
                               Fold((a, b) => a + b);

      using (var writer = File.CreateText(Path.Combine(folder.Path, filename))) {
        writer.Write(beforeCode);

        foreach (var primitiveType in primitiveTypes) {
          writer.Write(codeTemplate.Replace(toReplace, primitiveType));
        }

        writer.Write(afterCode);
      }
    }
  }

  public struct _Primitive_ { }

  public static class _BitConverterTestMock_ {
    public static System.Single ToSingle(byte[] bytes, int offset) { return 0; }
    public static void GetBytes(System.Single value, byte[] bytes, ref int offset) { return; }
  }
}
