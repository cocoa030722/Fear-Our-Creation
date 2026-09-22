using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 원거리 공격(투척 가시, 폭탄알, 권총). 발사 간격을 지키고, 조준 방향으로 투사체를 생성하며 탄약을 소모한다.
    /// 권총은 발사 시 SoundEventBus로 소리 이벤트를 발행한다(수신자는 M3의 적 AI).
    /// </summary>
    public class RangedAttack : MonoBehaviour
    {
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] WeaponHolder holder;

        float _nextFireTime;

        /// <summary>발사 시도. 간격이 지나지 않았거나 탄약이 없으면 무시한다.</summary>
        public bool TryFire(WeaponData data)
        {
            if (data == null || Time.time < _nextFireTime || holder.Ammo <= 0) return false;
            _nextFireTime = Time.time + data.intervalSeconds;

            // 몸 중심에서 출발해 벽에 붙어 쏴도 벽을 넘지 않게 한다(투사체가 플레이어 레이어는 무시)
            Vector2 origin = transform.position;
            Vector2 dir = transform.up;
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.Init(data, dir);

            if (data.kind == WeaponKind.Gun)
            {
                float radius = GameConstants.FromScreenWidths(data.aggroRadiusInScreenWidths);
                SoundEventBus.Publish(new SoundEvent(origin, radius, data.aggroSeconds));
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.Shoot);
            }
            else
            {
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.Throw);
            }

            holder.ConsumeAmmo(1);
            return true;
        }
    }
}
