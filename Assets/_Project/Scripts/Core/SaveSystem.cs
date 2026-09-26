using System.IO;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 디스크에 저장하는 "이어하기" 기능(수정사항.md: 저장/불러오기 기능을 확실하게 구현).
    /// 저장 시점은 스테이지 맵에 진입할 때마다(RestartController.Awake)이고, 저장 내용은
    /// "지금 어느 맵에 있고 무엇을 들고 있는지"뿐이다. 적/무기/시신 배치는 그 씬 자체가 이미 기억하므로
    /// (Editor/StageSetup 등이 씬을 만들어 두었음) 따로 저장할 필요가 없다.
    /// </summary>
    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool HasSave() => File.Exists(FilePath);

        /// <summary>현재 씬 이름 + PlayerLoadout.Current를 디스크에 저장한다.</summary>
        public static void Save(string sceneName)
        {
            var data = new SaveData
            {
                sceneName = sceneName,
                weaponId = PlayerLoadout.Current.WeaponId,
                ammo = PlayerLoadout.Current.Ammo,
            };
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveSystem] 저장 실패: " + e.Message);
            }
        }

        /// <summary>저장된 씬 이름을 반환하고, PlayerLoadout.Current를 저장된 소지품으로 채운다. 실패하면 false.</summary>
        public static bool TryLoad(out string sceneName)
        {
            sceneName = null;
            if (!HasSave()) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null || string.IsNullOrEmpty(data.sceneName)) return false;
                PlayerLoadout.Current = new PlayerLoadout { WeaponId = data.weaponId, Ammo = data.ammo };
                sceneName = data.sceneName;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveSystem] 불러오기 실패: " + e.Message);
                return false;
            }
        }
    }
}
