using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField, FormerlySerializedAs("currentState")]
    private PlayerState m_eCurrentState = PlayerState.Idle;

    public PlayerState CurrentState => m_eCurrentState;

    private Player m_cPlayer;

    [Header("Gizmo Debug Settings")]
    [Tooltip("Scene 뷰에 플레이어 디버그 기즈모 표시 여부")]
    [FormerlySerializedAs("showGizmos")]
    public bool m_bShowGizmos = true;

    void Awake()
    {
        m_cPlayer = GetComponent<Player>();
    }

    void Start()
    {
        SetState(PlayerState.Idle);
    }

    void Update()
    {
        if (!m_cPlayer) return;

        // 매 프레임 현재 상태에 따른 행동 및 전이 검사 실행
        UpdateState();
    }

    // ==================== 유한상태머신 (FSM) 함수 ====================

    /// <summary>
    /// 상태 설정 및 전환 함수 (State Enter / Exit 처리)
    /// </summary>
    public void SetState(PlayerState _eNewState)
    {
        if (m_eCurrentState == _eNewState) return;

        // 이전 상태 종료(Exit) 처리
        switch (m_eCurrentState)
        {
            case PlayerState.Move:
                m_cPlayer.Move(Vector2.zero);
                break;
            case PlayerState.Attack:
            case PlayerState.Reload:
            case PlayerState.Melee:
                break;
        }

        m_eCurrentState = _eNewState;

        // 새 상태 진입(Enter) 처리
        switch (m_eCurrentState)
        {
            case PlayerState.Idle:
                m_cPlayer.Move(Vector2.zero);
                break;

            case PlayerState.Move:
                break;

            case PlayerState.Attack:
                m_cPlayer.Shoot();
                break;

            case PlayerState.Reload:
                m_cPlayer.Reload();
                break;

            case PlayerState.Melee:
                m_cPlayer.Melee();
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

        switch (m_eCurrentState)
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
        float fH = Input.GetAxisRaw("Horizontal");
        float fV = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(fH) > 0.01f || Mathf.Abs(fV) > 0.01f)
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
        float fH = Input.GetAxisRaw("Horizontal");
        float fV = Input.GetAxisRaw("Vertical");

        // 입력이 없으면 Idle로 전환
        if (Mathf.Abs(fH) < 0.01f && Mathf.Abs(fV) < 0.01f)
        {
            SetState(PlayerState.Idle);
            return;
        }

        // 이동 실행
        m_cPlayer.Move(new Vector2(fH, fV));

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
            m_cPlayer.Shoot();

            // 사격 중 이동 허용
            float fH = Input.GetAxisRaw("Horizontal");
            float fV = Input.GetAxisRaw("Vertical");
            m_cPlayer.Move(new Vector2(fH, fV));
        }
        else
        {
            // 사격 중지 시 이동 또는 대기로 전환
            float fH = Input.GetAxisRaw("Horizontal");
            float fV = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(fH) > 0.01f || Mathf.Abs(fV) > 0.01f)
                SetState(PlayerState.Move);
            else
                SetState(PlayerState.Idle);
        }
    }

    private void UpdateReload()
    {
        // 재장전 중 상태 검사: 재장전이 끝나면 대기로 전환
        if (m_cPlayer.Gun != null && !m_cPlayer.Gun.IsReloading)
        {
            SetState(PlayerState.Idle);
            return;
        }

        // 재장전 중에도 이동 입력은 처리
        float fH = Input.GetAxisRaw("Horizontal");
        float fV = Input.GetAxisRaw("Vertical");
        m_cPlayer.Move(new Vector2(fH, fV));
    }

    private void UpdateMelee()
    {
        // 근접 공격 애니메이션이 끝나면 대기로 복귀
        if (m_cPlayer.Gun != null && !m_cPlayer.Gun.IsMeleeAttack)
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
            m_cPlayer.Jump();
        }
        m_cPlayer.Crouch(Input.GetKey(KeyCode.C));

        // 무기 교체 (E키 또는 1, 2번 숫자키)
        if (Input.GetKeyDown(KeyCode.E))
        {
            m_cPlayer.NextWeapon();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_cPlayer.SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            m_cPlayer.SwitchWeapon(1);
        }

        // 우클릭 조준
        m_cPlayer.Aim(Input.GetButton("Fire2"));
    }

    private bool IsFireInput()
    {
        if (m_cPlayer.Gun == null) return false;
        if (m_cPlayer.Gun.CurrentStyle == GunStyles.automatic)
            return Input.GetButton("Fire1");
        else
            return Input.GetButtonDown("Fire1");
    }

    void OnDrawGizmos()
    {
        if (!m_bShowGizmos) return;

        // 1. 현재 상태에 따른 머리 위 상태 인디케이터 구체
        Vector3 vIndicatorPos = transform.position + Vector3.up * 2.2f;
        switch (m_eCurrentState)
        {
            case PlayerState.Idle: Gizmos.color = Color.gray; break;
            case PlayerState.Move: Gizmos.color = Color.green; break;
            case PlayerState.Attack: Gizmos.color = Color.red; break;
            case PlayerState.Reload: Gizmos.color = Color.yellow; break;
            case PlayerState.Melee: Gizmos.color = new Color(1f, 0.5f, 0f); break;
        }
        Gizmos.DrawSphere(vIndicatorPos, 0.15f);

        // 2. 캐릭터 몸체 Forward 방향 레이 (초록)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * 1f, transform.forward * 2f);

        // 3. 발사 원점(BulletSpawnPlace) 위치 표시 (청록)
        if (m_cPlayer != null && m_cPlayer.BulletSpawnPlace != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(m_cPlayer.BulletSpawnPlace.transform.position, 0.08f);
            Gizmos.DrawRay(m_cPlayer.BulletSpawnPlace.transform.position, m_cPlayer.BulletSpawnPlace.transform.forward * 1.5f);
        }
    }
}
