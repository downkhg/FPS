using UnityEngine;

/// <summary>
/// 플레이어(또는 AI) 캐릭터의 모든 신체/무기 부품(이동, 시선, 인벤토리, 총기)을 하나로 묶어주는 마리오네트 본체(Pawn) 허브입니다.
/// 자체적으로 입력을 감지하지 않으며, 오직 외부 컨트롤러(IMarionetteController)가 당기는 줄(명령)에 의해서만 동작합니다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerMarionette : MonoBehaviour
{
	[Header("Marionette Parts (Auto-bound if empty)")]
	[Tooltip("이동, 점프, 앉기, 근접 판정을 담당하는 컴포넌트")]
	public PlayerMovementScript movement;

	[Tooltip("시선 및 몸체 회전을 담당하는 컴포넌트")]
	public MouseLookScript mouseLook;

	[Tooltip("무기 장착 및 교체를 담당하는 컴포넌트")]
	public GunInventory gunInventory;

	[Header("Controller Binding")]
	[Tooltip("씬 시작 시 연결된 컨트롤러가 하나도 없으면 기본 PlayerInputController를 자동 추가할지 여부")]
	public bool autoAddDefaultController = true;

	/// <summary>
	/// 현재 이 마리오네트를 조종하고 있는 컨트롤러입니다.
	/// </summary>
	public IMarionetteController ActiveController { get; private set; }

	/// <summary>
	/// 현재 손에 들고 있는 무기의 GunScript입니다.
	/// </summary>
	public GunScript CurrentGun
	{
		get
		{
			if (gunInventory == null)
				return null;
			if (gunInventory.CurrentGunScript != null)
				return gunInventory.CurrentGunScript;
			if (gunInventory.currentGun != null)
				return gunInventory.currentGun.GetComponent<GunScript>();
			return null;
		}
	}

	void Awake()
	{
		BindComponents();
	}

	void Start()
	{
		BindComponents();

		if (ActiveController == null)
		{
			BaseMarionetteController existingController = GetComponent<BaseMarionetteController>();
			if (existingController != null && existingController.enabled)
			{
				existingController.Possess(this);
			}
			else if (autoAddDefaultController && existingController == null)
			{
				PlayerInputController defaultController = gameObject.AddComponent<PlayerInputController>();
				defaultController.Possess(this);
			}
		}
	}

	/// <summary>
	/// 마리오네트 하위 부품 참조를 자동으로 연결합니다.
	/// </summary>
	public void BindComponents()
	{
		if (movement == null)
			movement = GetComponent<PlayerMovementScript>();
		if (mouseLook == null)
			mouseLook = GetComponent<MouseLookScript>();
		if (gunInventory == null)
			gunInventory = GetComponent<GunInventory>();
	}

	/// <summary>
	/// 컨트롤러가 이 마리오네트의 제어권을 획득할 때 호출됩니다.
	/// </summary>
	public void OnPossessedBy(IMarionetteController controller)
	{
		if (ActiveController != null && ActiveController != controller)
		{
			ActiveController.Unpossess();
		}
		ActiveController = controller;
		ResetInput();
	}

	/// <summary>
	/// 컨트롤러가 제어권을 해제할 때 호출됩니다.
	/// </summary>
	public void OnUnpossessedBy(IMarionetteController controller)
	{
		if (ActiveController == controller)
		{
			ActiveController = null;
		}
		ResetInput();
	}

	/// <summary>
	/// 프레임 단위 입력 구조체(MarionetteInput)를 받아 마리오네트의 모든 줄을 한 번에 조작합니다.
	/// </summary>
	public void ApplyInput(in MarionetteInput input)
	{
		// 1. 시선 회전 및 정조준(ADS)
		Aim(input.AimHold);
		Look(input.LookDelta);

		// 2. 전후좌우 이동, 앉기, 점프
		Move(input.MoveAxis);
		Crouch(input.CrouchHold);
		if (input.JumpTriggered)
		{
			Jump();
		}

		// 3. 달리기 (총기 유무에 따라 총기 무게 속도 또는 기본 속도 적용)
		Sprint(input.MoveAxis.y, input.SprintToggleTriggered);

		// 4. 무기 교체
		if (input.WeaponSelectSlot >= 0)
		{
			SelectWeapon(input.WeaponSelectSlot, input.SprintHold);
		}
		else if (input.WeaponCycleDirection != 0)
		{
			CycleWeapon(input.WeaponCycleDirection, input.SprintHold);
		}

		// 5. 사격, 재장전, 근접 공격
		Fire(input.FireHold, input.FireTriggered);

		if (input.ReloadTriggered)
		{
			Reload();
		}

		if (input.MeleeTriggered)
		{
			MeleeAttack();
		}
	}

	/// <summary>
	/// 모든 제어 입력을 초기 상태(정지)로 되돌립니다.
	/// 컨트롤러가 교체되거나 비활성화될 때 마리오네트가 폭주하지 않도록 보호합니다.
	/// </summary>
	public void ResetInput()
	{
		Move(Vector2.zero);
		Crouch(false);
		Aim(false);
		Fire(false, false);
		SetSprint(false);
	}

	// =========================================================================
	// 개별 줄(String) 제어 API — 컨트롤러에서 필요한 동작만 직접 호출할 수도 있습니다.
	// =========================================================================

	public void Move(Vector2 moveAxis)
	{
		if (movement != null)
		{
			movement.MoveAction(moveAxis);
		}
	}

	public void Look(Vector2 lookDelta)
	{
		if (mouseLook != null)
		{
			mouseLook.LookAction(lookDelta);
		}
	}

	public void SetLookAngles(float yaw, float pitch)
	{
		if (mouseLook != null)
		{
			mouseLook.SetLookAnglesAction(yaw, pitch);
		}
	}

	public bool Jump()
	{
		return movement != null && movement.JumpAction();
	}

	public bool Jump(float customJumpForce)
	{
		return movement != null && movement.JumpAction(customJumpForce);
	}

	public bool Crouch(bool isCrouching)
	{
		return movement != null && movement.CrouchingAction(isCrouching);
	}

	public void Sprint(float forwardInput, bool sprintToggleDown)
	{
		GunScript gun = CurrentGun;
		if (gun != null)
		{
			gun.SprintWithGunAction(forwardInput, sprintToggleDown);
		}
		else if (movement != null)
		{
			bool canSprint = forwardInput > 0;
			movement.SprintAction(canSprint, sprintToggleDown, movement.defaultWalkingSpeed, movement.defaultRunningSpeed);
		}
	}

	public void SetSprint(bool isSprinting)
	{
		if (movement == null)
			return;

		GunScript gun = CurrentGun;
		int walkSpd = gun != null ? gun.walkingSpeed : movement.defaultWalkingSpeed;
		int runSpd = gun != null ? gun.runningSpeed : movement.defaultRunningSpeed;
		movement.SetSprintState(isSprinting, walkSpd, runSpd);
	}

	public void Aim(bool isAiming)
	{
		if (mouseLook != null)
		{
			mouseLook.AimLookAction(isAiming);
		}
		GunScript gun = CurrentGun;
		if (gun != null)
		{
			gun.AimAction(isAiming);
		}
	}

	public void Fire(bool isFireHold, bool isFireDown)
	{
		GunScript gun = CurrentGun;
		if (gun != null)
		{
			gun.FireAction(isFireHold, isFireDown);
		}
	}

	public void Reload()
	{
		GunScript gun = CurrentGun;
		if (gun != null)
		{
			gun.ReloadAction();
		}
	}

	public void MeleeAttack()
	{
		GunScript gun = CurrentGun;
		if (gun != null)
		{
			gun.MeleeAttackAction();
		}
	}

	public bool CycleWeapon(int direction, bool isSprintHeld = false)
	{
		return gunInventory != null && gunInventory.CycleWeaponAction(direction, isSprintHeld);
	}

	public bool SelectWeapon(int slotIndex, bool isSprintHeld = false)
	{
		return gunInventory != null && gunInventory.SelectWeaponAction(slotIndex, isSprintHeld);
	}
}
