using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothHalfOrbitalCamera : MonoBehaviour
{
    public float cameraDistance = 18;
    public Vector3 origin;
    public float speed = 0.2f;
    //[Range(0, 90)]
    //public float transitionAngle = 10;

    public float transitionTime = 0.25f;

    public Vector2 defaultRotation = Vector2.right * 90;
    Vector3 temp_rotation;

    //public bool flipCamera;
    //bool Flipped => Mathf.Abs(rotation.y) > 90;
    //[SerializeField] bool inTransition;
    //float elapsedTime;

    //float start, end;

    //bool rotateUp;
    //bool rotateDown;
    //bool rotateLeft;
    //bool rotateRight;

    //bool flipDebounce;

    //bool upIsDown;

    bool rotating;

    private void OnValidate()
    {
        defaultRotation.x = Mathf.Clamp(defaultRotation.x, 0, 90f);
        defaultRotation.y %= 360f;
        ResetRotation();
        LookTowardsOrigin();
    }

    private void Start()
    {
        ResetRotation();
    }

    [ContextMenu("Reset Rotation")]
    public void ResetRotation()
    {
        temp_rotation = defaultRotation;
        LookTowardsOrigin();
    }

    public void RightClick(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            rotating = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            //flipDebounce = false;
        }
        else if(context.canceled)
        {
            rotating = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            rotating = false;
            //flipDebounce = false;
            Cursor.lockState = CursorLockMode.None;
        }

        //if(inTransition)
        //{
        //    elapsedTime = Mathf.Clamp(elapsedTime + Time.deltaTime, 0, transitionTime);
        //    rotation.y = Mathf.Lerp(start, end, elapsedTime / transitionTime);

        //    LookTowardsOrigin();
        //    //rotateUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        //    if(elapsedTime >= transitionTime || !rotateUp)
        //    {
        //        inTransition = false;
        //        upIsDown = rotateUp;
        //    }
        //}
        //else 
        if(rotating)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * speed;
            temp_rotation += new Vector3(delta.y, delta.x);
            //rotation += delta;

            LookTowardsOrigin();

            //if(upIsDown)
            //    upIsDown = rotateUp && !rotateDown;
        }
    }

    public void LookTowardsOrigin()
    {
        //if(rotation.x > 90 - transitionAngle)
        //{
            //inTransition = Application.isPlaying && !flipDebounce && flipCamera;
            //if(inTransition)
            //    flipDebounce = true;
            //rotation.x = 89.999f - transitionAngle;
            //start = rotation.y;
            //end = start + (Flipped ? 180 : -180);
            //elapsedTime = 0;
        //}

        temp_rotation.x = Mathf.Clamp(temp_rotation.x, 0, 89.999f);
        temp_rotation.y %= 360f;

        //temp_rotation = rotation;

        var rot = Quaternion.identity;
        rot *= Quaternion.Euler(temp_rotation * Vector2.one);
        transform.rotation = rot;

        transform.position = origin - transform.forward * cameraDistance;

        transform.LookAt(origin);
    }

}
