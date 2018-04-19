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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Generation {
  using Query;

  [CreateAssetMenu(menuName = "Generator/BitConverter", order = 900)]
  public class BitConverterGenerator : GeneratorBase {

    public const string BEGIN_KEY = "//BEGIN";
    public const string END_KEY = "//END";
    public const string TO_KEY = "TO";
    public const string GET_KEY = "GET";
    public const string FILL_BYTES_KEY = "//FILL BYTES";

    public TextAsset codeTemplate;
    public TextAsset testTemplate;

    public AssetFolder targetFolder;
    public AssetFolder testFolder;

    public string[] primitiveTypes;

    public override void Generate() {
      generateCode();
      generateUnitTests();
    }

    private void generateCode() {
      List<string> lines = getLines(codeTemplate);
      lines = lines.Select(l => l.Replace("Leap.Unity.Generation", "Leap.Unity").
                                  Replace("BitConverterNonAlloc_Template_", "BitConverterNonAlloc")).
                    ToList();

      using (var writer = File.CreateText(Path.Combine(targetFolder.Path, "BitConverterNonAlloc.cs"))) {
        for (int i = 0; i < lines.Count; i++) {
          string line = lines[i];

          if (line.Contains(BEGIN_KEY)) {
            List<string> methodTemplate = new List<string>();
            while (true) {
              i++;
              string methodLine = lines[i];
              if (methodLine.Contains(END_KEY)) {
                break;
              }
              methodTemplate.Add(methodLine);
            }

            Func<int, string> byteExpr;
            if (line.Contains(TO_KEY)) {
              byteExpr = b => "_c.Byte" + b + " = bytes[offset++];";
            } else if (line.Contains(GET_KEY)) {
              byteExpr = b => "bytes[offset++] = _c.Byte" + b + ";";
            } else {
              throw new InvalidOperationException("Invalid template type [" + line + "]");
            }

            expandMethodTemplate(methodTemplate, writer, byteExpr);
          } else {
            writer.WriteLine(line);
          }
        }
      }
    }

    private void expandMethodTemplate(List<string> methodTemplate, StreamWriter writer, Func<int, string> byteExpr) {
      foreach (string primitiveType in primitiveTypes) {
        Type type = Assembly.GetAssembly(typeof(int)).GetTypes().First(t => t.Name == primitiveType);

        int bytes = Marshal.SizeOf(type);

        for (int i = 0; i < methodTemplate.Count; i++) {
          string line = methodTemplate[i];
          line = line.Replace("Single", primitiveType);

          if (line.Contains(FILL_BYTES_KEY)) {
            string indent = new string(line.TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            for (int j = 0; j < bytes; j++) {
              writer.Write(indent);
              writer.WriteLine(byteExpr(j));
            }
          } else {
            writer.WriteLine(line);
          }
        }
      }
    }

    private void generateUnitTests() {
      List<string> lines = getLines(testTemplate);

      lines = lines.Select(l => l.Replace("Leap.Unity.Generation", "Leap.Unity.Tests").
                                  Replace("_Template_", "").
                                  Replace("_BitConverterTestMock_", "BitConverterNonAlloc")).
                    ToList();

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

      using (var writer = File.CreateText(Path.Combine(testFolder.Path, "BitConverterNonAllocTests.cs"))) {
        writer.Write(beforeCode);

        foreach (var primitiveType in primitiveTypes) {
          //Replace Single with the actual primitive
          //Also uncomment the Test attribute
          writer.Write(codeTemplate.Replace("Single", primitiveType).
                                    Replace("//[Test]", "[Test]"));
        }

        writer.Write(afterCode);
      }
    }

    private List<string> getLines(TextAsset asset) {
      List<string> lines = new List<string>();
      using (var reader = new StringReader(asset.text)) {
        while (true) {
          string line = reader.ReadLine();
          if (line == null) {
            break;
          }
          lines.Add(line);
        }
      }
      return lines;
    }
  }

  public static class _BitConverterTestMock_ {
    public static System.Single ToSingle(byte[] bytes, int offset) { return 0; }
    public static void GetBytes(System.Single value, byte[] bytes, ref int offset) { return; }
  }
}
