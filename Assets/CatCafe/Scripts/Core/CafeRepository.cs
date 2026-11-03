using System;
using System.IO;
using UnityEngine;

namespace SpatialCatCafe
{
    // Synchronous small save, atomic replacement and previous-save recovery.
    public sealed class CafeRepository
    {
        readonly string path = Path.Combine(Application.persistentDataPath, "cat-cafe-v1.json");
        public CafeState Load()
        {
            foreach (var candidate in new[] { path, path + ".backup" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var value = JsonUtility.FromJson<CafeState>(File.ReadAllText(candidate));
                    if (value == null || value.version != 1 || value.cats == null) continue;
                    var result = CafeState.Fresh();
                    result.selectedCat = Mathf.Clamp(value.selectedCat, 0, result.cats.Length - 1);
                    result.evening = value.evening; result.sound = value.sound;
                    for (int i = 0; i < result.cats.Length; i++)
                    {
                        var match = Array.Find(value.cats, cat => cat != null && cat.id == result.cats[i].id);
                        if (match == null) continue;
                        match.affection = Clamp(match.affection);
                        match.fullness = Clamp(match.fullness);
                        match.energy = Clamp(match.energy);
                        match.happiness = Clamp(match.happiness);
                        match.interactions = Math.Max(0, match.interactions);
                        result.cats[i] = match;
                    }
                    return result;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
                { Debug.LogWarning("猫咖存档读取失败，将尝试备份：" + e.Message); }
            }
            return CafeState.Fresh();
        }

        static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 50 : Mathf.Clamp(value, 0, 100);

        public bool Save(CafeState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(state, true));
                if (File.Exists(path))
                    File.Replace(path + ".tmp", path, path + ".backup");
                else
                    File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Debug.LogWarning("猫咖存档写入失败：" + e.Message); return false; }
        }
    }
}
