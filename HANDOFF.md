# Handoff: John Lemon AR — on-screen movement buttons

## Project
- Unity **6.0 (6000.0.85f2)**, target **Android**, AR Foundation with image tracking (marker).
- Project name: `LabRob8`, scene: `AR_Marker`.
- Character: **JohnLemon** from Unity's John Lemon's Haunted Jaunt tutorial. It is spawned on the tracked image marker. Marker spawning already works.
- Scene hierarchy: `XR Origin (AR Rig)`, `AR Session`, `Directional Light`, `JohnLemon` (a prefab instance placed directly in the scene; it may be an unnecessary copy if the marker spawns the prefab), `Canvas`, `EventSystem`.

## Goal
Two on-screen buttons, **Left** and **Right**. Holding one makes JohnLemon walk in that direction, using his existing Idle/Walk animations and root motion.

## What's done
1. **`HoldButton.cs`**: on the two UI buttons under `Canvas` (labelled `<` and `>`).
   - Uses `IPointerDownHandler`/`IPointerUpHandler`/`IPointerExitHandler`, so the character walks while a button is held.
   - Writes to the static floats `HoldButton.Horizontal` / `HoldButton.Vertical`. They are static because the character is spawned at runtime, so scene buttons can't reference it.
   - Settings: Left = Axis Horizontal, Value -1. Right = Axis Horizontal, Value +1.
2. **`ARPlayerMovement.cs`**: replaces the tutorial's `PlayerMovement` on the JohnLemon prefab, which has been removed.
   - Reads the static input, plus `Input.GetAxis` in the Editor only (under `#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER`).
   - Camera-relative movement (`cameraRelative = true`), projected onto the marker plane. "Up" is `transform.parent.up`, falling back to world up.
   - Sets the animator bool `IsWalking`. Rotation uses `RotateTowards` + `LookRotation(…, up)`. The starting rotation is kept from spawn.
   - `OnAnimatorMove`: `MovePosition(position + m_Movement * animator.deltaPosition.magnitude)`, as in the tutorial, so speed comes from root motion.
   - `makeKinematic = true`: the Rigidbody is made kinematic with gravity off, because there's no floor in AR.
3. **Prefab Animator** (root `JohnLemon`, scale 0.2): Update Mode **Fixed**. In Unity 6, "Animate Physics" is a separate checkbox; it's currently unchecked and still worth testing both ways. Culling: Always Animate. Apply Root Motion shows "Handled by Script".
4. **Animator Controller**: created by the user, in `Assets/_ARMarker/Animations/Animators/`. It should have:
   - Idle as the default state, plus a Walk state.
   - Bool parameter `IsWalking`.
   - Idle→Walk when `IsWalking == true`, Walk→Idle when `IsWalking == false`. Has Exit Time off on both.
   - **Not yet verified.**

## Current status / open bug
- The buttons work, and **the Walk animation plays** when a button is held.
- **JohnLemon doesn't move position.** It hasn't been reported yet whether he rotates to face the direction.

## Findings so far
- The clips are **standalone `.anim` files**: `Assets/_ARMarker/Animations/Animation/Walk.anim` and `Idle.anim`. There is also `FuseboxLeverFlip.anim`.
- Walk.anim settings: Loop Time ✔. Root Transform Rotation Bake Into Pose ✔. Root Transform Position (Y) Bake Into Pose ✔. **Root Transform Position (XZ) Bake Into Pose ✘**, Based Upon = **Root Node Position**. Length 0.633 s, 60 FPS.
- Curve stats suggest a **Generic** clip (Pos 12, Quat 33, Scale 9, Muscles 6, Generic 1), not Humanoid.
  - Earlier advice to switch the rig to Humanoid was **wrong**; keep it Generic.
- **Leading hypothesis:** for Generic root motion, the model's avatar needs a **Root node** set. If it's None, `deltaPosition` stays at about 0, so the animation plays but he doesn't move.
  - Check `Assets/_ARMarker/SourceFiles/Models/JohnLemon/JohnLemon` (fbx) → Rig tab: Animation Type Generic, Avatar Definition Create From This Model, Root node = top bone.
  - In the `.fbx.meta`, look at `animationType` and `rootMotionBoneName`.
  - The prefab's Animator Avatar should be **JohnLemonAvatar** from that model. It was None in an earlier screenshot, and the user was told to assign it.

## Next steps
1. Read the prefab YAML for JohnLemon:
   - Is `m_Controller` assigned, and is `m_Avatar` assigned?
   - Rigidbody constraints should be: no position freeze; rotation X and Z frozen, Y free.
2. Check the JohnLemon model's `.meta` for rig type and root node, and check the controller's transitions and parameter name.
3. Diagnostic: add `Debug.Log(m_Animator.deltaPosition.magnitude)` in `OnAnimatorMove`.
   - About 0 means the root motion or avatar setup is wrong.
   - A value above 0 means the problem is the Rigidbody, or the AR parent transform overriding the position.
4. Fallback if root motion stays a problem in AR: add a `walkSpeed` option that moves the character by `m_Movement * walkSpeed * lossyScale * Time.fixedDeltaTime` without root motion. The feet may slide a little.
5. Confirm which prefab the AR tracked image manager or spawn script actually instantiates. Remove the extra JohnLemon from the scene if it isn't needed.
6. Possible extras: forward/back buttons (`HoldButton` with Axis Vertical), and a limit that keeps him within a radius of the marker.

## User notes
- Working in the Unity Editor GUI, and fairly new to Unity: give click-by-click directions, saying where to click (Project window, Inspector tab, and so on).
- The Windows UI is partly in Ukrainian; the Unity UI is in English.

## Update (session 2)
- **Root cause 1 (fixed):** the prefab nested the bones under `JohnLemonMesh/Parent`, but the clips/avatar expect `Root/Hips` directly under the Animator. `Root` and the mesh were moved directly under `JohnLemon`, the wrappers removed, and the scale changed from 0.2 to 0.1 to keep the size. Has Exit Time is now off on all transitions.
- **Root cause 2 (fixed):** `ARTrackedImageManager` makes the spawned prefab's *top object* the tracked image and rewrites its pose every frame, so movement and rotation on the top object were undone. The prefab now has an empty root `JohnLemonAnchor`, with `JohnLemon` (Animator, Rigidbody, ARPlayerMovement) as its child. `m_TrackedImagePrefab` in `AR_Marker.unity` now points at the anchor.
- The scene's own JohnLemon copy is inactive (`m_IsActive: 0`). It's harmless and can be deleted.
- **Root cause 3 (fixed):** `Walk.anim` is an in-place walk (Hips move about 0.003 units per loop), so root motion is always 0. `ARPlayerMovement` now moves at `walkSpeed` (1 unit/s in character units, times lossyScale) unless `useRootMotion` is on. Turning was confirmed working in XR Simulation.
- **Spawn drift (session 2, fixed in code):** the script pinned a world position and rotation through `Rigidbody.MovePosition/MoveRotation`, using a stale rigidbody position and the rotation from spawn time. When ARCore refined the marker's pose after its rough first guess, JohnLemon stayed at the old spot instead of following the marker. He now moves through `transform` relative to the marker. The Animator Update Mode is now Normal. He stays hidden until the image reaches `TrackingState.Tracking` (`waitForGoodTracking`).
- **Still to do by the user:** the reference image `ARMarker` in `Assets/_ARMarker/Markers/ReferenceImageLibrary.asset` has **Specify Size off**, so ARCore has to guess the marker's size and distance. Set it to the printed marker's real width in metres.
