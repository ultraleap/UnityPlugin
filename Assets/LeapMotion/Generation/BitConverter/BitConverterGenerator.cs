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

    public TextAsset template;
    public AssetFolder targetFolder;
    public string[] primitiveTypes;

    public override void Generate() {
      string[] lines = template.text.Replace(TEMPLATE_NAMESPACE, TARGET_NAMESPACE).
                                     Split('\n');

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

      using (var writer = File.CreateText(Path.Combine(targetFolder.Path, "BitConverterNonAlloc.cs"))) {
        writer.Write(beforeCode);

        foreach (var primitiveType in primitiveTypes) {
          writer.Write(codeTemplate.Replace("_Primitive_", primitiveType));
        }

        writer.Write(afterCode);
      }
    }
  }

  public struct _Primitive_ { }
}
