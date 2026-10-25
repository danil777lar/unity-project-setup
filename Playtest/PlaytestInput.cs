using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Larje.ProjectSetup.Editor.Playtest
{
    public sealed class PlaytestInput : IDisposable
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Touchscreen _touchscreen;

        public Keyboard Keyboard => _keyboard ??= InputSystem.AddDevice<Keyboard>("PlaytestKeyboard");
        public Mouse Mouse => _mouse ??= InputSystem.AddDevice<Mouse>("PlaytestMouse");
        public Touchscreen Touchscreen => _touchscreen ??= InputSystem.AddDevice<Touchscreen>("PlaytestTouchscreen");

        public void SetKeys(params Key[] keys)
        {
            KeyboardState state = new KeyboardState(keys);
            InputSystem.QueueStateEvent(Keyboard, state);
            InputSystem.Update();
        }

        public void ReleaseKeys()
        {
            SetKeys();
        }

        public void SetMouse(Vector2 position, bool leftPressed, Vector2 delta = default)
        {
            MouseState state = new MouseState { position = position, delta = delta };
            if (leftPressed)
            {
                state = state.WithButton(MouseButton.Left);
            }

            InputSystem.QueueStateEvent(Mouse, state);
            InputSystem.Update();
        }

        public void SetTouch(int touchId, Vector2 position, UnityEngine.InputSystem.TouchPhase phase)
        {
            TouchState state = new TouchState { touchId = touchId, position = position, phase = phase };
            InputSystem.QueueStateEvent(Touchscreen, state);
            InputSystem.Update();
        }

        public void Dispose()
        {
            RemoveDevice(_keyboard);
            RemoveDevice(_mouse);
            RemoveDevice(_touchscreen);
            _keyboard = null;
            _mouse = null;
            _touchscreen = null;
        }

        private void RemoveDevice(InputDevice device)
        {
            if (device != null && device.added)
            {
                InputSystem.RemoveDevice(device);
            }
        }
    }
}
