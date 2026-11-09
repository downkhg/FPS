using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (!player) return;

        // 1. 방향키 / WASD -> 이동
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        player.Move(new Vector2(h, v));

        // 2. 점프 & 앉기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.Jump();
        }
        player.Crouch(Input.GetKey(KeyCode.C));

        // 3. 스킬 키 조작 (Q, E, R 등)
        // Q: 근접 공격 (스킬)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            player.Melee();
        }

        // E: 무기 교환 (스킬)
        if (Input.GetKeyDown(KeyCode.E))
        {
            player.NextWeapon();
        }

        // 숫자키 1, 2로 직관적인 무기 선택도 지원
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            player.SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            player.SwitchWeapon(1);
        }

        // R: 재장전 (스킬)
        if (Input.GetKeyDown(KeyCode.R))
        {
            player.Reload();
        }

        // 4. 마우스 조작 (사격 & 조준)
        // 마우스 우클릭 -> 조준
        player.Aim(Input.GetButton("Fire2"));

        // 마우스 좌클릭 -> 사격
        if (player.Gun != null)
        {
            if (player.Gun.currentStyle == GunStyles.automatic)
            {
                if (Input.GetButton("Fire1"))
                    player.Shoot();
            }
            else
            {
                if (Input.GetButtonDown("Fire1"))
                    player.Shoot();
            }
        }
    }
}
