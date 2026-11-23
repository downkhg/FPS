using UnityEngine;

/// <summary>
/// 각 GUI 화면(씬) 객체의 추상 기반 클래스
/// Unity 프리팹/인스펙터 직렬화 및 오브젝트명(SceneName) 기반 식별을 지원합니다.
/// </summary>
public abstract class GuiSceneBase : MonoBehaviour, IGuiScene
{
    // 컨텍스트(GUI 관리자) 참조
    protected GUIManager m_cManager;

    /// <summary>
    /// 오브젝트명(프리팹 이름)을 화면 식별 키로 사용
    /// </summary>
    public string SceneName => gameObject.name;

    public bool IsActive => gameObject.activeSelf;

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
