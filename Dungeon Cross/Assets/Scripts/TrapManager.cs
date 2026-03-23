using System.Collections.Generic;
using UnityEngine;

public class TrapManager : MonoBehaviour
{
    public static TrapManager Instance { get; private set; }

    private readonly List<TrapBase> activeTraps = new List<TrapBase>();
    private readonly List<TrapBase> stepBuffer = new List<TrapBase>();

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
        stepBuffer.Remove(trap);
    }

    public void StepTraps()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            return;
        }

        Debug.Log($"[STEP START] Trap count: {activeTraps.Count} | Player pos: {player.CurrentGridPosition}");

        stepBuffer.Clear();

        for (int i = 0; i < activeTraps.Count; i++)
        {
            if (activeTraps[i] != null)
            {
                stepBuffer.Add(activeTraps[i]);
            }
        }

        for (int i = 0; i < stepBuffer.Count; i++)
        {
            if (stepBuffer[i] != null)
            {
                stepBuffer[i].MoveStep();
            }
        }

        Vector2Int playerGridPosition = player.CurrentGridPosition;

        for (int i = 0; i < activeTraps.Count; i++)
        {
            TrapBase trap = activeTraps[i];
            if (trap == null)
            {
                continue;
            }

            Debug.Log($"[TRAP] {trap.name} gridPos: {trap.GridPosition}");
            Debug.Log($"[COLLISION CHECK] trap {trap.GridPosition} == player {playerGridPosition} -> {(trap.GridPosition == playerGridPosition).ToString().ToLower()}");

            if (trap.GridPosition == playerGridPosition)
            {
                trap.Activate(player);
                break;
            }
        }

        Debug.Log($"[STEP END] HP after step: {(GameManager.Instance != null ? GameManager.Instance.CurrentHp : -1)}");
    }
}
