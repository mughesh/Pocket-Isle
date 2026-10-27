using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>Who says a line; the bubble appears above that speaker's registered anchor.</summary>
    public enum DialogueSpeaker { Spirit, Keeper, Heart, Other }

    [Serializable]
    public class DialogueLine
    {
        public DialogueSpeaker speaker;
        [Tooltip("Name as written in the script (Spirit, Hearthstag, Heart).")]
        public string speakerName;
        [TextArea(1, 3)] public string text;
    }

    /// <summary>All lines one trigger id says, in order.</summary>
    [Serializable]
    public class DialogueTrigger
    {
        public string id;
        [Tooltip("Only the first time (remembered; saved in step 7).")]
        public bool once;
        [Tooltip("Step 12a: an objective (a lesson, a task, a hand moment's line): gold bubble with a target icon, bold, kept through a scene change. Script: \"· objective\".")]
        public bool objective;
        [Tooltip("Higher interrupts lower (idle hints are 0, tutorials 1, story 2).")]
        public int priority = 1;
        [Tooltip("The script's comment, e.g. when it fires.")]
        public string note;
        [Tooltip("Where the Spirit flies while saying these lines: leadPod, cradle, runes, nearPad, wildPad, tile:x,y. Empty = stays home.")]
        public string target;
        [Tooltip("Only shown while this holds (GameConditions: battle, swap, canWalk, encounters, ...; ! & |). The bubble closes as soon as it stops holding, and a battle or scene swap starting drops it unless it still holds. Empty = always.")]
        public string validWhile;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    /// <summary>
    /// The game's lines by trigger id, imported from Docs/Story/Script_v1.md
    /// (Tools > Pocket Isle > Dialogue > Import Script_v1). Systems fire ids with <see cref="Dialogue.Say"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Dialogue/Script", fileName = "Script")]
    public class DialogueScript : ScriptableObject
    {
        public List<DialogueTrigger> triggers = new List<DialogueTrigger>();
        [Tooltip("Said at random when nothing happens for a while.")]
        public List<DialogueLine> idleHints = new List<DialogueLine>();

        public DialogueTrigger Find(string id)
        {
            foreach (var t in triggers)
                if (t.id == id) return t;
            return null;
        }
    }
}
