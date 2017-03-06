using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing {

  public class Stroke {

    private List<StrokePoint> strokePoints;
    public IEnumerator<StrokePoint> GetPointsEnumerator() {
      return strokePoints.GetEnumerator();
    }
    public int Count { get { return strokePoints.Count; } }
    public StrokePoint this[int index] {
      get { return strokePoints[index]; }
      set { strokePoints[index] = value; }
    }

    public float timeCreated;
    public bool autoOrient;

    public Action<Stroke, StrokeModificationHint> OnStrokeModified = (x, y) => { };
    private List<int> _cachedModifiedPointIndices = new List<int>();

    public Stroke(bool autoOrient=true) {
      strokePoints = new List<StrokePoint>();
      this.autoOrient = autoOrient;
      timeCreated = Time.time;
    }

    public void Add(StrokePoint strokePoint) {
      strokePoints.Add(strokePoint);
      OnStrokeModified(this, StrokeModificationHint.AddedPoint());

      if (autoOrient && Count > 1) {
        StrokePoint p0 = strokePoints[Count - 2];
        StrokePoint p1 = strokePoints[Count - 1];
        Quaternion rotCorrection = StrokeUtil.CalculateRotation(p0, p1);
        p0.rotation = rotCorrection * p0.rotation;
        p1.rotation = p0.rotation;

        _cachedModifiedPointIndices.Clear();
        _cachedModifiedPointIndices.Add(Count - 2);
        _cachedModifiedPointIndices.Add(Count - 1);
        OnStrokeModified(this, StrokeModificationHint.ModifiedPoints(_cachedModifiedPointIndices));
      }
    }

    /// <summary>
    /// Returns the rotation at the given index by interpolating halfway between the strokePoints[index].rotation
    /// and the strokePoints[index + 1].rotation. (Also handles the edge cases at either end of the stroke.)
    /// </summary>
    public Quaternion OrientationAt(int index) {
      if (index == 0 || index == Count - 2 || index == Count - 1) return strokePoints[index].rotation;
      else return Quaternion.Slerp(strokePoints[index].rotation, strokePoints[index + 1].rotation, 0.5F);
    }

  }

  public enum StrokeModificationType {
    /// <summary>
    /// Overhaul modifications indicate that the whole stroke must
    /// be re-rendered from the beginning. This may be called when
    /// a new Stroke object is given to the StrokeFilter, or otherwise
    /// some fundamental property of the Stroke renderer is modified
    /// at runtime.
    /// </summary>
    Overhaul       = 1,

    /// <summary>
    /// Indicates the only modification to the Stroke object is a new
    /// StrokePoint at the end of the Stroke.
    /// </summary>
    AddedPoint     = 2,

    /// <summary>
    /// Indicates the only modification to the Stroke object is a series
    /// of new StrokePoints at the end of the Stroke.
    /// If the StrokeModificationType is AddedPoints, numPointsAdded will
    /// indicate how many points were added.
    /// </summary>
    AddedPoints    = 3,

    /// <summary>
    /// Indicates that StrokePoint objects were modified within the Stroke.
    /// The only valid modifications to StrokePoints are scales, rotations, and translations,
    /// so the vertices rendered for a given StrokePoint index (provided via pointsModified)
    /// only need to have their positions moved and re-uploaded accordingly.
    /// </summary>
    ModifiedPoints = 4
  }

  public class StrokeModificationHint {

    public StrokeModificationType modType;

    public List<int>    pointsModified;
    public int          numPointsAdded;

    private StrokeModificationHint(StrokeModificationType type) {
      this.modType = type;
    }

    public static StrokeModificationHint Overhaul() {
      var modHint = new StrokeModificationHint(StrokeModificationType.Overhaul);
      return modHint;
    }

    public static StrokeModificationHint AddedPoints(int numPointsAdded) {
      var modHint = new StrokeModificationHint(StrokeModificationType.AddedPoints);
      modHint.numPointsAdded = numPointsAdded;
      return modHint;
    }

    public static StrokeModificationHint AddedPoint() {
      var modHint = new StrokeModificationHint(StrokeModificationType.AddedPoint);
      return modHint;
    }

    public static StrokeModificationHint ModifiedPoints(List<int> pointsModified) {
      var modHint = new StrokeModificationHint(StrokeModificationType.ModifiedPoints);
      modHint.pointsModified = pointsModified;
      return modHint;
    }

  }

}