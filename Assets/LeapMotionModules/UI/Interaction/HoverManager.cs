using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public enum InterfaceMode {
    Ambidextrous,
    Lefthand,
    Righthand
  }

  public class HoverManager : MonoBehaviour {

    public InterfaceMode interfaceMode;

    private HashSet<Hoverable> _hovered = new HashSet<Hoverable>();
    private HashSet<Hoverable> _notHovered = new HashSet<Hoverable>();
    private Hoverable _leftPrimary = null;
    private Hoverable _rightPrimary = null;

    /// <summary> Called automatically by Hoverables childed somewhere underneath a HoverManager transform. </summary>
    public void Add(Hoverable selectable) {
      _notHovered.Add(selectable);
    }

    private struct HoverCheckResults {
      public HashSet<Hoverable> hovered;
      public Hoverable          primaryHovered;
      public float              primaryHoveredScore;
      public Hand               checkedHand;
    }

    void Update() {
      Hand lHand = Hands.Left;
      Hand rHand = Hands.Right;
      bool checkRightHand = false, checkLeftHand = false;

      if (interfaceMode == InterfaceMode.Ambidextrous && lHand != null && rHand != null) {
        checkRightHand = true;
        checkLeftHand = true;
      }
      else if ((interfaceMode == InterfaceMode.Ambidextrous || interfaceMode == InterfaceMode.Lefthand) && lHand != null) {
        checkLeftHand = true;
      }
      else if ((interfaceMode == InterfaceMode.Ambidextrous || interfaceMode == InterfaceMode.Righthand) && rHand != null) {
        checkRightHand = true;
      }

      if (checkLeftHand) {
        ProcessHoverCheckResults(CheckHoverForHand(lHand));
      }
      if (checkRightHand) {
        ProcessHoverCheckResults(CheckHoverForHand(rHand));
      }
    }

    private HashSet<Hoverable> _hoverableCache = new HashSet<Hoverable>();
    private HoverCheckResults CheckHoverForHand(Hand hand) {
      _hoverableCache.Clear();
      HoverCheckResults results = new HoverCheckResults() { hovered = _hoverableCache,
                                                            primaryHovered = null,
                                                            primaryHoveredScore = float.NegativeInfinity,
                                                            checkedHand = hand };

      foreach (var hoverable in _notHovered) {
        results = CheckHoverForElement(hand, hoverable, results);
      }
      foreach (var hoverable in _hovered) {
        results = CheckHoverForElement(hand, hoverable, results);
      }

      return results;
    }

    private HoverCheckResults CheckHoverForElement(Hand hand, Hoverable hoverable, HoverCheckResults curResults) {
      float score = hoverable.GetHoverScore(hand);
      if (score > 0F) {
        curResults.hovered.Add(hoverable);
      }
      if (score > curResults.primaryHoveredScore) {
        curResults.primaryHovered = hoverable;
        curResults.primaryHoveredScore = score;
      }
      return curResults;
    }

    private List<Hoverable> _hoveredRemovalCache = new List<Hoverable>();
    private void ProcessHoverCheckResults(HoverCheckResults hoverResults) {
      // General hovering
      foreach (var hoverable in _hovered) {
        if (hoverResults.hovered.Contains(hoverable)) {
          hoverable.OnHoverStay(hoverResults.checkedHand);
        }
        else {
          hoverable.OnHoverEnd(hoverResults.checkedHand);
          _hoveredRemovalCache.Add(hoverable);
        }
      }
      foreach (var hoverable in _notHovered) {
        if (hoverResults.hovered.Contains(hoverable)) {
          hoverable.OnHoverBegin(hoverResults.checkedHand);
          _hovered.Add(hoverable);
        }
      }
      foreach (var hoverable in _hoveredRemovalCache) {
        _hoverableCache.Remove(hoverable);
      }

      // Primary hover
      Hoverable curPrimary = hoverResults.checkedHand.IsLeft ? _leftPrimary : _rightPrimary;
      if (hoverResults.primaryHovered == curPrimary) {
        hoverResults.primaryHovered.OnPrimaryHoverStay(hoverResults.checkedHand);
      }
      else {
        if (hoverResults.checkedHand.IsLeft) {
          if (_leftPrimary != null) _leftPrimary.OnPrimaryHoverEnd(hoverResults.checkedHand);
          _leftPrimary = curPrimary = hoverResults.primaryHovered;
        }
        else {
          if (_rightPrimary != null) _rightPrimary.OnPrimaryHoverEnd(hoverResults.checkedHand);
          _rightPrimary = curPrimary = hoverResults.primaryHovered;
        }
        curPrimary.OnPrimaryHoverBegin(hoverResults.checkedHand);
      }
    }

  }

}