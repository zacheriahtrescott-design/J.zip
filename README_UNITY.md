# Gorilla Horror Forest — Unity starting point

This is a **starting point**, not a finished port. It's an honest first
increment of what a real Unity rewrite looks like — four scripts that
translate the most portable pieces of logic from `index.html`. It is
**not** a working game yet, and can't be made into a working game by
file conversion alone; the rest is Editor work (scenes, prefabs, meshes,
materials, UI, audio) that has to be built by hand in Unity.

## What's included
- `Assets/Scripts/GorillaLocomotion.cs` — the arm-swing VR movement, with
  the same tuning constants as the web version's `GL` object (max arm
  length, unstick distance, jump multiplier, etc.).
- `Assets/Scripts/MonsterAI.cs` — the Regular-mode monster's wander/chase
  logic, rewritten to use Unity's `NavMeshAgent` instead of the web
  version's hand-rolled grid BFS.
- `Assets/Scripts/CatchRequester.cs` and `Assets/Scripts/ArmUnlock.cs` —
  the two "server-sided" features from the last update, reimplemented
  with Netcode for GameObjects `ServerRpc`/`ClientRpc`/`NetworkVariable`
  instead of the web version's PeerJS host-relay messages. Same idea
  (the server decides, not the client), different networking stack.

## What's not included, and has to be built in the Editor
- Any actual 3D models — the house, corn maze, back rooms, the monster,
  survivor NPCs, player hands/arms. The web version draws all of this
  procedurally in code (`buildMap()`, `mkHammer()`, etc.); Unity projects
  normally use imported meshes instead, so these need to be modeled
  (or bought/found as assets) rather than translated from the JS.
- The gore/dismemberment system (blood, gibs, wound meshes, pooling
  blood) — this was all custom particle/mesh code in the web version
  and needs an equivalent built with Unity's particle system or a VFX
  asset.
- Scenes, prefabs, UI (the mod menu, arm menu, leaderboard, start
  screen) — none of this can be represented as plain text safely, so
  it isn't faked here. Build these in the Unity Editor itself.
- The rest of the networking (player position sync, remote hand/arm
  rendering, the tear/punch/throw messages, lobby and room-code
  system, staff keys, bans) — only the two features you most recently
  asked to be "server-sided" are ported here as a worked example of the
  pattern; the rest follows the same approach but is real additional
  work.
- Audio, haptics, and the VR hand rig itself (grip/trigger curl,
  3-finger hands) — needs XR Interaction Toolkit setup specific to
  your target headset.

## Packages you'll need
- **XR Interaction Toolkit** (for VR input/hands)
- **Netcode for GameObjects** (for the multiplayer pieces) — or Mirror,
  if you prefer; `CatchRequester.cs`/`ArmUnlock.cs` would need minor
  changes for Mirror's API instead of NGO's.
- **AI Navigation** package (for `NavMeshAgent` in `MonsterAI.cs`)

## Honest scope estimate
The original web version took many iterative updates to reach its
current state (visible in its own README's changelog). A Unity
equivalent with matching scope — maps, gore, VR hands, full netcode,
mod/staff system — is realistically weeks of work for someone familiar
with Unity and VR development, not something that compresses into a
single conversion step. I'm glad to keep building this out piece by
piece with you — tell me which system to tackle next (gore/dismember,
the map geometry, the networking for remote players, the UI) and I'll
write the real C# for it.
