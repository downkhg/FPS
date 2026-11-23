/// <summary>
/// 팝업 UI 객체의 공통 제어 인터페이스
/// enum 하드코딩 대신 오브젝트명(PopupName)을 고유 식별자로 사용합니다.
/// </summary>
public interface IGuiPopup
{
    /// <summary>
    /// 해당 팝업 객체의 고유 이름 (프리팹/오브젝트명)
    /// </summary>
    string PopupName { get; }

    /// <summary>
    /// 팝업 활성화 여부
    /// </summary>
    bool IsActive { get; }

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
}
