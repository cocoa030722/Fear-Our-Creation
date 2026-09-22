using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 근접 공격(주먹/가시창). 클릭 → 발동 딜레이 → 그 시점의 위치/방향으로 판정을 재계산해 범위 내 대상을 타격한다.
    /// 부채꼴 각도가 0이면 몸 가장자리에서 앞으로 뻗는 직선 상자, 0보다 크면 부채꼴. 벽 뒤 대상은 WallQuery로 걸러낸다.
    /// 판정 자체는 MeleeHit(적과 공용)가 수행하고, 이 클래스는 입력 타이밍/범위 표시를 담당한다. 무기 수치는 WeaponHolder가 SetWeapon으로 넘긴다.
    /// </summary>
    public class MeleeAttack : MonoBehaviour
    {
        [Header("공격 범위 표시(반투명)")]
        [Tooltip("판정 범위와 같은 모양으로 그려지는 스프라이트. 플레이어의 자식이며 +Y가 전방")]
        [SerializeField] SpriteRenderer rangeIndicator;
        [Tooltip("판정 후에도 표시를 유지하는 시간(초)")]
        [SerializeField] float indicatorLingerSeconds = 0.1f;
        [Tooltip("공격하지 않아도 항상 표시(범위 확인/디버그용)")]
        [SerializeField] bool alwaysShowIndicator;

        const float BodyRadius = GameConstants.PlayerDiameter * 0.5f;
        const int SectorTextureSize = 256;

        WeaponData _data;
        Sprite _boxSprite;
        readonly Dictionary<WeaponData, Sprite> _sectorSprites = new Dictionary<WeaponData, Sprite>();
        float _nextAttackTime;
        float _hitTime = -1f;
        float _indicatorUntil = -1f;
        int _targetMask;

        void Awake()
        {
            _targetMask = Layers.Mask(Layers.Enemy, Layers.Destructible);
            if (rangeIndicator != null) _boxSprite = rangeIndicator.sprite;
        }

        void OnDestroy()
        {
            foreach (var s in _sectorSprites.Values)
            {
                if (s == null) continue;
                Destroy(s.texture);
                Destroy(s);
            }
        }

        /// <summary>현재 근접 무기를 바꾼다. 대기 중인 판정은 취소하고 표시 범위를 새 수치로 맞춘다.</summary>
        public void SetWeapon(WeaponData data)
        {
            _data = data;
            Cancel();
            SetupIndicator();
        }

        bool IsArc => _data != null && _data.arcDegrees > 0f;
        float Reach => GameConstants.FromPlayerDiameters(_data.rangeInDiameters);
        float Width => GameConstants.FromPlayerDiameters(_data.widthInDiameters);

        /// <summary>표시 스프라이트를 판정 범위와 동일한 모양/크기로 맞춘다(수치는 SO에서 옴).</summary>
        void SetupIndicator()
        {
            if (rangeIndicator == null || _data == null) return;
            var t = rangeIndicator.transform;
            t.localRotation = Quaternion.identity;
            if (IsArc)
            {
                // 부채꼴: 플레이어 중심 기준, 바깥 반지름 = 몸 가장자리 + 사거리
                rangeIndicator.sprite = GetSectorSprite(_data);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }
            else
            {
                rangeIndicator.sprite = _boxSprite;
                t.localPosition = new Vector3(0f, BodyRadius + Reach * 0.5f, 0f);
                t.localScale = new Vector3(Width, Reach, 1f);
            }
            rangeIndicator.enabled = alwaysShowIndicator;
        }

        /// <summary>플레이어 몸 밖 ~ 바깥 반지름 사이의 부채꼴(+Y 중심) 스프라이트를 코드로 생성한다.</summary>
        Sprite GetSectorSprite(WeaponData data)
        {
            if (_sectorSprites.TryGetValue(data, out var cached) && cached != null) return cached;

            float outer = BodyRadius + Reach;
            float halfArc = data.arcDegrees * 0.5f;
            int n = SectorTextureSize;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // 텍스처 중심 = 플레이어 중심. 한 변 = 2*outer 월드 유닛
                    float wx = ((x + 0.5f) / n * 2f - 1f) * outer;
                    float wy = ((y + 0.5f) / n * 2f - 1f) * outer;
                    float d = Mathf.Sqrt(wx * wx + wy * wy);
                    bool inside = d >= BodyRadius && d <= outer && Mathf.Abs(Mathf.Atan2(wx, wy) * Mathf.Rad2Deg) <= halfArc;
                    pixels[y * n + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            float ppu = n / (outer * 2f);
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ppu);
            _sectorSprites[data] = sprite;
            return sprite;
        }

        /// <summary>공격 시도. 간격이 지나지 않았으면 무시한다.</summary>
        public bool TryAttack()
        {
            if (_data == null || Time.time < _nextAttackTime) return false;
            _nextAttackTime = Time.time + _data.intervalSeconds;
            _hitTime = Time.time + _data.windupSeconds;
            _indicatorUntil = _hitTime + indicatorLingerSeconds;
            Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.MeleeSwing);
            return true;
        }

        /// <summary>죽거나 무기를 바꾸면 대기 중인 판정을 취소한다.</summary>
        public void Cancel()
        {
            _hitTime = -1f;
            _indicatorUntil = -1f;
            if (rangeIndicator != null) rangeIndicator.enabled = alwaysShowIndicator;
        }

        /// <summary>근접 무기가 아닌 무기를 들었을 때 표시를 끈다.</summary>
        public void ClearWeapon()
        {
            _data = null;
            Cancel();
            if (rangeIndicator != null) rangeIndicator.enabled = false;
        }

        void Update()
        {
            if (rangeIndicator != null && _data != null)
                rangeIndicator.enabled = alwaysShowIndicator || Time.time < _indicatorUntil;

            if (_hitTime >= 0f && Time.time >= _hitTime)
            {
                _hitTime = -1f;
                ResolveHit();
            }
        }

        void ResolveHit()
        {
            if (_data == null) return;
            MeleeHit.Strike(transform.position, transform.up, BodyRadius, _data, _targetMask, HitSource.Player);
        }

        void GetBox(out Vector2 center, out Vector2 size, out float angle)
        {
            MeleeHit.GetBox(transform.position, transform.up, BodyRadius, _data, out center, out size, out angle);
        }

        void OnDrawGizmosSelected()
        {
            if (_data == null) return;
            Gizmos.color = Color.yellow;
            if (IsArc)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                float outer = BodyRadius + Reach;
                float half = _data.arcDegrees * 0.5f;
                Vector3 a = Quaternion.Euler(0, 0, half) * Vector3.up * outer;
                Vector3 b = Quaternion.Euler(0, 0, -half) * Vector3.up * outer;
                Gizmos.DrawLine(Vector3.zero, a);
                Gizmos.DrawLine(Vector3.zero, b);
                Gizmos.DrawLine(a, b);
                return;
            }
            GetBox(out var center, out var size, out var ang);
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, ang), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
        }
    }
}
