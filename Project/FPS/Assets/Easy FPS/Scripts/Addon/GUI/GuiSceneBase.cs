using UnityEngine;

/// <summary>
/// 각 GUI 화면(씬) 객체의 추상 기반 클래스
/// Unity 프리팹/인스펙터 직렬화, 오브젝트명(SceneName) 기반 식별 및 씬별 시간/입력/커서 정책을 지원합니다.
/// </summary>
public abstract class GuiSceneBase : MonoBehaviour, IGuiScene
{
    [Header("Scene State Policy")]
    [Tooltip("게임 시간이 정상적으로 흐르는 플레이 화면 여부 (FPS, RTS 등)")]
    [SerializeField]
    protected bool m_bIsPlayableScene = false;

    [Tooltip("1인칭 직접 이동/시점/사격 입력 허용 여부 (FPS true, RTS/메뉴 false)")]
    [SerializeField]
    protected bool m_bAllowsDirectFpsInput = false;

    [Tooltip("ESC 키 등으로 일시정지 팝업을 열 수 있는지 여부")]
    [SerializeField]
    protected bool m_bCanPause = false;

    [Tooltip("3D 월드 탄약 HUD(HudBullets) 표시 여부")]
    [SerializeField]
    protected bool m_bShowWorldHudBullets = false;

    [Tooltip("이 화면에서 기본으로 사용할 마우스 커서 잠금 모드")]
    [SerializeField]
    protected CursorLockMode m_eCursorLockMode = CursorLockMode.None;

    [Tooltip("이 화면에서 기본으로 사용할 마우스 커서 표시 여부")]
    [SerializeField]
    protected bool m_bCursorVisible = true;

    // 컨텍스트(GUI 관리자) 참조
    protected GUIManager m_cManager;

    /// <summary>
    /// 오브젝트명(프리팹 이름)을 화면 식별 키로 사용
    /// </summary>
    public string SceneName => gameObject.name;

    public bool IsActive => gameObject.activeSelf;

    public bool IsPlayableScene => m_bIsPlayableScene;

    public bool AllowsDirectFpsInput => m_bAllowsDirectFpsInput;

    public bool CanPause => m_bCanPause;

    public bool ShowWorldHudBullets => m_bShowWorldHudBullets;

    public CursorLockMode PreferredCursorLockMode => m_eCursorLockMode;

    public bool PreferredCursorVisible => m_bCursorVisible;

    public virtual void Initialize(GUIManager _cManager)
    {
        m_cManager = _cManager;
    }

    public virtual void OnEnter()
    {
        SetActive(true);
    }

    public virtual void OnExit()
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
}
