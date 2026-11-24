using UnityEngine;

/// <summary>
/// RTS 전술 모드용 탑다운/쿼터뷰 카메라 컨트롤러
/// - WASD / 화살표 및 화면 가장자리(Edge Pan) 이동
/// - 마우스 휠 줌 (높이 클램핑)
/// - 우클릭 드래그 / 중클릭 드래그 / Q·E 키로 수평 회전
/// </summary>
public class RtsCameraController : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField]
    private Camera m_camRts;

    [Header("Movement Settings")]
    [SerializeField]
    private float m_fPanSpeed = 22f;

    [SerializeField]
    private bool m_bEnableEdgePan = true;

    [SerializeField]
    private float m_fEdgeThickness = 15f;

    [Header("Zoom Settings")]
    [SerializeField]
    private float m_fZoomSpeed = 18f;

    [SerializeField]
    private float m_fMinHeight = 8f;

    [SerializeField]
    private float m_fMaxHeight = 40f;

    [Header("Rotation & Pitch Settings")]
    [SerializeField]
    private float m_fPitchAngle = 55f;

    [SerializeField]
    private float m_fRotateSpeed = 0.28f;

    [SerializeField]
    private float m_fKeyRotateSpeed = 90f;

    [SerializeField]
    private float m_fRightDragThreshold = 8f;

    private bool m_bIsActive = false;
    private float m_fYawAngle = 0f;
    private float m_fCurrentHeight = 18f;
    private Vector3 m_vFocusPosition = Vector3.zero;

    private bool m_bRightMouseHeld = false;
    private bool m_bIsRightDragging = false;
    private Vector3 m_vRightMouseDownPos;

    public Camera TacticalCamera => m_camRts;
    public bool IsActive => m_bIsActive;
    public bool IsRightDragRotating => m_bIsRightDragging;

    public void Initialize()
    {
        EnsureRtsCamera();
        SetCameraActive(false);
    }

    private void EnsureRtsCamera()
    {
        if (m_camRts != null) return;

        Transform trExisting = transform.Find("RTS_TacticalCamera");
        if (trExisting != null)
        {
            m_camRts = trExisting.GetComponent<Camera>();
        }

        if (m_camRts == null)
        {
            GameObject objCam = new GameObject("RTS_TacticalCamera");
            objCam.transform.SetParent(transform, false);
            m_camRts = objCam.AddComponent<Camera>();
            m_camRts.fieldOfView = 50f;
            m_camRts.nearClipPlane = 0.3f;
            m_camRts.farClipPlane = 500f;
            m_camRts.depth = 1f;
        }
    }

    public void SetCameraActive(bool _bActive)
    {
        EnsureRtsCamera();
        m_bIsActive = _bActive;
        m_bRightMouseHeld = false;
        m_bIsRightDragging = false;

        if (m_camRts != null)
        {
            m_camRts.enabled = _bActive;
            m_camRts.gameObject.SetActive(_bActive);
        }

        if (_bActive)
        {
            UpdateCameraTransform();
        }
    }

    /// <summary>
    /// 지정한 월드 좌표를 전술 카메라가 바라보도록 즉시 포커스합니다.
    /// </summary>
    public void FocusOnPosition(Vector3 _vWorldPos)
    {
        m_vFocusPosition = new Vector3(_vWorldPos.x, _vWorldPos.y, _vWorldPos.z);
        UpdateCameraTransform();
    }

    private void Update()
    {
        if (!m_bIsActive || m_camRts == null) return;
        if (!GUIManager.CanRunSimulation()) return;

        HandlePanInput();
        HandleZoomInput();
        HandleRotationInput();
        UpdateCameraTransform();
    }

    private void HandlePanInput()
    {
        Vector3 vInput = Vector3.zero;

        // 1. WASD / 화살표 키 이동
        float fHorizontal = Input.GetAxisRaw("Horizontal");
        float fVertical = Input.GetAxisRaw("Vertical");
        vInput.x += fHorizontal;
        vInput.z += fVertical;

        // 2. 화면 가장자리(Edge Pan) 이동
        if (m_bEnableEdgePan && Application.isFocused)
        {
            Vector3 vMouse = Input.mousePosition;
            if (vMouse.x >= 0f && vMouse.x <= Screen.width && vMouse.y >= 0f && vMouse.y <= Screen.height)
            {
                if (vMouse.x <= m_fEdgeThickness) vInput.x -= 1f;
                else if (vMouse.x >= Screen.width - m_fEdgeThickness) vInput.x += 1f;

                if (vMouse.y <= m_fEdgeThickness) vInput.z -= 1f;
                else if (vMouse.y >= Screen.height - m_fEdgeThickness) vInput.z += 1f;
            }
        }

        if (vInput.sqrMagnitude > 0.001f)
        {
            vInput.Normalize();
            Quaternion qYaw = Quaternion.Euler(0f, m_fYawAngle, 0f);
            Vector3 vMoveWorld = qYaw * vInput;
            float fHeightScale = Mathf.Lerp(0.7f, 1.8f, Mathf.InverseLerp(m_fMinHeight, m_fMaxHeight, m_fCurrentHeight));
            m_vFocusPosition += vMoveWorld * (m_fPanSpeed * fHeightScale * Time.unscaledDeltaTime);
        }
    }

    private void HandleZoomInput()
    {
        float fScroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(fScroll) > 0.001f)
        {
            m_fCurrentHeight = Mathf.Clamp(m_fCurrentHeight - fScroll * m_fZoomSpeed, m_fMinHeight, m_fMaxHeight);
        }
    }

    private void HandleRotationInput()
    {
        // Q / E 키 수평 회전
        if (Input.GetKey(KeyCode.Q))
        {
            m_fYawAngle -= m_fKeyRotateSpeed * Time.unscaledDeltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            m_fYawAngle += m_fKeyRotateSpeed * Time.unscaledDeltaTime;
        }

        // 마우스 우클릭 드래그 회전 (짧은 우클릭 명령과 구분하기 위해 드래그 임계값 적용)
        if (Input.GetMouseButtonDown(1))
        {
            m_bRightMouseHeld = true;
            m_bIsRightDragging = false;
            m_vRightMouseDownPos = Input.mousePosition;
        }

        if (m_bRightMouseHeld && Input.GetMouseButton(1))
        {
            Vector3 vDelta = Input.mousePosition - m_vRightMouseDownPos;
            if (!m_bIsRightDragging && vDelta.magnitude >= m_fRightDragThreshold)
            {
                m_bIsRightDragging = true;
            }

            if (m_bIsRightDragging)
            {
                float fMouseX = Input.GetAxis("Mouse X");
                m_fYawAngle += fMouseX * m_fRotateSpeed * 15f;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            m_bRightMouseHeld = false;
            m_bIsRightDragging = false;
        }

        // 마우스 중클릭 드래그 회전
        if (Input.GetMouseButton(2))
        {
            float fMouseX = Input.GetAxis("Mouse X");
            m_fYawAngle += fMouseX * m_fRotateSpeed * 15f;
        }
    }

    private void UpdateCameraTransform()
    {
        if (m_camRts == null) return;

        float fPitchRad = m_fPitchAngle * Mathf.Deg2Rad;
        float fBackDist = m_fCurrentHeight / Mathf.Max(0.2f, Mathf.Tan(fPitchRad));

        Quaternion qYaw = Quaternion.Euler(0f, m_fYawAngle, 0f);
        Vector3 vOffset = qYaw * new Vector3(0f, m_fCurrentHeight, -fBackDist);

        m_camRts.transform.position = m_vFocusPosition + vOffset;
        m_camRts.transform.LookAt(m_vFocusPosition);
    }
}
