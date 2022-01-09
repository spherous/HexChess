using UnityEngine;
using TMPro;

public class PopulatePatrons : MonoBehaviour
{
    [SerializeField] private TextAsset patronList;
    [SerializeField] private TextMeshProUGUI patronText;
    private void Awake() => patronText.text = patronList.text;
}