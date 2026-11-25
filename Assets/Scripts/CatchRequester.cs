// CatchRequester.cs
// Mirrors the server-sided catch confirmation added to the web version:
// the client's own monster-contact detection *requests* a catch, but the
// server (here: the Netcode for GameObjects server, not just "the host's
// browser tab") is the one that actually applies death.
//
// Requires the "Netcode for GameObjects" package (com.unity.netcode.gameobjects).
// Attach to the player prefab alongside a NetworkObject.

using Unity.Netcode;
using UnityEngine;

public class CatchRequester : NetworkBehaviour
{
    public bool isDead { get; private set; }
    public bool godMode; // set this from your mod menu's networked state, not a local bool a client can flip

    bool pending;

    /// <summary>Called locally by MonsterAI when it detects contact with this player.</summary>
    public void RequestCaught(MonsterAI mon)
    {
        if (isDead || godMode || pending) return;
        pending = true;
        RequestCaughtServerRpc();
    }

    [ServerRpc]
    void RequestCaughtServerRpc(ServerRpcParams rpcParams = default)
    {
        // This is the actual authority check - same spirit as the host's
        // `else if(m.t==='caught'){if(!bansDB[c])sendTo(c,{t:'caughtok'})}`
        // Add real validation here (is this player in a running Regular-mode
        // round? not already dead server-side? not banned?) before approving.
        if (isDead || godMode) return;

        ApplyCaughtClientRpc();
    }

    [ClientRpc]
    void ApplyCaughtClientRpc()
    {
        pending = false;
        if (!IsOwner) return; // only the caught player's own client plays the death sequence
        ApplyDeathLocally();
    }

    void ApplyDeathLocally()
    {
        isDead = true;
        // Hook up your jump scare, gore/ragdoll, respawn timer etc. here -
        // this is the equivalent of the web version's die(mon) function.
    }
}
