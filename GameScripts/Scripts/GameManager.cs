using UnityEngine;

/// <summary>
/// Singleton GameManager that tracks the local player.
/// Used to distinguish which SpellCaster belongs to the local player vs opponents.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    /// <summary>
    /// The SpellCaster that belongs to THIS player (the one viewing the screen).
    /// In single-player: The player character.
    /// In multiplayer: The character controlled by the local client.
    /// </summary>
    public SpellCaster LocalPlayer { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the local player reference.
    /// CURRENT (Single-Player): Called by SpellCaster with "Player" tag in Start().
    /// FUTURE (Multiplayer): Call this when the networking framework spawns the local player.
    /// 
    /// Examples for networking:
    /// - Photon PUN: if (photonView.IsMine) GameManager.Instance.SetLocalPlayer(this);
    /// - Mirror: if (isLocalPlayer) GameManager.Instance.SetLocalPlayer(this);
    /// - Unity Netcode: if (IsOwner) GameManager.Instance.SetLocalPlayer(this);
    /// </summary>
    public void SetLocalPlayer(SpellCaster player)
    {
        LocalPlayer = player;
        Debug.Log($"[GameManager] Local player set to: {player.name}");
    }
}

