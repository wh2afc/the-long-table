using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LongTable.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LongTable.Game
{
    /// <summary>
    /// Runs one day on the green: Notice, Act, Gather, then the morning after at
    /// Marta's oven and a debrief. Input, scripted autoplay and tests all go
    /// through the same public methods.
    /// </summary>
    public sealed class SliceDirector : MonoBehaviour
    {
        private enum Mode { Busy, Roam, Choosing, Summary }

        private const float InteractRange = 1.9f;

        public bool Instant { get; private set; }
        public SliceSession Session { get; private set; }
        public MotiveReport Report => Session.Evaluate();
        public IReadOnlyList<string> Transcript => transcript;
        public VillageWorld World => world;
        public Camera Camera => cam;

        public bool IsIdle => mode == Mode.Roam && running == 0;
        public bool IsAwaitingGatherChoice => awaitingGather && mode == Mode.Choosing;
        public bool IsFinished => mode == Mode.Summary;
        public bool IsAutoplaying => autoplay != null;

        private SliceData data;
        private VillageWorld world;
        private SliceHud hud;
        private Camera cam;
        private PlayerController player;

        private Mode mode = Mode.Busy;
        private int running;
        private readonly List<string> transcript = new List<string>();

        private bool followPlayer = true;
        private float cameraTargetX;
        private float cameraVelocity;

        private bool waitingForContinue;
        private int lineShownFrame;
        private float autoContinueAt = -1f;

        private int choiceResult = int.MinValue;
        private List<bool> choiceEnabled;
        private bool choiceCancellable;
        private bool awaitingGather;

        private Coroutine autoplay;
        private float autoplayStepDelay;

        public void Init(SliceData sliceData, VillageWorld village, SliceHud screen, Camera camera, bool instant)
        {
            data = sliceData;
            world = village;
            hud = screen;
            cam = camera;
            SetInstant(instant);

            player = world.Player.gameObject.AddComponent<PlayerController>();
            player.minX = data.Config.worldMinX;
            player.maxX = data.Config.worldMaxX;

            ResetSlice();
        }

        public void SetInstant(bool instant)
        {
            Instant = instant;
            world.Look.Instant = instant;
            world.Oven.Instant = instant;
            world.Player.Instant = instant;
            foreach (var v in world.Villagers.Values) v.Instant = instant;
        }

        /// <summary>Starts the day over with an empty event log.</summary>
        public void ResetSlice()
        {
            StopAllCoroutines();
            autoplay = null;
            running = 0;
            awaitingGather = false;
            waitingForContinue = false;
            choiceResult = int.MinValue;
            transcript.Clear();

            Session = new SliceSession(data);
            Session.Log.Appended += _ => RefreshLog();

            world.ResetPositions(data);
            world.Oven.SetOutcome(null);
            world.SetLanterns(0);
            world.Look.TargetTime = 0.2f;
            world.Look.TargetPulse = 0f;
            world.Look.TargetWarmth01 = Warmth01(Report.Warmth);
            world.Look.Snap();

            player.enabled = true;
            followPlayer = true;
            SnapCamera(world.Player.X);

            hud.HideSummary();
            hud.HideChoices();
            hud.HideDialogue();
            hud.FadeAlpha = 0f;
            RefreshLog();
            RefreshHud();

            Run(Say("intro"));
        }

        // ---------- public actions (input, autoplay and tests) ----------

        public bool NoticeVillager(string villagerId)
        {
            if (!IsIdle || Session.Loop.Phase != DayPhase.Notice) return false;
            Run(NoticeRoutine(villagerId));
            return true;
        }

        public bool TalkTo(string villagerId)
        {
            if (!IsIdle || Session.Loop.Phase != DayPhase.Act) return false;
            Run(TalkRoutine(villagerId));
            return true;
        }

        /// <summary>Does an action directly, walking the player over first. Used by autoplay and tests.</summary>
        public bool PerformAction(string actionId)
        {
            if (!IsIdle || Session.Loop.Phase != DayPhase.Act) return false;
            var def = data.Action(actionId);
            if (Session.WhyNot(def) != null) return false;
            StandBeside(def.target);
            Run(PerformRoutine(def));
            return true;
        }

        public bool AdvancePhase()
        {
            if (!IsIdle) return false;
            switch (Session.Loop.Phase)
            {
                case DayPhase.Notice: Run(MiddayRoutine()); return true;
                case DayPhase.Act: Run(GatherRoutine()); return true;
                default: return false;
            }
        }

        public bool ChooseGather(bool tellThemWhatYouDid)
        {
            if (!IsAwaitingGatherChoice) return false;
            return Choose(tellThemWhatYouDid ? 0 : 1);
        }

        public bool Choose(int index)
        {
            if (mode != Mode.Choosing) return false;
            if (index < 0) { if (!choiceCancellable) return false; choiceResult = -1; return true; }
            if (index >= choiceEnabled.Count || !choiceEnabled[index]) return false;
            choiceResult = index;
            return true;
        }

        public void ContinueDialogue()
        {
            if (waitingForContinue && Time.frameCount > lineShownFrame) waitingForContinue = false;
        }

        /// <summary>Replays one of the reference plays from the start so a viewer can watch it.</summary>
        public void StartAutoplay(string playName, float stepDelay = 0.6f)
        {
            var steps = ScriptedPlays.ByName(playName);
            ResetSlice();
            autoplayStepDelay = stepDelay;
            autoplay = StartCoroutine(AutoplayRoutine(steps));
        }

        // ---------- the day ----------

        private void Run(IEnumerator routine) => StartCoroutine(Track(routine));

        private IEnumerator Track(IEnumerator routine)
        {
            running++;
            mode = Mode.Busy;
            try
            {
                yield return routine;
            }
            finally
            {
                running--;
                if (running == 0 && mode != Mode.Summary) mode = Mode.Roam;
                RefreshHud();
            }
        }

        private IEnumerator NoticeRoutine(string villagerId)
        {
            bool fresh = Session.Notice(villagerId);
            yield return Say(fresh ? $"notice:{villagerId}" : "notice:already");
        }

        private IEnumerator MiddayRoutine()
        {
            Session.AdvancePhase();
            world.Look.TargetTime = 1f;
            yield return Say("hint:act");
        }

        private IEnumerator TalkRoutine(string villagerId)
        {
            yield return Say($"talk:{villagerId}");

            var options = Session.OptionsFor(villagerId).ToList();
            var labels = new List<string>();
            var enabled = new List<bool>();
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                string cost = CostText(o.Def);
                labels.Add(o.Available
                    ? $"[{i + 1}]  {o.Def.label}   <color=#ffffff77>{cost}</color>"
                    : $"[{i + 1}]  {o.Def.label}   <color=#ffffff77>({o.Reason})</color>");
                enabled.Add(o.Available);
            }
            labels.Add($"[{options.Count + 1}]  Leave them be");
            enabled.Add(true);

            int picked = -1;
            yield return ChooseRoutine($"With {data.Villager(villagerId).displayName}", labels, enabled, true, i => picked = i);
            if (picked >= 0 && picked < options.Count) yield return PerformRoutine(options[picked].Def);
        }

        private IEnumerator PerformRoutine(ActionDef def)
        {
            var result = Session.Perform(def.id, WitnessesFor(def));
            if (!result.Performed) yield break;

            world.Villagers[def.target].Face(world.Player.X - world.Villagers[def.target].X);
            world.Look.TargetWarmth01 = Warmth01(Report.Warmth);
            RefreshHud();

            yield return Say((result.Rebuffed ? "rebuff:" : "act:") + def.id);
            if (Session.Loop.AttentionLeft == 0) yield return Say("hint:spent");
        }

        private IEnumerator GatherRoutine()
        {
            Session.AdvancePhase(); // Act -> Gather
            player.enabled = false;
            followPlayer = false;
            cameraTargetX = world.TableX;
            world.Look.TargetTime = 2.15f;

            var before = Report;
            world.Player.WalkTo(world.PlayerSeatX);
            foreach (var v in data.Villagers) ApplySeat(v, before.SeatOf(v.id));
            world.SetLanterns(LanternsFor(before));
            yield return WaitForWalkers(7f);
            world.Player.Face(-1f);

            yield return Say("gather:intro");
            foreach (var v in data.Villagers) yield return Say($"gather:{v.id}:{before.SeatOf(v.id)}");
            yield return Say("gather:prompt");

            bool canTell = Session.Log.Events.Any(e => e.IsPlayerAct && e.day == Session.Loop.Day);
            int picked = 1;
            awaitingGather = true;
            yield return ChooseRoutine("At the table",
                new List<string> { "[1]  Tell everyone what you did today", "[2]  Just eat, and listen" },
                new List<bool> { canTell, true }, false, i => picked = i);
            awaitingGather = false;

            if (picked == 0 && Session.Remind()) yield return Say("gather:remind");
            else yield return Say("gather:quiet");

            var after = Report;
            foreach (var v in data.Villagers)
            {
                if (after.SeatOf(v.id) >= before.SeatOf(v.id)) continue;
                yield return Say($"gather:{v.id}:leaves");
                ApplySeat(v, after.SeatOf(v.id));
            }
            world.SetLanterns(LanternsFor(after));
            world.Look.TargetWarmth01 = Warmth01(after.Warmth);
            world.Look.TargetTime = 2.7f;
            yield return Wait(2f);

            Session.AdvancePhase(); // Gather -> DayEnd (or the Feast at season's end)
            if (Session.Loop.Phase == DayPhase.Feast) Session.AdvancePhase();

            yield return MorningRoutine(after);
        }

        private IEnumerator MorningRoutine(MotiveReport report)
        {
            yield return FadeTo(1f, 1.4f);

            // The night passes.
            world.SetLanterns(0);
            world.Look.TargetTime = 0.1f;
            world.Look.TargetPulse = 0f;
            world.Look.Snap();
            world.ResetPositions(data);
            world.Oven.SetOutcome(report.Oven);

            var marta = world.Villagers[data.Rules.oven.owner];
            var martaDef = data.Villager(data.Rules.oven.owner);
            switch (report.Oven)
            {
                case OvenOutcome.Relit:
                    marta.Place(world.Oven.KneadingX);
                    marta.Kneading = true;
                    marta.Face(1f);
                    break;
                case OvenOutcome.Polite:
                    marta.Place(martaDef.apartX);
                    marta.Face(1f);
                    break;
                default:
                    marta.Place(martaDef.homeX);
                    marta.Face(1f);
                    break;
            }

            world.Player.Place(world.MorningPlayerX);
            world.Player.Face(-1f);
            followPlayer = false;
            cameraTargetX = world.MorningCameraX;
            SnapCamera(cameraTargetX);
            yield return Wait(0.6f);
            yield return FadeTo(0f, 1.6f);

            // "The whole screen warms."
            if (report.Oven == OvenOutcome.Relit) world.Look.TargetPulse = 1f;

            yield return Say($"morning:{report.Oven}");
            if (report.Oven == OvenOutcome.Polite) marta.SetVisible(false);
            yield return Wait(1.2f);

            hud.ShowSummary(Debrief(report));
            mode = Mode.Summary;
        }

        private IEnumerator AutoplayRoutine(PlayStep[] steps)
        {
            foreach (var step in steps)
            {
                // Wait until the director can take the next step.
                while (!(IsIdle || IsAwaitingGatherChoice || IsFinished)) yield return null;
                if (IsFinished) break;
                if (!Instant) yield return new WaitForSeconds(autoplayStepDelay);

                switch (step.Kind)
                {
                    case PlayStepKind.Notice:
                        StandBeside(step.Id);
                        NoticeVillager(step.Id);
                        break;
                    case PlayStepKind.Act:
                        if (!PerformAction(step.Id)) Debug.LogWarning($"Autoplay step refused: {step}");
                        break;
                    case PlayStepKind.Remind:
                        while (!IsAwaitingGatherChoice) yield return null;
                        ChooseGather(true);
                        break;
                    case PlayStepKind.Advance:
                        if (IsAwaitingGatherChoice) ChooseGather(false);
                        else if (Session.Loop.Phase == DayPhase.Notice || Session.Loop.Phase == DayPhase.Act) AdvancePhase();
                        break;
                }
                yield return null;
            }
            autoplay = null;
        }

        // ---------- dialogue and choices ----------

        private IEnumerator Say(string context)
        {
            var lines = data.Dialogue.Pick(context, Session);
            foreach (var line in lines)
            {
                transcript.Add(line.id);
                if (Instant) continue;

                hud.ShowLine(NameOf(line.who), ColorOf(line.who), line.text, autoplay == null);
                lineShownFrame = Time.frameCount;
                waitingForContinue = true;
                autoContinueAt = autoplay != null ? Time.time + 1.4f + line.text.Length * 0.035f : -1f;
                while (waitingForContinue)
                {
                    if (autoContinueAt > 0f && Time.time >= autoContinueAt) waitingForContinue = false;
                    yield return null;
                }
            }
            hud.HideDialogue();
        }

        private IEnumerator ChooseRoutine(string title, List<string> labels, List<bool> enabled, bool cancellable, Action<int> done)
        {
            choiceEnabled = enabled;
            choiceCancellable = cancellable;
            choiceResult = int.MinValue;
            if (!Instant) hud.ShowChoices(title, labels, enabled);
            mode = Mode.Choosing;
            while (choiceResult == int.MinValue) yield return null;
            hud.HideChoices();
            mode = Mode.Busy;
            int r = choiceResult;
            // The last option of a cancellable menu is "leave".
            if (cancellable && r == labels.Count - 1) r = -1;
            done(r);
        }

        private IEnumerator Wait(float seconds)
        {
            if (Instant) yield break;
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator WaitForWalkers(float timeout)
        {
            if (Instant) yield break;
            float end = Time.time + timeout;
            while (Time.time < end && (!world.Player.Arrived || world.Villagers.Values.Any(v => !v.Arrived)))
                yield return null;
        }

        private IEnumerator FadeTo(float target, float seconds)
        {
            if (Instant) { hud.FadeAlpha = target; yield break; }
            float start = hud.FadeAlpha;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                hud.FadeAlpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            hud.FadeAlpha = target;
        }

        // ---------- per-frame ----------

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null) HandleKeys(kb);
            RefreshHud();
        }

        private void LateUpdate()
        {
            float target = followPlayer ? world.Player.X : cameraTargetX;
            target = ClampCamera(target);
            var p = cam.transform.position;
            p.x = Instant ? target : Mathf.SmoothDamp(p.x, target, ref cameraVelocity, 0.45f);
            cam.transform.position = p;
        }

        private void HandleKeys(Keyboard kb)
        {
            if (kb.tabKey.wasPressedThisFrame) hud.ToggleLog();
            if (kb.f1Key.wasPressedThisFrame) hud.ToggleWarmth();
            if (kb.f5Key.wasPressedThisFrame) StartAutoplay("sincere");
            if (kb.f6Key.wasPressedThisFrame) StartAutoplay("transactional");

            if (waitingForContinue && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                ContinueDialogue();

            if (mode == Mode.Choosing)
            {
                for (int i = 0; i < 9; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame) Choose(i);
                if (kb.escapeKey.wasPressedThisFrame && choiceCancellable) Choose(-1);
            }
            else if (IsIdle && autoplay == null)
            {
                var near = Nearest();
                if (near != null && kb.eKey.wasPressedThisFrame)
                {
                    if (Session.Loop.Phase == DayPhase.Notice) NoticeVillager(near);
                    else TalkTo(near);
                }
                if (kb.nKey.wasPressedThisFrame) AdvancePhase();
            }
            else if (mode == Mode.Summary)
            {
                if (kb.rKey.wasPressedThisFrame) ResetSlice();
                if (kb.escapeKey.wasPressedThisFrame) Quit();
            }
        }

        // ---------- helpers ----------

        private string Nearest()
        {
            string best = null;
            float bestD = InteractRange;
            foreach (var kv in world.Villagers)
            {
                if (!kv.Value.Visible) continue;
                float d = Mathf.Abs(kv.Value.X - world.Player.X);
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            return best;
        }

        private void StandBeside(string villagerId)
        {
            var v = world.Villagers[villagerId];
            float side = v.X > data.Config.worldMaxX - 2f ? -1.2f : 1.2f;
            world.Player.Place(Mathf.Clamp(v.X + side, data.Config.worldMinX, data.Config.worldMaxX));
            world.Player.Face(v.X - world.Player.X);
        }

        private IReadOnlyList<string> WitnessesFor(ActionDef def)
        {
            if (def.Visibility == Visibility.Private) return Array.Empty<string>();
            float radius = data.Config.witnessRadius;
            return world.Villagers
                .Where(kv => kv.Key != def.target && kv.Value.Visible && Mathf.Abs(kv.Value.X - world.Player.X) <= radius)
                .Select(kv => kv.Key)
                .ToList();
        }

        private void ApplySeat(VillagerDef v, Seat seat)
        {
            var view = world.Villagers[v.id];
            switch (seat)
            {
                case Seat.Close: view.WalkTo(v.seatCloseX); break;
                case Seat.Edge: view.WalkTo(v.seatEdgeX); break;
                default: view.WalkTo(v.apartX, v.hiddenWhenApart); break;
            }
        }

        private int LanternsFor(MotiveReport r) =>
            Mathf.Clamp(1 + Mathf.RoundToInt(Warmth01(r.Warmth) * (world.LanternCount - 1)), 1, world.LanternCount);

        private float Warmth01(float warmth) =>
            Mathf.InverseLerp(data.Config.mutedLookAtWarmth, data.Config.fullLookAtWarmth, warmth);

        private float ClampCamera(float x)
        {
            float half = cam.orthographicSize * cam.aspect;
            float min = data.Config.worldMinX + half, max = data.Config.worldMaxX - half;
            return min > max ? 0f : Mathf.Clamp(x, min, max);
        }

        private void SnapCamera(float x)
        {
            var p = cam.transform.position;
            p.x = ClampCamera(x);
            cam.transform.position = p;
            cameraVelocity = 0f;
        }

        private string NameOf(string who)
        {
            if (string.IsNullOrEmpty(who)) return "";
            if (who == "player") return "You";
            return data.TryVillager(who, out var v) ? v.displayName : who;
        }

        private Color ColorOf(string who)
        {
            if (who == "player") return ProceduralArt.Hex("#F2C48D");
            if (data.TryVillager(who, out var v)) return Color.Lerp(ProceduralArt.Hex(v.bodyColor), Color.white, 0.45f);
            return Color.white;
        }

        private static string CostText(ActionDef a)
        {
            var parts = new List<string>();
            if (a.costAttention > 0) parts.Add($"{a.costAttention} attention");
            if (a.costBread > 0) parts.Add($"{a.costBread} bread");
            if (a.costCoin > 0) parts.Add($"{a.costCoin} coin");
            return string.Join(", ", parts);
        }

        private void RefreshHud()
        {
            if (Session == null) return;
            var loop = Session.Loop;
            string phase = loop.Phase switch
            {
                DayPhase.Notice => "Morning  ·  Notice",
                DayPhase.Act => "Midday  ·  Act",
                DayPhase.Gather => "Dusk  ·  Gather",
                DayPhase.Feast => "The Feast",
                _ => "The morning after",
            };
            string attention = new string('●', loop.AttentionLeft) + new string('○', loop.AttentionPerDay - loop.AttentionLeft);
            string items = Session.Inventory.Count > 0 ? $"   ·   {string.Join(", ", Session.Inventory)}" : "";
            hud.SetStatus($"<b>Day {loop.Day}</b>   {phase}\nAttention  {attention}\nBread {Session.Bread}   ·   Coin {Session.Coin}{items}");

            var r = Report;
            hud.SetWarmth(r.Warmth, r.Tier, r.Warmth / data.Rules.maxWarmth);

            string prompt = "";
            if (autoplay != null) prompt = "<color=#ffffffaa>Autoplay. [F5] sincere  [F6] transactional</color>";
            else if (IsIdle)
            {
                var near = Nearest();
                string action = near == null ? "" :
                    loop.Phase == DayPhase.Notice ? $"[E] Notice {data.Villager(near).displayName}     " :
                    loop.Phase == DayPhase.Act ? $"[E] Spend time with {data.Villager(near).displayName}     " : "";
                string next = loop.Phase == DayPhase.Notice ? "[N] Let the morning pass" :
                              loop.Phase == DayPhase.Act ? "[N] Head to the table at dusk" : "";
                prompt = $"{action}{next}\n<size=19><color=#ffffff88>[A/D] walk   [Tab] event log   [F1] warmth   [F5/F6] watch a sincere / transactional day</color></size>";
            }
            hud.SetPrompt(prompt);
        }

        private void RefreshLog()
        {
            if (Session == null) return;
            var sb = new StringBuilder("<b>Event log</b>  <color=#ffffff77>(append-only, what the evaluator reads)</color>\n");
            var events = Session.Log.Events;
            for (int i = Math.Max(0, events.Count - 18); i < events.Count; i++)
                sb.AppendLine(EventText.Describe(events[i], data));
            hud.SetLog(sb.ToString());
        }

        private string Debrief(MotiveReport r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<size=34><b>The morning after</b></size>   <color=#ffffff77>slice debrief, for playtesting</color>\n");
            sb.AppendLine($"Warmth <b>{r.Warmth:0}</b> ({r.Tier})     Sincerity <b>{r.Sincerity:0.00}</b>     {r.Acts} acts, {r.Reminders} reminders\n");
            sb.AppendLine("<b>What the village noticed</b>");
            foreach (var s in r.Signals)
            {
                string score = s.Evidence == 0 ? "<color=#ffffff66>  –  </color>" : $"{s.Score:0.00}";
                sb.AppendLine($"  {score}   {Pretty(s.Id)}  <color=#ffffff88>{s.Explanation}</color>");
            }
            sb.AppendLine("\n<b>At the table</b>");
            foreach (var v in data.Villagers)
                sb.AppendLine($"  {v.displayName,-8} trust {r.TrustOf(v.id):0.0}   {SeatWords(r.SeatOf(v.id))}");
            sb.AppendLine($"\n<b>Marta's oven:</b> {OvenWords(r.Oven)}");
            if (r.RewardsReceived.Count > 0)
                sb.AppendLine($"<b>You received:</b> {string.Join(", ", r.RewardsReceived)}. Nobody mentioned them again.");
            sb.AppendLine("\n<color=#ffffffaa>[R] Live the day again    [F5] Watch a sincere day    [F6] Watch a transactional day    [Esc] Quit</color>");
            return sb.ToString();
        }

        private static string Pretty(string id) => id switch
        {
            SignalIds.PrivateHelp => "Helped when nobody was watching",
            SignalIds.NoReminding => "Didn't point it out afterwards",
            SignalIds.CannotRepay => "Helped someone who can't repay",
            SignalIds.StaysThroughRebuff => "Came back after being turned away",
            SignalIds.NoRewardAsked => "Asked for nothing back",
            SignalIds.NoticedFirst => "Noticed before helping",
            _ => id,
        };

        private static string SeatWords(Seat s) => s switch
        {
            Seat.Close => "sat close",
            Seat.Edge => "sat at the edge",
            _ => "stayed away",
        };

        private static string OvenWords(OvenOutcome o) => o switch
        {
            OvenOutcome.Relit => "relit. She's baking because she wants to.",
            OvenOutcome.Polite => "lit for an order. The door is closed.",
            _ => "still cold.",
        };

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
