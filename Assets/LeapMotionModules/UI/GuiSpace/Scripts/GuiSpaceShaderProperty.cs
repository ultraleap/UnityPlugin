using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  public abstract class GuiSpaceShaderProperty<T> {
    protected T[] _array;
    protected string _property;
    private bool _dirty;

    public GuiSpaceShaderProperty(string property, int size) {
      _array = new T[size];
      _property = property;
      _dirty = false;
    }

    public void UploadIfDirty() {
      if (_dirty) {
        doShaderUpload();
        _dirty = false;
      }
    }

    protected abstract void doShaderUpload();

    public T this[int index] {
      get {
        return _array[index];
      }
      set {
        if (!_array[index].Equals(value)) {
          _dirty = true;
          _array[index] = value;
        }
      }
    }
  }

  public class GuiSpaceFloatProperty : GuiSpaceShaderProperty<float> {

    public GuiSpaceFloatProperty(string propertyName, int size) : base(propertyName, size) { }

    protected override void doShaderUpload() {
      Shader.SetGlobalFloatArray(_property, _array);
    }
  }

  public class GuiSpaceVectorProperty : GuiSpaceShaderProperty<Vector4> {

    public GuiSpaceVectorProperty(string propertyName, int size) : base(propertyName, size) { }

    protected override void doShaderUpload() {
      Shader.SetGlobalVectorArray(_property, _array);
    }
  }

  public class GuiSpaceMatrixProperty : GuiSpaceShaderProperty<Matrix4x4> {

    public GuiSpaceMatrixProperty(string propertyName, int size) : base(propertyName, size) { }

    protected override void doShaderUpload() {
      Shader.SetGlobalMatrixArray(_property, _array);
    }
  }
}
