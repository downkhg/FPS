using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,       // 대기
        Move,       // 이동
        Attack,     // 사격
        Reload,     // 재장전
        Melee       // 근접 공격
    }

    [Header("FSM State")]
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    public PlayerState CurrentState => currentState;

    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    void Start()
    {
        Debug.Log($"{gameObject.name}.Start()");
        SetState(PlayerState.Idle);
    }

    void Update()
    {
        if (!player) return;

        // 매 프레임 현재 상태에 따른 행동 및 전이 검사 실행
        UpdateState();
    }

    // ==================== 유한상태머신 (FSM) 함수 ====================

    /// <summary>
    /// 상태 설정 및 전환 함수 (State Enter / Exit 처리)
    /// </summary>
    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        // 이전 상태 종료(Exit) 처리
        switch (currentState)
        {
            case PlayerState.Move:
                player.Move(Vector2.zero);
                break;
            case PlayerState.Attack:
                break;
            case PlayerState.Reload:
                break;
            case PlayerState.Melee:
                break;
        }

        currentState = newState;

        // 새 상태 진입(Enter) 처리
        switch (currentState)
        {
            case PlayerState.Idle:
                player.Move(Vector2.zero);
                break;

            case PlayerState.Move:
                break;

            case PlayerState.Attack:
                player.Shoot();
                break;

            case PlayerState.Reload:
                player.Reload();
                break;

            case PlayerState.Melee:
                player.Melee();
                break;
        }
    }

    /// <summary>
    /// 상태 업데이트용 함수 (매 프레임 호출되어 현재 상태 로직 실행)
    /// </summary>
    public void UpdateState()
    {
        // 공통 입력 (조준, 점프, 앉기, 무기 교체는 상시 수신)
        HandleCommonInputs();

        switch (currentState)
        {
            case PlayerState.Idle:
                UpdateIdle();
                break;

            case PlayerState.Move:
                UpdateMove();
                break;

            case PlayerState.Attack:
                UpdateAttack();
                break;

            case PlayerState.Reload:
                UpdateReload();
                break;

            case PlayerState.Melee:
                UpdateMelee();
                break;
        }
    }

    // ==================== 개별 상태 업데이트 로직 ====================

    private void UpdateIdle()
    {
        // 1. 이동 입력 감지 시 Move 상태로 전환
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            SetState(PlayerState.Move);
            return;
        }

        // 2. 공격 입력 감지
        if (IsFireInput())
        {
            SetState(PlayerState.Attack);
            return;
        }

        // 3. 재장전 입력 감지 (R키)
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetState(PlayerState.Reload);
            return;
        }

        // 4. 근접 공격 입력 감지 (Q키)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetState(PlayerState.Melee);
            return;
        }
    }

    private void UpdateMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 입력이 없으면 Idle로 전환
        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
        {
            SetState(PlayerState.Idle);
            return;
        }

        // 이동 실행
        player.Move(new Vector2(h, v));

        // 이동 중 사격
        if (IsFireInput())
        {
            SetState(PlayerState.Attack);
            return;
        }

        // 이동 중 재장전
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetState(PlayerState.Reload);
            return;
        }

        // 이동 중 근접 공격
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetState(PlayerState.Melee);
            return;
        }
    }

    private void UpdateAttack()
    {
        // 사격 실행
        if (IsFireInput())
        {
            player.Shoot();

            // 사격 중 이동 허용
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            player.Move(new Vector2(h, v));
        }
        else
        {
            // 사격 중지 시 이동 또는 대기로 전환
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                SetState(PlayerState.Move);
            else
                SetState(PlayerState.Idle);
        }
    }

    private void UpdateReload()
    {
        // 재장전 중 상태 검사: 재장전이 끝나면 대기로 전환
        if (player.Gun != null && !player.Gun.reloading)
        {
            SetState(PlayerState.Idle);
            return;
        }

        // 재장전 중에도 이동 입력은 처리
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        player.Move(new Vector2(h, v));
    }

    private void UpdateMelee()
    {
        // 근접 공격 애니메이션이 끝나면 대기로 복귀
        if (player.Gun != null && !player.Gun.meeleAttack)
        {
            SetState(PlayerState.Idle);
        }
    }

    // ==================== 공통 보조 입력 ====================

    private void HandleCommonInputs()
    {
        // 점프 & 앉기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.Jump();
        }
        player.Crouch(Input.GetKey(KeyCode.C));

        // 무기 교체 (E키 또는 1, 2번 숫자키)
        if (Input.GetKeyDown(KeyCode.E))
        {
            player.NextWeapon();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            player.SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            player.SwitchWeapon(1);
        }

        // 우클릭 조준
        player.Aim(Input.GetButton("Fire2"));
    }

    private bool IsFireInput()
    {
        if (player.Gun == null) return false;
        if (player.Gun.currentStyle == GunStyles.automatic)
            return Input.GetButton("Fire1");
        else
            return Input.GetButtonDown("Fire1");
    }

    [Header("Gizmo Debug Settings")]
    [Tooltip("Scene 뷰에 플레이어 디버그 기즈모 표시 여부")]
    public bool showGizmos = true;

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 1. 현재 상태에 따른 머리 위 상태 인디케이터 구체
        Vector3 indicatorPos = transform.position + Vector3.up * 2.2f;
        switch (currentState)
        {
            case PlayerState.Idle: Gizmos.color = Color.gray; break;
            case PlayerState.Move: Gizmos.color = Color.green; break;
            case PlayerState.Attack: Gizmos.color = Color.red; break;
            case PlayerState.Reload: Gizmos.color = Color.yellow; break;
            case PlayerState.Melee: Gizmos.color = new Color(1f, 0.5f, 0f); break;
        }
        Gizmos.DrawSphere(indicatorPos, 0.15f);

        // 2. 캐릭터 몸체 Forward 방향 레이 (초록)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * 1f, transform.forward * 2f);

        // 3. 발사 원점(BulletSpawnPlace) 위치 표시 (청록)
        if (player != null && player.BulletSpawnPlace != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.BulletSpawnPlace.transform.position, 0.08f);
            Gizmos.DrawRay(player.BulletSpawnPlace.transform.position, player.BulletSpawnPlace.transform.forward * 1.5f);
        }
    }
}
