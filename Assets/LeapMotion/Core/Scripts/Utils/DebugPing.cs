using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  [ExecuteInEditMode]
  public class DebugPing : MonoBehaviour, IRuntimeGizmoComponent {

    public const string PING_OBJECT_NAME = "__Debug Ping Runner__";

    public const float PING_DURATION = 0.25f;

    private static DebugPing s_instance = null;

    private static void ensurePingRunnerExists() {
      if (s_instance == null) {
        s_instance = Utils.FindObjectInHierarchy<DebugPing>();

        if (s_instance == null) {
          s_instance = new GameObject(PING_OBJECT_NAME).AddComponent<DebugPing>();
        }
      }
    }

    private void Update() {
      updatePings();
      updateLines();
      //updateLabels(); // TODO: Support Label pings without Lemur dependency.
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawPingGizmos(drawer);
      drawLineGizmos(drawer);
      //drawLabelGizmos(drawer);  // TODO: Support Label pings without Lemur
                                  // dependency.
    }

    #region DebugPing.Ping

    public const float DEFAULT_PING_RADIUS = 0.10f;

    public static AnimationCurve pingAnimCurve = DefaultCurve.SigmoidUp;

    public enum ShapeType {
      Sphere,
      Capsule,
      Cone
    }

    public enum AnimType {
      Expand,
      Fade,
      ExpandAndFade
    }

    public struct PingState {
      public Vector3 position0, position1;
      public Func<Vector3> position0Func, position1Func;
      public Quaternion rotation0;
      public float sizeMultiplier;
      public float time;
      public Color color;
      public ShapeType shapeType;
      public AnimType animType;
    }

    private static void Ping(Vector3 worldPosition0,
                             Vector3 worldPosition1 = default(Vector3),
                             Func<Vector3> worldPosition0Func = null,
                             Func<Vector3> worldPosition1Func = null,
                             Quaternion rotation0 = default(Quaternion),
                             float sizeMultiplier = 1f,
                             Color color = default(Color),
                             AnimType animType = default(AnimType),
                             ShapeType shapeType = default(ShapeType)) {
      ensurePingRunnerExists();

      rotation0 = rotation0.ToNormalized();

      s_instance.AddPing(new PingState() {
        position0 = worldPosition0,
        position1 = worldPosition1,
        position0Func = worldPosition0Func,
        position1Func = worldPosition1Func,
        rotation0 = rotation0,
        sizeMultiplier = sizeMultiplier,
        time = 0f,
        color = color,
        animType = animType,
        shapeType = shapeType
      });
    }

    #region Static Ping API

    public static void Ping(Pose worldPose) {
      Ping(worldPose, Color.white, 1f);
    }

    public static void Ping(Vector3 worldPosition) {
      Ping(worldPosition, Color.white, 1f);
    }

    public static void Ping(Vector3 worldPosition, Color color) {
      Ping(worldPosition, color, 1f);
    }

    public static void Ping(Pose worldPose, Color color) {
      Ping(worldPose, color, 1f);
    }

    public static void Ping(Func<Vector3> worldPositionFunc,
                            Color color,
                            float sizeMultiplier,
                            AnimType animType = AnimType.Expand) {
      Ping(
        Vector3.zero,
        worldPosition0Func: worldPositionFunc,
        color: color,
        sizeMultiplier: sizeMultiplier,
        animType: animType
      );
    }

    public static void Ping(Vector3 worldPosition,
                            Color color,
                            float sizeMultiplier,
                            AnimType animType = AnimType.Expand) {
      Ping(
        worldPosition,
        Vector3.zero,
        color: color,
        sizeMultiplier: sizeMultiplier,
        animType: animType
      );
    }

    public static void Ping(Pose worldPose,
                            Color color,
                            float sizeMultiplier,
                            AnimType animType = AnimType.Expand) {
      Ping(
        worldPose.position,
        default(Vector3),
        null, null,
        worldPose.rotation,
        color: color,
        sizeMultiplier: sizeMultiplier,
        animType: animType
      );
    }

    public static void PingCapsule(Func<Vector3> worldPosition0Func,
                                   Func<Vector3> worldPosition1Func,
                                   Color color,
                                   float sizeMultiplier,
                                   AnimType animType = AnimType.Expand) {
      Ping(
        Vector3.zero,
        worldPosition0Func: worldPosition0Func,
        worldPosition1Func: worldPosition1Func,
        color: color,
        sizeMultiplier: sizeMultiplier,
        shapeType: ShapeType.Capsule,
        animType: animType
      );
    }

    public static void PingCapsule(Vector3 worldPosition0,
                                   Vector3 worldPosition1,
                                   Color color,
                                   float sizeMultiplier,
                                   AnimType animType = AnimType.Expand) {
      Ping(
        worldPosition0, worldPosition1,
        color: color,
        sizeMultiplier: sizeMultiplier,
        shapeType: ShapeType.Capsule,
        animType: animType
      );
    }

    public static void PingCone(Vector3 worldPosition0,
                                Vector3 worldPosition1,
                                Color color,
                                float sizeMultiplier,
                                AnimType animType = AnimType.Expand) {
      Ping(
        worldPosition0,
        worldPosition1,
        color: color,
        sizeMultiplier: sizeMultiplier,
        animType: animType,
        shapeType: ShapeType.Cone
      );
    }

    public static void PingCone(Func<Vector3> worldPosition0Func,
                                Func<Vector3> worldPosition1Func,
                                Color color,
                                float sizeMultiplier,
                                AnimType animType = AnimType.Expand) {
      Ping(
        Vector3.zero,
        worldPosition0Func: worldPosition0Func,
        worldPosition1Func: worldPosition1Func,
        color: color,
        sizeMultiplier: sizeMultiplier,
        animType: animType,
        shapeType: ShapeType.Cone
      );
    }

    #endregion

    private List<PingState> _activePings = new List<PingState>();

    public void AddPing(PingState ping) {
      _activePings.Add(ping);
    }

    private void updatePings() {
      var indicesToRemove = Pool<List<int>>.Spawn();
      try {
        for (int i = 0; i < _activePings.Count; i++) {
          var curPing = _activePings[i];

          curPing.time += Time.deltaTime;

          if (curPing.time > 1f) {
            indicesToRemove.Add(i);
          }

          _activePings[i] = curPing;
        }

        for (int i = indicesToRemove.Count - 1; i >= 0; i--) {
          _activePings.RemoveAt(i);
        }
      }
      finally {
        indicesToRemove.Clear();
        Pool<List<int>>.Recycle(indicesToRemove);
      }
    }

    public void drawPingGizmos(RuntimeGizmoDrawer drawer) {
      Color pingColor;
      float pingSize;
      float animTime;
      Vector3 pingPos0, pingPos1;
      Quaternion pingRot0;
      foreach (var ping in _activePings) {

        if (ping.position0Func != null) {
          pingPos0 = ping.position0Func();
        }
        else {
          pingPos0 = ping.position0;
        }

        if (ping.position1Func != null) {
          pingPos1 = ping.position1Func();
        }
        else {
          pingPos1 = ping.position1;
        }

        pingRot0 = ping.rotation0;

        pingColor = ping.color;

        animTime = Mathf.Lerp(0f, 1f, ping.time / PING_DURATION);
        animTime = pingAnimCurve.Evaluate(animTime);

        pingSize = ping.sizeMultiplier * DEFAULT_PING_RADIUS;

        switch (ping.animType) {
          case AnimType.Expand:
            pingSize = pingSize * animTime;
            break;
          case AnimType.Fade:
            pingColor = ping.color.WithAlpha(1f - animTime);
            break;
          case AnimType.ExpandAndFade:
            pingSize = pingSize * animTime;
            pingColor = ping.color.WithAlpha(1f - animTime);
            break;
        }


        drawer.color = pingColor;

        switch (ping.shapeType) {
          case ShapeType.Sphere:
            drawer.DrawWireSphere(pingPos0, pingRot0, pingSize);
            break;
          case ShapeType.Capsule:
            drawer.DrawWireCapsule(pingPos0, pingPos1, pingSize);
            break;
          case ShapeType.Cone:
            drawer.DrawCone(pingPos0, pingPos1, pingSize);
            break;
        }

      }
    }

    #endregion

    #region DebugPing.Line

    public abstract class PingObject {
      public Color color = LeapColor.white;
      public float time = 0f;
      public float lifespan = PING_DURATION;

      public abstract void Draw(RuntimeGizmoDrawer drawer);
    }

    public class PingLine : PingObject {
      public Func<Vector3> p0Func;
      public Vector3 p0;
      public Func<Vector3> p1Func;
      public Vector3 p1;

      private Vector3 getP0() {
        if (p0Func != null) return p0Func();
        return p0;
      }

      private Vector3 getP1() {
        if (p1Func != null) return p1Func();
        return p1;
      }

      public override void Draw(RuntimeGizmoDrawer drawer) {
        var p0 = getP0(); var p1 = getP1();
        drawer.DrawLine(p0, p1);
      }
    }

    private static Dictionary<string, PingLine> _pingLines = new Dictionary<string, PingLine>();

    public static void Line(string lineIdentifier, Vector3 p0, Vector3 p1, Color color) {
      ensurePingRunnerExists();

      PingLine line;
      if (!_pingLines.TryGetValue(lineIdentifier, out line)) {
        _pingLines[lineIdentifier] = line = new PingLine();
      }
      line.p0 = p0;
      line.p1 = p1;
      line.color = color;
    }

    private static void updateLines() {
      var identifiersToRemove = Pool<HashSet<string>>.Spawn();
      try {
        foreach (var idLinePair in _pingLines) {
          var curLine = idLinePair.Value;

          curLine.time += Time.deltaTime;

          if (curLine.time > curLine.lifespan) {
            identifiersToRemove.Add(idLinePair.Key);
          }
        }
        foreach (var idToRemove in identifiersToRemove) {
          _pingLines.Remove(idToRemove);
        }
      }
      finally {
        identifiersToRemove.Clear();
        Pool<HashSet<string>>.Recycle(identifiersToRemove);
      }
    }

    private static void drawLineGizmos(RuntimeGizmoDrawer drawer) {
      var color = drawer.color;
      foreach (var idLinePair in _pingLines) {
        if (idLinePair.Value.color != color) {
          color = drawer.color = idLinePair.Value.color;
        }
        idLinePair.Value.Draw(drawer);
      }
    }

    #endregion

    #region DebugPing.Label

    // public class PingLabel : PingObject {
    //   private Label _backingLabel;
    //   public Label label {
    //     get {
    //       if (_backingLabel == null) {
    //         _backingLabel = Lemur.Default<Label>();
    //       }
    //       return _backingLabel;
    //     }
    //   }

    //   public string text = "";

    //   public Func<Vector3> overrideFacingPositionFunc;
    //   public Vector3? overrideFacingPosition; // otherwise Camera.main.transform.position
    //   private Vector3 getFacingPosition() {
    //     if (overrideFacingPositionFunc != null) {
    //       return overrideFacingPositionFunc();
    //     }
    //     else if (overrideFacingPosition.HasValue) {
    //       return overrideFacingPosition.Value;
    //     }
    //     else {
    //       var cam = Camera.main;
    //       if (cam != null) {
    //         return cam.transform.position;
    //       }
    //       else {
    //         return Vector3.zero;
    //       }
    //     }
    //   }

    //   public Func<Vector3> labeledPositionFunc;
    //   public Vector3 labeledPosition;
    //   private Vector3 getLabeledPosition() {
    //     if (labeledPositionFunc != null) {
    //       return labeledPositionFunc();
    //     }
    //     return labeledPosition;
    //   }

    //   public override void Draw(RuntimeGizmoDrawer drawer) {
    //     var facingPosition = getFacingPosition();
    //     var labeledPosition = getLabeledPosition();

    //     // Instead of "drawing", we're going to update the data for the Label,
    //     // which handles its own drawing.
    //     label.gameObject.transform.position = labeledPosition;
    //     label.gameObject.transform.rotation = Utils.FaceTargetWithoutTwist(
    //                                             labeledPosition,
    //                                             facingPosition,
    //                                             flip180: true);

    //     label.textStyle.Overlay(Style.Color(this.color));

    //     label.text = this.text;
    //   }
    // }

    // public static Dictionary<string, PingLabel> _labels
    //   = new Dictionary<string, PingLabel>();

    // public static void Label(string labelName,
    //                          string labelText,
    //                          Vector3 labeledPosition,
    //                          Color color) {
    //   ensurePingRunnerExists();

    //   PingLabel label;
    //   if (!_labels.TryGetValue(labelName, out label)) {
    //     _labels[labelName] = label = new PingLabel() {
    //       labeledPosition = labeledPosition,
    //       color = color,
    //       text = labelText
    //     };
    //   }
    //   else {
    //     label.labeledPosition = labeledPosition;
    //     label.color = color;
    //     label.text = labelText;
    //   }
    // }

    // private static void updateLabels() {
    //   var identifiersToRemove = Pool<HashSet<string>>.Spawn();
    //   try {
    //     foreach (var idLabelPair in _labels) {
    //       var curLabel = idLabelPair.Value;

    //       curLabel.time += Time.deltaTime;

    //       if (curLabel.time > curLabel.lifespan) {
    //         identifiersToRemove.Add(idLabelPair.Key);
    //       }
    //     }
    //     foreach (var idToRemove in identifiersToRemove) {
    //       _pingLines.Remove(idToRemove);
    //     }
    //   }
    //   finally {
    //     identifiersToRemove.Clear();
    //     Pool<HashSet<string>>.Recycle(identifiersToRemove);
    //   }
    // }

    // private static void drawLabelGizmos(RuntimeGizmoDrawer drawer) {
    //   foreach (var idLabelPair in _labels) {
    //     idLabelPair.Value.Draw(drawer);
    //   }
    // }

    #endregion

  }

  #region Drawing Extensions

  public static class RuntimeGizmoDrawerExtensions {

    public static void DrawWireSphere(this RuntimeGizmoDrawer drawer,
                                      Vector3 position,
                                      Quaternion rotation,
                                      float radius) {
      drawer.PushMatrix();

      drawer.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

      drawer.DrawWireSphere(Vector3.zero, radius);

      drawer.PopMatrix();
    }

    public static void DrawCone(this RuntimeGizmoDrawer drawer,
                                Vector3 pos0, Vector3 pos1,
                                float radius,
                                int resolution = 24) {
      var dir = pos1 - pos0;
      var R = dir.Perpendicular().normalized * radius;
      Quaternion rot = Quaternion.AngleAxis(360f / 24, dir);
      for (int i = 0; i < resolution; i++) {
        drawer.DrawLine(pos0 + R, pos1);
        var nextR = rot * R;
        drawer.DrawLine(pos0 + R, pos0 + nextR);
        R = nextR;
      }
    }

  }

  #endregion

}