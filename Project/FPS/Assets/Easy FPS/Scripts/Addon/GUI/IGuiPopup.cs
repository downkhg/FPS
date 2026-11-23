/// <summary>
/// 기획 의도에 따른 팝업 렌더링 우선순위 레이어
/// 숫자가 클수록 화면 최상단에 렌더링됩니다.
/// </summary>
public enum EPopupLayer
{
    Active = 0, // 활성화 팝업 (게임 플레이 가능, Dim 미사용 - 예: 토스트/알림)
    Menu = 1,   // 기본 메뉴형 비활성화 팝업 (예: 일시정지 메뉴)
    Modal = 2,  // 메뉴/씬 위에 덮이는 모달 팝업 (예: 음량 조절, 확인 팝업)
    System = 3  // 최상단 강제 안내/경고 팝업
}

/// <summary>
/// 팝업 인스턴스 공급 정책
/// </summary>
public enum EPopupInstanceMode
{
    SingleReuse, // 프리팹 이름당 단일 인스턴스 재사용
    PooledMulti  // 무한 중첩을 위한 프리팹 기반 오브젝트 풀링 (필요 시 추가 생성)
}

/// <summary>
/// 팝업 중첩 시 스택 저장 및 복구 정책
/// </summary>
public enum EPopupStackMode
{
    Overlay,         // 기존 팝업을 화면에 남긴 채 위에 덮기
    StashAndRestore, // 직전 팝업의 상태는 유지한 채 화면에서만 숨기고, 상위 팝업 종료 시 복구
    Replace          // 동일 레이어의 기존 팝업을 닫고 새 팝업으로 교체
}

/// <summary>
/// 팝업 UI 객체의 공통 제어 인터페이스
/// 프리팹/오브젝트명(PopupName)을 고유 식별자로 사용합니다.
/// </summary>
public interface IGuiPopup
{
    /// <summary>
    /// 해당 팝업 객체의 고유 이름 (프리팹/오브젝트명)
    /// </summary>
    string PopupName { get; }

    /// <summary>
    /// 해당 팝업이 속하는 기획 레이어
    /// </summary>
    EPopupLayer Layer { get; }

    /// <summary>
    /// 팝업 활성화 여부
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 스택 중첩(StashAndRestore)에 의해 시각적으로 숨겨져 있는지 여부
    /// </summary>
    bool IsVisuallyHidden { get; }

    /// <summary>
    /// 비활성화 팝업 여부
    /// true: 화면 및 게임 진행을 정지시키고 뒤쪽 UI 클릭을 차단하는 모달 팝업
    /// false: 게임 플레이(조작/시간)를 유지한 채 표시되는 활성화 팝업
    /// </summary>
    bool BlocksGameplay { get; }

    /// <summary>
    /// 초기화 메서드 (GUIManager 및 GuiPopupManager 컨텍스트 전달)
    /// </summary>
    /// <param name="_cGuiManager">상위 GUI 관리자</param>
    /// <param name="_cPopupManager">팝업 관리자</param>
    void Initialize(GUIManager _cGuiManager, GuiPopupManager _cPopupManager);

    /// <summary>
    /// 팝업이 화면에 열릴 때 호출
    /// </summary>
    void OnOpen();

    /// <summary>
    /// 팝업이 화면에서 닫힐 때 호출
    /// </summary>
    void OnClose();

    /// <summary>
    /// 오브젝트 활성화/비활성화 처리
    /// </summary>
    /// <param name="_bActive">활성화 여부</param>
    void SetActive(bool _bActive);

    /// <summary>
    /// 스택 저장/복구 시 오브젝트의 데이터와 활성 상태를 유지한 채 시각적 표시/입력만 토글합니다.
    /// </summary>
    /// <param name="_bVisible">표시 여부</param>
    void SetVisualVisible(bool _bVisible);

    /// <summary>
    /// 풀링(PooledMulti) 반납 또는 재사용 직전에 내부 임시 데이터/콜백을 초기화합니다.
    /// </summary>
    void OnResetForPool();
}
