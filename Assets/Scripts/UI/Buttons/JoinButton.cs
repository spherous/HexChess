using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoinButton : TwigglyButton
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private GameObject state1Container;
    [SerializeField] private GameObject state2Container;

    private new void Awake() {
        base.Awake();
        base.onClick += Clicked;
    }

    private void Clicked()
    {
        titleText.text = "JOIN";
        state2Container.SetActive(true);
        state1Container.SetActive(false);
        EventSystem.current.SetSelectedGameObject(ipInput.gameObject);
    }
}