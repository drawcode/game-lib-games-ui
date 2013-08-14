// GameMessenger.cs v1.0 by Magnus Wolffelt, magnus.wolffelt@gmail.com
//
// Inspired by and based on Rod Hyde's GameMessenger:
// http://www.unifycommunity.com/wiki/index.php?title=CSharpGameMessenger
//
// This is a C# messenger (notification center). It uses delegates
// and generics to provide type-checked messaging between event producers and
// event consumers, without the need for producers or consumers to be aware of
// each other. The major improvement from Hyde's implementation is that
// there is more extensive error detection, preventing silent bugs.
//
// Usage example:
// GameMessenger<float>.AddListener("myEvent", MyEventHandler);
// ...
// GameMessenger<float>.Broadcast("myEvent", 1.0f);

using System;
using System.Collections.Generic;

namespace Gameverses {

    public delegate void MessengerCallback();

    public delegate void MessengerCallback<T>(T arg1);

    public delegate void MessengerCallback<T, U>(T arg1, U arg2);

    public delegate void MessengerCallback<T, U, V>(T arg1, U arg2, V arg3);

    public enum GameMessengerMode {
        DONT_REQUIRE_LISTENER,
        REQUIRE_LISTENER,
    }

    static internal class GameMessengerInternal {
        public static Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();
        public static readonly GameMessengerMode DEFAULT_MODE = GameMessengerMode.DONT_REQUIRE_LISTENER;

        public static void OnListenerAdding(string eventType, Delegate listenerBeingAdded) {
            if (!eventTable.ContainsKey(eventType)) {
                eventTable.Add(eventType, null);
            }

            Delegate d = eventTable[eventType];
            if (d != null && d.GetType() != listenerBeingAdded.GetType()) {
                throw new ListenerException(string.Format("Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}", eventType, d.GetType().Name, listenerBeingAdded.GetType().Name));
            }
        }

        public static void OnListenerRemoving(string eventType, Delegate listenerBeingRemoved) {
            if (eventTable.ContainsKey(eventType)) {
                Delegate d = eventTable[eventType];

                if (d == null) {
                    throw new ListenerException(string.Format("Attempting to remove listener with for event type {0} but current listener is null.", eventType));
                }
                else if (d.GetType() != listenerBeingRemoved.GetType()) {
                    throw new ListenerException(string.Format("Attempting to remove listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being removed has type {2}", eventType, d.GetType().Name, listenerBeingRemoved.GetType().Name));
                }
            }
            else {
                throw new ListenerException(string.Format("Attempting to remove listener for type {0} but GameMessenger doesn't know about this event type.", eventType));
            }
        }

        public static void OnListenerRemoved(string eventType) {
            if (eventTable[eventType] == null) {
                eventTable.Remove(eventType);
            }
        }

        public static void OnBroadcasting(string eventType, GameMessengerMode mode) {
            if (mode == GameMessengerMode.REQUIRE_LISTENER && !eventTable.ContainsKey(eventType)) {
                throw new GameMessengerInternal.BroadcastException(string.Format("Broadcasting message {0} but no listener found.", eventType));
            }
        }

        public static BroadcastException CreateBroadcastSignatureException(string eventType) {
            return new BroadcastException(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", eventType));
        }

        public class BroadcastException : Exception {

            public BroadcastException(string msg)
                : base(msg) {
            }
        }

        public class ListenerException : Exception {

            public ListenerException(string msg)
                : base(msg) {
            }
        }
    }

    // No parameters
    public static class GameMessenger {
        private static Dictionary<string, Delegate> eventTable = GameMessengerInternal.eventTable;

        public static void AddListener(string eventType, MessengerCallback handler) {
            GameMessengerInternal.OnListenerAdding(eventType, handler);
            eventTable[eventType] = (MessengerCallback)eventTable[eventType] + handler;
        }

        public static void RemoveListener(string eventType, MessengerCallback handler) {
            GameMessengerInternal.OnListenerRemoving(eventType, handler);
            eventTable[eventType] = (MessengerCallback)eventTable[eventType] - handler;
            GameMessengerInternal.OnListenerRemoved(eventType);
        }

        public static void Broadcast(string eventType) {
            Broadcast(eventType, GameMessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventType, GameMessengerMode mode) {
            GameMessengerInternal.OnBroadcasting(eventType, mode);
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                MessengerCallback callback = d as MessengerCallback;
                if (callback != null) {
                    callback();
                }
                else {
                    throw GameMessengerInternal.CreateBroadcastSignatureException(eventType);
                }
            }
        }
    }

    // One parameter
    public static class GameMessenger<T> {
        private static Dictionary<string, Delegate> eventTable = GameMessengerInternal.eventTable;

        public static void AddListener(string eventType, MessengerCallback<T> handler) {
            GameMessengerInternal.OnListenerAdding(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T>)eventTable[eventType] + handler;
        }

        public static void RemoveListener(string eventType, MessengerCallback<T> handler) {
            GameMessengerInternal.OnListenerRemoving(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T>)eventTable[eventType] - handler;
            GameMessengerInternal.OnListenerRemoved(eventType);
        }

        public static void Broadcast(string eventType, T arg1) {
            Broadcast(eventType, arg1, GameMessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventType, T arg1, GameMessengerMode mode) {
            GameMessengerInternal.OnBroadcasting(eventType, mode);
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                MessengerCallback<T> callback = d as MessengerCallback<T>;
                if (callback != null) {
                    callback(arg1);
                }
                else {
                    throw GameMessengerInternal.CreateBroadcastSignatureException(eventType);
                }
            }
        }
    }

    // Two parameters
    public static class GameMessenger<T, U> {
        private static Dictionary<string, Delegate> eventTable = GameMessengerInternal.eventTable;

        public static void AddListener(string eventType, MessengerCallback<T, U> handler) {
            GameMessengerInternal.OnListenerAdding(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T, U>)eventTable[eventType] + handler;
        }

        public static void RemoveListener(string eventType, MessengerCallback<T, U> handler) {
            GameMessengerInternal.OnListenerRemoving(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T, U>)eventTable[eventType] - handler;
            GameMessengerInternal.OnListenerRemoved(eventType);
        }

        public static void Broadcast(string eventType, T arg1, U arg2) {
            Broadcast(eventType, arg1, arg2, GameMessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventType, T arg1, U arg2, GameMessengerMode mode) {
            GameMessengerInternal.OnBroadcasting(eventType, mode);
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                MessengerCallback<T, U> callback = d as MessengerCallback<T, U>;
                if (callback != null) {
                    callback(arg1, arg2);
                }
                else {
                    throw GameMessengerInternal.CreateBroadcastSignatureException(eventType);
                }
            }
        }
    }

    // Three parameters
    public static class GameMessenger<T, U, V> {
        private static Dictionary<string, Delegate> eventTable = GameMessengerInternal.eventTable;

        public static void AddListener(string eventType, MessengerCallback<T, U, V> handler) {
            GameMessengerInternal.OnListenerAdding(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T, U, V>)eventTable[eventType] + handler;
        }

        public static void RemoveListener(string eventType, MessengerCallback<T, U, V> handler) {
            GameMessengerInternal.OnListenerRemoving(eventType, handler);
            eventTable[eventType] = (MessengerCallback<T, U, V>)eventTable[eventType] - handler;
            GameMessengerInternal.OnListenerRemoved(eventType);
        }

        public static void Broadcast(string eventType, T arg1, U arg2, V arg3) {
            Broadcast(eventType, arg1, arg2, arg3, GameMessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventType, T arg1, U arg2, V arg3, GameMessengerMode mode) {
            GameMessengerInternal.OnBroadcasting(eventType, mode);
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                MessengerCallback<T, U, V> callback = d as MessengerCallback<T, U, V>;
                if (callback != null) {
                    callback(arg1, arg2, arg3);
                }
                else {
                    throw GameMessengerInternal.CreateBroadcastSignatureException(eventType);
                }
            }
        }
    }
}