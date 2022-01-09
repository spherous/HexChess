using System;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.EventSystems;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private Timers timers;
    VirtualCursor cursor;

    private void Awake() 
    {
        if(board == null)
            board = GameObject.FindObjectOfType<Board>();
        
        cursor = GameObject.FindObjectOfType<VirtualCursor>();
    }

    public void Save()
    {
        // Prevents it from opening too many save file browsers
        EventSystem.current.SetSelectedGameObject(null);

        string path = Application.persistentDataPath + $"/saves";
        Directory.CreateDirectory(path);
        
        cursor?.SetCursor(CursorType.None);

        string file = StandaloneFileBrowser.SaveFilePanel(
            title: "Save File", 
            directory: path, 
            defaultName: $"/{DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")}.json", 
            extensions: new []{
                new ExtensionFilter("Json Files", "json"),
                new ExtensionFilter("All FIles", "*")
            }
        );

        cursor?.SetCursor(CursorType.Default);

        if(string.IsNullOrEmpty(file))
        {
            Debug.Log("Failed to save to file. Path empty.");
            return;
        }

        File.WriteAllText(
            file, 
            board.currentGame.Serialize()
        );

        Debug.Log($"Saved to file: {file}");
    }
}