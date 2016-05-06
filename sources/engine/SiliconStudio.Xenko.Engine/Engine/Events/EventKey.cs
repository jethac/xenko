﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Xenko.Engine.Events
{
    /// <summary>
    /// Creates a new EventKey used to broadcast events.
    /// </summary>
    public class EventKey : EventKey<bool>
    {
        public EventKey(string category = "General", string eventName = "Event") : base(category, eventName)
        {       
        }

        /// <summary>
        /// Broadcasts the event to all the receivers
        /// </summary>
        public void Broadcast()
        {
            Broadcast(true);
        }
    }

    internal static class EventKeyCounter
    {
        private static long eventKeysCounter;

        public static ulong New()
        {
            return (ulong)Interlocked.Increment(ref eventKeysCounter);
        }
    }

    /// <summary>
    /// Creates a new EventKey used to broadcast T type events.
    /// </summary>
    /// <typeparam name="T">The data type of the event you wish to send</typeparam>
    public class EventKey<T>
    {
        internal readonly Logger Logger;
        internal readonly ulong EventId = EventKeyCounter.New();
        internal readonly string EventName;

        private readonly string broadcastDebug;

        private readonly BroadcastBlock<T> broadcastBlock = new BroadcastBlock<T>(null);

        public EventKey(string category = "General", string eventName = "Event")
        {
            EventName = eventName;
            Logger = GlobalLogger.GetLogger($"Event - {category}");
            broadcastDebug = $"Broadcasting '{eventName}' ({EventId})";
        }

        internal IDisposable Connect(EventReceiver<T> target)
        {
            return broadcastBlock.LinkTo(target.BufferBlock);
        }

        /// <summary>
        /// Broadcasts the event data to all the receivers
        /// </summary>
        /// <param name="data"></param>
        public void Broadcast(T data)
        {
            Logger.Debug(broadcastDebug);
            broadcastBlock.Post(data);
        }
    }
}
