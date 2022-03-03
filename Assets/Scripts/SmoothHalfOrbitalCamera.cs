using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class SmoothHalfOrbitalCamera : MonoBehaviour
{
    [SerializeField] Keys keys;

    [SerializeField] Team team;
    public Team Team
    {
        get => team;
        set
        {
            if(value == team)
                return;
            team = value;
            keys?.SetKeys(value);
        }
    }

    public int SelectedView
    {
        get => selectedView; set
        {
            selectedView = value;
            ApplyView();
        }
    }
    [SerializeField] int selectedView;
    [SerializeField] Vector3 cameraRotation;

    public CameraView View => views?[selectedView];

    [SerializeField] CameraView[] views;
    [SerializeField] MultiCamOptions options;

    [SerializeField, HideInInspector] SelectPiece selectPiece;

    public bool FreeLooking { get; private set; }

    #region private variables
    float nomalizedElaspedTime;
    float adjustedResetTime;

    Vector3 release_rotation;

    bool needsReset = false;
    VirtualCursor cursor;
    PieceNameTooltip tooltip;
    #endregion

    public bool IsSandboxMode { get; private set; }

    [SerializeField] private Camera cam;

    #region Init Methods
    void OnValidate()
    {
        selectPiece = FindObjectOfType<SelectPiece>();

        ApplyView();

        if(keys)
            keys.SetKeys(team);
    }


    void Awake()
    {
        cursor = GameObject.FindObjectOfType<VirtualCursor>();
    }

    void Start()
    {
        tooltip = FindObjectOfType<PieceNameTooltip>();
        IsSandboxMode = !FindObjectOfType<Multiplayer>();
        ApplyView();
    }

    #endregion

    void ApplyView()
    {
        if(views.Length >= 1)
        {
            selectedView = Mathf.Clamp(selectedView, 0, views.Length - 1);
            Apply(views[selectedView]);
        }

        void Apply(CameraView view)
        {
            Vector3 final_position = options.trueOrigin;
            final_position.y += options.cameraHeight;

            transform.position = final_position;
            transform.LookAt(options.trueOrigin);

            transform.position += view.postCameraOffset;

            var pivot = options.trueOrigin;
            pivot.y += options.cameraHeight;

            cameraRotation.x %= 360;
            cameraRotation.y %= 360;
            cameraRotation.z %= 360;

            Vector3 rotation = cameraRotation + view.postCameraRotation;

            Vector3 clamped = Vector3.Max(rotation, view.minRotation);
            clamped = Vector3.Min(clamped, view.maxRotation);

            cameraRotation -= rotation - clamped;

            transform.RotateAround(options.trueOrigin, Vector3.left, clamped.x);
            transform.RotateAround(options.trueOrigin, Vector3.up, clamped.y);
            transform.RotateAround(options.trueOrigin, Vector3.forward, clamped.z);

            if(team == Team.Black)
                transform.RotateAround(pivot, Vector3.up, 180);
            
            cam.rect = new Rect(0, 0, 1, view.viewportHeight);
        }
    }

    public void SetDefaultTeam(Team team)
    {
        this.team = team;
    }

    public void SetToTeam(Team team)
    {
        if(FreeLooking)
            return;

        needsReset = true;
        this.team = team;
        keys.SetKeys(team);
        SetDefaultTeam(team);
        StopRotating();
    }

    public void ToggleTeam()
    {
        if(FreeLooking)
            return;

        team = team switch
        {
            Team.None => Team.White,
            Team.White => Team.Black,
            Team.Black => Team.White,
            _ => throw new System.NotSupportedException($"Team {team} not supported"),
        };
        needsReset = true;
        cursor?.Hide();
        keys.SetKeys(team);
        SetDefaultTeam(team);
        StopRotating();
    }

    public void OnSpacebar(InputAction.CallbackContext context)
    {
        if(context.performed && IsSandboxMode)
            ToggleTeam();
    }

    public void MiddleClick(InputAction.CallbackContext context)
    {
        if(selectPiece.selectedPiece != null)
            return;

        if(context.started)
        {
            var mousePos = Mouse.current.position.ReadValue();
            if(mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                StartRotating();
        }
        else if(context.canceled)
            StopRotating();
    }

    public void NextView()
    {
        selectedView += 1;
        if(selectedView >= views.Length)
            selectedView = 0;
    }

    public void PrevView()
    {
        selectedView -= 1;
        if(selectedView < 0)
            selectedView = views.Length - 1;
    }

    void StartRotating()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cursor?.SetCursor(CursorType.Default);
        cursor?.Hide();
        if(tooltip != null)
        {
            tooltip.Hide();
            tooltip.blockDisplay = true;
        }
        needsReset = true;
        FreeLooking = true;
    }

    void StopRotating()
    {
        if(tooltip != null)
            tooltip.blockDisplay = false;

        nomalizedElaspedTime = 0;
        release_rotation = cameraRotation;
        float delta = (Vector3.zero - release_rotation).magnitude / options.minimumRotationMagnitude;
        if(delta >= 1)
            adjustedResetTime = options.cameraResetTime;
        else
            adjustedResetTime = options.cameraResetTime * delta;
        FreeLooking = false;
    }

    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
            StopRotating();
        else if(FreeLooking)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * options.speed;
            cameraRotation += new Vector3(delta.y, delta.x);
            ApplyView();
        }
        else
        {
            if(nomalizedElaspedTime < 1)
            {
                cameraRotation = Vector3.Slerp(release_rotation, Vector3.zero, nomalizedElaspedTime);
                nomalizedElaspedTime += Time.deltaTime / adjustedResetTime;
            }
            else
            {
                // This shouldn't run every frame, but only once when the camera returns to default position
                if(needsReset)
                {
                    if(Cursor.lockState != CursorLockMode.None)
                        Cursor.lockState = CursorLockMode.None;
                    needsReset = false;
                    cameraRotation = Vector3.zero;
                    // cursor?.Show();
                    StartCoroutine(EndOfFrameWork());
                }
            }
            ApplyView();
        }
    }

    IEnumerator EndOfFrameWork()
    {
        yield return new WaitForEndOfFrame();
        cursor?.Show();
    }

    #region Classes
    [System.Serializable]
    public class CameraView
    {
        [Tooltip("User friendly name")]
        public string name;
        [Tooltip("An offset applied to the camera after its position and rotation has been calculated")]
        public Vector3 postCameraOffset;
        [Tooltip("A rotation applied to the camera after its position and rotation has been calculated")]
        public Vector3 postCameraRotation;

        public Vector3 minRotation;
        public Vector3 maxRotation;
        public float viewportHeight;
    }

    [System.Serializable]
    public class MultiCamOptions
    {
        [Tooltip("The center of the board, used to cacluate camera offsets")]
        public Vector3 trueOrigin = new Vector3(6, 1, 7.794229f);
        [Tooltip("The height of the camera")]
        public float cameraHeight = 18;
        public float speed = 0.15f;

        public Vector3 minRotationValues;
        public Vector3 maxRotationValues;

        public float cameraResetTime = 0.5f;
        // Kind of like rubberbanding so that smaller rotations don't take the same amount of time as big ones
        public float minimumRotationMagnitude = 100f;
    }
    #endregion
}