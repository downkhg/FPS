using UnityEngine;

/// <summary>
/// 각 GUI 화면(씬) 객체의 공통 제어 인터페이스
/// enum 하드코딩 대신 오브젝트명(SceneName)을 고유 식별자로 사용하며,
/// 각 씬 상태 객체가 자신의 시간/입력/커서 정책을 직접 선언합니다.
/// </summary>
public interface IGuiScene
{
    /// <summary>
    /// 해당 화면 객체의 고유 이름 (프리팹/오브젝트명)
    /// </summary>
    string SceneName { get; }

    /// <summary>
    /// 화면 활성화 여부
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 게임 시간이 정상적으로 흐르는 플레이 화면 여부 (FPS, RTS 등 true / Title, GameOver 등 false)
    /// </summary>
    bool IsPlayableScene { get; }

    /// <summary>
    /// 1인칭 직접 조작(이동/시점/사격) 입력 허용 여부 (FPS true / RTS, 메뉴 false)
    /// </summary>
    bool AllowsDirectFpsInput { get; }

    /// <summary>
    /// ESC 키 등으로 일시정지 팝업을 열 수 있는지 여부
    /// </summary>
    bool CanPause { get; }

    /// <summary>
    /// 3D 월드 탄약 HUD(HudBullets) 표시 여부
    /// </summary>
    bool ShowWorldHudBullets { get; }

    /// <summary>
    /// 비활성화 팝업이 없을 때 이 화면이 요구하는 마우스 커서 잠금 모드
    /// </summary>
    CursorLockMode PreferredCursorLockMode { get; }

    /// <summary>
    /// 비활성화 팝업이 없을 때 이 화면이 요구하는 마우스 커서 표시 여부
    /// </summary>
    bool PreferredCursorVisible { get; }

    /// <summary>
    /// 초기화 메서드 (컨텍스트인 GUIManager 참조 전달 및 이벤트 바인딩)
    /// </summary>
    /// <param name="_cManager">GUI 관리자 인스턴스</param>
    void Initialize(GUIManager _cManager);

    /// <summary>
    /// 해당 화면으로 전환되어 진입할 때 호출
    /// </summary>
    void OnEnter();

    /// <summary>
    /// 다른 화면으로 전환되어 빠져나갈 때 호출
    /// </summary>
    void OnExit();

    /// <summary>
    /// 오브젝트 활성화/비활성화 처리
    /// </summary>
    /// <param name="_bActive">활성화 여부</param>
    void SetActive(bool _bActive);
}

/// <summary>
/// 볼륨 및 음소거 등 오디오 설정을 동기화할 수 있는 화면 인터페이스
/// </summary>
public interface IAudioSyncableGui
{
    /// <summary>
    /// BGM/SFX 볼륨 및 Mute 상태를 UI 컨트롤에 반영
    /// </summary>
    /// <param name="_fBgmVolume">배경음악 볼륨</param>
    /// <param name="_fSfxVolume">효과음 볼륨</param>
    /// <param name="_bIsMuted">음소거 여부</param>
    void SyncAudioSettings(float _fBgmVolume, float _fSfxVolume, bool _bIsMuted);
}
