using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임오버 화면 GUI 프리팹 컴포넌트
/// 컨텍스트(GUIManager)의 이벤트 함수를 UI 컨트롤에 바인딩하고 사망 사유 텍스트를 표시합니다.
/// </summary>
public class GameOverGuiScene : GuiSceneBase
{
    [Header("GameOver Buttons")]
    [SerializeField]
    private Button m_btnRestart;

    [SerializeField]
    private Button m_btnTitle;

    [SerializeField]
    private Button m_btnQuit;

    [Header("GameOver Reason")]
    [SerializeField]
    private TextMeshProUGUI m_txtReason;

    public override void Initialize(GUIManager _cManager)
    {
        base.Initialize(_cManager);
        if (m_cManager == null) return;

        if (m_btnRestart != null)
        {
            m_btnRestart.onClick.RemoveAllListeners();
            m_btnRestart.onClick.AddListener(m_cManager.RestartGame);
        }

        if (m_btnTitle != null)
        {
            m_btnTitle.onClick.RemoveAllListeners();
            m_btnTitle.onClick.AddListener(m_cManager.RequestReturnToTitle);
        }

        if (m_btnQuit != null)
        {
            m_btnQuit.onClick.RemoveAllListeners();
            m_btnQuit.onClick.AddListener(m_cManager.RequestQuitGame);
        }
    }

    public void ShowGameOver(string _strReason)
    {
        if (m_txtReason != null && !string.IsNullOrEmpty(_strReason))
        {
            m_txtReason.text = _strReason;
        }
    }
}
