using UnityEngine;

/// <summary>
/// 컨트롤러가 마리오네트(PlayerMarionette)의 줄을 당기기 위해 전달하는 프레임 단위 입력 상태 구조체입니다.
/// 플레이어 입력(키보드/마우스/패드), AI, 네트워크, 리플레이 등 모든 컨트롤러가 동일한 규격으로 마리오네트를 제어합니다.
/// </summary>
[System.Serializable]
public struct MarionetteInput
{
	[Tooltip("전후좌우 이동 입력 (x: 좌우 [-1~1], y: 전후 [-1~1])")]
	public Vector2 MoveAxis;

	[Tooltip("시선 회전 변화량 (x: 좌우 Yaw 변화량, y: 상하 Pitch 변화량)")]
	public Vector2 LookDelta;

	[Tooltip("이번 프레임에 점프를 시도했는지 여부")]
	public bool JumpTriggered;

	[Tooltip("앉기 상태를 유지 중인지 여부")]
	public bool CrouchHold;

	[Tooltip("이번 프레임에 달리기 토글(예: LeftShift Down)이 입력되었는지 여부")]
	public bool SprintToggleTriggered;

	[Tooltip("달리기 키를 누르고 있는지 여부 (무기 교체 제한 및 홀드 방식 달리기용)")]
	public bool SprintHold;

	[Tooltip("발사 버튼을 누르고 있는지 여부 (연사 무기용)")]
	public bool FireHold;

	[Tooltip("이번 프레임에 발사 버튼을 눌렀는지 여부 (단발 무기용)")]
	public bool FireTriggered;

	[Tooltip("정조준(ADS) 상태를 유지 중인지 여부")]
	public bool AimHold;

	[Tooltip("이번 프레임에 재장전을 시도했는지 여부")]
	public bool ReloadTriggered;

	[Tooltip("이번 프레임에 근접 공격을 시도했는지 여부")]
	public bool MeleeTriggered;

	[Tooltip("무기 순환 방향 (+1: 다음 무기, -1: 이전 무기, 0: 유지)")]
	public int WeaponCycleDirection;

	[Tooltip("직접 선택할 무기 슬롯 인덱스 (-1: 선택 안 함, 0: 1번 무기, 1: 2번 무기 ...)")]
	public int WeaponSelectSlot;

	/// <summary>
	/// 아무런 입력도 없는 기본(정지) 상태를 반환합니다.
	/// </summary>
	public static MarionetteInput Empty => new MarionetteInput
	{
		MoveAxis = Vector2.zero,
		LookDelta = Vector2.zero,
		JumpTriggered = false,
		CrouchHold = false,
		SprintToggleTriggered = false,
		SprintHold = false,
		FireHold = false,
		FireTriggered = false,
		AimHold = false,
		ReloadTriggered = false,
		MeleeTriggered = false,
		WeaponCycleDirection = 0,
		WeaponSelectSlot = -1
	};
}

/// <summary>
/// 마리오네트를 조종하는 모든 컨트롤러의 공통 인터페이스입니다.
/// </summary>
public interface IMarionetteController
{
	PlayerMarionette TargetMarionette { get; }
	void Possess(PlayerMarionette marionette);
	void Unpossess();
}
