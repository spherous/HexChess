using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HexmachinaAcademy : MonoBehaviour
{
    public List<string> lessonScenes = new List<string>();

    private void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        DontDestroyOnLoad(gameObject);
    } 

    private void EnvironmentReset()
    {
        int lessonVal = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("Lessons", 0);

        string lessonToLoad = lessonScenes[lessonVal];

        SceneManager.LoadScene(lessonToLoad, LoadSceneMode.Single);
    }
}