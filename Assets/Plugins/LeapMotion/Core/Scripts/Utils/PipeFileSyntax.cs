/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity {

  public class PipeFileSyntax {

    /// <summary> The file path, cleaned of any pipe syntax. </summary>
    public string path;
    public int numChannels = 1;
    public bool didParseNumChannels { get; private set; }
    public bool combineChannels = false;
    public bool didParseCombineChannels { get; private set; }

    public bool didParseAnyPipeSyntax {
      get { return didParseNumChannels || didParseCombineChannels; }
    }

    public PipeFileSyntax(string pathMaybeWithPipes) {
      string[] pipeArgs = pathMaybeWithPipes.Split('|');
      this.path = pipeArgs[0];
      if (pipeArgs.Length > 1) {
        this.didParseNumChannels = Int32.TryParse(pipeArgs[1],
          out this.numChannels);
      }
      if (pipeArgs.Length > 2) {
        this.combineChannels = !string.IsNullOrEmpty(pipeArgs[2]) &&
          (pipeArgs[2].ToUpper().Equals("T") ? true : false);
        this.didParseCombineChannels = true;
      }
    }

    public static PipeFileSyntax Parse(string pathMaybeWithPipes) {
      return new PipeFileSyntax(pathMaybeWithPipes);
    }

    public PipeFileSyntax ChangePath(string newPath) {
      this.path = newPath;
      return this;
    }

    public override string ToString() {
      var sb = new System.Text.StringBuilder();
      sb.Append(path);
      if (didParseNumChannels) {
        sb.Append("|"); sb.Append(numChannels);
        if (didParseCombineChannels) {
          sb.Append("|"); sb.Append((combineChannels ? "T" : "F"));
        }
      }
      return sb.ToString();
    }

  }

}
