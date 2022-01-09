using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneNavigation : EditorWindow
{    
    [MenuItem("Scenes/MainMenu")]
    public static void GoToMainMenu() => EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    [MenuItem("Scenes/Sandbox")]
    public static void GoToSandbox() => EditorSceneManager.OpenScene("Assets/Scenes/SandboxMode.unity");
    [MenuItem("Scenes/Versus")]
    public static void GoToVersus() => EditorSceneManager.OpenScene("Assets/Scenes/VersusMode.unity");
    
    [MenuItem("Scenes/Credits")]
    public static void GoToCredits() => EditorSceneManager.OpenScene("Assets/Scenes/Credits.unity");

}