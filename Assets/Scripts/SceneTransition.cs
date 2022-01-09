using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    public float transitionTime = 0.2f;
    private float ellapsedTime = 0;
    private bool transitioning = false;
    private TransitionMode mode;
    private string toLoad;
    private void Awake() 
    {
        SceneTransition[] existingSTs = GameObject.FindObjectsOfType<SceneTransition>();
        if(existingSTs.Length > 1)
            Destroy(gameObject);

        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
        DontDestroyOnLoad(gameObject);
    } 

    private void Update() {
        if(transitioning)
        {
            ellapsedTime += Time.deltaTime;
            group.alpha = Mathf.Lerp(mode == TransitionMode.In ? 1 : 0, mode == TransitionMode.In ? 0 : 1, ellapsedTime / transitionTime);
            if(ellapsedTime >= transitionTime && mode == TransitionMode.Out)
            {
                SceneManager.activeSceneChanged += Flip;
                SceneManager.LoadScene(toLoad, LoadSceneMode.Single);
            }
            else if(ellapsedTime >= transitionTime)
            {
                transitioning = false;
                group.interactable = false;
                group.blocksRaycasts = false;
                group.alpha = 0;
            }
        }
    }

    private void Flip(Scene arg0, Scene arg1)
    {
        ellapsedTime = 0;
        mode = TransitionMode.In;
        SceneManager.activeSceneChanged -= Flip;
    }

    public void Transition(string sceneName)
    {
        if(transitioning)
            return;
        toLoad = sceneName;
        mode = TransitionMode.Out;
        ellapsedTime = 0;
        transitioning = true;
        group.interactable = true;
        group.blocksRaycasts = true;
    }
}

public enum TransitionMode {Out, In};