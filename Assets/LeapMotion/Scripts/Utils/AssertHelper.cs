using UnityEngine.Assertions;

public static class AssertHelper {

  public static void Implies(bool condition, bool result, string message = "") {
    if (condition) {
      Assert.IsTrue(result, message);
    }
  }

  public static void Implies(string conditionName, bool condition, string resultName, bool result) {
    Implies(condition, result, "When " + conditionName + " is true, " + resultName + " must always be true.");
  }

}
