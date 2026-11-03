using UnityEngine;

namespace SpatialCatCafe
{
    public sealed class CafeWorld : MonoBehaviour
    {
        public CatActor[] Cats { get; private set; }
        public Transform Focus { get; private set; }
        Light sun;
        Light[] lamps;
        SceneShapes s;
        CafeSession session;
        bool evening;
        Color wood = SceneShapes.Hex("#B98D66"), darkWood = SceneShapes.Hex("#795C47"),
            cream = SceneShapes.Hex("#F1E6D0"), sage = SceneShapes.Hex("#8AAB96");

        public void Construct(SceneShapes shapes, CafeSession model)
        {
            s = shapes; session = model;
            Focus = s.Group("Orbit focus", transform, new Vector3(0, .65f, 0));
            s.Box("Diorama foundation", transform, new Vector3(0, -.20f, .2f), new Vector3(8.8f, .36f, 6.8f), darkWood);
            for (int row = 0; row < 18; row++)
            {
                float z = -2.97f + row * .36f;
                for (int col = 0; col < 4; col++)
                    s.Box("Oak floor board", transform, new Vector3(-3.26f + col * 2.17f, -.008f, z), new Vector3(2.15f, .08f, .345f), Color.Lerp(wood, cream, ((row * 3 + col * 7) % 6) * .035f));
            }
            s.Box("Back plaster", transform, new Vector3(0, 1.8f, 3.43f), new Vector3(8.8f, 3.6f, .15f), cream);
            s.Box("Left plaster", transform, new Vector3(-4.35f, 1.8f, .2f), new Vector3(.15f, 3.6f, 6.6f), SceneShapes.Hex("#DCE3CF"));
            s.Box("Back skirting", transform, new Vector3(0, .14f, 3.32f), new Vector3(8.6f, .26f, .08f), wood);
            s.Box("Left skirting", transform, new Vector3(-4.24f, .14f, .2f), new Vector3(.08f, .26f, 6.4f), wood);
            Window(new Vector3(-4.23f, 2.1f, .7f));
            // Large woven rug grounds the cats without a visible grid.
            s.Cylinder("Woven rug border", transform, new Vector3(.2f, .045f, -.5f), 2.1f, .025f, SceneShapes.Hex("#C5AD83"));
            s.Cylinder("Woven rug", transform, new Vector3(.2f, .06f, -.5f), 1.98f, .025f, SceneShapes.Hex("#E3D2AE"));
            for (int i = 0; i < 4; i++)
                s.Cylinder("Rug stitch", transform, new Vector3(.2f, .075f + i * .0005f, -.5f), 1.82f - i * .28f, .001f, Color.Lerp(SceneShapes.Hex("#D9C49E"), SceneShapes.Hex("#E3D2AE"), i % 2));
            Sofa(); Counter(); CatTree();
            Table(new Vector3(2.9f, 0, -.5f));
            Plant(new Vector3(-3.5f, 0, -2.1f), 1);
            Plant(new Vector3(3.65f, 0, 2.8f), .85f);
            Plant(new Vector3(-2.7f, 1.9f, 3.03f), .27f);
            s.Box("Floating shelf", transform, new Vector3(-2.45f, 1.87f, 3.12f), new Vector3(2.3f, .12f, .45f), wood);
            for (int i = 0; i < 5; i++)
                s.Box("Book", transform, new Vector3(-2.15f + i * .12f, 2.08f, 3.10f), new Vector3(.09f, .31f + (i % 2) * .08f, .21f), i % 2 == 0 ? sage : SceneShapes.Hex("#C58E79"));
            // Cafe emblem: cup + two ears, created from original primitives.
            var emblem = s.Group("Cafe wall emblem", transform, new Vector3(.25f, 2.3f, 3.28f));
            s.Box("Emblem backing", emblem, Vector3.zero, new Vector3(1.8f, .95f, .06f), sage);
            s.Cylinder("Cup emblem", emblem, new Vector3(0, -.02f, -.08f), .20f, .33f, cream);
            s.Ear(emblem, new Vector3(-.14f, .10f, -.08f), cream).localScale = Vector3.one * .55f;
            s.Ear(emblem, new Vector3(.14f, .10f, -.08f), cream).localScale = Vector3.one * .55f;
            s.Ball("Emblem handle", emblem, new Vector3(.23f, 0, -.08f), new Vector3(.19f, .20f, .08f), cream);
            s.Ball("Emblem handle cutout", emblem, new Vector3(.24f, 0, -.13f), new Vector3(.10f, .11f, .04f), sage);
            for (int i = 0; i < 2; i++)
            {
                float x = -1.0f + i * 3.3f;
                s.Link("Pendant cord", transform, new Vector3(x, 3.6f, 1.0f), new Vector3(x, 2.9f, 1.0f), .012f, darkWood);
                s.Ball("Pendant shade", transform, new Vector3(x, 2.87f, 1), new Vector3(.65f, .25f, .65f), SceneShapes.Hex("#D9B470"));
            }
            lamps = new Light[2];
            for (int i = 0; i < lamps.Length; i++)
            {
                var lamp = s.Group("Warm pendant light", transform, new Vector3(-1 + i * 3.3f, 2.7f, 1));
                lamps[i] = lamp.gameObject.AddComponent<Light>(); lamps[i].type = LightType.Point;
                lamps[i].color = SceneShapes.Hex("#FFD8A0"); lamps[i].range = 5; lamps[i].shadows = LightShadows.None;
                lamps[i].renderMode = LightRenderMode.ForceVertex;
            }
            sun = s.Group("Afternoon sun", transform, Vector3.zero).gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .45f; sun.shadowBias = .06f;
            // Only the completed room is batched; the animated cats are added afterwards.
            StaticBatchingUtility.Combine(gameObject);
            Cats = new CatActor[3];
            var positions = new[] { new Vector3(0, .10f, -1.0f), new Vector3(-1.5f, .10f, .25f), new Vector3(1.25f, .10f, .65f) };
            for (int i = 0; i < Cats.Length; i++)
            {
                var root = s.Group(CatCatalog.All[i].Name, transform, positions[i]);
                root.localRotation = Quaternion.Euler(0, i == 1 ? -18 : i == 2 ? 15 : 0, 0);
                Cats[i] = root.gameObject.AddComponent<CatActor>(); Cats[i].Construct(s, i);
            }
            session.Changed += Refresh; session.Acted += OnActed;
            evening = !session.State.evening; Refresh();
        }

        void Window(Vector3 position)
        {
            var root = s.Group("Window", transform, position);
            s.Box("Window sky", root, Vector3.zero, new Vector3(.045f, 1.8f, 2.6f), SceneShapes.Hex("#C5DAD4"));
            for (int edge = -1; edge <= 1; edge += 2)
            {
                s.Box("Vertical window frame", root, new Vector3(.035f, 0, edge * 1.3f), new Vector3(.10f, 2, .10f), wood);
                s.Box("Horizontal window frame", root, new Vector3(.035f, edge * .95f, 0), new Vector3(.10f, .10f, 2.7f), wood);
            }
            s.Box("Window mullion", root, new Vector3(.06f, 0, 0), new Vector3(.10f, 1.8f, .07f), wood);
            s.Box("Window crossbar", root, new Vector3(.06f, 0, 0), new Vector3(.10f, .06f, 2.6f), wood);
            s.Box("Window sill", root, new Vector3(.12f, -1.02f, 0), new Vector3(.38f, .12f, 2.9f), wood);
        }
        void Sofa()
        {
            var root = s.Group("Sage sofa", transform, new Vector3(-2.9f, 0, 1.6f));
            s.Box("Sofa legs", root, new Vector3(0, .17f, 0), new Vector3(1.5f, .30f, 1), darkWood);
            s.Box("Sofa seat", root, new Vector3(0, .43f, -.05f), new Vector3(1.8f, .35f, 1.15f), sage);
            s.Box("Sofa back", root, new Vector3(0, .95f, .45f), new Vector3(1.8f, 1.1f, .30f), sage);
            for (int side = -1; side <= 1; side += 2)
            {
                s.Box("Sofa arm", root, new Vector3(side * .88f, .66f, 0), new Vector3(.27f, .65f, 1.2f), sage);
                s.Ball("Cushion", root, new Vector3(side * .47f, .83f, .2f), new Vector3(.53f, .48f, .20f), side < 0 ? cream : SceneShapes.Hex("#D1A08C"));
            }
        }
        void Counter()
        {
            var root = s.Group("Coffee counter", transform, new Vector3(2.7f, 0, 2.45f));
            s.Box("Counter", root, new Vector3(0, .55f, 0), new Vector3(2.05f, 1.1f, 1), sage);
            for (int i = 0; i < 10; i++) s.Box("Counter fluting", root, new Vector3(-.9f + i * .2f, .55f, -.51f), new Vector3(.055f, .95f, .04f), SceneShapes.Hex("#769982"));
            s.Box("Oak countertop", root, new Vector3(0, 1.14f, 0), new Vector3(2.25f, .14f, 1.15f), wood);
            s.Box("Espresso machine", root, new Vector3(.45f, 1.45f, .08f), new Vector3(.72f, .51f, .54f), SceneShapes.Hex("#ECE7DA"));
            s.Box("Machine face", root, new Vector3(.45f, 1.43f, -.20f), new Vector3(.58f, .23f, .04f), darkWood);
            Cup(root, new Vector3(-.6f, 1.23f, -.12f)); Cup(root, new Vector3(-.28f, 1.23f, -.22f));
        }
        void Cup(Transform p, Vector3 position)
        {
            var root = s.Group("Coffee cup", p, position);
            s.Cylinder("Saucer", root, Vector3.zero, .14f, .025f, cream);
            s.Cylinder("Cup", root, new Vector3(0, .09f, 0), .09f, .17f, cream);
            s.Cylinder("Coffee", root, new Vector3(0, .18f, 0), .074f, .006f, darkWood);
            s.Ball("Cup handle", root, new Vector3(.095f, .09f, 0), new Vector3(.1f, .09f, .06f), cream);
        }
        void Table(Vector3 position)
        {
            var root = s.Group("Round cafe table", transform, position);
            s.Cylinder("Table pedestal", root, new Vector3(0, .43f, 0), .09f, .8f, darkWood);
            s.Cylinder("Table foot", root, new Vector3(0, .08f, 0), .36f, .08f, darkWood);
            s.Cylinder("Tabletop", root, new Vector3(0, .89f, 0), .68f, .10f, wood);
            Cup(root, new Vector3(-.18f, .96f, 0));
            Plant(position + new Vector3(.2f, .96f, .15f), .23f);
            for (int side = -1; side <= 1; side += 2)
            {
                var stool = s.Group("Stool", root, new Vector3(side * .95f, 0, -.05f));
                s.Cylinder("Stool seat", stool, new Vector3(0, .52f, 0), .31f, .13f, sage);
                for (int leg = 0; leg < 3; leg++)
                {
                    float angle = leg * Mathf.PI * 2 / 3;
                    s.Link("Stool leg", stool, new Vector3(Mathf.Cos(angle) * .24f, 0, Mathf.Sin(angle) * .24f), new Vector3(Mathf.Cos(angle) * .18f, .5f, Mathf.Sin(angle) * .18f), .035f, wood);
                }
            }
        }
        void CatTree()
        {
            var root = s.Group("Cat climbing tree", transform, new Vector3(-.65f, 0, 2.1f));
            s.Cylinder("Tree base", root, new Vector3(0, .07f, 0), .65f, .14f, wood);
            s.Cylinder("Sisal post", root, new Vector3(0, .65f, 0), .12f, 1.2f, SceneShapes.Hex("#D1BC92"));
            for (int i = 0; i < 14; i++) s.Cylinder("Sisal rope", root, new Vector3(0, .15f + i * .07f, 0), .127f, .015f, SceneShapes.Hex("#BAA57D"));
            s.Cylinder("Perch", root, new Vector3(0, 1.3f, 0), .5f, .12f, cream);
            s.Cylinder("Lower perch", root, new Vector3(.4f, .62f, -.05f), .35f, .10f, cream);
        }
        void Plant(Vector3 position, float scale)
        {
            var root = s.Group("Leafy plant", transform, position); root.localScale = Vector3.one * scale;
            s.Cylinder("Terracotta pot", root, new Vector3(0, .23f, 0), .24f, .46f, SceneShapes.Hex("#C18D72"));
            s.Cylinder("Soil", root, new Vector3(0, .47f, 0), .21f, .02f, darkWood);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 2.4f;
                var end = new Vector3(Mathf.Cos(angle) * .30f, .75f + (i % 3) * .19f, Mathf.Sin(angle) * .30f);
                s.Link("Stem", root, new Vector3(0, .46f, 0), end, .014f, sage);
                var leaf = s.Ball("Leaf", root, end, new Vector3(.17f, .42f, .10f), i % 2 == 0 ? SceneShapes.Hex("#61876A") : sage);
                leaf.localRotation = Quaternion.Euler(30, angle * Mathf.Rad2Deg, 30);
            }
        }
        void OnActed(CatAction action, int index) { Cats[index].React(action); }
        void Refresh()
        {
            for (int i = 0; i < Cats.Length; i++) Cats[i].SetSelected(i == session.State.selectedCat);
            if (evening == session.State.evening) return;
            evening = session.State.evening;
            sun.color = SceneShapes.Hex(evening ? "#A0B9DB" : "#FFE1B2"); sun.intensity = evening ? .35f : 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = SceneShapes.Hex(evening ? "#6B7191" : "#C5C5B4");
            foreach (var lamp in lamps) lamp.intensity = evening ? 1.6f : .5f;
            if (Camera.main) Camera.main.backgroundColor = SceneShapes.Hex(evening ? "#303D48" : "#E5E7DD");
        }
        void OnDestroy() { if (session) { session.Changed -= Refresh; session.Acted -= OnActed; } }
    }
}
