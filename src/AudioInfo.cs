using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class AudioInfo : Resource
    {
        [Export]
        public AudioStream audioStream;
        [Export]
        public bool autoPlayAudio = false;
        [Export]
        public bool loopAudio = false;
        [Export]
        public AudioStreamPlayer3D.AttenuationModelEnum attenuation;
        [Export]
        public float audioMaxDistance;
        [Export]
        public float audioVolumeDb;
        [Export]
        public float audioPan;
    }
}