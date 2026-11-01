using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LongTable.Game
{
    /// <summary>Screen text and panels, built in code with uGUI. Keyboard driven, no clicks needed.</summary>
    public sealed class SliceHud : MonoBehaviour
    {
        private const int MaxChoices = 8;

        private Font font;
        private Text status, warmthLabel, prompt, speaker, body, hint, choiceTitle, logText, summaryText;
        private Image warmthFill, fade;
        private GameObject dialoguePanel, choicePanel, logPanel, summaryPanel, warmthPanel;
        private readonly Text[] choiceLines = new Text[MaxChoices];

        public bool LogOpen => logPanel.activeSelf;
        public bool WarmthVisible => warmthPanel.activeSelf;

        public float FadeAlpha
        {
            get => fade.color.a;
            set { var c = fade.color; c.a = Mathf.Clamp01(value); fade.color = c; fade.enabled = c.a > 0.001f; }
        }

        public static SliceHud Create(Transform parent, Camera cam)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            // Camera space so captures and screenshots include the UI.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 500;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = go.AddComponent<SliceHud>();
            hud.Build(go.transform);
            return hud;
        }

        private void Build(Transform root)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var ink = new Color(0.08f, 0.07f, 0.09f, 0.78f);

            // Top left: where you are in the day.
            var statusPanel = Panel(root, "Status", new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(560, 132), ink);
            status = Label(statusPanel.transform, "Status text", 26, TextAnchor.UpperLeft, new Vector2(18, 12));

            // Top right: the visible warmth value. A development readout; the shipped game shows warmth only through the world.
            warmthPanel = Panel(root, "Warmth", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(380, 118), ink);
            warmthLabel = Label(warmthPanel.transform, "Warmth text", 26, TextAnchor.UpperLeft, new Vector2(18, 12));
            var track = Panel(warmthPanel.transform, "Track", new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 44), new Vector2(344, 14), new Color(1, 1, 1, 0.12f));
            warmthFill = Panel(track.transform, "Fill", new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(344, 14), new Color(1f, 0.72f, 0.38f, 0.95f)).GetComponent<Image>();
            warmthFill.rectTransform.pivot = new Vector2(0, 0);
            var devNote = Label(warmthPanel.transform, "Dev note", 17, TextAnchor.LowerLeft, new Vector2(18, 12));
            devNote.text = "dev readout, hide with F1";
            devNote.color = new Color(1, 1, 1, 0.5f);

            // Bottom: prompt line (shares the strip with dialogue and choices, never shown with them).
            var promptPanel = Panel(root, "Prompt", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1600, 90), new Color(0, 0, 0, 0));
            prompt = Label(promptPanel.transform, "Prompt text", 28, TextAnchor.LowerCenter, Vector2.zero);
            prompt.GetComponent<Outline>().effectDistance = new Vector2(2, -2);

            dialoguePanel = Panel(root, "Dialogue", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 200), ink);
            speaker = Label(dialoguePanel.transform, "Speaker", 28, TextAnchor.UpperLeft, new Vector2(28, 16));
            speaker.fontStyle = FontStyle.Bold;
            body = Label(dialoguePanel.transform, "Line", 30, TextAnchor.UpperLeft, new Vector2(28, 56));
            hint = Label(dialoguePanel.transform, "Continue", 20, TextAnchor.LowerRight, new Vector2(28, 14));
            hint.text = "[Space] continue";
            hint.color = new Color(1, 1, 1, 0.55f);

            choicePanel = Panel(root, "Choices", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 420), ink);
            choiceTitle = Label(choicePanel.transform, "Choice title", 28, TextAnchor.UpperLeft, new Vector2(28, 16));
            choiceTitle.fontStyle = FontStyle.Bold;
            for (int i = 0; i < MaxChoices; i++)
            {
                choiceLines[i] = Label(choicePanel.transform, $"Choice {i + 1}", 27, TextAnchor.UpperLeft, new Vector2(40, 0));
                var rt = choiceLines[i].rectTransform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(40, -(64 + i * 42) - 40);
                rt.offsetMax = new Vector2(-40, -(64 + i * 42));
            }

            // Right side: the event log (Tab).
            logPanel = Panel(root, "Event log", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -150), new Vector2(760, 640), ink);
            logText = Label(logPanel.transform, "Log text", 20, TextAnchor.UpperLeft, new Vector2(18, 14));
            logPanel.SetActive(false);

            // Centre: the end of day debrief.
            summaryPanel = Panel(root, "Debrief", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1240, 720), new Color(0.06f, 0.05f, 0.07f, 0.9f));
            summaryPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            summaryText = Label(summaryPanel.transform, "Debrief text", 24, TextAnchor.UpperLeft, new Vector2(40, 32));
            summaryPanel.SetActive(false);

            // Full-screen fade for the night passing.
            var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fadeGo.transform.SetParent(root, false);
            var frt = fadeGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            fade = fadeGo.GetComponent<Image>();
            fade.color = new Color(0.03f, 0.03f, 0.05f, 0f);
            fade.raycastTarget = false;
            fade.enabled = false;

            HideDialogue();
            HideChoices();
        }

        public void SetStatus(string text) => status.text = text;

        public void SetWarmth(float warmth, string tier, float fill01)
        {
            warmthLabel.text = $"Warmth <b>{warmth:0}</b>   ·   {tier}";
            warmthFill.rectTransform.sizeDelta = new Vector2(344f * Mathf.Clamp01(fill01), 14f);
        }

        public void SetPrompt(string text) =>
            prompt.text = dialoguePanel.activeSelf || choicePanel.activeSelf || summaryPanel.activeSelf ? "" : text;

        public void ShowLine(string who, Color whoColor, string text, bool showHint)
        {
            dialoguePanel.SetActive(true);
            speaker.text = who;
            speaker.color = whoColor;
            body.text = text;
            body.fontStyle = string.IsNullOrEmpty(who) ? FontStyle.Italic : FontStyle.Normal;
            hint.enabled = showHint;
        }

        public void HideDialogue() => dialoguePanel.SetActive(false);

        public void ShowChoices(string title, IList<string> lines, IList<bool> enabled)
        {
            choicePanel.SetActive(true);
            choiceTitle.text = title;
            for (int i = 0; i < MaxChoices; i++)
            {
                bool used = i < lines.Count;
                choiceLines[i].gameObject.SetActive(used);
                if (!used) continue;
                choiceLines[i].text = lines[i];
                choiceLines[i].color = enabled[i] ? Color.white : new Color(1, 1, 1, 0.38f);
            }
            var rt = choicePanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1400, 90 + lines.Count * 42);
        }

        public void HideChoices() => choicePanel.SetActive(false);

        public void ToggleLog() => logPanel.SetActive(!logPanel.activeSelf);
        public void SetLog(string text) => logText.text = text;

        public void ToggleWarmth() => warmthPanel.SetActive(!warmthPanel.activeSelf);

        public void ShowSummary(string text)
        {
            summaryText.text = text;
            summaryPanel.SetActive(true);
        }

        public void HideSummary() => summaryPanel.SetActive(false);

        // ---------- helpers ----------

        private static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private Text Label(Transform parent, string name, int size, TextAnchor anchor, Vector2 inset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(inset.x, inset.y);
            rt.offsetMax = new Vector2(-inset.x, -inset.y);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            var o = go.GetComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.5f);
            o.effectDistance = new Vector2(1, -1);
            return t;
        }
    }
}
