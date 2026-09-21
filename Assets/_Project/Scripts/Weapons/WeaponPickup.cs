using UnityEngine;

namespace Game.Weapons
{
    /// <summary>바닥에 놓인 무기. 스페이스로 습득/교체한다. 드롭된 무기는 잔량을 유지한다.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponData data;
        public int ammo;

        [SerializeField] SpriteRenderer body;

        TextMesh _label;

        void Start() => Refresh();

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
