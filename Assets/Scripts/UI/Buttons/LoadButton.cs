using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

public class LoadButton : MonoBehaviour
{
    [SerializeField] private Board board;
    VirtualCursor cursor;

    private void Awake()
    {
        if(board == null)
            board = GameObject.FindObjectOfType<Board>();
        cursor = GameObject.FindObjectOfType<VirtualCursor>();
    }

    public void Load()
    {
        ExtensionFilter[] extensions = new []{
            new ExtensionFilter("Json Files", "json"),
            new ExtensionFilter("All FIles", "*")
        };
        string path = Application.persistentDataPath + $"/saves";
        
        // cursor?.SetCursor(CursorType.None);
        Cursor.visible = true;

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", path, extensions, false);

        Cursor.visible = false;

        if(paths.Length > 0)
            board.LoadGame(Game.Deserialize(File.ReadAllText(paths.First())));
    }
}
