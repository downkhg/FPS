using UnityEngine;
using UnityEngine.Serialization;

public class DestroyAfterTimeParticle : MonoBehaviour
{
    [Tooltip("Time to destroy")]
    [FormerlySerializedAs("timeToDestroy")]
    public float m_fTimeToDestroy = 0.8f;

    private void Start()
    {
        Destroy(gameObject, m_fTimeToDestroy);
    }
}
