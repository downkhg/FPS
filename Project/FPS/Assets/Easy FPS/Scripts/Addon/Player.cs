using UnityEngine;

public class Player : MonoBehaviour
{
    MouseLookScript m_cMouseLockScript;
    PlayerMovementScript m_cPlayerMovementScript;
    [SerializeField] GunScript gun;
    [SerializeField] AudioSource hitMarker;

    public GunInventory gunInventory;

    public MouseLookScript Mls => m_cMouseLockScript;
    public PlayerMovementScript PmS => m_cPlayerMovementScript;
    public GunScript Gun => gun;
    public AudioSource HitMarker => hitMarker;

    bool isInitGunScript = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isInitGunScript && gun) gun.UpdateProcess();
    }

    private void FixedUpdate()
    {
        if(isInitGunScript && gun) gun.FixedUpdateProcess();
    }

    public void Initialize(Transform trCamera, Camera secsecond)
    {
        m_cMouseLockScript = this.GetComponent<MouseLookScript>();
        m_cPlayerMovementScript = this.GetComponent<PlayerMovementScript>();
        gunInventory = this.GetComponent<GunInventory>();

        gunInventory.Initialize();

        GunChangeSetUpdate(trCamera, secsecond);
    }

    public void GunChangeSetUpdate(Transform trMainCamera, Camera secondCamera)
    {
        if (gunInventory && gunInventory.currentGun)
        {
            if (!hitMarker)
                hitMarker = gunInventory.currentGun.transform.Find("hitMarkerSound").GetComponent<AudioSource>();
            if (!gun)
            {
                gun = gunInventory.currentGun.GetComponent<GunScript>();
                gun.Initialize(this.transform,m_cMouseLockScript, trMainCamera, secondCamera, GameManager.instance.BulletSpawnPlace,hitMarker);
                isInitGunScript = true;
            }
        }
    }
}
