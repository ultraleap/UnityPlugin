using Leap.Unity;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public class DuplicatorHelper : MonoBehaviour {

    [Header("Hierarchy Setup")]

    [Tooltip("The parent object to contain the duplicates. Don't put anything "
      + "you don't want to lose as a child of this parent!")]
    [QuickButton("Clear Children!", "ClearDuplicationParentChildren")]
    public Transform duplicationParent;

    [Tooltip("The object to duplicate. Don't put the duplication source object "
      + "_itself_ in the duplication parent because it will get deleted when the "
      + "parent is cleared on re-duplication. "
      + "Careful: This duplication object will be fully cloned, including all "
      + "Components or children attached to it.")]
    public GameObject toDuplicate;

    [Tooltip("If the source duplication object is disabled, check this option to "
      + "automatically enable the duplicates cloned from it.")]
    public bool autoEnableDuplicates = false;

    [Header("Duplication")]

    [QuickButton("Duplicate!", "Duplicate")]
    [MinValue(0)]
    public int numWidthCopies = 1;
    [MinValue(0)]
    public int numHeightCopies = 1;

    public float horizontalSpacing = 0.10f;
    public float verticalSpacing = 0.10f;

    [Header("Names and Text In Duplicates")]
    [Tooltip("Use the textbox below this property to optionally set names "
      + "for the clones, delimited by newlines or commas.")]
    public bool setDuplicateNames = false;
    public bool searchAndSetTextMeshes = false;

    [Tooltip("Newline-or-comma delimited strings in column-major order for names "
      + "/ text meshes. Warning: The script only supports up to 256 names. If you "
      + "need more, edit or copy this script and modify the MAX_NAME_TOKENS "
      + "constant.")]
    [TextArea(6, 10)]
    public string sourceString = "";

    private const int MAX_NAME_TOKENS = 256;
    private string[] _childTokens = new string[MAX_NAME_TOKENS];

    public void Duplicate() {
      if (duplicationParent == null) {
        Debug.LogError("Can't duplicate without a duplication parent. Warning: "
          + "Pre-existing objects beneath the duplication parent will be destroyed.",
          this);
        return;
      }
      if (toDuplicate == null) {
        Debug.LogError("Can't duplicate without target GameObject toDuplicate.",
        this);
        return;
      }

      ClearDuplicationParentChildren();

      if (!string.IsNullOrEmpty(sourceString)) {
        var tokens = sourceString.Split(new char[] {',', '\n'}, MAX_NAME_TOKENS,
          System.StringSplitOptions.None);
        for (int i = 0; i < tokens.Length; i++) {
          tokens[i] = tokens[i].Trim();
        }
        tokens.CopyTo(_childTokens, 0);
      }

      for (int i = 0; i < numHeightCopies; i++) {
        for (int j = 0; j < numWidthCopies; j++) {
          var position = toDuplicate.transform.position
                        + j * horizontalSpacing * duplicationParent.transform.right * this.transform.lossyScale.x
                        + i * verticalSpacing * -duplicationParent.transform.up * this.transform.lossyScale.y;


          GameObject duplicate = GameObject.Instantiate(toDuplicate);
          duplicate.transform.parent = duplicationParent;
          duplicate.transform.position = position;
          duplicate.transform.rotation = toDuplicate.transform.rotation;
          duplicate.transform.localScale = toDuplicate.transform.localScale;

          if (autoEnableDuplicates) {
            duplicate.gameObject.SetActive(true);
          }

          if (setDuplicateNames || searchAndSetTextMeshes) {
            int k = i * numWidthCopies + j;

            if (setDuplicateNames) {
              duplicate.name = _childTokens[k];
            }
            if (searchAndSetTextMeshes) {
              var textMeshes = duplicate.GetComponentsInChildren<TextMesh>();
              foreach (var textMesh in textMeshes) {
                textMesh.text = _childTokens[k];
              }
            }
          }
        }
      }
    }

    public void ClearDuplicationParentChildren() {
      var numChildren = duplicationParent.childCount;
      for (int i = numChildren - 1; i >= 0; i--) {
        DestroyImmediate(duplicationParent.GetChild(i).gameObject);
      }
    }

  }

}
