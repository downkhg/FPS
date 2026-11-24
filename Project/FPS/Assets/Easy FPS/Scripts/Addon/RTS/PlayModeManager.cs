using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 딕셔너리 기반 확장 상태 패턴으로 플레이 모드(FPS ↔ RTS)를 관리하는 관리자
/// Tab 키 전환, 1인칭/전술 카메라 분리, 유닛 선택·명령 시스템 연동을 총괄합니다.
/// </summary>
public class PlayModeManager : MonoBehaviour
{
    public static PlayModeManager s_cInstance;

    [Header("Mode Switch Settings")]
    [SerializeField]
    private KeyCode m_eToggleModeKey = KeyCode.Tab;

    [Header("RTS Sub-Systems")]
    [SerializeField]
    private RtsCameraController m_cRtsCamera;

    [SerializeField]
    private UnitSelectionManager m_cSelectionManager;

    [SerializeField]
    private UnitCommandController m_cCommandController;

    private readonly Dictionary<string, IPlayModeState> m_dictPlayModes = new Dictionary<string, IPlayModeState>();
    private IPlayModeState m_cCurrentMode;
    private GameManager m_cGameManager;
    private bool m_bIsInitialized = false;

    public string CurrentModeName => m_cCurrentMode != null ? m_cCurrentMode.ModeName : string.Empty;
    public bool IsRtsMode => CurrentModeName == RtsPlayMode.MODE_NAME;
    public bool IsFpsMode => CurrentModeName == FpsPlayMode.MODE_NAME;

    public RtsCameraController RtsCamera => m_cRtsCamera;
    public UnitSelectionManager SelectionManager => m_cSelectionManager;
    public UnitCommandController CommandController => m_cCommandController;
    public Player PlayerCharacter => m_cGameManager != null ? m_cGameManager.CurrentPlayer : null;

    private void Awake()
    {
        if (s_cInstance != null && s_cInstance != this)
        {
            Destroy(this);
            return;
        }
        s_cInstance = this;
    }

    public void Initialize(GameManager _cGameManager)
    {
        m_cGameManager = _cGameManager;
        if (m_bIsInitialized) return;
        m_bIsInitialized = true;

        s_cInstance = this;
        EnsureSubSystems();
        RegisterDefaultModes();

        // 기본 시작 모드는 FPS (Title -> InGame 흐름과 일치)
        SetPlayMode(FpsPlayMode.MODE_NAME);
    }

    private void EnsureSubSystems()
    {
        if (m_cRtsCamera == null)
        {
            m_cRtsCamera = GetComponent<RtsCameraController>();
            if (m_cRtsCamera == null)
            {
                m_cRtsCamera = gameObject.AddComponent<RtsCameraController>();
            }
        }
        m_cRtsCamera.Initialize();

        if (m_cSelectionManager == null)
        {
            m_cSelectionManager = GetComponent<UnitSelectionManager>();
            if (m_cSelectionManager == null)
            {
                m_cSelectionManager = gameObject.AddComponent<UnitSelectionManager>();
            }
        }
        m_cSelectionManager.Initialize(m_cRtsCamera);

        if (m_cCommandController == null)
        {
            m_cCommandController = GetComponent<UnitCommandController>();
            if (m_cCommandController == null)
            {
                m_cCommandController = gameObject.AddComponent<UnitCommandController>();
            }
        }
        m_cCommandController.Initialize(m_cRtsCamera, m_cSelectionManager);
    }

    private void RegisterDefaultModes()
    {
        RegisterMode(new FpsPlayMode());
        RegisterMode(new RtsPlayMode());
    }

    /// <summary>
    /// 확장 플레이 모드를 딕셔너리에 등록합니다.
    /// </summary>
    public void RegisterMode(IPlayModeState _cMode)
    {
        if (_cMode == null || string.IsNullOrEmpty(_cMode.ModeName)) return;

        _cMode.Initialize(this);
        m_dictPlayModes[_cMode.ModeName] = _cMode;
    }

    /// <summary>
    /// 딕셔너리에서 ModeName 키로 플레이 모드를 전환합니다.
    /// </summary>
    public void SetPlayMode(string _strModeName)
    {
        if (string.IsNullOrEmpty(_strModeName)) return;
        if (!m_dictPlayModes.TryGetValue(_strModeName, out IPlayModeState cNextMode) || cNextMode == null) return;
        if (m_cCurrentMode == cNextMode) return;

        m_cCurrentMode?.OnExit();
        m_cCurrentMode = cNextMode;
        m_cCurrentMode.OnEnter();
    }

    public void SwitchToFps()
    {
        SetPlayMode(FpsPlayMode.MODE_NAME);
    }

    public void SwitchToRts()
    {
        SetPlayMode(RtsPlayMode.MODE_NAME);
    }

    public void TogglePlayMode()
    {
        if (IsRtsMode)
        {
            SwitchToFps();
        }
        else
        {
            SwitchToRts();
        }
    }

    /// <summary>
    /// FPS 1인칭 메인 카메라와 무기용 세컨드 카메라 활성 상태를 전환합니다. (1인칭 무기 계층 보존)
    /// </summary>
    public void SetFpsCamerasActive(bool _bActive)
    {
        if (m_cGameManager == null) return;

        if (m_cGameManager.CameraComponent != null)
        {
            m_cGameManager.CameraComponent.enabled = _bActive;
        }

        if (m_cGameManager.SecondCamera != null)
        {
            m_cGameManager.SecondCamera.enabled = _bActive;
        }
    }

    private void Update()
    {
        if (!m_bIsInitialized) return;
        if (!GUIManager.CanRunSimulation()) return;

        if (Input.GetKeyDown(m_eToggleModeKey))
        {
            if (GUIManager.s_cInstance != null)
            {
                GUIManager.s_cInstance.PlayUiClickSound();
            }
            TogglePlayMode();
            return;
        }

        m_cCurrentMode?.OnUpdate();
    }
}
