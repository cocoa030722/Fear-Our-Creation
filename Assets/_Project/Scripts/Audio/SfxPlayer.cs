using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 플레이스홀더 SFX 재생기. 씬마다 하나, M2Setup.CreateRig()(단일 배선 지점)와 Title 씬에서 생성한다.
    /// 클립은 PlaceholderTone으로 지연 생성 후 캐싱한다. AudioSource를 여러 개 돌려써서 겹쳐 재생해도 끊기지 않는다.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        const int VoiceCount = 4;
        static readonly Dictionary<Sfx, AudioClip> Clips = new Dictionary<Sfx, AudioClip>();

        AudioSource[] _voices;
        int _next;

        void Awake()
        {
            Instance = this;
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices[i] = src;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(Sfx sfx)
        {
            if (Instance == null) return;
            var clip = GetClip(sfx);
            var src = Instance._voices[Instance._next];
            Instance._next = (Instance._next + 1) % Instance._voices.Length;
            src.PlayOneShot(clip);
        }

        static AudioClip GetClip(Sfx sfx)
        {
            if (Clips.TryGetValue(sfx, out var clip) && clip != null) return clip;
            clip = Build(sfx);
            Clips[sfx] = clip;
            return clip;
        }

        static AudioClip Build(Sfx sfx) => sfx switch
        {
            Sfx.Shoot => PlaceholderTone.Create("sfx_shoot", Waveform.Square, 1200f, 900f, 0.05f, 0.35f),
            Sfx.Throw => PlaceholderTone.Create("sfx_throw", Waveform.Triangle, 700f, 500f, 0.08f, 0.4f),
            Sfx.MeleeSwing => PlaceholderTone.Create("sfx_melee", Waveform.Triangle, 500f, 180f, 0.09f, 0.45f),
            Sfx.PlayerHit => PlaceholderTone.Create("sfx_playerhit", Waveform.Square, 160f, 70f, 0.22f, 0.6f),
            Sfx.EnemyDeath => PlaceholderTone.Create("sfx_enemydeath", Waveform.Noise, 1f, 1f, 0.22f, 0.5f),
            Sfx.Explosion => PlaceholderTone.Create("sfx_explosion", Waveform.Noise, 1f, 1f, 0.35f, 0.7f),
            Sfx.Pickup => PlaceholderTone.Create("sfx_pickup", Waveform.Sine, 500f, 900f, 0.15f, 0.4f),
            Sfx.UiClick => PlaceholderTone.Create("sfx_uiclick", Waveform.Sine, 900f, 900f, 0.04f, 0.3f),
            _ => PlaceholderTone.Create("sfx_default", Waveform.Sine, 440f, 440f, 0.1f, 0.3f),
        };
    }
}
