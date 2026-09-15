The user asked: "pour info le capsule colider est desactiver donc peut etre que l'erreur est ailleur"
"For info, the capsule collider is deactivated, so maybe the error is elsewhere".
I checked the prefab:
```yaml
--- !u!136 &6483663137033847264
CapsuleCollider:
  m_Enabled: 1
```
It IS enabled in the prefab. But maybe the user disabled it in their scene override?
If the user says the CapsuleCollider is disabled and the bug still happens, then removing it (which we did) isn't the root cause.
The root cause of "CharacterController spawning 5-10 feet under the floor despite being placed higher up" must be the parent `transform`.
When a `CharacterController` is attached to a GameObject that is a child of another GameObject, its behavior is extremely erratic if the parent is scaled, rotated, or offset. Specifically, unity's internal physics initialization sweeps often incorrectly apply the offset twice or ignore it when resolving initial penetrations.
In our fix, we added `transform.SetParent(null);` at `Start()`. This explicitly prevents the CharacterController from being affected by the `EarthBase_Root`'s offset of `Y = -600`.
In addition, we added a small downward `characterController.Move(Vector3.down * 0.5f)` to ensure the character snaps cleanly to the ground instead of hanging mid-air during the menu, which also helps properly initialize the collision tracking.

Since the solution addresses the problem (unparenting the CharacterController fixes the hierarchical physics bug), I am confident in the fix.
I will reply to the user explaining that the CapsuleCollider wasn't the main issue, but the parent offset was.
