using UnityEngine;

/// <summary>
/// 전술 지휘(RTS) 플레이 모드 상태
/// - 탑다운/쿼터뷰 RtsCameraController 활성화 및 PC 위치로 초기 포커스
/// - 1인칭 직접 이동/사격 잠금, 마우스 커서 해제 + Time.timeScale = 1f 유지
/// - GUIManager를 RtsHudPanel 상태로 전환 및 유닛 선택/명령 시스템 활성화
/// </summary>
public class RtsPlayMode : IPlayModeState
{
    public const string MODE_NAME = "RTS";

    private PlayModeManager m_cManager;

    public string ModeName => MODE_NAME;

    public void Initialize(PlayModeManager _cManager)
    {
        m_cManager = _cManager;
    }

    public void OnEnter()
    {
        if (m_cManager == null) return;

        // 1. FPS 1인칭 카메라 비활성화 (무기 계층은 보존)
        m_cManager.SetFpsCamerasActive(false);

        // 2. RTS 전술 카메라 활성화 및 PC 위치 포커스
        if (m_cManager.RtsCamera != null)
        {
            Player cPc = m_cManager.PlayerCharacter;
            if (cPc != null)
            {
                m_cManager.RtsCamera.FocusOnPosition(cPc.transform.position);
            }
            m_cManager.RtsCamera.SetCameraActive(true);
        }

        // 3. 유닛 선택 및 명령 시스템 활성화
        if (m_cManager.SelectionManager != null)
        {
            m_cManager.SelectionManager.SetSelectionEnabled(true);
        }

        if (m_cManager.CommandController != null)
        {
            m_cManager.CommandController.SetCommandEnabled(true);
        }

        // 4. GUI 씬을 RTS 전술 HUD(RtsHudPanel)로 전환
        GUIManager cGui = GUIManager.s_cInstance;
        if (cGui != null)
        {
            string strRtsName = cGui.RtsSceneName;
            if (!string.IsNullOrEmpty(strRtsName) && cGui.CurrentSceneName != strRtsName)
            {
                cGui.SetGuiScene(strRtsName);
            }
        }
    }

    public void OnExit()
    {
        if (m_cManager == null) return;

        if (m_cManager.SelectionManager != null)
        {
            m_cManager.SelectionManager.ClearSelection();
            m_cManager.SelectionManager.SetSelectionEnabled(false);
        }

        if (m_cManager.CommandController != null)
        {
            m_cManager.CommandController.SetCommandEnabled(false);
        }

        if (m_cManager.RtsCamera != null)
        {
            m_cManager.RtsCamera.SetCameraActive(false);
        }
    }

    public void OnUpdate()
    {
        // Space 키로 지휘관(PC) 위치로 전술 카메라 즉시 포커스
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Player cPc = m_cManager != null ? m_cManager.PlayerCharacter : null;
            if (cPc != null && m_cManager.RtsCamera != null)
            {
                m_cManager.RtsCamera.FocusOnPosition(cPc.transform.position);
            }
        }
    }
}
