using UnityEngine;

/// <summary>
/// 각 GUI 팝업 객체의 추상 기반 클래스
/// 프리팹/오브젝트명(PopupName) 기반 식별, 레이어 지정, 활성화/비활성화 모드 및 스택 숨김·복구를 지원합니다.
/// </summary>
public abstract class GuiPopupBase : MonoBehaviour, IGuiPopup
{
    [Header("Popup Layer & Mode Settings")]
    [Tooltip("기획 의도에 따른 팝업 소속 레이어")]
    [SerializeField]
    protected EPopupLayer m_ePopupLayer = EPopupLayer.Modal;

    [Tooltip("체크 시 비활성화 팝업(화면/시간 정지, 뒤쪽 입력 차단), 해제 시 활성화 팝업(게임 플레이 가능)")]
    [SerializeField]
    protected bool m_bBlocksGameplay = true;

    // 상위 GUI 관리자 및 팝업 관리자 컨텍스트 참조
    protected GUIManager m_cGuiManager;
    protected GuiPopupManager m_cPopupManager;

    private CanvasGroup m_cgPopup;
    protected bool m_bIsVisuallyHidden = false;

    /// <summary>
    /// 오브젝트명(프리팹 이름)을 팝업 식별 키로 사용
    /// </summary>
    public string PopupName => gameObject.name;

    public EPopupLayer Layer => m_ePopupLayer;

    public bool IsActive => gameObject.activeSelf;

    public bool IsVisuallyHidden => m_bIsVisuallyHidden;

    public bool BlocksGameplay => m_bBlocksGameplay;

    public virtual void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager)
    {
        m_cGuiManager = _cGuiManager;
        m_cPopupManager = _cPopupManager;
        EnsureCanvasGroup();
        ApplyRaycastSettings();
    }

    public virtual void OnOpen()
    {
        SetActive(true);
        SetVisualVisible(true);
        // 동일 팝업 레이어 내에서 가장 마지막에 열린 팝업이 최상단에 덮어지도록 정렬
        transform.SetAsLastSibling();
    }

    public virtual void OnClose()
    {
        m_bIsVisuallyHidden = false;
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
    /// 스택 저장/복구(StashAndRestore) 시 오브젝트 상태는 보존한 채 화면 노출 및 입력만 제어합니다.
    /// </summary>
    public virtual void SetVisualVisible(bool _bVisible)
    {
        m_bIsVisuallyHidden = !_bVisible;
        CanvasGroup cgPopup = EnsureCanvasGroup();
        if (cgPopup == null) return;

        if (_bVisible)
        {
            cgPopup.alpha = 1f;
            ApplyRaycastSettings();
        }
        else
        {
            cgPopup.alpha = 0f;
            cgPopup.interactable = false;
            cgPopup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 풀 반납/재사용 시 임시 상태를 정리하는 가상 훅입니다.
    /// </summary>
    public virtual void OnResetForPool()
    {
        m_bIsVisuallyHidden = false;
        if (m_cgPopup != null)
        {
            m_cgPopup.alpha = 1f;
        }
    }

    protected CanvasGroup EnsureCanvasGroup()
    {
        if (m_cgPopup == null)
        {
            m_cgPopup = GetComponent<CanvasGroup>();
            if (m_cgPopup == null)
            {
                m_cgPopup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        return m_cgPopup;
    }

    /// <summary>
    /// 활성화 팝업(플레이 가능)일 경우 마우스/레이캐스트 입력을 통과시키고,
    /// 비활성화 팝업(화면 정지)일 경우 입력을 차단합니다.
    /// </summary>
    protected void ApplyRaycastSettings()
    {
        CanvasGroup cgPopup = EnsureCanvasGroup();
        if (cgPopup != null)
        {
            cgPopup.blocksRaycasts = m_bBlocksGameplay && !m_bIsVisuallyHidden;
            cgPopup.interactable = m_bBlocksGameplay && !m_bIsVisuallyHidden;
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
