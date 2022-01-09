using UnityEngine;

public class LoadingDial : MonoBehaviour
{
    public float delay;
    float rotateAtTime;
    RectTransform rt;
    public Vector3 rotPerStep = new Vector3();
    private void Awake() {
        rt = (RectTransform)transform;
        rotateAtTime = Time.timeSinceLevelLoad + delay;
    }   

    private void Update() {
        if(Time.timeSinceLevelLoad >= rotateAtTime)
        {
            Vector3 currentRot = rt.rotation.eulerAngles;
            rt.rotation = Quaternion.Euler(currentRot + rotPerStep);
            rotateAtTime = Time.timeSinceLevelLoad + delay;
        }
    }
}