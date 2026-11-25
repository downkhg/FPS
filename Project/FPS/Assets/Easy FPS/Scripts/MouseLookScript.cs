using UnityEngine;
using UnityEngine.Serialization;

public class MouseLookScript : MonoBehaviour
{
    [FormerlySerializedAs("myCamera")]
    public Transform m_trCamera;

    private Player m_cPlayer;
    private PlayerMovementScript m_cPlayerMovement;

    [Header("Z Rotation Camera")]
    [HideInInspector] [FormerlySerializedAs("timer")] public float m_fTimer;
    [HideInInspector] [FormerlySerializedAs("int_timer")] public int m_nTimer;
    [HideInInspector] [FormerlySerializedAs("zRotation")] public float m_fZRotation;
    [HideInInspector] [FormerlySerializedAs("wantedZ")] public float m_fWantedZ;
    [HideInInspector] [FormerlySerializedAs("timeSpeed")] public float m_fTimeSpeed = 2f;
    [HideInInspector] [FormerlySerializedAs("timerToRotateZ")] public float m_fTimerToRotateZ;

    [Tooltip("Current mouse sensivity, changes in the weapon properties")]
    [FormerlySerializedAs("mouseSensitvity")]
    public float m_fMouseSensitivity = 0f;

    [HideInInspector]
    [FormerlySerializedAs("mouseSensitvity_notAiming")]
    public float m_fMouseSensitivityNotAiming = 300f;

    [HideInInspector]
    [FormerlySerializedAs("mouseSensitvity_aiming")]
    public float m_fMouseSensitivityAiming = 50f;

    private float m_fRotationYVelocity;
    private float m_fCameraXVelocity;

    [Tooltip("Speed that determines how much camera rotation will lag behind mouse movement.")]
    [FormerlySerializedAs("yRotationSpeed")]
    public float m_fYRotationSpeed;

    [FormerlySerializedAs("xCameraSpeed")]
    public float m_fXCameraSpeed;

    [HideInInspector]
    [FormerlySerializedAs("wantedYRotation")]
    public float m_fWantedYRotation;

    [HideInInspector]
    [FormerlySerializedAs("currentYRotation")]
    public float m_fCurrentYRotation;

    [HideInInspector]
    [FormerlySerializedAs("wantedCameraXRotation")]
    public float m_fWantedCameraXRotation;

    [HideInInspector]
    [FormerlySerializedAs("currentCameraXRotation")]
    public float m_fCurrentCameraXRotation;

    [Tooltip("Top camera angle.")]
    [FormerlySerializedAs("topAngleView")]
    public float m_fTopAngleView = 60f;

    [Tooltip("Minimum camera angle.")]
    [FormerlySerializedAs("bottomAngleView")]
    public float m_fBottomAngleView = -45f;

    [Tooltip("Current weapon that player carries.")]
    [HideInInspector]
    [FormerlySerializedAs("weapon")]
    public GameObject m_objWeapon;

    private float m_fDeltaTime = 0.0f;
    private float m_fFpsTextUpdateTimer = 0.0f;
    private string m_strCachedFpsText = "0.0 ms (0 fps)";
    private GUIStyle m_styleFpsLabel;
    private int m_nLastFpsScreenHeight = -1;

    [Tooltip("Shows FPS in top left corner.")]
    [FormerlySerializedAs("showFps")]
    public bool m_bShowFps = true;

    // 외부 접근 프로퍼티
    public float CurrentYRotation => m_fCurrentYRotation;
    public float CurrentCameraXRotation => m_fCurrentCameraXRotation;
    public float WantedYRotation
    {
        get => m_fWantedYRotation;
        set => m_fWantedYRotation = value;
    }
    public float WantedCameraXRotation
    {
        get => m_fWantedCameraXRotation;
        set => m_fWantedCameraXRotation = value;
    }

    private void Awake()
    {
        m_cPlayer = GetComponent<Player>();
        m_cPlayerMovement = GetComponent<PlayerMovementScript>();
    }

    public void Initialize(Transform _trMainCamera)
    {
        if (!m_cPlayer)
            m_cPlayer = GetComponent<Player>();
        if (!m_cPlayerMovement)
            m_cPlayerMovement = GetComponent<PlayerMovementScript>();

        if ((m_cPlayer == null || m_cPlayer.IsPlayer) && GUIManager.CanProcessGameplayInput())
            Cursor.lockState = CursorLockMode.Locked;

        m_trCamera = _trMainCamera;
    }

    public void SetSensitivity(float _fNotAiming, float _fAiming)
    {
        m_fMouseSensitivityNotAiming = _fNotAiming;
        m_fMouseSensitivityAiming = _fAiming;
    }

    private void Update()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;
        if (!GUIManager.CanProcessGameplayInput()) return;

        UpdateMouseInput();

        m_fDeltaTime += (Time.deltaTime - m_fDeltaTime) * 0.1f;

        if (m_bShowFps)
        {
            m_fFpsTextUpdateTimer -= Time.unscaledDeltaTime;
            if (m_fFpsTextUpdateTimer <= 0f)
            {
                m_fFpsTextUpdateTimer = 0.2f;
                float fMsec = m_fDeltaTime * 1000.0f;
                float fFps = 1.0f / Mathf.Max(0.0001f, m_fDeltaTime);
                m_strCachedFpsText = string.Format("{0:0.0} ms ({1:0.} fps)", fMsec, fFps);
            }
        }

        if (m_cPlayerMovement && m_cPlayerMovement.CurrentSpeed > 1f)
            UpdateHeadBob();
        else if (m_cPlayerMovement)
        {
            m_cPlayerMovement.CurrentSpeed = 0;
        }
    }

    private void UpdateHeadBob()
    {
        m_fTimer += m_fTimeSpeed * Time.deltaTime;
        m_nTimer = Mathf.RoundToInt(m_fTimer);
        if (m_nTimer % 2 == 0)
            m_fWantedZ = -1f;
        else
            m_fWantedZ = 1f;

        m_fZRotation = Mathf.Lerp(m_fZRotation, m_fWantedZ, Time.deltaTime * m_fTimerToRotateZ);
    }

    private void FixedUpdate()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;
        if (!GUIManager.CanProcessGameplayInput()) return;

        if (Input.GetAxis("Fire2") != 0f)
        {
            m_fMouseSensitivity = m_fMouseSensitivityAiming;
        }
        else
        {
            m_fMouseSensitivity = m_fMouseSensitivityNotAiming;
        }

        ApplyCameraRotation();
    }

    private void UpdateMouseInput()
    {
        m_fWantedYRotation += Input.GetAxis("Mouse X") * m_fMouseSensitivity;
        m_fWantedCameraXRotation -= Input.GetAxis("Mouse Y") * m_fMouseSensitivity;
        m_fWantedCameraXRotation = Mathf.Clamp(m_fWantedCameraXRotation, m_fBottomAngleView, m_fTopAngleView);
    }

    private void ApplyCameraRotation()
    {
        m_fCurrentYRotation = Mathf.SmoothDamp(m_fCurrentYRotation, m_fWantedYRotation, ref m_fRotationYVelocity, m_fYRotationSpeed);
        m_fCurrentCameraXRotation = Mathf.SmoothDamp(m_fCurrentCameraXRotation, m_fWantedCameraXRotation, ref m_fCameraXVelocity, m_fXCameraSpeed);

        transform.rotation = Quaternion.Euler(0f, m_fCurrentYRotation, 0f);
        if (m_trCamera)
            m_trCamera.localRotation = Quaternion.Euler(m_fCurrentCameraXRotation, 0f, m_fZRotation);
    }

    private void OnGUI()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;
        if (!GUIManager.CanProcessGameplayInput()) return;

        if (m_bShowFps)
        {
            DrawFpsCounter();
        }
    }
    string strText;
    private void DrawFpsCounter()
    {
        if (Event.current.type != EventType.Repaint) return;

        int nWidth = Screen.width;
        int nHeight = Screen.height;

        if (m_styleFpsLabel == null)
        {
            m_styleFpsLabel = new GUIStyle();
            m_styleFpsLabel.alignment = TextAnchor.UpperLeft;
            m_styleFpsLabel.normal.textColor = Color.white;
        }

        if (m_nLastFpsScreenHeight != nHeight)
        {
            m_nLastFpsScreenHeight = nHeight;
            m_styleFpsLabel.fontSize = nHeight * 2 / 100;
        }

        Rect rectArea = new Rect(0f, 0f, nWidth, nHeight * 2f / 100f);
        GUI.Label(rectArea, m_strCachedFpsText, m_styleFpsLabel);
    }
}
