using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneObjectButton : MonoBehaviour, IObjectButton
{
    public string sceneToLoad;
    public void Click()
    {
        SceneTransition sceneTransition = GameObject.FindObjectOfType<SceneTransition>();

        if(sceneTransition != null)
            sceneTransition.Transition($"{sceneToLoad}");
        else
            SceneManager.LoadScene($"{sceneToLoad}");
    }

    public void HoverEnter(){}
    public void HoverExit(){}
}