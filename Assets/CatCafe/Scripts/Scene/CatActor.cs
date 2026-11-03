using UnityEngine;
using System.Linq;

namespace SpatialCatCafe
{
    public sealed class CatActor : MonoBehaviour
    {
        public int Index { get; private set; }
        Transform body, head, tail, eyes, toy, food, heart, tongue;
        Transform[] paws;
        Vector3[] pawRest;
        Renderer[] importedEyeRenderers;
        Transform[] importedPaws;
        Vector3[] importedPawRest;
        Animator importedAnimator;
        Transform importedHead;
        Quaternion importedHeadRest, importedBodyRest;
        NaturalCatAction naturalAction;
        Vector3 handTarget;
        public Vector3 NosePosition => head ? head.TransformPoint(new Vector3(0, -.08f, -.35f))
            : importedModel && importedHead ? importedHead.position + importedHead.forward * .14f : transform.position;
        public Vector3 HeadPosition => head ? head.position
            : importedModel && importedHead ? importedHead.position : transform.position;
        float started = -100;
        CatAction action;
        bool selected, walking, importedModel, catMatchModel;

        public void SetWalking(bool value) => walking = value;

        public bool IsPettingContact(Vector3 worldPosition, float tolerance)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            if (importedModel)
                return NearEllipsoid(local, new Vector3(0, .27f, 0), new Vector3(.25f, .23f, .34f), tolerance)
                    || NearEllipsoid(local, new Vector3(0, .52f, -.27f), new Vector3(.19f, .17f, .18f), tolerance);
            return NearEllipsoid(local, new Vector3(0, .44f, 0), new Vector3(.36f, .39f, .425f), tolerance)
                || NearEllipsoid(local, new Vector3(0, .93f, -.14f), new Vector3(.42f, .335f, .32f), tolerance);
        }

        static bool NearEllipsoid(Vector3 point, Vector3 center, Vector3 radii, float tolerance)
        {
            Vector3 normalized = new Vector3((point.x - center.x) / radii.x,
                (point.y - center.y) / radii.y, (point.z - center.z) / radii.z);
            float approximateSurfaceDistance = Mathf.Abs(normalized.magnitude - 1) * Mathf.Min(radii.x, radii.y, radii.z);
            return approximateSurfaceDistance <= tolerance;
        }

        public void Construct(SceneShapes s, int index)
        {
            Index = index; var profile = CatCatalog.All[index];
            // The single MR companion uses a consistent golden-shaded British shorthair coat,
            // regardless of which saved companion profile was selected in the earlier cafe UI.
            bool golden = true;
            var cream = SceneShapes.Hex("#F5E6CF"); var dark = SceneShapes.Hex("#34312C");
            var furAccent = golden ? SceneShapes.Hex("#A97030") : profile.Accent;
            var importedPrefab = Resources.Load<GameObject>("Cats/CatMatchCat");
            if (!importedPrefab) importedPrefab = Resources.Load<GameObject>("Cats/GoldenCat");
            var goldenCoatTexture = Resources.Load<Texture2D>("Cats/GoldenCoatAlbedo");
            if (importedPrefab)
            {
                importedModel = true;
                body = Instantiate(importedPrefab, transform).transform;
                body.name = importedPrefab.name.Contains("CatMatch") ? "Cat-Match-AR low-poly cat" : "Golden shorthair mesh";
                body.localPosition = Vector3.zero;
                // Cat-Match-AR's rig faces +Z in model space; turn it into the app's -Z forward axis.
                catMatchModel = importedPrefab.name.Contains("CatMatch");
                body.localRotation = Quaternion.Euler(0, catMatchModel ? 180 : 90, 0);
                importedBodyRest = body.localRotation;
                // Keep the source rig's internal scale, while resizing the whole cat
                // to the cafe's roughly 0.4 m companion footprint.
                // Keep the source rig's internal scale, while resizing the whole cat
                // to the cafe's roughly 0.4 m companion footprint.
                body.localScale = Vector3.one * (catMatchModel ? .14f : 1.18f);
                importedAnimator = body.GetComponent<Animator>();
                if (!importedAnimator) importedAnimator = body.gameObject.AddComponent<Animator>();
                importedAnimator.applyRootMotion = false;
                importedHead = body.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Head");
                if (importedHead) importedHeadRest = importedHead.localRotation;
                foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
                {
                    string part = renderer.gameObject.name;
                    if (catMatchModel) continue;
                    if (part == "Cat continuous body" || part.Contains("anatomical skin")
                        || part.Contains("body", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Keep the imported mesh's shape and smooth normals, but give the
                        // continuous coat a warm golden-shaded British Shorthair palette.
                        var coat = renderer.material;
                        coat.color = SceneShapes.Hex("#F5E8D4");
                        if (goldenCoatTexture)
                        {
                            if (coat.HasProperty("_BaseMap")) coat.SetTexture("_BaseMap", goldenCoatTexture);
                            else if (coat.HasProperty("_MainTex")) coat.SetTexture("_MainTex", goldenCoatTexture);
                        }
                        if (coat.HasProperty("_Metallic")) coat.SetFloat("_Metallic", 0);
                        if (coat.HasProperty("_Glossiness")) coat.SetFloat("_Glossiness", .16f);
                        if (coat.HasProperty("_Smoothness")) coat.SetFloat("_Smoothness", .16f);
                    }
                    else if (part.StartsWith("Muzzle pad") || part.StartsWith("Whisker"))
                        renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = cream };
                }
                importedEyeRenderers = catMatchModel ? System.Array.Empty<Renderer>() : new Renderer[2];
                // Both source models use separate mesh objects for the eye details.
                // Cache their renderers by name so blink behavior survives model swaps.
                var eyeParts = new System.Collections.Generic.List<Renderer>();
                foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
                {
                    string name = renderer.gameObject.name;
                    if (!catMatchModel && (name.Contains("eye", System.StringComparison.OrdinalIgnoreCase)
                        || name.Contains("iris", System.StringComparison.OrdinalIgnoreCase)
                        || name.Contains("pupil", System.StringComparison.OrdinalIgnoreCase)))
                        eyeParts.Add(renderer);
                }
                importedEyeRenderers = eyeParts.ToArray();
                var pawsFound = new System.Collections.Generic.List<Transform>();
                foreach (var child in body.GetComponentsInChildren<Transform>(true))
                    if (!catMatchModel && child.name.StartsWith("Rounded paw", System.StringComparison.OrdinalIgnoreCase))
                        pawsFound.Add(child);
                importedPaws = pawsFound.ToArray();
                importedPawRest = new Vector3[importedPaws.Length];
                for (int i = 0; i < importedPaws.Length; i++) importedPawRest[i] = importedPaws[i].localPosition;
            }
            else
            {
            body = s.Group("Breathing body", transform, Vector3.zero);
            if (golden) s.FurBall("Plush golden body", body, new Vector3(0, .48f, .02f), new Vector3(.76f, .62f, 1.02f), profile.Coat);
            else s.Ball("Soft body", body, new Vector3(0, .48f, .02f), new Vector3(.76f, .62f, 1.02f), profile.Coat);
            s.Ball("Warm cream bib", body, new Vector3(0, .48f, -.36f), new Vector3(.37f, .49f, .23f), cream);
            paws = new Transform[4];
            pawRest = new Vector3[4];
            int pawIndex = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                paws[pawIndex++] = golden
                    ? s.FurBall("Rounded haunch", body, new Vector3(side * .25f, .21f, .20f), new Vector3(.29f, .31f, .35f), profile.Coat)
                    : s.Ball("Rounded haunch", body, new Vector3(side * .25f, .21f, .20f), new Vector3(.29f, .31f, .35f), profile.Coat);
                paws[pawIndex++] = s.Ball("Front paw", body, new Vector3(side * .19f, .10f, -.40f), new Vector3(.18f, .16f, .27f), cream);
            }
            for (int i = 0; i < paws.Length; i++) pawRest[i] = paws[i].localPosition;
            head = s.Group("Expressive head", body, new Vector3(0, .91f, -.20f));
            if (golden) s.FurBall("Rounded British shorthair head", head, Vector3.zero, new Vector3(.74f, .61f, .59f), profile.Coat);
            else s.Ball("Rounded head", head, Vector3.zero, new Vector3(.74f, .61f, .59f), profile.Coat);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform ear = golden
                    ? s.FurBall("Soft rounded ear", head, new Vector3(side * .27f, .24f, .015f), new Vector3(.22f, .31f, .15f), profile.Coat)
                    : s.Ear(head, new Vector3(side * .27f, .20f, .01f), profile.Coat);
                ear.localRotation = Quaternion.Euler(0, 0, side * -12);
                var inner = s.Ear(head, new Vector3(side * .27f, .24f, -.075f), SceneShapes.Hex("#C88D76"));
                inner.localScale = new Vector3(.50f, .55f, .48f);
                inner.localRotation = ear.localRotation;
                s.Ball("Subtle cream muzzle", head, new Vector3(side * .09f, -.13f, -.285f), new Vector3(.19f, .14f, .13f), cream);
                if (!golden) s.Ball("Blush", head, new Vector3(side * .28f, -.075f, -.24f), new Vector3(.12f, .06f, .04f), SceneShapes.Hex("#DDA18D"));
                for (int stripe = 0; stripe < (golden ? 1 : 2); stripe++)
                {
                    var mark = s.Ball("Soft cheek marking", head, new Vector3(side * .34f, .02f + stripe * .09f, -.17f), new Vector3(.075f, .018f, .13f), furAccent);
                    mark.localRotation = Quaternion.Euler(0, side * 23, side * -15);
                }
                for (int w = 0; w < 2; w++) s.Link("Whisker", head,
                    new Vector3(side * .16f, -.14f - w * .03f, -.33f),
                    new Vector3(side * .43f, -.10f - w * .075f, -.31f), .0035f, cream);
            }
            eyes = s.Group("Blink", head, new Vector3(0, .015f, -.31f));
            for (int side = -1; side <= 1; side += 2)
            {
                float eyeX = side * .18f;
                s.Ball("Olive green iris", eyes, new Vector3(eyeX, 0, 0), new Vector3(.082f, .102f, .042f), golden ? SceneShapes.Hex("#78804F") : dark);
                s.Ball("Vertical pupil", eyes, new Vector3(eyeX, 0, -.031f), new Vector3(.026f, .071f, .016f), dark);
                s.Ball("Small eye catchlight", eyes, new Vector3(eyeX - .018f, .028f, -.045f), new Vector3(.014f, .019f, .008f), Color.white);
            }
            s.Ball("Soft rose nose", head, new Vector3(0, -.08f, -.35f), new Vector3(.052f, .038f, .03f), SceneShapes.Hex("#AD786F"));
            s.Link("Mouth", head, new Vector3(0, -.105f, -.35f), new Vector3(0, -.15f, -.34f), .005f, dark);
            tongue = s.Ball("Tongue", head, new Vector3(0, -.14f, -.37f), new Vector3(.05f, .014f, .075f), SceneShapes.Hex("#C98784"));
            tongue.gameObject.SetActive(false);
            tail = s.Group("Wagging tail", body, new Vector3(.28f, .27f, .31f));
            Vector3 last = Vector3.zero;
            for (int i = 1; i <= 10; i++)
            {
                var next = new Vector3(Mathf.Sin(i * .22f) * .32f, i * .085f, Mathf.Sin(i * .14f) * .25f);
                float radius = .052f - i * .0023f;
                s.Link("Tail segment", tail, last, next, radius, golden ? furAccent : profile.Coat);
                s.Ball("Tail joint", tail, next, Vector3.one * radius * 1.8f, golden ? furAccent : profile.Coat); last = next;
            }
            }
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = importedModel ? new Vector3(0, .40f, 0) : new Vector3(0, .57f, 0);
            collider.size = importedModel ? new Vector3(.55f, .78f, .90f) : new Vector3(.98f, 1.35f, 1.32f);
            toy = s.Ball("Play ball", transform, new Vector3(.75f, .13f, -.7f), Vector3.one * .25f, SceneShapes.Hex("#C68D81")); toy.gameObject.SetActive(false);
            food = s.Group("Fish treat", transform, new Vector3(0, .14f, -.7f));
            s.Ball("Fish", food, Vector3.zero, new Vector3(.24f, .08f, .10f), SceneShapes.Hex("#BE8957"));
            s.Ear(food, new Vector3(.14f, -.03f, 0), SceneShapes.Hex("#BE8957")).localScale = Vector3.one * .35f;
            food.gameObject.SetActive(false);
            heart = s.Group("Affection heart", transform, new Vector3(0, 1.65f, 0));
            var rose = SceneShapes.Hex("#D9898A");
            s.Ball("Heart lobe", heart, new Vector3(-.06f, .03f, 0), Vector3.one * .16f, rose);
            s.Ball("Heart lobe", heart, new Vector3(.06f, .03f, 0), Vector3.one * .16f, rose);
            s.Box("Heart tip", heart, new Vector3(0, -.03f, 0), new Vector3(.14f, .14f, .10f), rose).localRotation = Quaternion.Euler(0, 0, 45);
            heart.gameObject.SetActive(false);
        }

        public void SetSelected(bool value) { selected = value; }
        public void React(CatAction value) { action = value; started = Time.time; }
        public void SetNaturalPose(NaturalCatAction value, Vector3 worldHandPosition)
        { naturalAction = value; handTarget = worldHandPosition; }
        void Update()
        {
            float phase = Time.time * 1.8f + Index * 1.7f, elapsed = Time.time - started;
            if (importedModel)
            {
                float walkBob = walking ? Mathf.Abs(Mathf.Sin(Time.time * 9)) * .012f : 0;
                float petSway = naturalAction == NaturalCatAction.EnjoyPetting ? Mathf.Sin(Time.time * 6) * 2f : 0;
                body.localPosition = new Vector3(0, walkBob - (naturalAction == NaturalCatAction.EnjoyPetting ? .018f : 0), 0);
                body.localRotation = importedBodyRest * Quaternion.Euler(petSway, 0, naturalAction == NaturalCatAction.EnjoyPetting
                    ? Mathf.Sin(Time.time * 6) * 2.5f : 0);
                if (importedAnimator)
                {
                    importedAnimator.SetBool("Walking", walking);
                    if (action == CatAction.Play && elapsed < .25f) importedAnimator.SetTrigger("Play");
                }
                float blink = Mathf.Repeat(Time.time + Index * 1.31f, 4.1f);
                bool eyesClosed = naturalAction == NaturalCatAction.EnjoyPetting || blink < .15f;
                if (importedEyeRenderers != null)
                    foreach (var eye in importedEyeRenderers) if (eye) eye.enabled = !eyesClosed;
                if (importedPaws != null)
                {
                    for (int i = 0; i < importedPaws.Length; i++)
                    {
                        float phaseOffset = i == 0 || i == 3 ? 0 : Mathf.PI;
                        float stride = walking ? Mathf.Sin(Time.time * 10 + phaseOffset) : 0;
                        float lift = walking ? Mathf.Max(0, stride) * .035f : 0;
                        importedPaws[i].localPosition = importedPawRest[i]
                            + Vector3.up * lift + Vector3.back * (stride * .035f);
                    }
                }
            }
            else
            {
            body.localScale = new Vector3(1, 1 + Mathf.Sin(phase) * .012f, 1);
            if (naturalAction == NaturalCatAction.None)
                head.localRotation = Quaternion.Euler(Mathf.Sin(phase * .5f) * 3, Mathf.Sin(phase * .4f) * (selected ? 8 : 16), selected ? -4 : 0);
            tail.localRotation = Quaternion.Euler(walking ? Mathf.Sin(Time.time * 12) * 8 : 0,
                Mathf.Sin(phase * 1.5f) * (walking ? 34 : selected ? 22 : 10), 0);
            float blink = Mathf.Repeat(Time.time + Index * 1.31f, 4.1f);
            eyes.localScale = new Vector3(1, blink < .15f ? .12f : 1, 1);
            // Animate below the root so a tracked-space controller can own placement.
            float walkBob = walking ? Mathf.Abs(Mathf.Sin(Time.time * 12)) * .035f : 0;
            float petSway = naturalAction == NaturalCatAction.EnjoyPetting ? Mathf.Sin(Time.time * 8) * .025f : 0;
            body.localPosition = new Vector3(petSway, walkBob, 0);
            body.localRotation = Quaternion.Euler(0, 0, naturalAction == NaturalCatAction.EnjoyPetting
                ? Mathf.Sin(Time.time * 8) * 3 : 0);
            for (int i = 0; i < paws.Length; i++)
            {
                float stride = walking ? Mathf.Sin(Time.time * 11 + i * Mathf.PI * .5f) : 0;
                paws[i].localPosition = pawRest[i] + Vector3.up * (Mathf.Max(0, stride) * .055f);
                paws[i].localRotation = Quaternion.Euler(walking ? stride * 16 : 0, 0, 0);
            }
            }
            heart.gameObject.SetActive(elapsed < 1.8f && action != CatAction.Rest);
            heart.localPosition = new Vector3(0, 1.5f + elapsed * .15f, 0);
            heart.localScale = Vector3.one * Mathf.Clamp01(1.8f - elapsed);
            food.gameObject.SetActive(elapsed < 1.8f && action == CatAction.Feed);
            toy.gameObject.SetActive(elapsed < 2.8f && action == CatAction.Play);
            if (elapsed > 3 || naturalAction != NaturalCatAction.None) return;
            if (importedModel)
            {
                if (!importedHead) return;
                float nod = action == CatAction.Feed ? 18 + Mathf.Sin(elapsed * 14) * 5
                    : action == CatAction.Pet ? -10 : 0;
                importedHead.localRotation = Quaternion.Slerp(importedHead.localRotation,
                    importedHeadRest * Quaternion.Euler(nod, 0, 0), 1 - Mathf.Exp(-Time.deltaTime * 9));
                return;
            }
            switch (action)
            {
                case CatAction.Pet:
                    head.localRotation = Quaternion.Euler(-12, Mathf.Sin(elapsed * 5) * 10, -10); eyes.localScale = new Vector3(1, .15f, 1); break;
                case CatAction.Feed:
                    head.localRotation = Quaternion.Euler(23 + Mathf.Sin(elapsed * 14) * 6, 0, 0); break;
                case CatAction.Play:
                    toy.localPosition = new Vector3(Mathf.Sin(elapsed * 4) * .7f, .13f + Mathf.Abs(Mathf.Sin(elapsed * 6)) * .16f, -.7f);
                    body.localPosition = new Vector3(Mathf.Sin(elapsed * 4) * .13f, Mathf.Abs(Mathf.Sin(elapsed * 6)) * .16f, 0);
                    head.localRotation = Quaternion.Euler(13, -Mathf.Sin(elapsed * 4) * 18, 0); break;
                case CatAction.Rest:
                    body.localScale = new Vector3(1.1f, .72f, 1.1f); eyes.localScale = new Vector3(1, .1f, 1); break;
            }
        }

        void LateUpdate()
        {
            if (importedModel) return;
            if (!head || !tongue) return;
            tongue.gameObject.SetActive(naturalAction == NaturalCatAction.LickHand);
            if (naturalAction == NaturalCatAction.None) return;
            Vector3 direction = handTarget - head.position;
            if (direction.sqrMagnitude > .000001f)
            {
                Quaternion desired = Quaternion.Inverse(head.parent.rotation) * Quaternion.LookRotation(-direction, Vector3.up);
                var euler = desired.eulerAngles;
                desired = Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0, euler.x), -28, 35),
                    Mathf.Clamp(Mathf.DeltaAngle(0, euler.y), -55, 55), 0);
                head.localRotation = Quaternion.Slerp(head.localRotation, desired, 1 - Mathf.Exp(-Time.deltaTime * 12));
            }
            if (naturalAction == NaturalCatAction.LickHand)
            {
                float stroke = Mathf.Max(0, Mathf.Sin(Time.time * 15)) * .09f;
                tongue.localPosition = new Vector3(0, -.14f, -.39f - stroke);
                tongue.localScale = new Vector3(.075f, .018f, .10f + stroke);
                eyes.localScale = new Vector3(1, .25f, 1);
            }
            else if (naturalAction == NaturalCatAction.EnjoyPetting) eyes.localScale = new Vector3(1, .12f, 1);
        }
    }
}
