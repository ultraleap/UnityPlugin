/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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

    #region Grayscale

    public static Color black {
      get { return Color.black; }
    }

    public static Color gray {
      get { return new Color(0.5f, 0.5f, 0.5f); }
    }

    public static Color white {
      get { return Color.white; }
    }

    #endregion

    #region Warm Colors & Browns

    public static Color pink {
      get { return new Color(255f / 255f, 0xC0 / 255f, 0xCB / 255f); }
    }

    public static Color magenta {
      get { return Color.magenta; }
    }

    /// <summary>
    /// Warning: Not VERY distinct from magenta.
    /// </summary>
    public static Color fuschia {
      get { return lerp(Color.magenta, Color.blue, 0.1f); }
    }

    public static Color red {
      get { return Color.red; }
    }

    public static Color brown {
      get { return new Color(0x96 / 255f, 0x4B / 255f, 0x00 / 255f); }
    }

    public static Color beige {
      get { return new Color(0xF5 / 255f, 0xF5 / 255f, 0xDC / 255f); }
    }

    public static Color coral {
      get { return new Color(0xFF / 255f, 0x7F / 255f, 0x50 / 255f); }
    }

    public static Color orange {
      get { return lerp(red, yellow, 0.5f); }
    }

    public static Color khaki {
      get { return new Color(0xC3 / 255f, 0xB0 / 255f, 0x91 / 255f); }
    }

    public static Color amber {
      get { return new Color(0xFF / 255f, 0xBF / 255f, 0x00 / 255f); }
    }

    public static Color yellow {
      get { return Color.yellow; }
    }

    public static Color gold {
      get { return new Color(0xD4 / 255f, 0xAF / 255f, 0x37 / 255f); }
    }

    #endregion

    #region Cool Colors

    public static Color green {
      get { return Color.green; }
    }

    public static Color forest {
      get { return new Color(0x22 / 255f, 0x8B / 255f, 0x22 / 255f); }
    }

    public static Color lime {
      get { return new Color(158f / 255f, 253f / 255f, 56f / 255f); }
    }

    public static Color mint {
      get { return new Color(0x98 / 255f, 0xFB / 255f, 0x98 / 255f); }
    }

    public static Color olive {
      get { return new Color(0x80 / 255f, 0x80 / 255f, 0x00 / 255f); }
    }

    public static Color jade {
      get { return new Color(0x00 / 255f, 0xA8 / 255f, 0x6B / 255f); }
    }

    public static Color teal {
      get { return new Color(0x00 / 255f, 0x80 / 255f, 0x80 / 255f); }
    }

    public static Color veridian {
      get { return new Color(0x40 / 255f, 0x82 / 255f, 0x6D / 255f); }
    }

    public static Color turquoise {
      get { return new Color(0x40 / 255f, 0xE0 / 255f, 0xD0 / 255f); }
    }

    public static Color cyan {
      get { return Color.cyan; }
    }

    public static Color cerulean {
      get { return new Color(0x00 / 255f, 0x7B / 255f, 0xA7 / 255f); }
    }

    public static Color aqua {
      get { return new Color(143f / 255f, 224f / 255f, 247f / 255f); }
    }

    public static Color electricBlue {
      get { return new Color(0x7D / 255f, 0xF9 / 255f, 0xFF / 255f); }
    }

    public static Color blue {
      get { return Color.blue; }
    }

    public static Color navy {
      get { return new Color(0x00 / 255f, 0x00 / 255f, 0x80 / 255f); }
    }

    public static Color periwinkle {
      get { return new Color(0xCC / 255f, 0xCC / 255f, 0xFF / 255f); }
    }

    public static Color purple {
      get { return lerp(magenta, blue, 0.3f); }
    }

    public static Color violet {
      get { return new Color(0x7F / 255f, 0x00 / 255f, 0xFF / 255f); }
    }

    public static Color lavender {
      get { return new Color(0xB5 / 255f, 0x7E / 255f, 0xDC / 255f); }
    }

    #endregion

    #region Shorthand

    private static Color lerp(Color a, Color b, float amount) {
      return Color.Lerp(a, b, amount);
    }

    #endregion

  }

}
