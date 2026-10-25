namespace Diorama
{
    /// <summary>
    /// A realm Keeper's battle rules (step 9c, Hearthstag), applied by <see cref="BattleSystem"/> when it gets one:
    /// - It starts behind its crown shield: hits do <see cref="blockedShare"/> of their damage ("Blocked!") and each one
    ///   cracks a crystal; <see cref="hitsToBreak"/> cracks, or one hit of <see cref="breaksWith"/>, break it.
    /// - Broken, it takes normal damage for <see cref="openTurns"/> whole turns, then uses its shield move (move 3) and
    ///   is shielded again (cracks mended).
    /// - Every <see cref="chargeEvery"/>th turn it uses its charge (move 2), otherwise its shot (move 1); below
    ///   <see cref="phase2At"/> of its HP the shot hits <see cref="phase2Power"/> times harder (phase 2, once).
    /// - It can't be caught, running always works, and when it faints the battle is simply won (no daze).
    /// </summary>
    public sealed class KeeperRules
    {
        public Element breaksWith = Element.Water;
        public int hitsToBreak = 3;
        public float blockedShare = 0.1f;
        public int openTurns = 2;
        public int chargeEvery = 3;
        public float phase2At = 0.5f;
        public float phase2Power = 1.5f;

        public bool Shielded { get; internal set; } = true;
        /// <summary>Cracked crystals since the shield last rose.</summary>
        public int Cracks { get; internal set; }
        /// <summary>Open turns left after a break.</summary>
        public int OpenLeft { get; internal set; }
        /// <summary>The open turns are over: its next move raises the shield.</summary>
        public bool RaiseDue { get; internal set; }
        public bool Phase2 { get; internal set; }
        /// <summary>Its moves so far (1 = the first).</summary>
        public int Turn { get; internal set; }
        internal bool BrokeThisTurn;

        /// <summary>Its move this turn: shield when due, the charge every few turns, else the shot.</summary>
        internal MoveData Pick(MoveData[] moves)
        {
            Turn++;
            if (moves == null || moves.Length == 0) return null;
            if (RaiseDue && moves.Length > 2 && moves[2] != null) return moves[2];
            if (chargeEvery > 0 && Turn % chargeEvery == 0 && moves.Length > 1 && moves[1] != null) return moves[1];
            return moves[0];
        }

        internal void Break()
        {
            Shielded = false;
            OpenLeft = openTurns;
            RaiseDue = false;
            BrokeThisTurn = true;
        }

        internal void Raise()
        {
            Shielded = true;
            Cracks = 0;
            RaiseDue = false;
            OpenLeft = 0;
        }

        /// <summary>A turn ended: count the open turns down (not the one it broke in).</summary>
        internal void EndTurn()
        {
            if (!Shielded && !RaiseDue)
            {
                if (!BrokeThisTurn && --OpenLeft <= 0) RaiseDue = true;
            }
            BrokeThisTurn = false;
        }
    }
}
