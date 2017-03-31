using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.Attributes;

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
  private Color _color = Color.white;

  private bool _tokensDirty = true;
  private List<TextWrapper.Token> _cachedTokens = new List<TextWrapper.Token>();

  public List<TextWrapper.Token> tokens {
    get {
      if (_tokensDirty) {
        _cachedTokens.Clear();
        TextWrapper.Tokenize(_text, _cachedTokens);
        _tokensDirty = false;
      }
      return _cachedTokens;
    }
  }

  public string text {
    get {
      return _text;
    }
    set {
      _tokensDirty = true;
      _text = value;
      isRepresentationDirty = true;
    }
  }

  public FontStyle fontStyle {
    get {
      return _fontStyle;
    }
    set {
      _fontStyle = value;
      isRepresentationDirty = true;
    }
  }

  public int fontSize {
    get {
      return _fontSize;
    }
    set {
      _fontSize = value;
      isRepresentationDirty = true;
    }
  }

  public float lineSpacing {
    get {
      return _lineSpacing;
    }
    set {
      _lineSpacing = value;
      isRepresentationDirty = true;
    }
  }

  public HorizontalAlignment horizontalAlignment {
    get {
      return _horizontalAlignment;
    }
    set {
      _horizontalAlignment = value;
      isRepresentationDirty = true;
    }
  }

  public VerticalAlignment verticalAlignment {
    get {
      return _verticalAlignment;
    }
    set {
      _verticalAlignment = value;
      isRepresentationDirty = true;
    }
  }

  public Color color {
    get {
      return _color;
    }
    set {
      _color = value;
      isRepresentationDirty = true;
    }
  }

  protected override void OnValidate() {
    base.OnValidate();

    _tokensDirty = true;
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
