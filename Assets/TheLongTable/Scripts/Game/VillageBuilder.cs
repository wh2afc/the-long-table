using System.Collections.Generic;
using LongTable.Core;
using UnityEngine;

namespace LongTable.Game
{
    /// <summary>Everything the director needs to move and light.</summary>
    public sealed class VillageWorld
    {
        public Transform Root;
        public WorldLook Look;
        public CharacterView Player;
        public readonly Dictionary<string, CharacterView> Villagers = new Dictionary<string, CharacterView>();
        public OvenView Oven;
        public readonly List<Tinted> Lanterns = new List<Tinted>();
        public int LanternCount => Lanterns.Count / 2;
        public float TableX;
        public float PlayerSeatX;
        public float MorningPlayerX = -6.2f;
        public float MorningCameraX = -7.5f;

        public void SetLanterns(int lit)
        {
            // Lanterns holds a glow and a flame per lantern.
            for (int i = 0; i < Lanterns.Count; i++) Lanterns[i].strength = i / 2 < lit ? 1f : 0f;
        }

        public void ResetPositions(SliceData data)
        {
            Player.Place(data.Config.playerStartX);
            Player.SetVisible(true, true);
            Player.Face(-1f);
            foreach (var v in data.Villagers)
            {
                var view = Villagers[v.id];
                view.Kneading = false;
                view.Place(v.homeX);
                view.SetVisible(true, true);
            }
            Villagers["pip"].Face(1f);
            Villagers["marta"].Face(1f);
            Villagers["tobias"].Face(-1f);
        }
    }

    /// <summary>Builds the village green from placeholder shapes, one parallax layer at a time.</summary>
    public static class VillageBuilder
    {
        // Parallax follow values: 1 = moves with the camera, 0 = playfield, negative = foreground.
        private const float SkyFollow = 1f;
        private const float FarMountains = 0.9f;
        private const float NearMountains = 0.78f;
        private const float Hills = 0.55f;
        private const float Trees = 0.38f;
        private const float Village = 0.18f;
        private const float Foreground = -0.35f;

        public static VillageWorld Build(Transform parent, SliceData data, Camera cam)
        {
            var root = new GameObject("Village Green").transform;
            root.SetParent(parent, false);
            var look = root.gameObject.AddComponent<WorldLook>();
            var world = new VillageWorld { Root = root, Look = look, TableX = 0f, PlayerSeatX = 0f };

            BuildSky(world, cam);
            BuildDistance(world, cam);
            BuildVillageBackdrop(world, cam);
            BuildGround(world);
            BuildBakery(world);
            BuildForge(world);
            BuildTable(world);
            BuildForeground(world, cam);
            BuildCharacters(world, data);

            look.Snap();
            return world;
        }

        // ---------- layers ----------

        private static void BuildSky(VillageWorld w, Camera cam)
        {
            var layer = Layer(w, "Sky", SkyFollow, cam);
            Shape(w, layer, "Sky low", ProceduralArt.RectBottom, new Vector2(0f, -4f), new Vector2(60f, 16f), Color.white, -200, TintMode.SkyBottom);
            Shape(w, layer, "Sky high", ProceduralArt.SkyFade, new Vector2(0f, 1.8f), new Vector2(60f, 9f), Color.white, -199, TintMode.SkyTop);
            var sun = Shape(w, layer, "Low sun", ProceduralArt.Glow, new Vector2(5.5f, 1.4f), new Vector2(7f, 7f), ProceduralArt.Hex("#FFD7A0"), -195, TintMode.Emissive);
            sun.emissiveFloor = 0.2f;
            sun.strength = 0.8f;
        }

        private static void BuildDistance(VillageWorld w, Camera cam)
        {
            var far = Layer(w, "Far mountains", FarMountains, cam);
            Shape(w, far, "Range", ProceduralArt.Ridge(11, 70f, 5.2f, 1f, 0.3f), new Vector2(0f, -0.3f), Vector2.one, ProceduralArt.Hex("#7F8DA6"), -180, haze: 0.62f);

            var near = Layer(w, "Near mountains", NearMountains, cam);
            Shape(w, near, "Range", ProceduralArt.Ridge(23, 64f, 3.6f, 1.2f, 0.22f), new Vector2(0f, -0.3f), Vector2.one, ProceduralArt.Hex("#6D7E7A"), -170, haze: 0.42f);

            var hills = Layer(w, "Fields and hills", Hills, cam);
            Shape(w, hills, "Hills", ProceduralArt.Ridge(37, 56f, 2.1f, 0.4f, 0.4f), new Vector2(0f, -0.4f), Vector2.one, ProceduralArt.Hex("#7F8B5A"), -160, haze: 0.24f);
            for (int i = 0; i < 9; i++)
            {
                float x = -22f + i * 5.3f;
                Shape(w, hills, "Fallow field", ProceduralArt.Rect, new Vector2(x, 0.55f + (i % 3) * 0.12f), new Vector2(3.6f, 0.12f), ProceduralArt.Hex("#8E7B5C"), -159, haze: 0.24f);
            }

            var trees = Layer(w, "Trees", Trees, cam);
            var rng = new System.Random(5);
            for (float x = -26f; x < 26f; x += 2.2f + (float)rng.NextDouble() * 2.4f)
            {
                float s = 0.8f + (float)rng.NextDouble() * 0.7f;
                Shape(w, trees, "Trunk", ProceduralArt.RectBottom, new Vector2(x, -0.1f), new Vector2(0.18f * s, 1.1f * s), ProceduralArt.Hex("#4B3E33"), -151, haze: 0.14f);
                Shape(w, trees, "Canopy", ProceduralArt.Circle, new Vector2(x, 1.35f * s), new Vector2(1.5f * s, 1.3f * s), ProceduralArt.Hex("#4E6A45"), -150, haze: 0.14f);
                Shape(w, trees, "Canopy", ProceduralArt.Circle, new Vector2(x + 0.35f * s, 1.0f * s), new Vector2(1.1f * s, 1.0f * s), ProceduralArt.Hex("#577550"), -150, haze: 0.14f);
            }
        }

        private static void BuildVillageBackdrop(VillageWorld w, Camera cam)
        {
            var layer = Layer(w, "Village backdrop", Village, cam);
            float[] xs = { -7.5f, -3.2f, 3.6f, 7.8f };
            for (int i = 0; i < xs.Length; i++)
            {
                float s = 0.62f + (i % 2) * 0.08f;
                Cottage(w, layer, xs[i], s, -140);
            }

            // The chapel, quiet and nearly empty.
            Shape(w, layer, "Chapel", ProceduralArt.RectBottom, new Vector2(-13f, 0f), new Vector2(2.4f, 2.4f), ProceduralArt.Hex("#A9A398"), -141, haze: 0.08f);
            Shape(w, layer, "Chapel roof", ProceduralArt.Triangle, new Vector2(-13f, 2.4f), new Vector2(2.9f, 1.3f), ProceduralArt.Hex("#6A5D57"), -140, haze: 0.08f);
            Shape(w, layer, "Spire", ProceduralArt.Triangle, new Vector2(-13.8f, 3.1f), new Vector2(0.6f, 1.9f), ProceduralArt.Hex("#6A5D57"), -139, haze: 0.08f);
            Window(w, layer, new Vector2(-13f, 1.4f), new Vector2(0.4f, 0.7f), -139, 0.2f);

            // The half-collapsed mill.
            Shape(w, layer, "Mill", ProceduralArt.RectBottom, new Vector2(13.5f, 0f), new Vector2(2.2f, 3f), ProceduralArt.Hex("#958E84"), -141, haze: 0.08f);
            Shape(w, layer, "Mill roof", ProceduralArt.Triangle, new Vector2(13.2f, 3f), new Vector2(2.6f, 1.1f), ProceduralArt.Hex("#5F534D"), -140, haze: 0.08f, rotation: 9f);
            Shape(w, layer, "Mill wheel", ProceduralArt.Circle, new Vector2(15f, 1f), new Vector2(1.8f, 1.8f), ProceduralArt.Hex("#5D4C3E"), -142, haze: 0.08f);
        }

        private static void BuildGround(VillageWorld w)
        {
            var layer = Layer(w, "Green", 0f, null);
            Shape(w, layer, "Earth", ProceduralArt.RectBottom, new Vector2(0f, -8f), new Vector2(90f, 8f), ProceduralArt.Hex("#57623F"), -100);
            Shape(w, layer, "Path", ProceduralArt.RectBottom, new Vector2(0f, -0.5f), new Vector2(90f, 0.5f), ProceduralArt.Hex("#776651"), -99);
            Shape(w, layer, "Mud", ProceduralArt.Circle, new Vector2(-0.4f, -0.3f), new Vector2(8f, 0.6f), ProceduralArt.Hex("#6A5844"), -98);
            var rng = new System.Random(9);
            for (float x = -30f; x < 30f; x += 0.6f + (float)rng.NextDouble() * 1.4f)
            {
                float s = 0.15f + (float)rng.NextDouble() * 0.2f;
                Shape(w, layer, "Tuft", ProceduralArt.Triangle, new Vector2(x, -0.62f), new Vector2(s, s * 1.4f), ProceduralArt.Hex("#6C7C47"), -97);
            }
        }

        private static void BuildBakery(VillageWorld w)
        {
            var layer = Layer(w, "Bakery", 0f, null);
            var stone = ProceduralArt.Hex("#A29B8F");
            Shape(w, layer, "Chimney", ProceduralArt.RectBottom, new Vector2(-13.7f, 3.4f), new Vector2(0.55f, 1.6f), ProceduralArt.Hex("#887D73"), -42);
            Shape(w, layer, "Walls", ProceduralArt.RectBottom, new Vector2(-12.4f, 0f), new Vector2(5f, 3.2f), stone, -41);
            Shape(w, layer, "Roof", ProceduralArt.Triangle, new Vector2(-12.4f, 3.15f), new Vector2(5.9f, 1.8f), ProceduralArt.Hex("#6D5A51"), -40);
            Shape(w, layer, "Door", ProceduralArt.RectBottom, new Vector2(-10.9f, 0f), new Vector2(0.95f, 1.75f), ProceduralArt.Hex("#5B4232"), -39);
            Shape(w, layer, "Step", ProceduralArt.RectBottom, new Vector2(-10.6f, -0.15f), new Vector2(1.6f, 0.2f), ProceduralArt.Hex("#8C857B"), -38);
            Shape(w, layer, "Window frame", ProceduralArt.Rect, new Vector2(-13.3f, 1.75f), new Vector2(1.25f, 1.0f), ProceduralArt.Hex("#3C3430"), -39);
            var window = Shape(w, layer, "Window light", ProceduralArt.Rect, new Vector2(-13.3f, 1.75f), new Vector2(1.05f, 0.8f), ProceduralArt.Hex("#FFC66E"), -38, TintMode.Emissive);
            window.emissiveFloor = 0.6f;
            Shape(w, layer, "Sign", ProceduralArt.Rect, new Vector2(-11.6f, 2.55f), new Vector2(1.2f, 0.32f), ProceduralArt.Hex("#7A5B3F"), -38);

            // The oven, built against the outside wall.
            Shape(w, layer, "Oven", ProceduralArt.Dome, new Vector2(-8.5f, -0.05f), new Vector2(2.1f, 2.7f), ProceduralArt.Hex("#9B5F45"), -36);
            Shape(w, layer, "Oven mouth", ProceduralArt.Dome, new Vector2(-8.5f, -0.05f), new Vector2(0.95f, 1.15f), ProceduralArt.Hex("#2A1D18"), -35);
            var mouth = Shape(w, layer, "Oven fire", ProceduralArt.Dome, new Vector2(-8.5f, -0.05f), new Vector2(0.85f, 0.95f), ProceduralArt.Hex("#FF8A30"), -34, TintMode.Emissive);
            mouth.emissiveFloor = 0.9f;
            var halo = Shape(w, layer, "Oven light", ProceduralArt.Glow, new Vector2(-8.5f, 0.5f), new Vector2(5f, 4f), ProceduralArt.Hex("#FFAD62"), 40, TintMode.Emissive);
            halo.emissiveFloor = 0.75f;
            halo.baseColor.a = 0.55f;

            var oven = layer.gameObject.AddComponent<OvenView>();
            oven.MouthGlow = mouth;
            oven.Halo = halo;
            oven.WindowGlow = window;
            oven.ChimneyTop = new Vector2(-13.7f, 5.0f);
            oven.KneadingX = -9.6f;

            oven.KneadingTable.Add(Shape(w, layer, "Kneading table", ProceduralArt.RectBottom, new Vector2(-9.95f, 0f), new Vector2(1.1f, 0.8f), ProceduralArt.Hex("#7A5A40"), 9));
            oven.KneadingTable.Add(Shape(w, layer, "Dough", ProceduralArt.Dome, new Vector2(-9.95f, 0.8f), new Vector2(0.6f, 0.4f), ProceduralArt.Hex("#EBDCC0"), 10));

            for (int i = 0; i < 3; i++)
                oven.Loaves.Add(Shape(w, layer, "Loaf", ProceduralArt.Dome, new Vector2(-11.85f + i * 0.42f, 0.0f), new Vector2(0.4f, 0.5f), ProceduralArt.Hex("#C68B4B"), 9));

            for (int i = 0; i < 6; i++)
            {
                var puff = Shape(w, layer, "Smoke", ProceduralArt.Circle, oven.ChimneyTop, Vector2.one * 0.5f, ProceduralArt.Hex("#D8D4CE"), -43);
                puff.alphaScale = 0f;
                oven.Smoke.Add(puff);
            }

            w.Oven = oven;
        }

        private static void BuildForge(VillageWorld w)
        {
            var layer = Layer(w, "Forge", 0f, null);
            Shape(w, layer, "Back wall", ProceduralArt.RectBottom, new Vector2(12.1f, 0f), new Vector2(4.6f, 2.9f), ProceduralArt.Hex("#6B6159"), -45);
            Shape(w, layer, "Post", ProceduralArt.RectBottom, new Vector2(9.8f, 0f), new Vector2(0.26f, 3.1f), ProceduralArt.Hex("#4D3E34"), -40);
            Shape(w, layer, "Post", ProceduralArt.RectBottom, new Vector2(14.4f, 0f), new Vector2(0.26f, 3.3f), ProceduralArt.Hex("#4D3E34"), -40);
            Shape(w, layer, "Roof", ProceduralArt.Rect, new Vector2(12.1f, 3.25f), new Vector2(5.6f, 0.4f), ProceduralArt.Hex("#5A4C45"), -39, rotation: -3f);
            for (int i = 0; i < 6; i++)
            {
                float x = 12.6f + i * 0.28f;
                Shape(w, layer, "Hoe handle", ProceduralArt.RectBottom, new Vector2(x, 0.6f), new Vector2(0.06f, 1.5f), ProceduralArt.Hex("#8A7254"), -44);
                Shape(w, layer, "Hoe head", ProceduralArt.Rect, new Vector2(x + 0.06f, 2.1f), new Vector2(0.22f, 0.09f), ProceduralArt.Hex("#9EA3A8"), -43);
            }
            Shape(w, layer, "Hearth", ProceduralArt.RectBottom, new Vector2(11.5f, 0f), new Vector2(1.3f, 0.9f), ProceduralArt.Hex("#4A403A"), -38);
            var coals = Shape(w, layer, "Coals", ProceduralArt.Dome, new Vector2(11.5f, 0.88f), new Vector2(1f, 0.5f), ProceduralArt.Hex("#FF6A2A"), -37, TintMode.Emissive);
            coals.emissiveFloor = 0.8f;
            var glow = Shape(w, layer, "Forge light", ProceduralArt.Glow, new Vector2(11.5f, 1.2f), new Vector2(3.2f, 3f), ProceduralArt.Hex("#FF8A45"), 40, TintMode.Emissive);
            glow.emissiveFloor = 0.5f;
            glow.baseColor.a = 0.4f;
            Shape(w, layer, "Anvil base", ProceduralArt.RectBottom, new Vector2(9.6f, 0f), new Vector2(0.4f, 0.5f), ProceduralArt.Hex("#3A3A3F"), 9);
            Shape(w, layer, "Anvil", ProceduralArt.Rect, new Vector2(9.6f, 0.58f), new Vector2(0.9f, 0.18f), ProceduralArt.Hex("#45464C"), 9);
        }

        private static void BuildTable(VillageWorld w)
        {
            var layer = Layer(w, "The table", 0f, null);
            var wood = ProceduralArt.Hex("#8B694B");
            Shape(w, layer, "Trestle", ProceduralArt.RectBottom, new Vector2(-2.3f, 0f), new Vector2(0.18f, 0.7f), ProceduralArt.Hex("#5E4634"), 10);
            Shape(w, layer, "Trestle", ProceduralArt.RectBottom, new Vector2(2.3f, 0f), new Vector2(0.18f, 0.7f), ProceduralArt.Hex("#5E4634"), 10);
            // Three planks long.
            for (int i = 0; i < 3; i++)
                Shape(w, layer, "Plank", ProceduralArt.RectBottom, new Vector2(-1.84f + i * 1.84f, 0.7f), new Vector2(1.8f, 0.15f),
                    Color.Lerp(wood, ProceduralArt.Hex("#7A5A3E"), i * 0.4f), 11);

            // The empty chair.
            Shape(w, layer, "Empty stool", ProceduralArt.RectBottom, new Vector2(-2.95f, 0.42f), new Vector2(0.55f, 0.09f), wood, 11);
            Shape(w, layer, "Stool leg", ProceduralArt.RectBottom, new Vector2(-3.15f, 0f), new Vector2(0.07f, 0.42f), ProceduralArt.Hex("#5E4634"), 10);
            Shape(w, layer, "Stool leg", ProceduralArt.RectBottom, new Vector2(-2.75f, 0f), new Vector2(0.07f, 0.42f), ProceduralArt.Hex("#5E4634"), 10);

            // A lantern string over the table. How many are lit depends on the village.
            var dark = ProceduralArt.Hex("#3A2F29");
            Shape(w, layer, "Lantern pole", ProceduralArt.RectBottom, new Vector2(-3.4f, 0f), new Vector2(0.12f, 2.9f), dark, -20);
            Shape(w, layer, "Lantern pole", ProceduralArt.RectBottom, new Vector2(3.4f, 0f), new Vector2(0.12f, 2.9f), dark, -20);
            Shape(w, layer, "Lantern string", ProceduralArt.Rect, new Vector2(0f, 2.8f), new Vector2(6.8f, 0.03f), dark, -19);
            for (int i = 0; i < 5; i++)
            {
                float x = -2.4f + i * 1.2f;
                float y = 2.55f - Mathf.Abs(i - 2) * -0.05f;
                Shape(w, layer, "Lantern", ProceduralArt.Rect, new Vector2(x, y), new Vector2(0.2f, 0.28f), dark, -18);
                var core = Shape(w, layer, "Lantern flame", ProceduralArt.Circle, new Vector2(x, y), new Vector2(0.14f, 0.18f), ProceduralArt.Hex("#FFE3A6"), -17, TintMode.Emissive);
                var glow = Shape(w, layer, "Lantern light", ProceduralArt.Glow, new Vector2(x, y - 0.2f), new Vector2(2.2f, 2.2f), ProceduralArt.Hex("#FFC271"), 41, TintMode.Emissive);
                glow.baseColor.a = 0.5f;
                core.strength = glow.strength = 0f;
                w.Lanterns.Add(glow);
                w.Lanterns.Add(core);
            }
        }

        private static void BuildForeground(VillageWorld w, Camera cam)
        {
            var layer = Layer(w, "Foreground", Foreground, cam);
            var wood = ProceduralArt.Hex("#46382E");
            for (float x = -34f; x < 34f; x += 1.7f)
                Shape(w, layer, "Fence post", ProceduralArt.RectBottom, new Vector2(x, -2.4f), new Vector2(0.16f, 1.25f), wood, 30);
            Shape(w, layer, "Rail", ProceduralArt.Rect, new Vector2(0f, -1.55f), new Vector2(70f, 0.08f), wood, 30);
            Shape(w, layer, "Rail", ProceduralArt.Rect, new Vector2(0f, -1.9f), new Vector2(70f, 0.08f), wood, 30);

            // A laundry line, the sign that someone still lives here.
            Shape(w, layer, "Line pole", ProceduralArt.RectBottom, new Vector2(-6.6f, -2.4f), new Vector2(0.1f, 2.5f), wood, 31);
            Shape(w, layer, "Line pole", ProceduralArt.RectBottom, new Vector2(-2.4f, -2.4f), new Vector2(0.1f, 2.5f), wood, 31);
            Shape(w, layer, "Line", ProceduralArt.Rect, new Vector2(-4.5f, 0.0f), new Vector2(4.2f, 0.025f), wood, 31);
            string[] cloth = { "#CBC3B4", "#8FA4B2", "#B9907B", "#D8D0BE" };
            for (int i = 0; i < 4; i++)
                Shape(w, layer, "Washing", ProceduralArt.RectBottom, new Vector2(-6.0f + i * 1.0f, -0.75f + (i % 2) * 0.1f),
                    new Vector2(0.62f, 0.7f - (i % 2) * 0.12f), ProceduralArt.Hex(cloth[i]), 32);

            var rng = new System.Random(21);
            for (float x = -34f; x < 34f; x += 0.4f + (float)rng.NextDouble() * 0.8f)
            {
                float s = 0.3f + (float)rng.NextDouble() * 0.35f;
                Shape(w, layer, "Tall grass", ProceduralArt.Triangle, new Vector2(x, -2.45f), new Vector2(s * 0.6f, s * 1.6f), ProceduralArt.Hex("#47553A"), 33);
            }
        }

        private static void BuildCharacters(VillageWorld w, SliceData data)
        {
            var layer = new GameObject("Characters").transform;
            layer.SetParent(w.Root, false);

            w.Player = CharacterView.Create(layer, w.Look, "player", CharacterStyle.Player,
                ProceduralArt.Hex("#B5835A"), ProceduralArt.Hex("#7B5235"), 1.7f, 0.68f, 6);

            int order = 1;
            foreach (var v in data.Villagers)
            {
                var style = v.id switch
                {
                    "marta" => CharacterStyle.Marta,
                    "pip" => CharacterStyle.Pip,
                    "tobias" => CharacterStyle.Tobias,
                    _ => CharacterStyle.Player,
                };
                w.Villagers[v.id] = CharacterView.Create(layer, w.Look, v.id, style,
                    ProceduralArt.Hex(v.bodyColor), ProceduralArt.Hex(v.accentColor), v.height, v.width, order);
            }

            w.ResetPositions(data);
        }

        // ---------- helpers ----------

        private static void Cottage(VillageWorld w, Transform layer, float x, float s, int order)
        {
            Shape(w, layer, "Cottage", ProceduralArt.RectBottom, new Vector2(x, 0f), new Vector2(2.6f * s, 2f * s), ProceduralArt.Hex("#9D978D"), order, haze: 0.08f);
            Shape(w, layer, "Cottage roof", ProceduralArt.Triangle, new Vector2(x, 2f * s), new Vector2(3.1f * s, 1.3f * s), ProceduralArt.Hex("#6A5C55"), order + 1, haze: 0.08f);
            Shape(w, layer, "Cottage door", ProceduralArt.RectBottom, new Vector2(x + 0.6f * s, 0f), new Vector2(0.5f * s, 1f * s), ProceduralArt.Hex("#54443A"), order + 1, haze: 0.08f);
            Window(w, layer, new Vector2(x - 0.55f * s, 1.15f * s), new Vector2(0.55f * s, 0.45f * s), order + 2, 0f);
        }

        private static void Window(VillageWorld w, Transform layer, Vector2 pos, Vector2 size, int order, float floor)
        {
            Shape(w, layer, "Window", ProceduralArt.Rect, pos, size, ProceduralArt.Hex("#3D3633"), order, haze: 0.08f);
            var light = Shape(w, layer, "Window light", ProceduralArt.Rect, pos, size * 0.8f, ProceduralArt.Hex("#FFC774"), order + 1, TintMode.Emissive);
            light.emissiveFloor = floor;
        }

        private static Transform Layer(VillageWorld w, string name, float follow, Camera cam)
        {
            var t = new GameObject(name).transform;
            t.SetParent(w.Root, false);
            if (cam != null && !Mathf.Approximately(follow, 0f))
            {
                var p = t.gameObject.AddComponent<ParallaxLayer>();
                p.follow = follow;
                p.cameraTransform = cam.transform;
            }
            return t;
        }

        private static Tinted Shape(VillageWorld w, Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 scale,
            Color color, int order, TintMode mode = TintMode.Lit, float haze = 0f, float rotation = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var t = go.AddComponent<Tinted>();
            t.baseColor = color;
            t.mode = mode;
            t.haze = haze;
            w.Look.Register(t);
            return t;
        }
    }
}
