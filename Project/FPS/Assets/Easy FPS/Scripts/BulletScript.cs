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

    private RaycastHit m_hitInfo;
    private bool m_bHasProcessedHit = false;

    private void Start()
    {
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
    /// 매 프레임 중복 Raycast 및 Destroy 호출을 방지하고 생성 즉시 1회 히트스캔 판정을 수행합니다.
    /// </summary>
    private void ProcessHitscan()
    {
        if (m_bHasProcessedHit) return;
        m_bHasProcessedHit = true;

        Vector3 vRayOrigin = transform.position + transform.forward * 0.1f;

        if (Physics.Raycast(vRayOrigin, transform.forward, out m_hitInfo, m_fMaxDistance, ~m_maskIgnoreLayer))
        {
            if (m_prefabDecalHitWall != null)
            {
                if (m_hitInfo.transform.CompareTag("LevelPart"))
                {
                    Instantiate(m_prefabDecalHitWall, m_hitInfo.point + m_hitInfo.normal * m_fDecalOffset, Quaternion.LookRotation(m_hitInfo.normal));
                    Destroy(gameObject);
                    return;
                }

                if (m_hitInfo.transform.CompareTag("Dummie") || m_hitInfo.transform.CompareTag("Player"))
                {
                    if (m_prefabBloodEffect != null)
                    {
                        Instantiate(m_prefabBloodEffect, m_hitInfo.point, Quaternion.LookRotation(m_hitInfo.normal));
                    }
                    Destroy(gameObject);
                    return;
                }

                if (m_hitInfo.transform.CompareTag("PhysicObject"))
                {
                    Rigidbody rbHit = m_hitInfo.transform.GetComponent<Rigidbody>();
                    ApplyRigidbodyForce(rbHit, transform.forward, 100f);
                }
            }
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, 0.1f);
    }

    private void ApplyRigidbodyForce(Rigidbody _rbTarget, Vector3 _vShotDir, float _fPower)
    {
        if (_rbTarget == null) return;
        _rbTarget.AddForce(_vShotDir * _fPower);
    }
}
