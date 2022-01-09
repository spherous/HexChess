using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonArrowAnim : MonoBehaviour
{
    [SerializeField] private HorizontalLayoutGroup layoutGroup;
    [SerializeField] private GroupFader fader;
    public List<float> statePositions = new List<float>();
    private int currentState;
    
    public float animDelay;
    private float changeAtTime;

    private void Update() {
        if(!fader.visible)
            return;

        if(Time.timeSinceLevelLoad >= changeAtTime)
            Step();
    }

    private void Step()
    {
        changeAtTime = Time.timeSinceLevelLoad + animDelay;

        currentState = currentState + 1 > statePositions.Count - 1 ? 0 : currentState + 1;
        layoutGroup.spacing = statePositions[currentState];
    }

    public void Show()
    {
        currentState = 0;
        layoutGroup.spacing = statePositions[currentState];
        changeAtTime = Time.timeSinceLevelLoad + animDelay;
        fader.FadeIn();
    } 
    public void Hide() => fader.FadeOut();
}
