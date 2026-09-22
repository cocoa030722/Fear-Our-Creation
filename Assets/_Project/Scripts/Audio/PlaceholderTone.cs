using UnityEngine;

namespace Game.Audio
{
    public enum Waveform { Sine, Square, Triangle, Noise }

    /// <summary>
    /// 오디오 에셋 없이 파형을 코드로 생성하는 플레이스홀더 사운드(PlaceholderPalette의 오디오 버전).
    /// 이벤트별로 파형/주파수/길이를 다르게 줘서 서로 구별되게 한다.
    /// </summary>
    public static class PlaceholderTone
    {
        const int SampleRate = 44100;

        /// <summary>startFreq→endFreq로 스윕하는 waveform을 duration초 길이로 생성한다. 시작/끝 10%는 클릭 방지용 선형 엔벨로프.</summary>
        public static AudioClip Create(string name, Waveform wave, float startFreq, float endFreq, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            double phase = 0.0;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / SampleRate;
                float frac = (float)(phase - System.Math.Floor(phase));

                float raw = wave switch
                {
                    Waveform.Sine => Mathf.Sin((float)(phase * 2.0 * Mathf.PI)),
                    Waveform.Square => frac < 0.5f ? 1f : -1f,
                    Waveform.Triangle => 4f * Mathf.Abs(frac - 0.5f) - 1f,
                    Waveform.Noise => (float)(rng.NextDouble() * 2.0 - 1.0),
                    _ => 0f,
                };

                float envelope = Mathf.Min(1f, Mathf.Min(t, 1f - t) * 10f);
                data[i] = raw * volume * envelope;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
