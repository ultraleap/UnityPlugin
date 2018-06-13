using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamSkippingFilter : StreamSkippingFilter<Pose> {

    protected override void ShouldSkip(Pose data,
                                       Pose lastOutputData,
                                       List<Pose> skippedSoFar,
                                       out bool shouldSkip,
                                       out bool shouldRememberSkip,
                                       out Maybe<Pose> outputOverride) {
      shouldSkip = true;
      shouldRememberSkip = true;
      outputOverride = Maybe.None;

      if (skippedSoFar.Count == 0) {
        // Always skip a pose when there are no other skipped poses to compare it to.
        shouldSkip = true;
      }
      else {
        var a = lastOutputData;
        var b = skippedSoFar[skippedSoFar.Count - 1];
        var c = data;

        var ab = b.position - a.position;
        var ac = c.position - a.position;

        var sqrDistAC = ac.sqrMagnitude;
        if (sqrDistAC < alwaysSkipDistance * alwaysSkipDistance) {
          shouldSkip = true;
          shouldRememberSkip = false;
        }
        else {
          var angleError = Vector3.Angle(ab, ac);

          var rotAngleError = Quaternion.Angle(a.rotation, b.rotation);

          var sqrDistanceSoFar = (ab).sqrMagnitude
                                 + (c.position - b.position).sqrMagnitude;

          if (angleError >= sqrDistanceSoFar.Map(0f, maxSkipDistance * maxSkipDistance,
                                                 maxSkipAngle, 0f)

              || rotAngleError >= sqrDistanceSoFar.Map(0f,
                                                       maxSkipDistance * maxSkipDistance,
                                                       maxSkipRotationAngle, 0f)

              || sqrDistanceSoFar > maxSkipDistance * maxSkipDistance

              ) {
            outputOverride = b; // note that this is not the input pose,
                                // but the last-skipped pose.
            skippedSoFar.Clear(); // Clear the other (previous) skipped poses.
            shouldSkip = false;
          }
        }
      }
    }

  }

}
