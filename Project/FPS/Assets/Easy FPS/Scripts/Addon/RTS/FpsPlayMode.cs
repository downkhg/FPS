using UnityEngine;

/// <summary>
/// 1인칭 직접 전투(FPS) 플레이 모드 상태
/// - 1인칭 카메라 활성화 및 PlayerController / MouseLookScript 입력 허용
/// - GUIManager를 InGamePanel(FPS HUD) 상태로 전환
/// - PC의 RTS 명령 상태를 해제하여 즉시 1인칭 빙의(Possess)
/// </summary>
public class FpsPlayMode : IPlayModeState
{
    public const string MODE_NAME = "FPS";

    private PlayModeManager m_cManager;

    public string ModeName => MODE_NAME;

    public void Initialize(PlayModeManager _cManager)
    {
        m_cManager = _cManager;
    }

    public void OnEnter()
    {
        if (m_cManager == null) return;

        // 1. RTS 카메라 및 RTS 조작 컴포넌트 비활성화
        if (m_cManager.RtsCamera != null)
        {
            m_cManager.RtsCamera.SetCameraActive(false);
        }

        if (m_cManager.SelectionManager != null)
        {
            m_cManager.SelectionManager.ClearSelection();
            m_cManager.SelectionManager.SetSelectionEnabled(false);
        }

        if (m_cManager.CommandController != null)
        {
            m_cManager.CommandController.SetCommandEnabled(false);
        }

        // 2. FPS 1인칭 카메라 활성화
        m_cManager.SetFpsCamerasActive(true);

        // 3. PC에 걸려 있던 RTS 자율 이동/명령 해제 (즉시 1인칭 빙의)
        Player cPc = m_cManager.PlayerCharacter;
        if (cPc != null)
        {
            RtsUnitAgent cPcAgent = cPc.GetComponent<RtsUnitAgent>();
            if (cPcAgent != null)
            {
                cPcAgent.ClearCommand();
            }
        }

        // 4. GUI 씬을 FPS 인게임 HUD(InGamePanel)로 전환 (이미 플레이 중인 경우)
        GUIManager cGui = GUIManager.s_cInstance;
        if (cGui != null && cGui.IsPlayableSceneActive)
        {
            string strInGameName = cGui.InGameSceneName;
            if (!string.IsNullOrEmpty(strInGameName) && cGui.CurrentSceneName != strInGameName)
            {
                cGui.SetGuiScene(strInGameName);
            }
        }
    }

    public void OnExit()
    {
        if (m_cManager == null) return;

        // FPS 이탈 시 PC의 잔여 이동 입력 정지
        Player cPc = m_cManager.PlayerCharacter;
        if (cPc != null)
        {
            cPc.MoveWorld(Vector3.zero);
        }
    }

    public void OnUpdate()
    {
        // FPS 모드 중 매 프레임 처리할 추가 로직 확장 지점
    }
}
