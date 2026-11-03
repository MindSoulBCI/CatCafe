using System;
using UnityEngine;

namespace SpatialCatCafe
{
    public enum CatAction { Pet, Feed, Play, Rest }

    [Serializable]
    public sealed class CatProgress
    {
        public string id;
        public float affection = 20;
        public float fullness = 65;
        public float energy = 80;
        public float happiness = 70;
        public int interactions;
    }

    [Serializable]
    public sealed class CafeState
    {
        public int version = 1;
        public int selectedCat;
        public bool evening;
        public bool sound = true;
        public CatProgress[] cats;

        public static CafeState Fresh()
        {
            var state = new CafeState { cats = new CatProgress[CatCatalog.All.Length] };
            for (int i = 0; i < state.cats.Length; i++)
                state.cats[i] = new CatProgress { id = CatCatalog.All[i].Id };
            return state;
        }
    }

    public sealed class CatProfile
    {
        public readonly string Id, Name, Description, Favorite;
        public readonly Color Coat, Accent;
        public readonly CatAction PreferredAction;
        public CatProfile(string id, string name, string description, string favorite,
            string coat, string accent, CatAction preferred)
        {
            Id = id; Name = name; Description = description; Favorite = favorite;
            ColorUtility.TryParseHtmlString(coat, out var c); Coat = c;
            ColorUtility.TryParseHtmlString(accent, out var a); Accent = a;
            PreferredAction = preferred;
        }
    }

    public static class CatCatalog
    {
        public static readonly CatProfile[] All = {
            new CatProfile("latte", "拿铁", "黏人的小橘猫，最喜欢你摸摸它的头。", "偏爱抚摸", "#D89A5C", "#9D603B", CatAction.Pet),
            new CatProfile("sesame", "芝麻", "安静的灰猫，一份小鱼干就能收获它的心。", "偏爱小鱼干", "#8D9CA2", "#55636C", CatAction.Feed),
            new CatProfile("pudding", "布丁", "好奇的奶油猫，看到逗猫球就停不下来。", "偏爱玩耍", "#EADBC2", "#B7997C", CatAction.Play)
        };
    }
}
