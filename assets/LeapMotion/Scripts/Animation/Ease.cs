namespace Leap.Unity.Animation {

  /// <summary> Utility functions for producing smooth motion. </summary>
  public static class Ease {

    public static class Quadratic {

      /// <summary> Maps a linear 0-1 animation coefficient "t" to a quadratic in-out 0-1 curve. </summary>
      public static float InOut(float t) {
        t *= 2F;
        if (t < 1F) return 0.5F * t * t;
        t -= 1F;
        return (-0.5F) * (t * (t - 2F) - 1F);
      }

    }

    public static class Cubic {

      /// <summary> Maps a linear 0-1 animation coefficient "t" to a cubic in-out 0-1 curve. </summary>
      public static float InOut(float t) {
        t *= 2F;
        if (t < 1F) return 0.5F * t * t * t;
        t -= 2F;
        return 0.5F * (t * t * t + 2F);
      }

    }

    public static class Quartic {

      /// <summary> Maps a linear 0-1 animation coefficient "t" to a quartic in-out 0-1 curve. </summary>
      public static float InOut(float t) {
        t *= 2F;
        if (t < 1F) return 0.5F * t * t * t * t;
        t -= 2F;
        return -0.5F * (t * t * t * t - 2F);
      }

    }

  }

}