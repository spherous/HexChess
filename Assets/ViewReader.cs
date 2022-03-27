using UnityEngine;
using TMPro;

public class ViewReader : MonoBehaviour
{
    [SerializeField] SmoothHalfOrbitalCamera orbitalCamera;
    [SerializeField] TMP_Text text;

    void Start()
    {
        text.text = orbitalCamera.View.name;
    }

    public void OnClickPrev()
    {
        orbitalCamera.PrevView();
        text.text = orbitalCamera.View.name;
    }
    public void OnClickNext()
    {
        orbitalCamera.NextView();
        text.text = orbitalCamera.View.name;
    }
}