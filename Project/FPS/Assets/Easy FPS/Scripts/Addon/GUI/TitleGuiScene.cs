using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시작(타이틀) 화면 GUI 프리팹 컴포넌트
/// 컨텍스트(GUIManager)의 이벤트 함수를 UI 컨트롤에 바인딩합니다.
/// 음량 조절 및 종료 확인은 GUIManager -> GuiPopupManager를 통해 팝업으로 처리합니다.
/// </summary>
public class TitleGuiScene : GuiSceneBase
{
    [Header("Title Buttons")]
    [SerializeField]
    private Button m_btnStart;

    [SerializeField]
    private Button m_btnAudioSettings;

    [SerializeField]
    private Button m_btnQuit;

    public override void Initialize(GUIManager _cManager)
    {
        base.Initialize(_cManager);
        if (m_cManager == null) return;

        if (m_btnStart != null)
        {
            m_btnStart.onClick.RemoveAllListeners();
            m_btnStart.onClick.AddListener(m_cManager.StartGame);
        }

        if (m_btnAudioSettings != null)
        {
            m_btnAudioSettings.onClick.RemoveAllListeners();
            m_btnAudioSettings.onClick.AddListener(m_cManager.OpenAudioSettingPopup);
        }

        if (m_btnQuit != null)
        {
            m_btnQuit.onClick.RemoveAllListeners();
            m_btnQuit.onClick.AddListener(m_cManager.RequestQuitGame);
        }
    }
}
