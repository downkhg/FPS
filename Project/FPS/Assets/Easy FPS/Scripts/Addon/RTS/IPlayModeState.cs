/// <summary>
/// 플레이 모드(FPS / RTS 등) 확장 상태 패턴 인터페이스
/// PlayModeManager의 딕셔너리에서 ModeName 문자열 키로 관리됩니다.
/// </summary>
public interface IPlayModeState
{
    /// <summary>
    /// 모드 식별 키 (예: "FPS", "RTS")
    /// </summary>
    string ModeName { get; }

    /// <summary>
    /// 컨텍스트(PlayModeManager) 주입 및 초기 설정
    /// </summary>
    /// <param name="_cManager">플레이 모드 관리자</param>
    void Initialize(PlayModeManager _cManager);

    /// <summary>
    /// 해당 플레이 모드로 진입할 때 호출 (카메라, 입력, GUI 씬 전환)
    /// </summary>
    void OnEnter();

    /// <summary>
    /// 다른 플레이 모드로 전환되어 빠져나갈 때 호출
    /// </summary>
    void OnExit();

    /// <summary>
    /// 현재 활성 모드일 때 매 프레임 호출
    /// </summary>
    void OnUpdate();
}
