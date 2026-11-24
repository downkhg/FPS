using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD 화면 GUI 프리팹 컴포넌트
/// 플레이어 체력, 탄약 및 안내 문구 UI를 갱신/표시합니다.
/// </summary>
public class InGameGuiScene : GuiSceneBase
{
    [Header("InGame HUD Controls")]
    [SerializeField]
    private TextMeshProUGUI m_txtGuide;

    [SerializeField]
    private TextMeshProUGUI m_txtHp;

    [SerializeField]
    private Image m_imgHpFill;

    [SerializeField]
    private TextMeshProUGUI m_txtAmmo;

    private void Reset()
    {
        ApplyFpsScenePolicy();
    }

    public override void Initialize(GUIManager _cManager)
    {
        ApplyFpsScenePolicy();
        base.Initialize(_cManager);
    }

    private void ApplyFpsScenePolicy()
    {
        m_bIsPlayableScene = true;
        m_bAllowsDirectFpsInput = true;
        m_bCanPause = true;
        m_bShowWorldHudBullets = true;
        m_eCursorLockMode = CursorLockMode.Locked;
        m_bCursorVisible = false;
    }

    public void UpdateHp(float _fCurrentHp, float _fMaxHp)
    {
        float fSafeMax = Mathf.Max(1f, _fMaxHp);
        float fRatio = Mathf.Clamp01(_fCurrentHp / fSafeMax);

        if (m_txtHp != null)
        {
            m_txtHp.text = string.Format("HP: {0} / {1}", Mathf.CeilToInt(_fCurrentHp), Mathf.CeilToInt(fSafeMax));
        }

        if (m_imgHpFill != null)
        {
            if (m_imgHpFill.type == Image.Type.Filled)
            {
                m_imgHpFill.fillAmount = fRatio;
            }
            else
            {
                RectTransform rtFill = m_imgHpFill.rectTransform;
                rtFill.anchorMax = new Vector2(fRatio, 1f);
            }
            m_imgHpFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 0.9f), new Color(0.2f, 0.85f, 0.35f, 0.9f), fRatio);
        }
    }

    public void UpdateHp(int _nCurrentHp, int _nMaxHp)
    {
        UpdateHp((float)_nCurrentHp, (float)_nMaxHp);
    }

    public void UpdateAmmo(int _nCurrentBullets, int _nTotalBullets)
    {
        if (m_txtAmmo != null)
        {
            m_txtAmmo.text = string.Format("AMMO: {0} / {1}", _nCurrentBullets, _nTotalBullets);
        }
    }

    public void SetGuide(string _strGuideText)
    {
        if (m_txtGuide != null)
        {
            m_txtGuide.text = _strGuideText;
        }
    }
}
