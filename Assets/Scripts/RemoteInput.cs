﻿using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Unity.RenderStreaming
{
    enum KeyboardEventType
    {
        KeyUp = 0,
        KeyDown = 1,
    }
    enum EventType
    {
        Keyboard = 0,
        Mouse = 1,
        MouseWheel = 2,
        Touch = 3,
        ButtonClick = 4
    }

    public static class RemoteInput
    {
        public static Keyboard Keyboard { get; private set; }
        public static Mouse Mouse { get; private set; }
        public static Touchscreen Touch { get; private set; }
        public static Action<int> ActionButtonClick;

        static TDevice GetOrAddDevice<TDevice>() where TDevice : InputDevice
        {
            var device = InputSystem.GetDevice<TDevice>();
            if(device != null)
            {
                return device;
            }
            return InputSystem.AddDevice<TDevice>();
        }

        public static void Initialize()
        {
            Keyboard = GetOrAddDevice<Keyboard>();
            Mouse = GetOrAddDevice<Mouse>();
            Touch = GetOrAddDevice<Touchscreen>();
        }

        public static void ProcessInput(byte[] bytes)
        {
            switch ((EventType)bytes[0])
            {
                case EventType.Keyboard:
                    var type = (KeyboardEventType)bytes[1];
                    var repeat = bytes[2] == 1;
                    var key = bytes[3];
                    var character = (char)bytes[4];
                    ProcessKeyEvent(type, repeat, key, character);
                    break;
                case EventType.Mouse:
                    var deltaX = BitConverter.ToInt16(bytes, 1);
                    var deltaY = BitConverter.ToInt16(bytes, 3);
                    var button = bytes[5];
                    ProcessMouseMoveEvent(deltaX, deltaY, button);
                    break;
                case EventType.MouseWheel:
                    var scrollX = BitConverter.ToSingle(bytes, 1);
                    var scrollY = BitConverter.ToSingle(bytes, 5);
                    ProcessMouseWheelEvent(scrollX, scrollY);
                    break;
                case EventType.Touch:
                    var phase = (PointerPhase)bytes[1];
                    var length = bytes[2];
                    var index = 3;
                    for (int i = 0; i < length; i++)
                    {
                        var identifier = BitConverter.ToInt32(bytes, index);
                        var pageX = BitConverter.ToInt16(bytes, index+4);
                        var pageY = BitConverter.ToInt16(bytes, index+6);
                        var force = BitConverter.ToSingle(bytes, index+8);
                        ProcessTouchMoveEvent(identifier, phase, pageX, pageY, force);
                        index += 12;
                    }
                    break;
                case EventType.ButtonClick:
                    var elementId = BitConverter.ToInt16(bytes, 1);
                    ProcessButtonClickEvent(elementId);
                    break;
            }
        }

        public static void Reset()
        {
            InputSystem.QueueStateEvent(Mouse, new MouseState());
            InputSystem.QueueStateEvent(Keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(Touch, new TouchState());
            InputSystem.Update();
        }

        static void ProcessKeyEvent(KeyboardEventType state, bool repeat, byte keyCode, char character)
        {
            switch(state)
            {
                case KeyboardEventType.KeyDown:
                    if (!repeat)
                    {
                        InputSystem.QueueStateEvent(Keyboard, new KeyboardState((Key)keyCode));
                    }
                    if(character != 0)
                    {
                        InputSystem.QueueTextEvent(Keyboard, character);
                    }
                    break;
                case KeyboardEventType.KeyUp:
                    InputSystem.QueueStateEvent(Keyboard, new KeyboardState());
                    break;
            }
            InputSystem.Update();
        }

        static void ProcessMouseMoveEvent(short deltaX, short deltaY, byte button)
        {
            InputSystem.QueueStateEvent(Mouse, new MouseState { delta = new Vector2Int(deltaX, deltaY), buttons = button });
            InputSystem.Update();
        }

        static void ProcessMouseWheelEvent(float scrollX, float scrollY)
        {
            InputSystem.QueueStateEvent(Mouse, new MouseState { scroll = new Vector2(scrollX, scrollY) });
            InputSystem.Update();
        }

        static void ProcessTouchMoveEvent(int identifier, PointerPhase phase, short pageX, short pageY, float force)
        {
            InputSystem.QueueStateEvent(Touch,
                new TouchState
                {
                    touchId = identifier,
                    phase = phase,
                    position = new Vector2Int(pageX, pageY),
                    pressure = force
                });
            InputSystem.Update();
        }

        static void ProcessButtonClickEvent(int elementId)
        {
            if (ActionButtonClick != null)
            {
                ActionButtonClick(elementId);
            }
        }
    }
}
