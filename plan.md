The fix consists of three changes:
1. `transform.SetParent(null)`: This detaches the player from `EarthBase_Root` (which is positioned at Y=-600). CharacterControllers in Unity can have unreliable collision detection when parented to objects with extreme offsets or non-identity scales (even though scale is 1 here, the offset can still cause floating-point glitches in internal collision sweeps during initialization).
2. `Destroy(cc)`: We destroy the redundant `CapsuleCollider`. The player GameObject has both a `CharacterController` and a `CapsuleCollider`. This can create undefined behavior where the physics engine gets confused during the initial depenetration sweep, pushing the character downwards through the floor.
3. `characterController.Move(Vector3.down * 0.5f)`: Since the player is suspended slightly above the floor and skipping `Update()` during the main menu, we snap them to the ground manually so they don't accumulate a falling state while suspended.

I will request a pre-commit review.
