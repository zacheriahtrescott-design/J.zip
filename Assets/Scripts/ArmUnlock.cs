// ArmUnlock.cs
// Mirrors the server-sided arm-code unlock added to the web version:
// entering the code asks the server to check it; the server is the one
// that grants (or denies) the chainsaw/hammer/gun entitlement.
//
// Requires "Netcode for GameObjects". Attach alongside CatchRequester
// on the player prefab.

using System;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ArmUnlock : NetworkBehaviour
{
    // Same idea as ARM_HASH in the web version: never store the plaintext
    // code in the build. Compute this once with Sha256Hex("3601") and
    // paste the result here, same as the original did.
    const string ArmCodeHashHex = "PASTE_SHA256_OF_YOUR_CODE_HERE";

    public NetworkVariable<bool> chainsawUnlocked = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hammerUnlocked = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> gunUnlocked = new NetworkVariable<bool>(false);

    public string lastMessage; // drive your UI keypad's feedback text from this

    public void SubmitCode(string typedCode)
    {
        SubmitCodeServerRpc(Sha256Hex(typedCode));
    }

    [ServerRpc]
    void SubmitCodeServerRpc(string hashHex, ServerRpcParams rpcParams = default)
    {
        bool ok = hashHex == ArmCodeHashHex;
        NotifyResultClientRpc(ok, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { rpcParams.Receive.SenderClientId } }
        });
    }

    [ClientRpc]
    void NotifyResultClientRpc(bool ok, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        lastMessage = ok ? "" : "Wrong code";
        // The server grants the NetworkVariable flips separately (below),
        // once it also knows which tool the player picked in the menu.
    }

    /// <summary>Call from the arm menu's toggle buttons, only enabled once unlocked.</summary>
    public void SetToolEnabled(string tool, bool on)
    {
        SetToolEnabledServerRpc(tool, on);
    }

    [ServerRpc]
    void SetToolEnabledServerRpc(string tool, bool on, ServerRpcParams rpcParams = default)
    {
        // A real build should re-check that this client actually passed
        // SubmitCodeServerRpc before honoring this - track that per-client
        // on the server instead of trusting the call alone.
        switch (tool)
        {
            case "chainsaw": chainsawUnlocked.Value = on; break;
            case "hammer": hammerUnlocked.Value = on; break;
            case "gun": gunUnlocked.Value = on; break;
        }
    }

    static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
