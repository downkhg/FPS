using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 음량 조절 비활성화 팝업 (BlocksGameplay = true)
/// 기존 GUI 화면 위에 반투명 배경으로 덮어지며 BGM/SFX 볼륨 및 음소거를 조절합니다.
/// </summary>
public class AudioSettingPopup : GuiPopupBase, IAudioSyncableGui
{
    [Header("Audio Controls")]
    [SerializeField]
    private Slider m_sliderBgm;

    [SerializeField]
    private Slider m_sliderSfx;

    [SerializeField]
    private Toggle m_toggleMute;

    [SerializeField]
    private TextMeshProUGUI m_txtBgmValue;

    [SerializeField]
    private TextMeshProUGUI m_txtSfxValue;

    [Header("Popup Buttons")]
    [SerializeField]
    private Button m_btnClose;

    private void Reset()
    {
        m_bBlocksGameplay = true;
    }

    public override void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_bBlocksGameplay = true;
        base.Initialize(_cGuiManager, _cPopupManager);

        if (m_cGuiManager == null) return;

        if (m_sliderBgm != null)
        {
            m_sliderBgm.onValueChanged.RemoveAllListeners();
            m_sliderBgm.onValueChanged.AddListener(m_cGuiManager.OnBgmSliderChanged);
        }

        if (m_sliderSfx != null)
        {
            m_sliderSfx.onValueChanged.RemoveAllListeners();
            m_sliderSfx.onValueChanged.AddListener(m_cGuiManager.OnSfxSliderChanged);
        }

        if (m_toggleMute != null)
        {
            m_toggleMute.onValueChanged.RemoveAllListeners();
            m_toggleMute.onValueChanged.AddListener(m_cGuiManager.OnMuteToggleChanged);
        }

        if (m_btnClose != null)
        {
            m_btnClose.onClick.RemoveAllListeners();
            m_btnClose.onClick.AddListener(OnClickClose);
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();

        if (m_cGuiManager != null)
        {
            SyncAudioSettings(m_cGuiManager.BgmVolume, m_cGuiManager.SfxVolume, m_cGuiManager.IsMuted);
        }
    }

    private void OnClickClose()
    {
        if (m_cGuiManager != null)
        {
            m_cGuiManager.PlayUiClickSound();
        }
        CloseSelf();
    }

    public void SyncAudioSettings(float _fBgmVolume, float _fSfxVolume, bool _bIsMuted)
    {
        if (m_sliderBgm != null && !Mathf.Approximately(m_sliderBgm.value, _fBgmVolume))
        {
            m_sliderBgm.SetValueWithoutNotify(_fBgmVolume);
        }

        if (m_sliderSfx != null && !Mathf.Approximately(m_sliderSfx.value, _fSfxVolume))
        {
            m_sliderSfx.SetValueWithoutNotify(_fSfxVolume);
        }

        if (m_toggleMute != null && m_toggleMute.isOn != _bIsMuted)
        {
            m_toggleMute.SetIsOnWithoutNotify(_bIsMuted);
        }

        if (m_txtBgmValue != null)
        {
            m_txtBgmValue.text = string.Format("BGM Volume: {0}%", Mathf.RoundToInt(_fBgmVolume * 100f));
        }

        if (m_txtSfxValue != null)
        {
            m_txtSfxValue.text = string.Format("SFX Volume: {0}%", Mathf.RoundToInt(_fSfxVolume * 100f));
        }
    }
}
