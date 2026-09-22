namespace Game.Audio
{
    /// <summary>게임 내 효과음 종류. 실체 클립은 SfxPlayer가 PlaceholderTone으로 생성한다.</summary>
    public enum Sfx
    {
        Shoot,
        Throw,
        MeleeSwing,
        PlayerHit,
        EnemyDeath,
        Explosion,
        Pickup,
        UiClick,
    }
}
