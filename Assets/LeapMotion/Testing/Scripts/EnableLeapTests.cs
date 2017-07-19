using UnityEditor;

public static class EnableLeapTests {

  [MenuItem("Assets/Enable Leap Tests")]
  public static void enableTests() {
    string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines + " LEAP_TESTS");
  }
}
