/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Contains color constants like UnityEngine.Color, but for _all_ the colors you
  /// can think of. If you can think of a color that doesn't exist here, add it!
  /// 
  /// (Note: This class exists for convenience, not runtime speed.)
  /// </summary>
  public static class LeapColor {

    private static Color lerp(Color a, Color b, float amount) {
      return Color.Lerp(a, b, amount);
    }

    private static Color hex(byte r, byte g, byte b) {
      return new Color(r / 255f, g / 255f, b / 255f);
    }

    private static Color hex(float r, float g, float b) {
      return new Color(r / 255f, g / 255f, b / 255f);
    }

    public static Color amber {
      get { return hex(0xFF, 0xBF, 0x00); }
    }

    public static Color aqua {
      get { return hex(143f, 224f, 247f); }
    }

    public static Color auburn {
      get { return hex(0x92, 0x27, 0x24); }
    }

    public static Color beige {
      get { return hex(0xF5, 0xF5, 0xDC); }
    }

    public static Color black {
      get { return Color.black; }
    }

    public static Color blue {
      get { return Color.blue; }
    }

    public static Color brickRed {
      get { return hex(0xcb, 0x41, 0x54); }
    }

    public static Color brown {
      get { return hex(0x96, 0x4B, 0x00); }
    }

    public static Color burgundy {
      get { return hex(0x80, 0x00, 0x20); }
    }

    /// <summary> Shade of yellow-green. </summary>
    public static Color chartreuse {
      get { return hex(0x7f, 0xff, 0x00); }
    }

    public static Color coral {
      get { return hex(0xFF, 0x7F, 0x50); }
    }

    /// <summary> Muted cyan/blue. </summary>
    public static Color cerulean {
      get { return hex(0x00, 0x7B, 0xA7); }
    }

    public static Color cyan {
      get { return Color.cyan; }
    }

    public static Color electricBlue {
      get { return hex(0x7D, 0xF9, 0xFF); }
    }

    public static Color forest {
      get { return hex(0x22, 0x8B, 0x22); }
    }

    /// <summary> Unity's magenta -- RGBA (1, 0, 1, 1). </summary>
    public static Color fuschia {
      get { return Color.magenta; }
    }

    public static Color gold {
      get { return hex(0xD4, 0xAF, 0x37); }
    }

    public static Color gray {
      get { return new Color(0.5f, 0.5f, 0.5f); }
    }
    
    /// <summary> Expects float from 0-1. </summary>
    public static Color grayPercent(float percent) {
      return new Color(percent, percent, percent);
    }

    public static Color green {
      get { return Color.green; }
    }

    public static Color hotPink {
      get { return hex(0xf8, 0x18, 0x94); }
    }

    public static Color jade {
      get { return hex(0x00, 0xA8, 0x6B); }
    }

    public static Color khaki {
      get { return hex(0xC3, 0xB0, 0x91); }
    }

    public static Color lavender {
      get { return hex(0xB5, 0x7E, 0xDC); }
    }

    public static Color leapGreen {
      get { return hex(0x5d, 0xaa, 0x00); }
    }

    public static Color lilac {
      get { return hex(0xc8, 0xa2, 0xc8); }
    }

    public static Color lime {
      get { return hex(158f, 253f, 56f); }
    }

    /// <summary> `#FF0090` -- less blue than Unity's magenta
    /// (see LeapColor.fuchsia). </summary>
    public static Color magenta {
      get { return hex(0xff, 0x00, 0x90); }
    }

    /// <summary> Pale purple. </summary>
    public static Color mauve {
      get { return hex(0xe0, 0xb0, 0xff); }
    }

    public static Color mint {
      get { return hex(0x98, 0xFB, 0x98); }
    }

    public static Color navy {
      get { return hex(0x00, 0x00, 0x80); }
    }

    public static Color olive {
      get { return hex(0x80, 0x80, 0x00); }
    }

    public static Color orange {
      get { return lerp(red, yellow, 0.5f); }
    }

    public static Color periwinkle {
      get { return hex(0xCC, 0xCC, 0xFF); }
    }

    public static Color pink {
      get { return hex(255f, 0xC0, 0xCB); }
    }

    public static Color purple {
      get { return lerp(magenta, blue, 0.3f); }
    }

    public static Color red {
      get { return Color.red; }
    }

    public static Color robinsEgg {
      get { return hex(0x1f, 0xce, 0xcb); }
    }

    public static Color royalPurple { get { return hex(0x78, 0x51, 0xa9); }}

    public static Color ruby { get { return hex(0x9b, 0x11, 0x1e); }}

    public static Color saffron {
      get { return hex(0xff, 0x99, 0x33); }
    }

    public static Color teal {
      get { return hex(0x00, 0x80, 0x80); }
    }

    public static Color turquoise {
      get { return hex(0x40, 0xE0, 0xD0); }
    }

    public static Color veridian {
      get { return hex(0x40, 0x82, 0x6D); }
    }

    public static Color violet {
      get { return hex(0x7F, 0x00, 0xFF); }
    }

    public static Color white {
      get { return Color.white; }
    }

    public static Color yellow {
      get { return Color.yellow; }
    }

  }

}
