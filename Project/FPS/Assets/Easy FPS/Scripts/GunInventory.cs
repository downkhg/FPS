using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public enum MenuStyle
{
    horizontal,
    vertical
}

public class GunInventory : MonoBehaviour
{
    [Tooltip("Current weapon gameObject (Synced from Player).")]
    [FormerlySerializedAs("currentGun")]
    public GameObject m_objCurrentGun;

    private int m_nCurrentGunCounter = 0;

    [Tooltip("Put Strings of weapon objects from Resources Folder.")]
    [FormerlySerializedAs("gunsIHave")]
    public List<string> m_listGunsIHave = new List<string>();

    [Tooltip("Icons from weapons.(Fetched when you run the game)*MUST HAVE ICONS WITH CORRESPONDING NAMES IN RESOUCES FOLDER*")]
    [FormerlySerializedAs("icons")]
    public Texture[] m_arrTexIcons;

    [HideInInspector]
    [FormerlySerializedAs("switchWeaponCooldown")]
    public float m_fSwitchWeaponCooldown;

    private Player m_cPlayer;

    [Header("GUI Gun preview variables")]
    [Tooltip("Weapon icons style to pick.")]
    [FormerlySerializedAs("menuStyle")]
    public MenuStyle m_eMenuStyle = MenuStyle.horizontal;

    [Tooltip("Spacing between icons.")]
    [FormerlySerializedAs("spacing")]
    public int m_nSpacing = 10;

    [Tooltip("Begin position in percetanges of screen.")]
    [FormerlySerializedAs("beginPosition")]
    public Vector2 m_vBeginPosition;

    [Tooltip("Size of icon in percetanges of screen.")]
    [FormerlySerializedAs("size")]
    public Vector2 m_vSize;

    [Header("Sounds")]
    [Tooltip("Sound of weapon changing.")]
    [FormerlySerializedAs("weaponChanging")]
    public AudioSource m_asWeaponChanging;

    // ==================== 프로퍼티 ====================
    public int CurrentGunCounter => m_cPlayer != null ? m_cPlayer.CurrentWeaponIndex : m_nCurrentGunCounter;
    public List<string> GunsIHave => m_listGunsIHave;
    public AudioSource WeaponChanging => m_asWeaponChanging;

    void Awake()
    {
        m_cPlayer = GetComponent<Player>();
    }

    public void Initialize()
    {
        if (!m_cPlayer)
            m_cPlayer = GetComponent<Player>();

        UpdateIconsFromResources();

        if (m_cPlayer && m_cPlayer.Gun)
        {
            m_nCurrentGunCounter = m_cPlayer.CurrentWeaponIndex;
            m_objCurrentGun = m_cPlayer.Gun.gameObject;
        }
    }

    /// <summary>
    /// Player(마리오네트)에서 무기가 전환될 때 HUD 표시용 참조를 동기화합니다.
    /// </summary>
    public void SyncWeaponState(int _nWeaponIndex, GameObject _objActiveGun)
    {
        m_nCurrentGunCounter = _nWeaponIndex;
        m_objCurrentGun = _objActiveGun;
    }

    void Update()
    {
        m_fSwitchWeaponCooldown += 1f * Time.deltaTime;
    }

    public void SwitchWeapon(int _nIndex)
    {
        if (!m_cPlayer)
            m_cPlayer = GetComponent<Player>();

        if (m_cPlayer)
        {
            m_cPlayer.SwitchWeapon(_nIndex);
        }
    }

    public void NextWeapon()
    {
        if (!m_cPlayer)
            m_cPlayer = GetComponent<Player>();

        if (m_cPlayer)
        {
            m_cPlayer.NextWeapon();
        }
    }

    private void UpdateIconsFromResources()
    {
        if (m_listGunsIHave == null) return;

        m_arrTexIcons = new Texture[m_listGunsIHave.Count];
        for (int i = 0; i < m_listGunsIHave.Count; i++)
        {
            m_arrTexIcons[i] = (Texture)Resources.Load("Weap_Icons/" + m_listGunsIHave[i] + "_img");
        }
    }

    void OnGUI()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;

        if (m_objCurrentGun && m_listGunsIHave != null && m_listGunsIHave.Count > 0)
        {
            if (m_arrTexIcons == null || m_arrTexIcons.Length != m_listGunsIHave.Count)
            {
                UpdateIconsFromResources();
            }

            for (int i = 0; i < m_listGunsIHave.Count; i++)
            {
                DrawCorrespondingImage(i);
            }
        }
    }

    private void DrawCorrespondingImage(int _nSlotNumber)
    {
        if (m_arrTexIcons == null || _nSlotNumber >= m_arrTexIcons.Length || m_arrTexIcons[_nSlotNumber] == null)
            return;

        if (_nSlotNumber >= m_listGunsIHave.Count)
            return;

        bool bIsCurrentSelected = (CurrentGunCounter == _nSlotNumber);

        Vector2 vBeginPx = ToScreenVec2(m_vBeginPosition);
        Vector2 vSizePx = ToScreenVec2(m_vSize);

        if (m_eMenuStyle == MenuStyle.horizontal)
        {
            if (bIsCurrentSelected)
            {
                GUI.DrawTexture(new Rect(vBeginPx.x + (_nSlotNumber * ToScreenX(m_nSpacing)), vBeginPx.y,
                    vSizePx.x, vSizePx.y),
                    m_arrTexIcons[_nSlotNumber]);
            }
            else
            {
                GUI.DrawTexture(new Rect(vBeginPx.x + (_nSlotNumber * ToScreenX(m_nSpacing) + 10f), vBeginPx.y + 10f,
                    vSizePx.x - 20f, vSizePx.y - 20f),
                    m_arrTexIcons[_nSlotNumber]);
            }
        }
        else if (m_eMenuStyle == MenuStyle.vertical)
        {
            if (bIsCurrentSelected)
            {
                GUI.DrawTexture(new Rect(vBeginPx.x, vBeginPx.y + (_nSlotNumber * ToScreenY(m_nSpacing)),
                    vSizePx.x, vSizePx.y),
                    m_arrTexIcons[_nSlotNumber]);
            }
            else
            {
                GUI.DrawTexture(new Rect(vBeginPx.x, vBeginPx.y + 10f + (_nSlotNumber * ToScreenY(m_nSpacing)),
                    vSizePx.x - 20f, vSizePx.y - 20f),
                    m_arrTexIcons[_nSlotNumber]);
            }
        }
    }

    public void DeadMethod()
    {
        Destroy(this);
    }

    private float ToScreenX(float _fPercent)
    {
        return Screen.width * _fPercent / 100f;
    }

    private float ToScreenY(float _fPercent)
    {
        return Screen.height * _fPercent / 100f;
    }

    private Vector2 ToScreenVec2(Vector2 _vPercent)
    {
        return new Vector2(Screen.width * _vPercent.x / 100f, Screen.height * _vPercent.y / 100f);
    }
}
