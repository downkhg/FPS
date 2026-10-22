using UnityEngine;

/// <summary>
/// 마리오네트(PlayerMarionette)를 조종하는 모든 컨트롤러의 추상 부모 클래스입니다.
/// 새로운 조작 방식(키보드/마우스, 패드, AI, 네트워크, 리플레이 등)을 만들 때 이 클래스를 상속받아
/// BuildInput() 또는 ControlMarionette()만 구현하면 플레이어 본체 코드 수정 없이 즉시 작동합니다.
/// </summary>
public abstract class BaseMarionetteController : MonoBehaviour, IMarionetteController
{
	[Header("Target Marionette")]
	[Tooltip("조종할 대상 마리오네트입니다. 비워두면 같은 GameObject의 PlayerMarionette를 자동 연결합니다.")]
	[SerializeField] protected PlayerMarionette targetMarionette;

	public PlayerMarionette TargetMarionette => targetMarionette;

	protected virtual void Awake()
	{
		if (targetMarionette == null)
		{
			targetMarionette = GetComponent<PlayerMarionette>();
		}
	}

	protected virtual void OnEnable()
	{
		if (targetMarionette == null)
		{
			targetMarionette = GetComponent<PlayerMarionette>();
		}
		if (targetMarionette != null)
		{
			Possess(targetMarionette);
		}
	}

	protected virtual void OnDisable()
	{
		if (targetMarionette != null && targetMarionette.ActiveController == (IMarionetteController)this)
		{
			targetMarionette.OnUnpossessedBy(this);
		}
	}

	/// <summary>
	/// 지정한 마리오네트의 제어권을 획득합니다.
	/// </summary>
	public virtual void Possess(PlayerMarionette marionette)
	{
		if (marionette == null)
			return;

		if (targetMarionette != null && targetMarionette != marionette)
		{
			Unpossess();
		}

		targetMarionette = marionette;
		targetMarionette.OnPossessedBy(this);
	}

	/// <summary>
	/// 현재 조종 중인 마리오네트의 제어권을 내려놓습니다.
	/// </summary>
	public virtual void Unpossess()
	{
		if (targetMarionette != null)
		{
			PlayerMarionette prev = targetMarionette;
			targetMarionette = null;
			prev.OnUnpossessedBy(this);
		}
	}

	protected virtual void Update()
	{
		if (targetMarionette == null)
			return;

		ControlMarionette(targetMarionette);
	}

	/// <summary>
	/// 매 프레임 마리오네트를 제어합니다. 기본 구현은 BuildInput()으로 생성한 입력 구조체를 마리오네트에 전달합니다.
	/// 직접 개별 액션 메서드를 호출하고 싶다면 자식 클래스에서 이 메서드를 오버라이드할 수 있습니다.
	/// </summary>
	protected virtual void ControlMarionette(PlayerMarionette marionette)
	{
		MarionetteInput input = BuildInput();
		marionette.ApplyInput(in input);
	}

	/// <summary>
	/// 하위 컨트롤러에서 현재 프레임의 입력 상태(MarionetteInput)를 구성하여 반환합니다.
	/// </summary>
	protected abstract MarionetteInput BuildInput();
}
