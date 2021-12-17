using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResetButton : MonoBehaviour
{
    Board board;
    [SerializeField] private Button button;

    private void Awake() {
        button.onClick.AddListener(() => {
            board = GameObject.FindObjectOfType<Board>();
            AIBattleController aiBattleController = board?.GetComponent<AIBattleController>();
            
            int blackAI = aiBattleController != null ? aiBattleController.selectedBlackAI : 0;
            int whiteAI = aiBattleController != null ? aiBattleController.selectedWhiteAI : 0;

            SceneManager.activeSceneChanged += ResetAI;

            board?.Reset();

            void ResetAI(Scene arg0, Scene arg1)
            {
                Board b = GameObject.FindObjectOfType<Board>();
                AIBattleController aiController = b?.GetComponent<AIBattleController>();
                aiController?.SetAI(whiteAI, blackAI);
                aiController?.StartGame();
                SceneManager.activeSceneChanged -= ResetAI;
            }
        });
    }
}