using System;
using System.Collections.Generic;
using Plugins.RProjects.RUtils.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EscapeButtonManager : SingleBehaviour<EscapeButtonManager>
{
    private readonly List<EscapeHandler> handlers = new();

    public event Action EscapePressedWithoutHandler;

    public bool HasOpenWindow {
        get {
            RemoveDestroyedHandlers();
            return handlers.Count > 0;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (TryCloseTopHandler())
        {
            return;
        }

        EscapePressedWithoutHandler?.Invoke();
    }

    public void RegisterWindow(object owner, Action closeAction)
    {
        if (owner == null || closeAction == null)
        {
            return;
        }

        UnregisterWindow(owner);
        handlers.Add(new EscapeHandler(owner, closeAction));
    }

    public void UnregisterWindow(object owner)
    {
        if (owner == null)
        {
            return;
        }

        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(handlers[i].Owner, owner))
            {
                handlers.RemoveAt(i);
            }
        }
    }

    public void ClearWindows()
    {
        handlers.Clear();
    }

    public bool IsWindowRegistered(object owner)
    {
        if (owner == null)
        {
            return false;
        }

        RemoveDestroyedHandlers();

        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(handlers[i].Owner, owner))
            {
                return true;
            }
        }

        return false;
    }

    public void Register(object owner, Action closeAction) => RegisterWindow(owner, closeAction);

    public void Unregister(object owner) => UnregisterWindow(owner);

    private bool TryCloseTopHandler()
    {
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            EscapeHandler handler = handlers[i];
            handlers.RemoveAt(i);

            if (handler.Owner is UnityEngine.Object unityOwner && unityOwner == null)
            {
                continue;
            }

            handler.CloseAction.Invoke();
            return true;
        }

        return false;
    }

    private void RemoveDestroyedHandlers()
    {
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            if (handlers[i].Owner is UnityEngine.Object unityOwner && unityOwner == null)
            {
                handlers.RemoveAt(i);
            }
        }
    }

    private readonly struct EscapeHandler
    {
        public readonly object Owner;
        public readonly Action CloseAction;

        public EscapeHandler(object owner, Action closeAction)
        {
            Owner = owner;
            CloseAction = closeAction;
        }
    }
}
