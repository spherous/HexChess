using UnityEngine;

public class SettingsButton : TwigglyButton
{
    [SerializeField] private GroupFader settingsPanel;

    private new void Awake() {
        base.Awake();
        onClick += () => {
            if(!settingsPanel.visible)
                settingsPanel.FadeIn();
            else
                settingsPanel.FadeOut();
        };
    }
}