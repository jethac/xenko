﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Xenko.Audio
{
    /// <summary>
    /// Base class for all the sounds and sound instances.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    [ContentSerializer(typeof(DataContentSerializer<Sound>))]
    [DataSerializerGlobal(typeof(ReferenceSerializer<Sound>), Profile = "Content")]
    [DataSerializer(typeof(SoundBaseSerializer))]
    public partial class Sound : ComponentBase
    {
        public Sound()
        {
        }

        public void Attach(AudioEngine engine)
        {
            AttachEngine(engine);

            Name = "Sound Effect " + Interlocked.Add(ref soundEffectCreationCount, 1);

            // register the sound to the AudioEngine so that it will be properly freed if AudioEngine is disposed before this.
            AudioEngine.RegisterSound(this);
        }

        /// <summary>
        /// The number of SoundEffect Created so far. Used only to give a unique name to the SoundEffect.
        /// </summary>
        private static int soundEffectCreationCount;

        internal string CompressedDataUrl { get; set; }

        internal int SampleRate { get; set; } = 44100;

        internal const int SamplesPerFrame = 512;

        internal int Channels { get; set; } = 2;

        internal bool StreamFromDisk { get; set; }

        internal bool Spatialized { get; set; }

        internal int NumberOfPackets { get; set; }

        internal int MaxPacketLength { get; set; }

        [DataMemberIgnore]
        internal Stream CompressedDataStream;

        [DataMemberIgnore]
        internal UnmanagedArray<short> PreloadedData;

        [DataMemberIgnore]
        internal AudioEngineState EngineState => AudioEngine.State;

        #region Disposing Utilities
        
        internal void CheckNotDisposed()
        {
            if(IsDisposed)
                throw new ObjectDisposedException("this");
        }

        #endregion


        /// <summary>
        /// The number of Instances Created so far by this SoundEffect. Used only to give a unique name to the SoundEffectInstance.
        /// </summary>
        private int intancesCreationCount;

        /// <summary>
        /// Create a new sound effect instance of the sound effect. 
        /// The audio data are shared between the instances so that useless memory copies is avoided. 
        /// Each instance that can be played and localized independently from others.
        /// </summary>
        /// <returns>A new sound instance</returns>
        /// <exception cref="ObjectDisposedException">The sound has already been disposed</exception>
        public SoundInstance CreateInstance()
        {
            CheckNotDisposed();

            var newInstance = new SoundInstance(this) { Name = Name + " - Instance " + intancesCreationCount };

            RegisterInstance(newInstance);

            ++intancesCreationCount;

            return newInstance;
        }

        /// <summary>
        /// Current instances of the SoundEffect.
        /// We need to keep track of them to stop and dispose them when the soundEffect is disposed.
        /// </summary>
        internal readonly List<SoundInstance> Instances = new List<SoundInstance>();

        /// <summary>
        /// Register a new instance to the soundEffect.
        /// </summary>
        /// <param name="instance">new instance to register.</param>
        private void RegisterInstance(SoundInstance instance)
        {
            Instances.Add(instance);
        }

        /// <summary>
        /// Stop all registered instances of the <see cref="SoundEffect"/>.
        /// </summary>
        internal void StopAllInstances()
        {
            foreach (var instance in Instances)
                instance.Stop();
        }

        /// <summary>
        /// Stop all registered instances different from the provided main instance
        /// </summary>
        /// <param name="mainInstance">The main instance of the sound effect</param>
        internal void StopConcurrentInstances(SoundInstance mainInstance)
        {
            foreach (var instance in Instances)
            {
                if (instance != mainInstance)
                    instance.Stop();
            }
        }

        /// <summary>
        /// Unregister a disposed Instance.
        /// </summary>
        /// <param name="instance"></param>
        internal void UnregisterInstance(SoundInstance instance)
        {
            if (!Instances.Remove(instance))
                throw new AudioSystemInternalException("Tried to unregister soundEffectInstance while not contained in the instance list.");
        }
    }
}
