/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  public static class TextWrapper {

    /// <summary>
    /// References a contiguous sequence of characters in a string.
    /// </summary>
    public struct Token {
      public int start;
      public int end;

      public int length {
        get {
          return end - start;
        }
        set {
          end = start + value;
        }
      }

      public bool IsNewline(string source) {
        return source[start] == '\n';
      }

      public bool IsWhitespace(string source) {
        return char.IsWhiteSpace(source[start]);
      }

      public float GetWidth(string source, Func<char, float> charWidth) {
        float width = 0;
        for (int i = 0; i < length; i++) {
          width += charWidth(source[i + start]);
        }
        return width;
      }
    }

    /// <summary>
    /// References a contiguous sequence of characters in a string
    /// </summary>
    public struct Line {
      public int start;
      public int end;
      public float width;

      public int length {
        get {
          return end - start;
        }
        set {
          end = start + value;
        }
      }

      public void TrimEnd(string source, Func<char, float> charWidth) {
        while (length > 0) {
          char end = source[start + length - 1];
          if (char.IsWhiteSpace(end)) {
            width -= charWidth(end);
            length--;
          } else {
            return;
          }
        }
      }
    }

    /// <summary>
    /// Given a string, turn it into a list of tokens.  A token is either:
    ///  - A single whitespace character
    ///  - A collection of non-whitespace characters
    /// This algorithm will never create more than one token from a contiguous
    /// section of non-whitespace tokens.
    /// </summary>
    public static void Tokenize(string text, List<Token> tokens) {
      int index = 0;
      int textLength = text.Length;

      while (true) {
        while (true) {
          if (index >= textLength) {
            return;
          }

          if (!char.IsWhiteSpace(text[index])) {
            break;
          }

          tokens.Add(new Token() {
            start = index,
            length = 1
          });

          index++;
        }

        Token token = new Token() {
          start = index
        };

        index++;
        while (index < textLength && !char.IsWhiteSpace(text[index])) {
          index++;
        }

        token.length = index - token.start;
        tokens.Add(token);
      }
    }

    /// <summary>
    /// Given a list of tokens, turn it into a list of lines.
    /// The lines will satisfy the following properties:
    ///  - No line will be longer than the maxLineWidth
    ///  - No token will be split unless it is both
    ///     - the first token on a line
    ///     - too long to fit on the line
    ///  - No line will contain any newline characters
    ///  
    /// Note that width is not measured in character count, but in 
    /// character width provided by the given delegate
    /// </summary>
    public static void Wrap(string source, List<Token> tokens, List<Line> lines, Func<char, float> widthFunc, float maxLineWidth) {
      if (tokens.Count == 0) {
        return;
      }

      int tokenIndex = 0;
      Token token = tokens[0];

      while (true) {
        if (token.IsNewline(source)) {
          lines.Add(new Line() {
            start = token.start,
            length = 0,
            width = 0
          });

          goto LOOP_END;
        }

        NEW_LINE_CONTINUE_TOKEN:

        float firstTokenWidth = widthFunc(source[token.start]);

        //Line starts out equal to the first character of the first token
        Line line = new Line() {
          start = token.start,
          end = token.end,
          width = firstTokenWidth
        };

        //Add as many of the rest of the characters of the token as we can
        for (int i = token.start + 1; i < token.end; i++) {
          float charWidth = widthFunc(source[i]);

          //If the line gets too big, we are forced to truncate!
          if (firstTokenWidth + charWidth > maxLineWidth) {
            line.end = i;
            line.width = firstTokenWidth;
            lines.Add(line);

            token.start = i;

            //Start a new line with the remainder of the current token
            goto NEW_LINE_CONTINUE_TOKEN;
          }

          firstTokenWidth += charWidth;
        }

        //Set line equal  to the first token
        line.width = firstTokenWidth;
        line.length = token.length;

        //Move to the next token to begin adding them to the line
        tokenIndex++;
        if (tokenIndex >= tokens.Count) {
          //Line only contains first token, which cannot be whitespace, so we don't trim
          lines.Add(line);
          return;
        }
        token = tokens[tokenIndex];

        //Fit the rest of the tokens on this line
        while (true) {
          //If the token is a newline, simply add the line and finish the main loop
          if (token.IsNewline(source)) {
            line.TrimEnd(source, widthFunc);
            lines.Add(line);
            break;
          }

          //If a non-whitespace token is too big, finish the line
          float tokenWidth = token.GetWidth(source, widthFunc);
          if (line.width + tokenWidth > maxLineWidth && !token.IsWhitespace(source)) {
            line.TrimEnd(source, widthFunc);
            lines.Add(line);

            //Go start a new line with the current token
            goto NEW_LINE_CONTINUE_TOKEN;
          }

          //Otherwise append to the line and keep trying tokens
          line.length += token.length;
          line.width += tokenWidth;

          tokenIndex++;
          if (tokenIndex >= tokens.Count) {
            lines.Add(line);
            return;
          }
          token = tokens[tokenIndex];
        }

        LOOP_END:

        tokenIndex++;
        if (tokenIndex >= tokens.Count) {
          return;
        }
        token = tokens[tokenIndex];
      }
    }
  }
}
