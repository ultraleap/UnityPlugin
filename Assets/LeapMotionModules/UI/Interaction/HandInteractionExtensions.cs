using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public static class HandInteractionExtensions {

    private struct WeightedVector {
      public Vector3 vector;
      public float weight;
    }
    private static List<WeightedVector> s_weightedVectorCache = new List<WeightedVector>();

    public static Vector3 AttentionPosition(this Hand hand) {
      s_weightedVectorCache.Clear();
      Vector3 palmOutPosition = hand.PalmPosition.ToVector3() + hand.PalmarAxis() * hand.PalmWidth * 0.8F;
      s_weightedVectorCache.Add(new WeightedVector() { vector = palmOutPosition, weight = 1F });
      s_weightedVectorCache.Add(new WeightedVector() { vector = hand.Fingers[0].TipPosition.ToVector3(), weight = Vector3.Dot(hand.Fingers[0].bones[0].Direction.ToVector3(), hand.DistalAxis()) });
      s_weightedVectorCache.Add(new WeightedVector() { vector = hand.Fingers[1].TipPosition.ToVector3(), weight = Vector3.Dot(hand.Fingers[1].bones[0].Direction.ToVector3(), hand.DistalAxis()) });
      s_weightedVectorCache.Add(new WeightedVector() { vector = hand.Fingers[2].TipPosition.ToVector3(), weight = Vector3.Dot(hand.Fingers[2].bones[0].Direction.ToVector3(), hand.DistalAxis()) });
      s_weightedVectorCache.Add(new WeightedVector() { vector = hand.Fingers[3].TipPosition.ToVector3(), weight = Vector3.Dot(hand.Fingers[3].bones[0].Direction.ToVector3(), hand.DistalAxis()) });
      s_weightedVectorCache.Add(new WeightedVector() { vector = hand.Fingers[4].TipPosition.ToVector3(), weight = Vector3.Dot(hand.Fingers[4].bones[0].Direction.ToVector3(), hand.DistalAxis()) });

      return CalculateWeightedAverage(s_weightedVectorCache);
    }

    private static Vector3 CalculateWeightedAverage(List<WeightedVector> weightedVectors) {
      float weightSum = 0F;
      foreach (WeightedVector weightedVector in weightedVectors) {
        weightSum += weightedVector.weight;
      }
      float weightNormalizer = 1F / weightSum;

      Vector3 weightedAverage = Vector3.zero;
      foreach (WeightedVector weightedVector in weightedVectors) {
        weightedAverage += weightedVector.vector * weightedVector.weight * weightNormalizer;
      }

      return weightedAverage;
    }

  }

}