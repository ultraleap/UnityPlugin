/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public class LeapTextGraphic : LeapGraphic {

    [TextArea]
    [SerializeField]
    private string _text;

    [Header("Character")]
    [EditTimeOnly, SerializeField]
    private FontStyle _fontStyle;

    [EditTimeOnly, SerializeField]
    private int _fontSize = 14;

    [EditTimeOnly, SerializeField]
    private float _lineSpacing = 1;

    [Header("Paragraph")]
    [EditTimeOnly, SerializeField]
    private HorizontalAlignment _horizontalAlignment;

    [EditTimeOnly, SerializeField]
    private VerticalAlignment _verticalAlignment;

    [EditTimeOnly, SerializeField]
    private Color _color = Color.black;

    private bool _tokensDirty = true;
    private List<TextWrapper.Token> _cachedTokens = new List<TextWrapper.Token>();

    public List<TextWrapper.Token> tokens {
      get {
        if (_tokensDirty) {
          _cachedTokens.Clear();
          TextWrapper.Tokenize(text, _cachedTokens);
          _tokensDirty = false;
        }
        return _cachedTokens;
      }
    }

    public string text {
      get {
        if (_text == null) {
          return "";
        } else {
          return _text;
        }
      }
      set {
        if (value != _text) {
          _tokensDirty = true;
          _text = value;
          isRepresentationDirty = true;
        }
      }
    }

    public FontStyle fontStyle {
      get {
        return _fontStyle;
      }
      set {
        if (value != _fontStyle) {
          _fontStyle = value;
          isRepresentationDirty = true;
        }
      }
    }

    public int fontSize {
      get {
        return _fontSize;
      }
      set {
        if (value != _fontSize) {
          _fontSize = value;
          isRepresentationDirty = true;
        }
      }
    }

    public float lineSpacing {
      get {
        return _lineSpacing;
      }
      set {
        if (value != _lineSpacing) {
          _lineSpacing = value;
          isRepresentationDirty = true;
        }
      }
    }

    public HorizontalAlignment horizontalAlignment {
      get {
        return _horizontalAlignment;
      }
      set {
        if (value != _horizontalAlignment) {
          _horizontalAlignment = value;
          isRepresentationDirty = true;
        }
      }
    }

    public VerticalAlignment verticalAlignment {
      get {
        return _verticalAlignment;
      }
      set {
        if (value != _verticalAlignment) {
          _verticalAlignment = value;
          isRepresentationDirty = true;
        }
      }
    }

    public Color color {
      get {
        return _color;
      }
      set {
        if (value != _color) {
          _color = value;
          isRepresentationDirty = true;
        }
      }
    }

    protected override void OnValidate() {
      base.OnValidate();

      _tokensDirty = true;
    }

    private Rect _prevRect;
    public bool HasRectChanged() {
      RectTransform rectTransform = transform as RectTransform;
      if (rectTransform == null) {
        return false;
      }

      Rect newRect = rectTransform.rect;
      if (newRect != _prevRect) {
        _prevRect = newRect;
        return true;
      }

      return false;
    }

    public enum HorizontalAlignment {
      Left,
      Center,
      Right
    }

    public enum VerticalAlignment {
      Top,
      Center,
      Bottom
    }
  }
}
