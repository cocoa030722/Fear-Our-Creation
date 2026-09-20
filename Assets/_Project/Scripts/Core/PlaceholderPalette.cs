using UnityEngine;

namespace Game.Core
{
    /// <summary>플레이스홀더 아트 색 규칙. 흰 배경 / 청록 오브젝트 / 붉은 적 / 노란 위험 표식.</summary>
    public static class PlaceholderPalette
    {
        public static readonly Color Background = Color.white;
        public static readonly Color Player = new Color32(0x2B, 0x3A, 0x42, 0xFF);
        public static readonly Color LabObject = new Color32(0x1A, 0xA6, 0xA6, 0xFF);
        public static readonly Color Enemy = new Color32(0xD6, 0x28, 0x28, 0xFF);
        public static readonly Color Hazard = new Color32(0xF5, 0xC5, 0x18, 0xFF);
    }
}
