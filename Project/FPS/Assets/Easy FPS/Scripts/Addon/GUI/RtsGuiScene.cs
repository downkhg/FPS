using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RTS 전술 화면 GUI 프리팹 컴포넌트 (Step 1 확장 슬롯)
/// 기본 게임 흐름에서는 비활성 대기하며, RTS 전환 시 Time.timeScale = 1f 유지 + 마우스 커서 해제 정책을 제공합니다.
/// </summary>
public class RtsGuiScene : GuiSceneBase
{
    [Header("RTS HUD Controls")]
    [SerializeField]
    private TextMeshProUGUI m_txtModeHeader;

    [SerializeField]
    private TextMeshProUGUI m_txtTacticalStatus;

    [SerializeField]
    private Button m_btnReturnToFps;

    private void Reset()
    {
        ApplyRtsScenePolicy();
    }

    public override void Initialize(GUIManager _cManager)
    {
        ApplyRtsScenePolicy();
        base.Initialize(_cManager);

        if (m_cManager != null && m_btnReturnToFps != null)
        {
            m_btnReturnToFps.onClick.RemoveAllListeners();
            m_btnReturnToFps.onClick.AddListener(m_cManager.SwitchToFpsScene);
        }
    }

    private void ApplyRtsScenePolicy()
    {
        m_bIsPlayableScene = true;
        m_bAllowsDirectFpsInput = false;
        m_bCanPause = true;
        m_bShowWorldHudBullets = false;
        m_eCursorLockMode = CursorLockMode.None;
        m_bCursorVisible = true;
    }

    public void SetTacticalStatus(string _strStatus)
    {
        if (m_txtTacticalStatus != null)
        {
            m_txtTacticalStatus.text = _strStatus;
        }
    }
}
