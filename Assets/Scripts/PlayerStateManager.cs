using System;
using System.Collections.Generic;
using Plugins.RProjects.RUtils.Scripts.Core;
using UnityEngine;

public sealed class PlayerStateManager : SingleBehaviour<PlayerStateManager>
{
    private readonly List<object> playerInputBlockers = new();
    private bool lastCanProcessPlayerInput = true;

    public event Action<bool> PlayerInputAllowedChanged;

    public bool CanProcessPlayerInput {
        get {
            RemoveDestroyedBlockers();
            return playerInputBlockers.Count == 0;
        }
    }

    private void Update()
    {
        if (RemoveDestroyedBlockers())
        {
            NotifyIfInputStateChanged();
        }
    }

    public void BlockPlayerInput(object owner)
    {
        if (owner == null || playerInputBlockers.Contains(owner))
        {
            return;
        }

        playerInputBlockers.Add(owner);
        NotifyIfInputStateChanged();
    }

    public void UnblockPlayerInput(object owner)
    {
        if (owner == null)
        {
            return;
        }

        for (int i = playerInputBlockers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(playerInputBlockers[i], owner))
            {
                playerInputBlockers.RemoveAt(i);
            }
        }

        NotifyIfInputStateChanged();
    }

    private void NotifyIfInputStateChanged()
    {
        bool canProcessPlayerInput = playerInputBlockers.Count == 0;
        if (lastCanProcessPlayerInput == canProcessPlayerInput)
        {
            return;
        }

        lastCanProcessPlayerInput = canProcessPlayerInput;
        PlayerInputAllowedChanged?.Invoke(canProcessPlayerInput);
    }

    private bool RemoveDestroyedBlockers()
    {
        bool changed = false;

        for (int i = playerInputBlockers.Count - 1; i >= 0; i--)
        {
            if (playerInputBlockers[i] is UnityEngine.Object unityObject && unityObject == null)
            {
                playerInputBlockers.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }
}
