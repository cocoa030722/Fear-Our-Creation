using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 플레이어의 현재 무기 1개 + 잔량. 무기가 없거나 소진되면 주먹.
    /// 스페이스로 발밑 Pickup을 습득/교체(현재 무기는 그 자리에 잔량을 유지한 채 드롭)하고, 같은 종류 stackable 무기는 합산한다.
    /// 상태는 PlayerLoadout.Current에 항상 동기화되어 스냅샷/스테이지 이월에 쓰인다.
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        [SerializeField] WeaponCatalog catalog;
        [Tooltip("빈손일 때의 기본 무기(주먹)")]
        [SerializeField] WeaponData fist;
        [SerializeField] WeaponPickup pickupPrefab;
        [SerializeField] MeleeAttack melee;
        [SerializeField] RangedAttack ranged;
        [Tooltip("발밑 픽업 탐색 반지름(월드 유닛)")]
        [SerializeField] float pickupReach = 0.6f;
        [SerializeField] bool showDebugHud = true;

        public WeaponData Current { get; private set; }
        public int Ammo { get; private set; }

        int _pickupMask;
        static readonly Collider2D[] Buffer = new Collider2D[16];

        void Awake()
        {
            _pickupMask = Layers.Mask(Layers.Pickup);
        }

        void Start()
        {
            // 씬 시작 시 PlayerLoadout(스냅샷 복원/이월)을 읽어 장착한다
            var loadout = PlayerLoadout.Current;
            var data = catalog != null ? catalog.Find(loadout.WeaponId) : null;
            if (data != null && (!data.UsesAmmo || loadout.Ammo > 0)) Equip(data, loadout.Ammo);
            else Equip(fist, 0);
        }

        /// <summary>
        /// 좌클릭 입력 처리. 권총은 누르고 있으면 연사(간격은 SO), 그 외 무기는 눌렀을 때 1회 공격한다.
        /// </summary>
        public bool HandleAttackInput(bool pressed, bool held)
        {
            if (Current == null) return false;
            switch (Current.kind)
            {
                case WeaponKind.Melee:
                    return pressed && melee.TryAttack();
                case WeaponKind.Gun:
                    return held && ranged.TryFire(Current);
                default: // 투척 가시, 폭탄알
                    return pressed && ranged.TryFire(Current);
            }
        }

        /// <summary>죽으면 대기 중인 공격을 취소한다.</summary>
        public void CancelAttack()
        {
            if (melee != null) melee.Cancel();
        }

        /// <summary>스페이스: 발밑에서 가장 가까운 무기를 습득/교체한다.</summary>
        public void TryInteract()
        {
            var pickup = FindPickup();
            if (pickup == null) return;

            // 같은 종류 합산 습득: 상한을 넘는 분량은 바닥에 남긴다
            if (Current == pickup.data && Current.stackable)
            {
                int room = Current.maxAmmo - Ammo;
                if (room <= 0) return;
                int taken = Mathf.Min(room, pickup.ammo);
                SetAmmo(Ammo + taken);
                if (taken >= pickup.ammo) Destroy(pickup.gameObject);
                else pickup.SetAmmo(pickup.ammo - taken);
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.Pickup);
                return;
            }

            // 교체: 현재 무기(주먹 제외)를 픽업이 있던 자리에 잔량 그대로 내려놓는다
            Vector2 spot = pickup.transform.position;
            var newData = pickup.data;
            int newAmmo = newData.UsesAmmo ? Mathf.Min(pickup.ammo, newData.maxAmmo) : 0;
            int leftover = pickup.ammo - newAmmo;

            if (leftover > 0) pickup.SetAmmo(leftover);
            else Destroy(pickup.gameObject);

            if (Current != fist) WeaponPickup.Spawn(pickupPrefab, Current, Ammo, spot);
            Equip(newData, newAmmo);
            Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.Pickup);
        }

        /// <summary>같은 종류 무기를 들고 있을 때만 잔량을 더한다(투척 가시 회수 등). 실제로 더해진 수를 반환.</summary>
        public int TryAddAmmo(WeaponData data, int amount)
        {
            if (Current != data || !data.stackable) return 0;
            int added = Mathf.Clamp(amount, 0, data.maxAmmo - Ammo);
            if (added > 0) SetAmmo(Ammo + added);
            return added;
        }

        /// <summary>탄약을 소모한다. 소진되면 무기가 사라지고 주먹으로 복귀한다.</summary>
        public void ConsumeAmmo(int amount = 1)
        {
            if (Current == null || !Current.UsesAmmo) return;
            SetAmmo(Ammo - amount);
        }

        WeaponPickup FindPickup()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _pickupMask, useTriggers = true };
            int count = Physics2D.OverlapCircle(transform.position, pickupReach, filter, Buffer);
            WeaponPickup best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!Buffer[i].TryGetComponent<WeaponPickup>(out var p) || p.data == null) continue;
                float d = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        void Equip(WeaponData data, int ammo)
        {
            Current = data;
            Ammo = data.UsesAmmo ? Mathf.Clamp(ammo, 0, data.maxAmmo) : 0;

            if (melee != null)
            {
                if (data.kind == WeaponKind.Melee) melee.SetWeapon(data);
                else melee.ClearWeapon();
            }
            SyncLoadout();
        }

        void SetAmmo(int value)
        {
            Ammo = Mathf.Max(0, value);
            if (Ammo <= 0 && Current.UsesAmmo)
            {
                Equip(fist, 0); // 소진된 무기(빈 총 포함)는 사라진다
                return;
            }
            SyncLoadout();
        }

        void SyncLoadout()
        {
            var loadout = PlayerLoadout.Current;
            loadout.WeaponId = Current == fist ? string.Empty : Current.id;
            loadout.Ammo = Ammo;
        }

        void OnGUI()
        {
            if (!showDebugHud || Current == null) return;
            string text = Current.UsesAmmo ? $"{Current.displayName}  {Ammo}/{Current.maxAmmo}" : Current.displayName;
            GUI.Label(new Rect(12, 8, 400, 24), text);
        }
    }
}
