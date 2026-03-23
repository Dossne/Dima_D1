using System.Collections.Generic;
using UnityEngine;

public class TrapManager : MonoBehaviour
{
    public static TrapManager Instance { get; private set; }

    private readonly List<TrapBase> activeTraps = new List<TrapBase>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterTrap(TrapBase trap)
    {
        if (trap == null || activeTraps.Contains(trap))
        {
            return;
        }

        activeTraps.Add(trap);
    }

    public void UnregisterTrap(TrapBase trap)
    {
        if (trap == null)
        {
            return;
        }

        activeTraps.Remove(trap);
    }

    public void CheckCollision(TrapBase trap)
    {
        if (trap == null || PlayerController.Instance == null)
        {
            return;
        }

        if (trap.IsPlayerInDanger(PlayerController.Instance))
        {
            trap.Activate(PlayerController.Instance);
        }
    }

    public void CheckPlayerPosition()
    {
        if (PlayerController.Instance == null)
        {
            return;
        }

        for (int i = 0; i < activeTraps.Count; i++)
        {
            TrapBase trap = activeTraps[i];
            if (trap != null && trap.IsPlayerInDanger(PlayerController.Instance))
            {
                trap.Activate(PlayerController.Instance);
                break;
            }
        }
    }
}
