using System;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Read-only view of the live run score. The score is derived entirely from gameplay events,
    /// so no system ever pushes a value in: the HUD and the result screen only read from here.
    /// </summary>
    public interface IScoreService
    {
        /// <summary>Total score: coins * 10 + distance * 2 + combo * 50.</summary>
        int Score { get; }

        /// <summary>Coins collected during the current run.</summary>
        int Coins { get; }

        /// <summary>Distance travelled during the current run, in meters.</summary>
        float Distance { get; }

        /// <summary>Number of obstacles cleared back to back without a hit.</summary>
        int Combo { get; }

        /// <summary>Highest combo reached during the current run.</summary>
        int BestCombo { get; }

        /// <summary>Best score stored in the player profile.</summary>
        int HighScore { get; }

        /// <summary>True when the current run has already beaten the stored best score.</summary>
        bool IsNewHighScore { get; }

        /// <summary>Raised whenever the total score changes, carrying the new value.</summary>
        event Action<int> ScoreChanged;
    }
}
