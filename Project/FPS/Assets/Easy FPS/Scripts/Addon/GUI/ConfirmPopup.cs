using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타이틀 돌아가기, 게임 종료 등 중요 동작 전 한 번 더 확인을 묻는 비활성화 팝업 (BlocksGameplay = true)
/// </summary>
public class ConfirmPopup : GuiPopupBase
{
    [Header("Confirm UI")]
    [SerializeField]
    private TextMeshProUGUI m_txtTitle;

    [SerializeField]
    private TextMeshProUGUI m_txtMessage;

    [SerializeField]
    private Button m_btnConfirm;

    [SerializeField]
    private Button m_btnCancel;

    private Action m_fnOnConfirm;

    private void Reset()
    {
        m_bBlocksGameplay = true;
    }

    public override void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_bBlocksGameplay = true;
        base.Initialize(_cGuiManager, _cPopupManager);

        if (m_btnConfirm != null)
        {
            m_btnConfirm.onClick.RemoveAllListeners();
            m_btnConfirm.onClick.AddListener(OnClickConfirm);
        }

        if (m_btnCancel != null)
        {
            m_btnCancel.onClick.RemoveAllListeners();
            m_btnCancel.onClick.AddListener(OnClickCancel);
        }
    }

    /// <summary>
    /// 확인 팝업의 제목, 안내 문구 및 확인 시 실행할 콜백을 설정합니다.
    /// </summary>
    public void Setup(string _strTitle, string _strMessage, Action _fnOnConfirm)
    {
        if (m_txtTitle != null)
        {
            m_txtTitle.text = string.IsNullOrEmpty(_strTitle) ? "CONFIRM" : _strTitle;
        }

        if (m_txtMessage != null)
        {
            m_txtMessage.text = string.IsNullOrEmpty(_strMessage) ? string.Empty : _strMessage;
        }

        m_fnOnConfirm = _fnOnConfirm;
    }

    public override void OnClose()
    {
        m_fnOnConfirm = null;
        base.OnClose();
    }

    private void OnClickConfirm()
    {
        Action fnCallback = m_fnOnConfirm;
        if (m_cGuiManager != null)
        {
            m_cGuiManager.PlayUiClickSound();
        }

        CloseSelf();
        fnCallback?.Invoke();
    }

    private void OnClickCancel()
    {
        if (m_cGuiManager != null)
        {
            m_cGuiManager.PlayUiClickSound();
        }
        CloseSelf();
    }
}
