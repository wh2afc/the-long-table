using System.Linq;
using LongTable.Core;

namespace LongTable.Game
{
    /// <summary>Plain-words version of an event for the on-screen log.</summary>
    public static class EventText
    {
        public static string Describe(GameEvent e, SliceData data)
        {
            string Name(string id) => data.TryVillager(id, out var v) ? v.displayName : id;
            string when = $"<color=#ffffff66>d{e.day} {e.phase,-6}</color>";

            switch (e.kind)
            {
                case EventKind.Noticed:
                    return $"{when}  Noticed {Name(e.target)}";
                case EventKind.Acted:
                {
                    string label = data.TryAction(e.actionId, out var a) ? a.label : e.actionId;
                    string seen = e.Witnessed ? "seen by " + string.Join(", ", e.witnesses.Select(Name)) : "unseen";
                    string reward = e.rewardRequested ? $", got {e.rewardItem}" : "";
                    string tried = e.wasRebuffed ? "Tried: " : "";
                    return $"{when}  {tried}{label}  <color=#ffffff99>[{e.family}, {seen}{reward}]</color>";
                }
                case EventKind.Rebuffed:
                    return $"{when}  {Name(e.actor)} turned you away";
                case EventKind.Reminded:
                    return $"{when}  Told the table what you did today";
                case EventKind.PhaseChanged:
                    return $"<color=#ffffff55>{when}  {e.note.Replace("->", " to ")}</color>";
                default:
                    return $"{when}  {e.kind}";
            }
        }
    }
}
