using UnityEngine;
using TMPro;

/// <summary>
/// 게임 플레이(이동/사격/시간 흐름)를 방해하지 않고 화면 위에 표시되는 활성화 팝업 (BlocksGameplay = false)
/// 일정 시간이 지나면 자동으로 닫힙니다.
/// </summary>
public class NoticeToastPopup : GuiPopupBase
{
    [Header("Toast UI")]
    [SerializeField]
    private TextMeshProUGUI m_txtMessage;

    [SerializeField]
    private float m_fDefaultDuration = 2.5f;

    private float m_fRemainTime = 0f;

    private void Reset()
    {
        m_ePopupLayer = EPopupLayer.Active;
        m_bBlocksGameplay = false;
    }

    public override void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_ePopupLayer = EPopupLayer.Active;
        m_bBlocksGameplay = false;
        base.Initialize(_cGuiManager, _cPopupManager);
    }

    public override void OnResetForPool()
    {
        m_fRemainTime = 0f;
        base.OnResetForPool();
    }

    /// <summary>
    /// 안내 메시지를 설정하고 지정 시간 동안 활성화 팝업으로 표시합니다.
    /// </summary>
    public void ShowMessage(string _strMessage, float _fDuration = -1f)
    {
        if (m_txtMessage != null)
        {
            m_txtMessage.text = string.IsNullOrEmpty(_strMessage) ? string.Empty : _strMessage;
        }

        m_fRemainTime = _fDuration > 0f ? _fDuration : m_fDefaultDuration;
    }

    private void Update()
    {
        if (!IsActive) return;

        if (m_fRemainTime > 0f)
        {
            m_fRemainTime -= Time.deltaTime;
            if (m_fRemainTime <= 0f)
            {
                CloseSelf();
            }
        }
    }
}
