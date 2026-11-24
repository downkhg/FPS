using System;
using UnityEngine;
using UnityEngine.Serialization;

public class BulletScript : MonoBehaviour
{
    [Tooltip("Furthest distance bullet will look for target")]
    [FormerlySerializedAs("maxDistance")]
    public float m_fMaxDistance = 1000000f;

    [Tooltip("Prefab of wall damange hit. The object needs 'LevelPart' tag to create decal on it.")]
    [FormerlySerializedAs("decalHitWall")]
    public GameObject m_prefabDecalHitWall;

    [Tooltip("Decal will need to be sligtly infront of the wall so it doesnt cause rendeing problems so for best feel put from 0.01-0.1.")]
    [FormerlySerializedAs("floatInfrontOfWall")]
    public float m_fDecalOffset = 0.05f;

    [Tooltip("Blood prefab particle this bullet will create upoon hitting enemy")]
    [FormerlySerializedAs("bloodEffect")]
    public GameObject m_prefabBloodEffect;

    [Tooltip("Put Weapon layer and Player layer to ignore bullet raycast.")]
    [FormerlySerializedAs("ignoreLayer")]
    public LayerMask m_maskIgnoreLayer;

    [Tooltip("Damage dealt on hit")]
    public float m_fDamage = 20f;

    private Transform m_trShooter;
    private string m_strShooterTag = string.Empty;
    private bool m_bHasProcessedHit = false;

    // ==================== 공용 진영 태그 피아식별 유틸리티 ====================

    public static bool IsAllyTag(string _strTag)
    {
        return _strTag == "Player" || _strTag == "Ally";
    }

    public static bool IsEnemyTag(string _strTag)
    {
        return _strTag == "Enemy" || _strTag == "Dummie";
    }

    public static bool IsCombatUnitTag(string _strTag)
    {
        return IsAllyTag(_strTag) || IsEnemyTag(_strTag);
    }

    /// <summary>
    /// 발사자 태그와 대상 태그가 서로 적대 진영인지 판별합니다.
    /// (아군: Player/Ally <-> 적군: Enemy/Dummie)
    /// </summary>
    public static bool IsHostileTag(string _strMyTag, string _strTargetTag)
    {
        if (IsAllyTag(_strMyTag)) return IsEnemyTag(_strTargetTag);
        if (IsEnemyTag(_strMyTag)) return IsAllyTag(_strTargetTag);
        return false;
    }

    /// <summary>
    /// 발사자 정보를 주입한 직후 히트스캔을 실행합니다. (아군 오사 및 자가 피격 방지)
    /// </summary>
    public void InitializeShooter(Transform _trShooter, string _strShooterTag)
    {
        m_trShooter = _trShooter;
        m_strShooterTag = _strShooterTag ?? string.Empty;
        ProcessHitscan();
    }

    private void Update()
    {
        if (!m_bHasProcessedHit)
        {
            ProcessHitscan();
        }
    }

    /// <summary>
    /// RaycastAll을 거리순으로 정렬하여 발사자 본인은 무시하고,
    /// 아군 오사는 데미지 없이 차단하며, 적대 진영에만 데미지를 적용합니다.
    /// </summary>
    private void ProcessHitscan()
    {
        if (m_bHasProcessedHit) return;
        m_bHasProcessedHit = true;

        Vector3 vRayOrigin = transform.position + transform.forward * 0.1f;
        RaycastHit[] arrHits = Physics.RaycastAll(vRayOrigin, transform.forward, m_fMaxDistance, ~m_maskIgnoreLayer);

        if (arrHits != null && arrHits.Length > 0)
        {
            Array.Sort(arrHits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < arrHits.Length; i++)
            {
                RaycastHit sHit = arrHits[i];
                if (sHit.transform == null) continue;

                // 발사자 본인의 콜라이더는 무조건 통과
                if (m_trShooter != null && (sHit.transform == m_trShooter || sHit.transform.IsChildOf(m_trShooter)))
                {
                    continue;
                }

                if (sHit.transform.CompareTag("LevelPart"))
                {
                    if (m_prefabDecalHitWall != null)
                    {
                        Instantiate(m_prefabDecalHitWall, sHit.point + sHit.normal * m_fDecalOffset, Quaternion.LookRotation(sHit.normal));
                    }
                    Destroy(gameObject);
                    return;
                }

                Player cHitPlayer = sHit.transform.GetComponentInParent<Player>();
                string strTargetTag = cHitPlayer != null ? cHitPlayer.tag : sHit.transform.tag;

                if (cHitPlayer != null || IsCombatUnitTag(strTargetTag))
                {
                    // 발사자 진영이 지정되어 있고, 대상이 같은 진영(아군)이면 오사 차단
                    if (!string.IsNullOrEmpty(m_strShooterTag) && !IsHostileTag(m_strShooterTag, strTargetTag))
                    {
                        Destroy(gameObject);
                        return;
                    }

                    if (cHitPlayer != null)
                    {
                        cHitPlayer.TakeDamage(m_fDamage);
                    }

                    if (m_prefabBloodEffect != null)
                    {
                        Instantiate(m_prefabBloodEffect, sHit.point, Quaternion.LookRotation(sHit.normal));
                    }
                    Destroy(gameObject);
                    return;
                }

                if (sHit.transform.CompareTag("PhysicObject"))
                {
                    Rigidbody rbHit = sHit.transform.GetComponent<Rigidbody>();
                    ApplyRigidbodyForce(rbHit, transform.forward, 100f);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        Destroy(gameObject, 0.1f);
    }

    private void ApplyRigidbodyForce(Rigidbody _rbTarget, Vector3 _vShotDir, float _fPower)
    {
        if (_rbTarget == null) return;
        _rbTarget.AddForce(_vShotDir * _fPower);
    }
}
