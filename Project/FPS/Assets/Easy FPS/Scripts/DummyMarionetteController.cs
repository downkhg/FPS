using UnityEngine;

/// <summary>
/// 컨트롤러만 교체했을 때 플레이어(PlayerMarionette)가 마리오네트 인형처럼 정상 작동하는지 검증하기 위한
/// 자동 조종(AI / 더미) 컨트롤러입니다.
/// PlayerInputController를 비활성화하고 이 컴포넌트를 활성화하면 플레이어가 스스로 움직이고 행동합니다.
/// </summary>
public class DummyMarionetteController : BaseMarionetteController
{
	[Header("Auto Movement Puppet Strings")]
	[Tooltip("자동으로 앞뒤/좌우 원형 패턴 이동을 수행할지 여부")]
	public bool autoPatrolMove = true;
	[Tooltip("고정 이동 방향 (autoPatrolMove가 꺼져 있을 때 사용)")]
	public Vector2 manualMoveAxis = Vector2.zero;

	[Header("Auto Look Puppet Strings")]
	[Tooltip("자동으로 시선을 좌우/상하로 둘러볼지 여부")]
	public bool autoLookAround = true;
	[Tooltip("시선 자동 회전 속도")]
	public float lookSwipeSpeed = 0.3f;

	[Header("Periodic Actions")]
	[Tooltip("일정 주기마다 점프 줄을 당길지 여부")]
	public bool periodicJump = false;
	public float jumpInterval = 2.5f;

	[Tooltip("일정 주기마다 앉기/일어서기를 반복할지 여부")]
	public bool periodicCrouch = false;
	public float crouchInterval = 2.0f;

	[Header("Combat Puppet Strings")]
	[Tooltip("정조준(ADS) 상태를 유지할지 여부")]
	public bool holdAim = false;
	[Tooltip("자동으로 사격을 수행할지 여부")]
	public bool autoFire = false;
	public float fireInterval = 0.5f;

	private float jumpTimer;
	private float fireTimer;

	protected override MarionetteInput BuildInput()
	{
		MarionetteInput input = MarionetteInput.Empty;
		float t = Time.time;

		// 1. 이동 제어
		if (autoPatrolMove)
		{
			input.MoveAxis = new Vector2(Mathf.Cos(t * 1.2f), Mathf.Sin(t * 1.2f));
		}
		else
		{
			input.MoveAxis = manualMoveAxis;
		}

		// 2. 시선 제어
		if (autoLookAround)
		{
			input.LookDelta = new Vector2(
				Mathf.Sin(t * 1.5f) * lookSwipeSpeed,
				Mathf.Cos(t * 2.0f) * (lookSwipeSpeed * 0.3f)
			);
		}

		// 3. 주기적 점프
		if (periodicJump)
		{
			jumpTimer += Time.deltaTime;
			if (jumpTimer >= jumpInterval)
			{
				jumpTimer = 0f;
				input.JumpTriggered = true;
			}
		}

		// 4. 주기적 앉기
		if (periodicCrouch && crouchInterval > 0.1f)
		{
			input.CrouchHold = Mathf.FloorToInt(t / crouchInterval) % 2 == 1;
		}

		// 5. 조준 및 사격
		input.AimHold = holdAim;
		if (autoFire)
		{
			input.FireHold = true;
			fireTimer += Time.deltaTime;
			if (fireTimer >= fireInterval)
			{
				fireTimer = 0f;
				input.FireTriggered = true;
			}
		}

		return input;
	}
}
