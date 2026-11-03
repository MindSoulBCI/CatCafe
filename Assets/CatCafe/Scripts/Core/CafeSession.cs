using System;
using UnityEngine;

namespace SpatialCatCafe
{
    // Shared command boundary for touch, mouse and a future XR adapter.
    public interface ICatInteractionTarget
    {
        void SelectCat(int index);
        void Interact(CatAction action);
    }

    public sealed class CafeSession : MonoBehaviour, ICatInteractionTarget
    {
        public CafeState State { get; private set; }
        public CatProgress Current => State.cats[State.selectedCat];
        public CatProfile Profile => CatCatalog.All[State.selectedCat];
        public event Action Changed;
        public event Action<CatAction, int> Acted;
        public event Action<string> Message;
        CafeRepository repository;
        float nextAction, autosave, tick;

        void Awake() { repository = new CafeRepository(); State = repository.Load(); }
        void Update()
        {
            // Unity can deliver Update during an Editor hot reload before Awake has rebuilt
            // this runtime-only state. Ignore that short transition instead of throwing.
            if (repository == null || State == null) return;
            tick += Time.deltaTime; autosave += Time.deltaTime;
            if (tick >= 1)
            {
                float elapsed = tick; tick = 0;
                foreach (var cat in State.cats)
                {
                    cat.fullness = Mathf.Max(0, cat.fullness - elapsed * .035f);
                    cat.happiness = Mathf.Max(0, cat.happiness - elapsed * .018f);
                    cat.energy = Mathf.Min(100, cat.energy + elapsed * .045f);
                }
                Changed?.Invoke();
            }
            if (autosave >= 30) { autosave = 0; Save(); }
        }

        public void SelectCat(int index)
        {
            if (index < 0 || index >= State.cats.Length || index == State.selectedCat) return;
            State.selectedCat = index; Changed?.Invoke();
            Message?.Invoke(Profile.Name + "向你走来了。"); Save();
        }

        public void Interact(CatAction action)
        {
            if (Time.unscaledTime < nextAction) return;
            var cat = Current;
            if (action == CatAction.Feed && cat.fullness >= 94)
            { Message?.Invoke(Profile.Name + "已经吃饱啦，陪它玩一会儿吧。"); return; }
            if (action == CatAction.Play && cat.energy < 15)
            { Message?.Invoke(Profile.Name + "有点累了，让它休息一下。"); return; }
            nextAction = Time.unscaledTime + (action == CatAction.Play ? 2.8f : 1.2f);
            float bonus = action == Profile.PreferredAction ? 1.6f : 1;
            switch (action)
            {
                case CatAction.Pet:
                    cat.affection += 3 * bonus; cat.happiness += 6;
                    Message?.Invoke(Profile.Name + "眯起眼睛，发出了轻轻的呼噜声。"); break;
                case CatAction.Feed:
                    cat.fullness += 23; cat.affection += 4 * bonus; cat.happiness += 4;
                    Message?.Invoke(Profile.Name + "吃到了小鱼干，开心地摇了摇尾巴。"); break;
                case CatAction.Play:
                    cat.energy -= 14; cat.happiness += 13; cat.affection += 5 * bonus;
                    Message?.Invoke(Profile.Name + "扑向了逗猫球！"); break;
                case CatAction.Rest:
                    cat.energy += 22; cat.happiness += 3;
                    Message?.Invoke(Profile.Name + "蜷成了软软的一团。"); break;
            }
            cat.affection = Mathf.Clamp(cat.affection, 0, 100);
            cat.fullness = Mathf.Clamp(cat.fullness, 0, 100);
            cat.energy = Mathf.Clamp(cat.energy, 0, 100);
            cat.happiness = Mathf.Clamp(cat.happiness, 0, 100);
            cat.interactions++;
            Acted?.Invoke(action, State.selectedCat); Changed?.Invoke(); Save();
        }

        public void ToggleEvening() { State.evening = !State.evening; Changed?.Invoke(); Save(); }
        public void ToggleSound() { State.sound = !State.sound; Changed?.Invoke(); Save(); }
        public string Bond => Current.affection >= 80 ? "最亲密的伙伴" : Current.affection >= 50 ? "信任的朋友" : "初见的温柔";
        void Save()
        {
            if (repository == null || State == null) return;
            if (!repository.Save(State)) Message?.Invoke("存档暂时未能保存，请检查设备存储空间。");
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
