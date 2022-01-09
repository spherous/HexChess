using System.Collections.Generic;
using Extensions;
using UnityEngine;

public class LiftOnHover : MonoBehaviour
{
    [SerializeField] private Hex hex;
    [SerializeField] private AudioSource source;
    public List<AudioClip> clips = new List<AudioClip>();
    public Color orangeColor;
    private Color orgColor;
    public float raiseDuration = 0.333f;
    public float power = 0.333f;
    private float ellapsedDuration;
    private bool transitioning = false;
    private TransitionMode mode;

    public float from = 1f;
    public float to = 3f;

    public bool isUp => transform.localPosition.y == to;
    public bool isDown => transform.localPosition.y == from;

    public bool lockedUp {get; private set;} = false;
    public bool lockedDown {get; private   set;} = false;

    private void Update()
    {
        if(transitioning)
        {
            float goal = mode == TransitionMode.In ? to : from;
            float t = Mathf.Clamp01(ellapsedDuration/raiseDuration);
            
            float pow = Mathf.Lerp(0, power, t);
            hex.SetHighlightPower(pow);

            transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(from, to, t), transform.localPosition.z);
            
            if(transform.localPosition.y == goal)
            {
                transitioning = false;
                ellapsedDuration = 0;
            } 
            else
            {
                int mod = mode == TransitionMode.In ? 1 : -1;
                ellapsedDuration += Time.deltaTime * mod;
            }
        }
    }

    public void LockUp()
    {
        if(lockedDown)
            Unlock();
        lockedUp = true;
    }
    public void LockDown()
    {
        if(lockedUp)
            Unlock();
        lockedDown = true;
        if(isUp)
            Reset();
    }

    public void Unlock()
    {
        lockedUp = false;
        lockedDown = false;
    }

    public void Lift()
    {
        if(!this.enabled || lockedDown)
            return;

        // If the hex is locked up and is up, we still want to play the mouse over audio
        source.PlayOneShot(clips.ChooseRandom());
        
        if(lockedUp && isUp)
            return;

        transitioning = true;
        mode = TransitionMode.In;
        orgColor = hex.GetOutlineColor();
        hex.SetOutlineColor(orangeColor);
    }

    public void Reset()
    {
        if(lockedUp)
            return;
        
        if(lockedDown && isDown)
            return;

        transitioning = true;
        if(ellapsedDuration == 0)
            ellapsedDuration = raiseDuration;
        mode = TransitionMode.Out;
        hex.SetOutlineColor(orgColor);
    }
}