using System.Collections.Generic;
using UnityEngine;

namespace LongTable.Game
{
    public enum CharacterStyle { Player, Marta, Pip, Tobias }

    /// <summary>Placeholder body for a character: walks, faces, bobs, fades in and out.</summary>
    public sealed class CharacterView : MonoBehaviour
    {
        public string Id { get; private set; }
        public float Speed = 2.4f;
        public bool Instant;

        public float X => transform.position.x;
        public bool Arrived => Mathf.Abs(X - targetX) < 0.02f;
        public bool Visible => targetAlpha > 0.5f;
        public bool Kneading { get; set; }

        private readonly List<Tinted> parts = new List<Tinted>();
        private Transform visual;
        private float targetX;
        private float alpha = 1f, targetAlpha = 1f;
        private bool hideOnArrive;
        private float walkClock;
        private float facing = 1f;

        public static CharacterView Create(Transform parent, WorldLook look, string id, CharacterStyle style,
            Color body, Color accent, float height, float width, int order)
        {
            var go = new GameObject($"Character {id}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CharacterView>();
            view.Id = id;

            view.visual = new GameObject("Visual").transform;
            view.visual.SetParent(go.transform, false);

            float bodyH = height * 0.68f;
            float head = width * 0.78f;
            var skin = ProceduralArt.Hex(style == CharacterStyle.Tobias ? "#C99A7A" : "#E2B897");
            var dark = ProceduralArt.Hex("#2B2422");

            view.Part(look, "Shadow", ProceduralArt.Circle, new Vector2(0f, 0.02f), new Vector2(width * 1.3f, 0.16f),
                new Color(0f, 0f, 0f, 0.28f), order - 1, TintMode.Fixed);
            view.Part(look, "Body", ProceduralArt.Body, Vector2.zero, new Vector2(width, bodyH / 2f), body, order);
            view.Part(look, "Head", ProceduralArt.Circle, new Vector2(0f, bodyH + head * 0.38f), new Vector2(head, head), skin, order + 1);
            view.Part(look, "Eye", ProceduralArt.Circle, new Vector2(head * 0.22f, bodyH + head * 0.45f), new Vector2(0.07f, 0.07f), dark, order + 2);

            switch (style)
            {
                case CharacterStyle.Marta:
                    view.Part(look, "Bun", ProceduralArt.Circle, new Vector2(-head * 0.32f, bodyH + head * 0.78f), new Vector2(head * 0.5f, head * 0.5f), ProceduralArt.Hex("#B9B4AE"), order);
                    view.Part(look, "Apron", ProceduralArt.RectBottom, new Vector2(width * 0.08f, bodyH * 0.08f), new Vector2(width * 0.62f, bodyH * 0.62f), accent, order + 1);
                    view.Part(look, "Shawl", ProceduralArt.Triangle, new Vector2(0f, bodyH * 0.62f), new Vector2(width * 1.05f, bodyH * 0.4f), ProceduralArt.Hex("#6E5470"), order + 2);
                    break;
                case CharacterStyle.Pip:
                    view.Part(look, "Cap", ProceduralArt.Dome, new Vector2(0f, bodyH + head * 0.55f), new Vector2(head * 1.08f, head * 1.0f), accent, order + 2);
                    view.Part(look, "Brim", ProceduralArt.RectBottom, new Vector2(head * 0.35f, bodyH + head * 0.52f), new Vector2(head * 0.55f, 0.05f), accent, order + 2);
                    break;
                case CharacterStyle.Tobias:
                    view.Part(look, "Apron", ProceduralArt.RectBottom, new Vector2(width * 0.05f, bodyH * 0.05f), new Vector2(width * 0.7f, bodyH * 0.8f), accent, order + 1);
                    view.Part(look, "Beard", ProceduralArt.Dome, new Vector2(head * 0.08f, bodyH + head * 0.02f), new Vector2(head * 0.9f, -head * 0.75f), ProceduralArt.Hex("#4A3A30"), order + 2);
                    break;
                case CharacterStyle.Player:
                    view.Part(look, "Strap", ProceduralArt.Rect, new Vector2(0f, bodyH * 0.6f), new Vector2(0.07f, bodyH * 0.75f), ProceduralArt.Hex("#5A3D28"), order + 1, rotation: 32f);
                    view.Part(look, "Satchel", ProceduralArt.RectBottom, new Vector2(-width * 0.42f, bodyH * 0.28f), new Vector2(width * 0.42f, bodyH * 0.26f), accent, order + 2);
                    view.Part(look, "Hair", ProceduralArt.Dome, new Vector2(-head * 0.04f, bodyH + head * 0.5f), new Vector2(head * 1.04f, head * 0.9f), ProceduralArt.Hex("#5B3A26"), order + 2);
                    break;
            }

            return view;
        }

        private void Part(WorldLook look, string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order,
            TintMode mode = TintMode.Lit, float rotation = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(visual, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var t = go.AddComponent<Tinted>();
            t.baseColor = color;
            t.mode = mode;
            parts.Add(t);
            look.Register(t);
        }

        public void Place(float x)
        {
            transform.position = new Vector3(x, 0f, 0f);
            targetX = x;
            if (hideOnArrive) SetVisible(false);
        }

        /// <summary>Walk to x. With hideWhenThere the character fades out on arrival (goes indoors, goes home).</summary>
        public void WalkTo(float x, bool hideWhenThere = false)
        {
            targetX = x;
            hideOnArrive = hideWhenThere;
            if (!hideWhenThere && !Visible) SetVisible(true);
            if (Instant) Place(x);
        }

        public void SetVisible(bool visible, bool instant = false)
        {
            targetAlpha = visible ? 1f : 0f;
            hideOnArrive = false;
            if (instant || Instant) { alpha = targetAlpha; ApplyAlpha(); }
        }

        public void Face(float direction)
        {
            if (Mathf.Abs(direction) > 0.01f) facing = Mathf.Sign(direction);
        }

        private void Update()
        {
            float dx = targetX - X;
            bool walking = Mathf.Abs(dx) > 0.02f;
            if (walking)
            {
                Face(dx);
                float step = Mathf.Min(Mathf.Abs(dx), Speed * Time.deltaTime);
                transform.position += new Vector3(Mathf.Sign(dx) * step, 0f, 0f);
                walkClock += Time.deltaTime * 9f;
            }
            else if (hideOnArrive)
            {
                targetAlpha = 0f;
                hideOnArrive = false;
            }

            float bob = walking ? Mathf.Abs(Mathf.Sin(walkClock)) * 0.06f
                      : Kneading ? Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.05f
                      : Mathf.Sin(Time.time * 1.6f + Id.GetHashCode() % 7) * 0.008f;
            visual.localPosition = new Vector3(0f, bob, 0f);
            visual.localScale = new Vector3(facing, 1f, 1f);

            if (!Mathf.Approximately(alpha, targetAlpha))
            {
                alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * 1.8f);
                ApplyAlpha();
            }
        }

        private void ApplyAlpha()
        {
            foreach (var p in parts) p.alphaScale = alpha;
        }
    }
}
