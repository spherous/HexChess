using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneNavigation : EditorWindow
{    
    [MenuItem("Scenes/MainMenu")]
    public static void GoToMainMenu() => EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    [MenuItem("Scenes/Lobby")]
    public static void GoToLobby() => EditorSceneManager.OpenScene("Assets/Scenes/ConnectionChoice.unity");
    [MenuItem("Scenes/Sandbox")]
    public static void GoToSandbox() => EditorSceneManager.OpenScene("Assets/Scenes/SandboxMode.unity");
    [MenuItem("Scenes/Versus")]
    public static void GoToVersus() => EditorSceneManager.OpenScene("Assets/Scenes/VersusMode.unity");
    [MenuItem("Scenes/Credits")]
    public static void GoToCredits() => EditorSceneManager.OpenScene("Assets/Scenes/Credits.unity");
}