﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TESUnity.Inputs
{
    public enum MWButton
    {
        None = 0, Jump, Light,
        Run, Slow, Attack,
        Recenter, Use, Menu,
        Teleport
    }

    public enum MWAxis
    {
        None = 0, Horizontal, Vertical, MouseX, MouseY
    }

    public static class InputManager2
    {
        private static InputActionAsset InputActions = null;

        static InputManager2()
        {
            InputActions = Resources.Load<InputActionAsset>("Input/TESInputActions");
        }

        public static InputActionMap GetActionMap(string actionMap)
        {
            var map = InputActions.FindActionMap(actionMap);

            if (map == null)
            {
                Debug.LogError($"Can't find the action map {actionMap}");
            }

            return map;
        }

        public static void Enable(string actionMap)
        {
            var map = GetActionMap(actionMap);
            map?.Enable();
        }

        public static void Disable(string actionMap)
        {
            var map = GetActionMap(actionMap);
            map?.Disable();
        }
    }

    public static class InputManager
    {
        private static IInputProvider[] InputProviders = null;

        private static void EnsureStarted()
        {
            if (InputProviders != null)
                return;

            var providers = new IInputProvider[]
            {
                new TouchInput(),
                new UnityXRInput(),
                new DesktopInput()
            };

            var list = new List<IInputProvider>();
            var touchEnabled = false;

            foreach (var provider in providers)
            {
                if (provider.TryInitialize())
                {
                    if (provider is TouchInput)
                        touchEnabled = true;

                    if (provider is DesktopInput && touchEnabled)
                        continue;

                    list.Add(provider);
                }
            }

            InputProviders = list.ToArray();
        }

        public static void AddInput(IInputProvider provider)
        {
            EnsureStarted();

            if (provider.TryInitialize())
            {
                var size = InputProviders.Length;
                Array.Resize(ref InputProviders, size + 1);
                InputProviders[size] = provider;
            }
        }

        public static float GetAxis(MWAxis axis)
        {
            EnsureStarted();

            var result = 0.0f;

            foreach (var provider in InputProviders)
            {
                result = provider.GetAxis(axis);

                if ((int)result != 0)
                {
                    Filter(ref result);
                    return result;
                }
            }

            return result;
        }

        private static void Filter(ref float value)
        {
            if (Mathf.Abs(value) < 0.15f)
                value = 0.0f;
        }

        public static bool GetButton(MWButton button)
        {
            EnsureStarted();

            foreach (var input in InputProviders)
            {
                if (input.Get(button))
                    return true;
            }

            return false;
        }

        public static bool GetButtonUp(MWButton button)
        {
            EnsureStarted();

            foreach (var input in InputProviders)
            {
                if (input.GetUp(button))
                    return true;
            }

            return false;
        }

        public static bool GetButtonDown(MWButton button)
        {
            EnsureStarted();

            foreach (var input in InputProviders)
            {
                if (input.GetDown(button))
                    return true;
            }

            return false;
        }
    }
}