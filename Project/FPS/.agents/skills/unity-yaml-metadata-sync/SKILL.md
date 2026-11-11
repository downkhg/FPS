---
name: unity-yaml-metadata-sync
description: >-
  Directly inspects and modifies Unity YAML text files (.unity scene, .prefab, .meta, .asset)
  to ensure shared prefabricated components (e.g. PC and NPC marionettes), maintain GUID/FileID
  integrity, eliminate broken overrides, and safely sync project state without requiring Unity Editor clicks.
---

# Unity YAML & Metadata Sync Workflow

This skill guides the inspection and safe modification of Unity YAML text files (`.unity`, `.prefab`, `.asset`, `.meta`).

## Core Principles

1. **Unity Files Are Structured YAML**:
   - Unity `.unity` (scenes) and `.prefab` files are human-readable YAML with serialized `fileID` and `guid` references.
   - When synchronizing components (such as sharing the same character prefab between Player and NPC), editing the YAML directly ensures perfect parity, strips broken overrides, and keeps serialized references valid.

2. **Prefab Instance (`PrefabInstance`) Structure in Scenes**:
   - `m_SourcePrefab`: Points to the source `.prefab` via `{fileID: 100100000, guid: <prefab_guid>, type: 3}`.
   - `m_Modifications`: Holds instance-specific overrides.
     - Keep overrides minimal (e.g., `m_Name`, `m_LocalPosition`, `m_LocalRotation`, or state flags like `isPlayer`).
     - Avoid overriding shared script variables so that prefab updates propagate cleanly to all instances (both PC and NPC).

3. **Stripped Objects (`stripped`)**:
   - Objects inside a `PrefabInstance` referenced by other scene components (e.g., `GameManager` references) use the `stripped` tag:
     ```yaml
     --- !u!114 &<instance_local_id> stripped
     MonoBehaviour:
       m_CorrespondingSourceObject: {fileID: <prefab_internal_file_id>, guid: <prefab_guid>, type: 3}
       m_PrefabInstance: {fileID: <prefab_instance_id>}
       m_PrefabAsset: {fileID: 0}
     ```
   - When modifying instance associations, verify that `m_CorrespondingSourceObject` matches the exact `fileID` defined in the target `.prefab`.

4. **Component Parity Between PC and NPC**:
   - Both PC and NPC act as Marionettes governed by the shared `Player` component.
   - Both inherit identical base movement, inventory, and weapon pools from the shared prefab.
   - The distinction is injected dynamically or minimally configured via `isPlayer`:
     - Player: receives `PlayerController` + Camera binding.
     - NPC: receives `AIController` + Target tracking.

## Standard Verification Checklist

- [ ] Target `.prefab.meta` verified to ensure valid GUID.
- [ ] No duplicated component overrides on the same GameObject.
- [ ] Strip-linked IDs correctly mapped to `GameManager` serialized references.
- [ ] `Player.isPlayer` correctly differentiated between PC (`1`) and NPC (`0`).
