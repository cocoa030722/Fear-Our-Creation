using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 플레이스홀더 아트 색 규칙. 검정 계열 배경 / 청록 오브젝트 / 붉은 적 / 노란 위험 표식.
    /// 배경을 흰색에서 검정 계열로 바꾸면서(수정사항.md) 배경 위에서 보여야 하는 Player만
    /// 어두운 남색에서 밝은 회백색으로 바꿨다(Enemy/LabObject/Hazard는 검정 배경에서도 그대로 대비됨).
    /// </summary>
    public static class PlaceholderPalette
    {
        public static readonly Color Background = new Color32(0x14, 0x16, 0x18, 0xFF);
        public static readonly Color Player = new Color32(0xE8, 0xEC, 0xEE, 0xFF);
        public static readonly Color LabObject = new Color32(0x1A, 0xA6, 0xA6, 0xFF);
        public static readonly Color Enemy = new Color32(0xD6, 0x28, 0x28, 0xFF);
        public static readonly Color Hazard = new Color32(0xF5, 0xC5, 0x18, 0xFF);
    }
}
