namespace Game.Core
{
    /// <summary>
    /// 모든 거리 수치의 기준점. 이 값들은 여기에서만 정의한다.
    /// 사거리/폭발 지름/어그로 반경 등은 데이터에 배수로 기록하고, 월드 단위로의 환산은 이 클래스를 거친다.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>플레이어 지름(월드 유닛). 모든 사거리·폭발 지름의 1배 기준.</summary>
        public const float PlayerDiameter = 1f;

        /// <summary>카메라 Orthographic Size 고정값. 변경하면 ScreenWidth 기반 수치가 모두 바뀐다.</summary>
        public const float CameraOrthographicSize = 5f;

        /// <summary>화면 비율 16:9 고정.</summary>
        public const float TargetAspect = 16f / 9f;

        public const float ScreenHeight = CameraOrthographicSize * 2f;

        /// <summary>'화면 가로 길이'의 월드 폭. 어그로 반경(1배), 적 시야(0.5배)의 기준.</summary>
        public const float ScreenWidth = ScreenHeight * TargetAspect;

        /// <summary>플레이어 지름의 배수를 월드 유닛으로 환산.</summary>
        public static float FromPlayerDiameters(float multiplier) => multiplier * PlayerDiameter;

        /// <summary>화면 가로 길이의 배수를 월드 유닛으로 환산.</summary>
        public static float FromScreenWidths(float multiplier) => multiplier * ScreenWidth;
    }
}
