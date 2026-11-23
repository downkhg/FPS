using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 팝업 프리팹 컴포넌트 (비활성화 팝업: BlocksGameplay = true)
/// GuiPopupManager의 팝업 루트에서 관리되며, 인게임 UI를 끄지 않고 그 위를 반투명 Dim과 함께 덮습니다.
/// </summary>
public class PauseGuiScene : GuiPopupBase
{
    [Header("Pause Buttons")]
    [SerializeField]
    private Button m_btnResume;

    [SerializeField]
    private Button m_btnRestart;

    [SerializeField]
    private Button m_btnAudioSettings;

    [SerializeField]
    private Button m_btnTitle;

    [SerializeField]
    private Button m_btnQuit;

    private void Reset()
    {
        m_ePopupLayer = EPopupLayer.Menu;
        m_bBlocksGameplay = true;
    }

    public override void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_ePopupLayer = EPopupLayer.Menu;
        m_bBlocksGameplay = true;
        base.Initialize(_cGuiManager, _cPopupManager);
        if (m_cGuiManager == null) return;

        if (m_btnResume != null)
        {
            m_btnResume.onClick.RemoveAllListeners();
            m_btnResume.onClick.AddListener(m_cGuiManager.ResumeGame);
        }

        if (m_btnRestart != null)
        {
            m_btnRestart.onClick.RemoveAllListeners();
            m_btnRestart.onClick.AddListener(m_cGuiManager.RestartGame);
        }

        if (m_btnAudioSettings != null)
        {
            m_btnAudioSettings.onClick.RemoveAllListeners();
            m_btnAudioSettings.onClick.AddListener(m_cGuiManager.OpenAudioSettingPopup);
        }

        if (m_btnTitle != null)
        {
            m_btnTitle.onClick.RemoveAllListeners();
            m_btnTitle.onClick.AddListener(m_cGuiManager.RequestReturnToTitle);
        }

        if (m_btnQuit != null)
        {
            m_btnQuit.onClick.RemoveAllListeners();
            m_btnQuit.onClick.AddListener(m_cGuiManager.RequestQuitGame);
        }
    }
}
