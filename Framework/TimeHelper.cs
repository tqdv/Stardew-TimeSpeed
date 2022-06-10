using System;
using StardewValley;

namespace TimeSpeed.Framework
{
    /// <summary>Provides helper methods to retrieve information about the game time.</summary>
    internal class TimeHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The previous elapsed time since the last clock change.</summary>
        private int PreviousElapsedGameTime;

        /// <summary>The current elapsed time since the last clock change.</summary>
        public int ElapsedGameTime {
            get => Game1.gameTimeInterval;
            set => Game1.gameTimeInterval = value;
        }

        /// <summary>The handlers to notify when some game time has elapsed.</summary>
        public event EventHandler<GameTimeElapsedEventArgs> GameTimeElapsed;


        /*********
        ** Accessors
        *********/
        /// <summary>The game's default clock interval in milliseconds for the current location.</summary>
        public int CurrentDefaultClockInterval => 7000 + (Game1.currentLocation?.getExtraMillisecondsPerInGameMinuteForThisLocation() ?? 0);


        /*********
        ** Public methods
        *********/
        /// <summary>Update the time tracking.</summary>
        public void Update()
        {
            if (PreviousElapsedGameTime != ElapsedGameTime)
                GameTimeElapsed?.Invoke(null, new GameTimeElapsedEventArgs(PreviousElapsedGameTime, ElapsedGameTime));

            PreviousElapsedGameTime = ElapsedGameTime;
        }

        /// <summary>Register an event handler to notify when the <see cref="ElapsedGameTime"/> changes.</summary>
        /// <param name="handler">The event handler to notify.</param>
        public void OnGameTimeElapsed(EventHandler<GameTimeElapsedEventArgs> handler)
        {
            GameTimeElapsed += handler;
        }
    }

    /// <summary>Contains information about a change to the <see cref="TimeHelper.ElapsedGameTime"/> value.</summary>
    internal class GameTimeElapsedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous elapsed time since the last clock change.</summary>
        public int PreviousTime { get; }

        /// <summary>The new elapsed time since the last clock change.</summary>
        public int NewTime { get; }

        /// <summary>Whether the clock ticked since the last check.</summary>
        public bool ClockChanged => NewTime < PreviousTime;

        /// <summary>If the game clock hasn't changed, the amount of time that has passed since the last check.
        /// Otherwise, the difference between the current time and the previous one.</summary>
        public int TimeDifference => NewTime - PreviousTime;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="previousProgress">The previous progress value.</param>
        /// <param name="newProgress">The new progress value.</param>
        public GameTimeElapsedEventArgs(int previousProgress, int newProgress)
        {
            PreviousTime = previousProgress;
            NewTime = newProgress;
        }
    }
}
