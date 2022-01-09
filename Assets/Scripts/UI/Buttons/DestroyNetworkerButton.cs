using UnityEngine;
using UnityEngine.UI;

public class DestroyNetworkerButton : MonoBehaviour
{
    [SerializeField] private Button button;
    private void Awake() => button.onClick.AddListener(() => DestroyNetworker());
    public void DestroyNetworker() => Destroy(GameObject.FindObjectOfType<Networker>()?.gameObject);
}