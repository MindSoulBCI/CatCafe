using System.Collections.Generic;
using UnityEngine;

namespace SpatialCatCafe
{
    public sealed class SceneShapes : MonoBehaviour
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Texture2D goldenFurTexture;
        Material goldenFurMaterial;

        public Material GoldenFurMaterial
        {
            get
            {
                if (goldenFurMaterial) return goldenFurMaterial;
                var shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                if (!shader) shader = Shader.Find("Standard");
                goldenFurMaterial = new Material(shader) { name = "Golden Shaded Fur", color = Color.white };
                goldenFurTexture = CreateGoldenFurTexture();
                if (goldenFurMaterial.HasProperty("_BaseMap")) goldenFurMaterial.SetTexture("_BaseMap", goldenFurTexture);
                if (goldenFurMaterial.HasProperty("_MainTex")) goldenFurMaterial.SetTexture("_MainTex", goldenFurTexture);
                if (goldenFurMaterial.HasProperty("_Smoothness")) goldenFurMaterial.SetFloat("_Smoothness", .08f);
                if (goldenFurMaterial.HasProperty("_Glossiness")) goldenFurMaterial.SetFloat("_Glossiness", .08f);
                return goldenFurMaterial;
            }
        }

        Texture2D CreateGoldenFurTexture()
        {
            const int width = 512, height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, true)
            {
                name = "Golden shaded coat grain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            Color cream = Hex("#F3E5CE");
            Color paleGold = Hex("#DDC08C");
            Color gold = Hex("#B68142");
            Color shaded = Hex("#72502E");
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                Color baseColor = v < .32f ? Color.Lerp(cream, paleGold, v / .32f)
                    : v < .78f ? Color.Lerp(paleGold, gold, (v - .32f) / .46f)
                    : Color.Lerp(gold, shaded, (v - .78f) / .22f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float softGrain = Mathf.PerlinNoise(u * 92f, v * 36f) - .5f;
                    float fineGrain = Mathf.PerlinNoise(u * 210f, v * 18f) - .5f;
                    float strand = Mathf.Sin((u * 470f + Mathf.PerlinNoise(u * 12f, v * 8f) * 5f) * Mathf.PI * 2f);
                    float variation = softGrain * .075f + fineGrain * .03f + strand * .009f;
                    Color pixel = baseColor * (1f + variation);
                    if (v > .68f && softGrain > .08f)
                        pixel = Color.Lerp(pixel, shaded, Mathf.InverseLerp(.68f, .96f, v) * .12f);
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply(true, true);
            return texture;
        }
        public Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var existing)) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null || shader == null)
                shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("猫咖 Shader 未打入构建，请运行 Cat Cafe/1. Configure project。");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .12f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .12f);
            materials.Add(color, mat); return mat;
        }
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
        public Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider)
            {
                var hit = go.GetComponent<Collider>();
                if (hit) { hit.enabled = false; Destroy(hit); }
            }
            return go.transform;
        }
        public Transform Box(string name, Transform p, Vector3 v, Vector3 s, Color c) => Shape(name, PrimitiveType.Cube, p, v, s, c);
        public Transform Ball(string name, Transform p, Vector3 v, Vector3 s, Color c) => Shape(name, PrimitiveType.Sphere, p, v, s, c);
        public Transform FurBall(string name, Transform p, Vector3 v, Vector3 s, Color c)
        {
            var shape = Ball(name, p, v, s, c);
            shape.GetComponent<Renderer>().sharedMaterial = GoldenFurMaterial;
            return shape;
        }
        public Transform Cylinder(string name, Transform p, Vector3 v, float radius, float height, Color c) => Shape(name, PrimitiveType.Cylinder, p, v, new Vector3(radius * 2, height / 2, radius * 2), c);
        public Transform Group(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        public Transform Ear(Transform parent, Vector3 position, Color color)
        {
            var go = new GameObject("Triangular ear"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var mesh = new Mesh { name = "Cat ear" };
            mesh.vertices = new[] { new Vector3(-.14f, 0, -.08f), new Vector3(.14f, 0, -.08f), new Vector3(0, .28f, 0), new Vector3(-.14f, 0, .09f), new Vector3(.14f, 0, .09f) };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 2, 0, 3, 2, 1, 2, 4, 0, 1, 4, 0, 4, 3 };
            mesh.RecalculateNormals(); meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
            return go.transform;
        }
        public void Link(string name, Transform parent, Vector3 a, Vector3 b, float radius, Color color)
        {
            var shape = Cylinder(name, parent, (a + b) / 2, radius, Vector3.Distance(a, b), color);
            shape.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }
        void OnDestroy()
        {
            foreach (var mat in materials.Values) Destroy(mat);
            if (goldenFurMaterial) Destroy(goldenFurMaterial);
            if (goldenFurTexture) Destroy(goldenFurTexture);
            foreach (var mesh in meshes) Destroy(mesh);
        }
    }
}
