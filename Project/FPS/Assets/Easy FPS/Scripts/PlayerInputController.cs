using UnityEngine;

/// <summary>
/// 키보드와 마우스 입력을 읽어 마리오네트(PlayerMarionette)의 줄을 당기는 기본 플레이어 컨트롤러입니다.
/// 플레이어 본체(PlayerMovementScript, MouseLookScript, GunInventory, GunScript)에서 분리된 모든 Input 로직을 담당합니다.
/// </summary>
public class PlayerInputController : BaseMarionetteController
{
	[Header("Axis Names")]
	public string horizontalAxis = "Horizontal";
	public string verticalAxis = "Vertical";
	public string mouseXAxis = "Mouse X";
	public string mouseYAxis = "Mouse Y";
	public string fireButton = "Fire1";
	public string aimButton = "Fire2";
	public string scrollWheelAxis = "Mouse ScrollWheel";

	[Header("Key Bindings")]
	public KeyCode jumpKey = KeyCode.Space;
	public KeyCode crouchKey = KeyCode.C;
	public KeyCode sprintKey = KeyCode.LeftShift;
	public KeyCode reloadKey = KeyCode.R;
	public KeyCode meleeKey = KeyCode.Q;
	public KeyCode nextWeaponKey = KeyCode.UpArrow;
	public KeyCode prevWeaponKey = KeyCode.DownArrow;
	public KeyCode weaponSlot1Key = KeyCode.Alpha1;
	public KeyCode weaponSlot2Key = KeyCode.Alpha2;
	public KeyCode lockCursorKey = KeyCode.L;

	[Header("Cursor Settings")]
	[Tooltip("컨트롤러 활성화 시 마우스 커서를 화면 중앙에 잠글지 여부")]
	public bool lockCursorOnStart = true;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (lockCursorOnStart)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	protected override void Update()
	{
		if (Input.GetKeyDown(lockCursorKey))
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		base.Update();
	}

	protected override MarionetteInput BuildInput()
	{
		MarionetteInput input = MarionetteInput.Empty;

		// 1. 이동 및 시선 입력
		input.MoveAxis = new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis));
		input.LookDelta = new Vector2(Input.GetAxis(mouseXAxis), Input.GetAxis(mouseYAxis));

		// 2. 점프, 앉기, 달리기 입력
		input.JumpTriggered = Input.GetKeyDown(jumpKey);
		input.CrouchHold = Input.GetKey(crouchKey);
		input.SprintToggleTriggered = Input.GetKeyDown(sprintKey);
		input.SprintHold = Input.GetKey(sprintKey);

		// 3. 사격, 정조준, 재장전, 근접공격 입력
		input.FireHold = Input.GetButton(fireButton);
		input.FireTriggered = Input.GetButtonDown(fireButton);
		input.AimHold = Input.GetAxisRaw(aimButton) != 0 || Input.GetButton(aimButton);
		input.ReloadTriggered = Input.GetKeyDown(reloadKey);
		input.MeleeTriggered = Input.GetKeyDown(meleeKey);

		// 4. 무기 교체 입력 (스크롤 휠, 방향키, 숫자키 1/2)
		float scroll = Input.GetAxis(scrollWheelAxis);
		if (Input.GetKeyDown(nextWeaponKey) || scroll > 0f)
		{
			input.WeaponCycleDirection = 1;
		}
		else if (Input.GetKeyDown(prevWeaponKey) || scroll < 0f)
		{
			input.WeaponCycleDirection = -1;
		}

		if (Input.GetKeyDown(weaponSlot1Key))
		{
			input.WeaponSelectSlot = 0;
		}
		else if (Input.GetKeyDown(weaponSlot2Key))
		{
			input.WeaponSelectSlot = 1;
		}

		return input;
	}
}
