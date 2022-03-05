using UnityEngine;
using TMPro;

public class ViewReader : MonoBehaviour
{
    [SerializeField] SmoothHalfOrbitalCamera orbitalCamera;
    [SerializeField] TMP_Text text;

    void Start()
    {
        int savedView = PlayerPrefs.GetInt("CameraView", orbitalCamera.SelectedView);

        text.text = orbitalCamera.View.name;
    }

    public void OnClick()
    {
        orbitalCamera.NextView();
        PlayerPrefs.SetInt("CameraView", orbitalCamera.SelectedView);
        text.text = orbitalCamera.View.name;
    }

}
