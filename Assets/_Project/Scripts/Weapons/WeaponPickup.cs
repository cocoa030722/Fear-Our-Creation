using UnityEngine;

namespace Game.Weapons
{
    /// <summary>바닥에 놓인 무기. 스페이스로 습득/교체한다. 드롭된 무기는 잔량을 유지한다.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponData data;
        public int ammo;

        [SerializeField] SpriteRenderer body;

        const float RecoverRadius = 0.6f;

        TextMesh _label;
        bool _autoRecover;
        WeaponHolder _holder;

        void Start() => Refresh();

        /// <summary>투척 가시 회수용으로 만든다: 같은 무기를 들고 있는 플레이어가 위를 지나가면 자동 습득(스페이스 교체와 분리).</summary>
        public void MakeAutoRecover()
        {
            _autoRecover = true;
            transform.localScale *= 0.6f;
        }

        void Update()
        {
            if (!_autoRecover || data == null) return;
            if (_holder == null) _holder = FindAnyObjectByType<WeaponHolder>();
            if (_holder == null || _holder.Current != data) return;
            if (((Vector2)_holder.transform.position - (Vector2)transform.position).sqrMagnitude > RecoverRadius * RecoverRadius) return;

            int added = _holder.TryAddAmmo(data, ammo);
            if (added <= 0) return;
            if (added >= ammo) Destroy(gameObject);
            else SetAmmo(ammo - added);
        }

        /// <summary>data/ammo에 맞춰 색과 라벨을 갱신한다.</summary>
        public void Refresh()
        {
            if (data == null) return;
            if (body != null) body.color = data.pickupColor;
            EnsureLabel();
            if (_label != null)
                _label.text = data.UsesAmmo ? $"{data.displayName}\n{ammo}" : data.displayName;
        }

        void EnsureLabel()
        {
            if (_label != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            go.transform.localScale = new Vector3(1f / transform.localScale.x, 1f / transform.localScale.y, 1f);
            _label = go.AddComponent<TextMesh>();
            _label.font = font;
            _label.fontSize = 48;
            _label.characterSize = 0.06f;
            _label.anchor = TextAnchor.LowerCenter;
            _label.alignment = TextAlignment.Center;
            _label.color = new Color(0.17f, 0.23f, 0.26f, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 5;
        }

        public void SetAmmo(int value)
        {
            ammo = value;
            Refresh();
        }

        /// <summary>프리팹으로 픽업을 생성한다.</summary>
        public static WeaponPickup Spawn(WeaponPickup prefab, WeaponData data, int ammo, Vector2 position)
        {
            var p = Instantiate(prefab, position, Quaternion.identity);
            p.data = data;
            p.ammo = ammo;
            p.Refresh();
            return p;
        }
    }
}
