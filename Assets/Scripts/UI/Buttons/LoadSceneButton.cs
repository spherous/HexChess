using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneButton : MonoBehaviour
{
    [SerializeField] private Button button;
    public string sceneToLoad;
    private void Awake() {
        if(button == null)
            gameObject.GetComponent<Button>();
        
        SceneTransition sceneTransition = GameObject.FindObjectOfType<SceneTransition>();

        button?.onClick.AddListener(() => {
            if(sceneTransition != null)
                sceneTransition.Transition($"{sceneToLoad}");
            else
                SceneManager.LoadScene($"{sceneToLoad}");

            if(sceneToLoad == "MainMenu") 
            {
                Networker networker = GameObject.FindObjectOfType<Networker>();
                if(networker != null)
                    Destroy(networker.gameObject);
            }
        });
    }
}