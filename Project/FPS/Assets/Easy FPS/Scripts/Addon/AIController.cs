using UnityEngine;

[RequireComponent(typeof(Player))]
public class AIController : MonoBehaviour
{
    private Player npc;

    [Header("AI Detection Settings")]
    [Tooltip("적 탐지 반경")]
    public float detectionRadius = 40f;
    [Tooltip("우선 탐색할 타깃 태그 (기본: Player)")]
    public string primaryTargetTag = "Player";
    [Tooltip("보조 탐색할 타깃 태그 (예: Dummie)")]
    public string secondaryTargetTag = "Dummie";

    [Header("Combat Ranges")]
    [Tooltip("저격 사거리 (이 거리 이상이면 저격총으로 교환 후 사격)")]
    public float sniperRangeMin = 15f;
    [Tooltip("일반 사거리 최대값 (이 거리 이내로 들어오면 일반총 사격, 벗어나면 추적 이동)")]
    public float normalRangeMax = 15f;
    [Tooltip("근접 공격 사거리")]
    public float meleeAttackRange = 2.5f;

    [Header("Target Tracking")]
    public Transform currentTarget;

    private float targetSearchTimer = 0f;

    void Awake()
    {
        npc = GetComponent<Player>();
    }

    void Update()
    {
        if (!npc) return;

        // 1. 적 탐색 주기
        targetSearchTimer -= Time.deltaTime;
        if (targetSearchTimer <= 0f || currentTarget == null)
        {
            targetSearchTimer = 0.5f;
            currentTarget = FindTarget();
        }

        // 적이 없을 때: 정지 및 조준 해제
        if (currentTarget == null)
        {
            npc.MoveWorld(Vector3.zero);
            npc.Aim(false);
            return;
        }

        // 2. 적 바라보기
        npc.LookAtTarget(currentTarget.position);

        // 3. 거리 계산
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        // 4. 요구사항 기반 AI 판단 루프
        if (distance <= meleeAttackRange)
        {
            // [초근접]: 근접 공격
            npc.MoveWorld(Vector3.zero);
            npc.Aim(false);
            npc.Melee();
        }
        else if (distance >= sniperRangeMin)
        {
            // [적이 저격거리에 있으면]: 저격총(1번 무기)으로 쏜다
            npc.MoveWorld(Vector3.zero); // 사격 시 안정적으로 정지
            npc.SwitchWeapon(1); // 저격총(NewGun_semi) 교체
            npc.Aim(true);        // 조준
            npc.Shoot();          // 저격 사격
        }
        else if (distance <= normalRangeMax)
        {
            // [범위에 들어왔다면]: 일반총(0번 무기)으로 쏜다
            npc.MoveWorld(Vector3.zero);
            npc.SwitchWeapon(0); // 일반총(NewGun_auto) 교체
            npc.Aim(false);       // 일반 지향 사격
            npc.Shoot();          // 연사 사격
        }
        else
        {
            // [범위에 들어오지 않았으면]: 이동
            npc.Aim(false);
            Vector3 toTargetDir = (currentTarget.position - transform.position).normalized;
            npc.MoveWorld(toTargetDir);
        }
    }

    Transform FindTarget()
    {
        // 1. 태그로 탐색 (Player 또는 Dummie)
        GameObject playerObj = GameObject.FindGameObjectWithTag(primaryTargetTag);
        if (playerObj && playerObj != this.gameObject)
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist <= detectionRadius)
                return playerObj.transform;
        }

        GameObject dummieObj = GameObject.FindGameObjectWithTag(secondaryTargetTag);
        if (dummieObj && dummieObj != this.gameObject)
        {
            float dist = Vector3.Distance(transform.position, dummieObj.transform.position);
            if (dist <= detectionRadius)
                return dummieObj.transform;
        }

        // 2. 콜라이더 오버랩 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (hit.CompareTag(primaryTargetTag) || hit.CompareTag(secondaryTargetTag))
            {
                return hit.transform;
            }
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sniperRangeMin);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    }
}
