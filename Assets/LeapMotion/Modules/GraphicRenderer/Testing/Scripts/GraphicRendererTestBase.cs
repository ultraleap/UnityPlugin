/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using System;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public abstract class GraphicRendererTestBase : LeapTestBase {

    protected LeapGraphicRenderer renderer { get; private set; }

    protected LeapGraphic oneGraphic { get; private set; }

    protected LeapGraphicGroup firstGroup { get; private set; }

    protected LeapGraphicGroup secondGroup { get; private set; }

    private string _sceneToUnload;

    [TearDown]
    protected virtual void Teardown() {
      UnityEngine.Object.DestroyImmediate(renderer.gameObject);

      if (!string.IsNullOrEmpty(_sceneToUnload)) {
        SceneManager.UnloadSceneAsync(_sceneToUnload);
        _sceneToUnload = null;
      }
    }

    /// <summary>
    /// Should be called at the start of the test.  The argument is 
    /// the name of the prefab to spawn, or the name of a gameobject
    /// in the current scene to load.  This object should have a
    /// graphic renderer component attached to the root.
    /// 
    /// This method will automatically populate the following fields:
    ///  - renderer : with the reference to the renderer on the base of the prefab
    ///  - oneGraphic : with a reference to one graphic that exists in the prefab, if any
    ///  - firstGroup : the first group attached to the renderer
    ///  - secondGroup : the second group attached to the renderer, if any
    /// </summary>
    protected override void InitTest(string objectName) {
      base.InitTest(objectName);

      renderer = testObj.GetComponent<LeapGraphicRenderer>();

      oneGraphic = renderer.GetComponentInChildren<LeapGraphic>(includeInactive: true);

      firstGroup = renderer.groups[0];

      secondGroup = renderer.groups.Count > 1 ? renderer.groups[1] : null;
    }

    /// <summary>
    /// Loads a scene with a given name.  This allows you to use InitTest
    /// to enable game objects that exist within that scene.  This also
    /// allows the scene to be automatically unloaded when the test ends.
    /// </summary>
    protected void LoadScene(string sceneName) {
      _sceneToUnload = sceneName;
      SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Should be called to spawn a graphic prefab and child it to the 
    /// renderer.  If the oneGraphic property has not yet been assigned to,
    /// this method will assign the newly spawned graphic to it.
    /// </summary>
    protected LeapGraphic CreateGraphic(string prefabName) {
      var prefab = EditorResources.Load<GameObject>(prefabName);
      var obj = UnityEngine.Object.Instantiate(prefab);
      obj.transform.SetParent(renderer.transform);

      var graphic = obj.GetComponent<LeapGraphic>();

      if (oneGraphic == null) {
        oneGraphic = graphic;
      }

      return graphic;
    }

    /// <summary>
    /// Makes a shallow copy of the object by copying the values of each instance field
    /// to a newly constructed object of the same type.
    /// </summary>
    protected object shallowCopy(object obj) {
      Assert.That(obj, Is.Not.Null);

      var copy = Activator.CreateInstance(obj.GetType());

      foreach (var field in copy.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
        field.SetValue(copy, field.GetValue(obj));
      }

      return copy;
    }

    /// <summary>
    /// Asserts that the two objects have equal values for all of their fields that
    /// are value types.  This method ignored variables that are reference types.
    /// </summary>
    protected void assertValueFieldsEqual(object a, object b) {
      Assert.That(a, Is.Not.Null);
      Assert.That(b, Is.Not.Null);

      Assert.That(a.GetType(), Is.EqualTo(b.GetType()));

      foreach (var field in a.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
        if (!field.FieldType.IsValueType) {
          continue;
        }

        var valueA = field.GetValue(a);
        var valueB = field.GetValue(b);
        Assert.That(valueA, Is.EqualTo(valueB), "Field " + field.Name + " did not match");
      }
    }
  }
}
#endif
