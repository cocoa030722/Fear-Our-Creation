using Game.Core;
using Game.Enemies;
using Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M6 셋업: 보스 SO/프리팹. 5스테이지 씬 구성은 StageSetup.BuildStage5가 담당한다(다른 스테이지와 같은 방식).
    /// 여러 번 실행해도 안전(SO는 id가 비어 있을 때만 초기화, 프리팹은 없을 때만 생성).
    /// </summary>
    public static class M6Setup
    {
        const string Root = "Assets/_Project";
        const string BossDataPath = Root + "/Data/Enemies/Boss.asset";
        internal const string BossPrefabPath = Root + "/Prefabs/Enemies/Boss.prefab";
        const string ProjectilePrefabPath = Root + "/Prefabs/Projectiles/Projectile.prefab";
        const string ThornWeaponPath = Root + "/Data/Weapons/ThrownThorn.asset";

        internal static bool EnsureAssets()
        {
            if (!M4Setup.EnsureAssets()) return false;

            var thorn = AssetDatabase.LoadAssetAtPath<WeaponData>(ThornWeaponPath);
            var data = AssetDatabase.LoadAssetAtPath<BossData>(BossDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BossData>();
                AssetDatabase.CreateAsset(data, BossDataPath);
            }
            if (string.IsNullOrEmpty(data.id))
            {
                data.id = "boss";
                data.displayName = "Director";
                data.maxHealth = 100f;            // 기획: 근접 4번 분량
                data.spikeWeapon = thorn;          // 기획: 가시 원거리 사격
                data.fireTelegraphSeconds = 0.8f;  // 기획: 예고 0.8초
                data.fireCooldownSeconds = 1.5f;   // 개발계획 6-2 임시값(예고 → 1발 → 쿨다운)
                data.summonCooldownSeconds = 12f;  // 기획: 쿨다운 12초
                data.summonMotionSeconds = 1.5f;   // 기획: 호출 모션 1.5초
                data.summonCount = 4;              // 기획: 동시 4기
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();

            EnsureBossPrefab(data);
            return true;
        }

        static void EnsureBossPrefab(BossData data)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) != null) return;

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath).GetComponent<Projectile>();

            // 몸(플레이스홀더): 진입 연출 전에는 방호복(슬레이트색), 공개 후 붉은색으로 바뀐다(BossIntro)
            float diameter = GameConstants.PlayerDiameter * 1.6f; // 기획에 수치 없음, 임시로 큰 체구
            var go = M1Setup.CreateSprite("Boss", circle, unlit, PlaceholderPalette.Player, Vector2.zero, Vector2.one * diameter, Layers.Enemy);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // 손상 단계 오버레이: 몸 위에 겹쳐 알파가 올라가며 촉수 노출을 표현(플레이스홀더)
            var overlay = M1Setup.CreateSprite("ArmorOverlay", circle, unlit, PlaceholderPalette.Enemy, Vector2.zero, Vector2.one * 0.9f, Layers.Enemy);
            overlay.transform.SetParent(go.transform, false);
            var overlaySr = overlay.GetComponent<SpriteRenderer>();
            overlaySr.sortingOrder = 1;
            var c = overlaySr.color; c.a = 0f; overlaySr.color = c;

            // 사격 예고선(플레이스홀더 LineRenderer)
            var lineGo = new GameObject("TelegraphLine");
            lineGo.transform.SetParent(go.transform, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true; // Boss가 월드 좌표로 예고선 위치를 지정한다
            line.sharedMaterial = unlit;
            line.positionCount = 2;
            line.widthMultiplier = 0.08f;
            line.sortingOrder = 2;
            line.enabled = false;

            var boss = go.AddComponent<Boss>();
            M1Setup.SetField(boss, "data", data);
            M1Setup.SetField(boss, "body", go.GetComponent<SpriteRenderer>());
            M1Setup.SetField(boss, "armorOverlay", overlaySr);
            M1Setup.SetField(boss, "hitCollider", col);
            M1Setup.SetField(boss, "projectilePrefab", projectile);
            M1Setup.SetField(boss, "telegraphLine", line);

            PrefabUtility.SaveAsPrefabAsset(go, BossPrefabPath);
            Object.DestroyImmediate(go);
        }
    }
}
