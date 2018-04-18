using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;

public class DocUpgrader : MonoBehaviour {

  [QuickButton("Convert", "convert")]
  public AssetFolder folder;

  private void convert() {
    string[] files = Directory.GetFiles(folder.Path, "*.cs", SearchOption.AllDirectories);
    foreach (var path in files) {
      List<string> lines = new List<string>(File.ReadAllLines(path));
      File.WriteAllLines(path, convertFile(lines).ToArray());
    }
  }

  private List<string> convertFile(List<string> lines) {
    bool isInBlock = false;
    List<string> block = new List<string>();
    List<string> output = new List<string>();

    foreach (var line in lines) {
      if (line.Contains("/**")) {
        block.Clear();
        isInBlock = true;
      }

      if (isInBlock) {
        block.Add(line);
      } else {
        output.Add(line);
      }

      if (line.Contains("*/")) {
        isInBlock = false;
        output.AddRange(convertBlock(block));
      }
    }

    return output;
  }

  private IEnumerable<string> convertBlock(List<string> block) {
    if (block.Count == 1) {
      yield return block[0];
      yield break;
    }

    if (!block.Any(l => l.Contains("@since")) ||
        block[0].Trim() != "/**") {
      foreach (var line in block) {
        yield return line;
      }
      yield break;
    }

    string indent = new string(block[0].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());

    yield return indent + "/// <summary>";
    for (int i = 1; i < block.Count - 1; i++) {
      string line = block[i].Trim();

      if (line.Contains("@param") || line.Contains("@returns")) {
        continue;
      }

      yield return indent + "/// " + line.Substring(2);
    }
    yield return indent + "/// </summary>";
  }
}
