# Hungarian Notation & Scope Coding Style Rules

이 프로젝트의 모든 C# 스크립트는 **`[소속]_[타입약어][객체이름(PascalCase)]`** 형식의 헝가리안 표기법(Hungarian Notation)을 준수합니다.

## 1. 소속(Scope) 접두어 규칙
- **`m_`** : 클래스 멤버 변수 (`private`, `protected`, `[SerializeField]`)
  - 예: `m_cPlayer`, `m_trViewPivot`, `m_fAttackTimer`, `m_eCurrentState`, `m_listWeaponPool`
- **`s_`** : 정적(Static) 멤버 변수
  - 예: `s_cInstance`
- **`_`** : 메서드/함수 매개변수 (Parameter)
  - 예: `_trMainCamera`, `_camSecond`, `_eNewState`, `_nWeaponIndex`
- **(접두어 없음)** :
  - 함수 내부 지역 변수(Local Variable)는 소속 접두어 없이 `[타입약어][객체이름]` 형식을 사용합니다. (예: `vMoveDir`, `fDistance`, `cTargetGun`)
  - 외부에 노출되는 `public` 프로퍼티(Property)와 메서드(Method)는 `PascalCase`를 사용합니다. (예: `CurrentPlayer`, `ViewPivot`, `SwitchWeapon()`)

## 2. 타입 약어(Type Prefix) 규칙
| 분류 | C# / Unity 타입 | 약어 | 멤버 변수 예시 (`m_`) | 매개변수 예시 (`_`) |
| :--- | :--- | :--- | :--- | :--- |
| **커스텀 클래스** | 프로젝트 내 클래스/컴포넌트 (`Player`, `GunScript` 등) | **`c`** | `m_cPlayer`, `m_cCurrentGun`, `m_cMovement` | `_cPlayer`, `_cMouseLook` |
| **Unity 컴포넌트** | `Transform` | **`tr`** | `m_trPlayer`, `m_trMainCamera`, `m_trViewPivot` | `_trCamera`, `_trTarget` |
| | `GameObject` (씬 오브젝트) | **`obj`** | `m_objBulletSpawn`, `m_objCurrentGun` | `_objBulletSpawn` |
| | `GameObject` (프리팹 에셋) | **`prefab`** | `m_prefabPlayer`, `m_prefabBullet`, `m_prefabBlood` | `_prefabWeapon` |
| | `Camera` | **`cam`** | `m_camMain`, `m_camSecond` | `_camSecond` |
| | `Rigidbody` | **`rb`** | `m_rbBody`, `m_rbHit` | `_rbTarget` |
| | `Animator` | **`anim`** | `m_animHands` | `_animHands` |
| | `AudioSource` | **`as`** | `m_asShoot`, `m_asReload`, `m_asHitMarker` | `_asHitMarker` |
| | `TextMesh` | **`tm`** | `m_tmHudBullets` | `_tmHudBullets` |
| | `Texture` / `Texture2D` | **`tex`** | `m_texCrosshairH`, `m_texCrosshairV` | `_texIcon` |
| **기본형/구조체** | `bool` | **`b`** | `m_bIsPlayer`, `m_bIsReloading`, `m_bIsAiming` | `_bEnable` |
| | `int` | **`n`** | `m_nCurrentWeaponIndex`, `m_nWalkSpeed`, `m_nMaxSpeed` | `_nIndex` |
| | `float` | **`f`** | `m_fAttackTimer`, `m_fDetectionRadius` | `_fDeltaTime` |
| | `string` | **`str`** | `m_strPrimaryTargetTag`, `m_strReloadAnimName` | `_strWeaponName` |
| | `Vector2` / `Vector3` | **`v`** | `m_vRestPosition`, `m_vAimPosition`, `m_vInputMove` | `_vWorldDir` |
| | `Quaternion` | **`q`** | `m_qTargetRotation` | `_qSpawnRot` |
| | `enum` | **`e`** | `m_eCurrentState`, `m_eGunStyle` | `_eNewState` |
| **컬렉션** | `List<T>` | **`list`** | `m_listWeaponPool`, `m_listWeaponNames` | `_listWeapons` |
| | `T[]` (배열) | **`arr`** | `m_arrPrefabMuzzleFlash`, `m_arrIcons` | `_arrIcons` |

## 3. Unity 직렬화(Serialization) 및 YAML 동기화 원칙
- 기존 인스펙터/프리팹 직렬화 필드의 이름을 변경할 때는 `using UnityEngine.Serialization;`의 `[FormerlySerializedAs("기존필드명")]`을 함께 부착하고, 필요 시 `.unity` 및 `.prefab` YAML 파일의 `propertyPath`도 동기화하여 에디터 참조 유실을 방지합니다.
