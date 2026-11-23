using UnityEngine;

/// <summary>
/// 각 GUI 팝업 객체의 추상 기반 클래스
/// 프리팹/오브젝트명(PopupName) 기반 식별 및 활성화/비활성화 팝업 모드를 지원합니다.
/// </summary>
public abstract class GuiPopupBase : MonoBehaviour, IGuiPopup
{
    [Header("Popup Mode Settings")]
    [Tooltip("체크 시 비활성화 팝업(화면/시간 정지, 뒤쪽 입력 차단), 해제 시 활성화 팝업(게임 플레이 가능)")]
    [SerializeField]
    protected bool m_bBlocksGameplay = true;

    // 상위 GUI 관리자 및 팝업 관리자 컨텍스트 참조
    protected GUIManager m_cGuiManager;
    protected GuiPopupManager m_cPopupManager;

    /// <summary>
    /// 오브젝트명(프리팹 이름)을 팝업 식별 키로 사용
    /// </summary>
    public string PopupName => gameObject.name;

    public bool IsActive => gameObject.activeSelf;

    public bool BlocksGameplay => m_bBlocksGameplay;

    public virtual void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_cGuiManager = _cGuiManager;
        m_cPopupManager = _cPopupManager;
        ApplyRaycastSettings();
    }

    public virtual void OnOpen()
    {
        ApplyRaycastSettings();
        // 동일 팝업 레이어 내에서 가장 마지막에 열린 팝업이 최상단에 덮어지도록 정렬
        transform.SetAsLastSibling();
        SetActive(true);
    }

    public virtual void OnClose()
    {
        SetActive(false);
    }

    public virtual void SetActive(bool _bActive)
    {
        if (gameObject.activeSelf != _bActive)
        {
            gameObject.SetActive(_bActive);
        }
    }

    /// <summary>
    /// 활성화 팝업(플레이 가능)일 경우 마우스/레이캐스트 입력을 통과시키고,
    /// 비활성화 팝업(화면 정지)일 경우 입력을 차단합니다.
    /// </summary>
    protected void ApplyRaycastSettings()
    {
        CanvasGroup cgPopup = GetComponent<CanvasGroup>();
        if (cgPopup != null)
        {
            cgPopup.blocksRaycasts = m_bBlocksGameplay;
            cgPopup.interactable = m_bBlocksGameplay;
        }
    }

    /// <summary>
    /// 자기 자신을 팝업 관리자를 통해 닫는 편의 메서드
    /// </summary>
    protected void CloseSelf()
    {
        if (m_cPopupManager != null)
        {
            m_cPopupManager.ClosePopup(this);
        }
        else
        {
            OnClose();
        }
    }
}
