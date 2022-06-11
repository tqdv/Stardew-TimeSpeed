using System;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TimeSpeed.Framework;

namespace TimeSpeed
{
    /// <summary>The entry class called by SMAPI.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>Displays messages to the user.</summary>
        private readonly Notifier Notifier = new();

        /// <summary>Provides helper methods for tracking time flow.</summary>
        private readonly TimeHelper TimeHelper = new();

        /// <summary>The mod configuration.</summary>
        private ModConfig Config;

        /// <summary>Backing field for <see cref="ManualFreeze"/>.</summary>
        private bool? _manualFreeze;

        /// <summary>Whether the player has manually frozen (<c>true</c>) or resumed (<c>false</c>) time.</summary>
        private bool? ManualFreeze {
            get => _manualFreeze;
            set {
                if (value != _manualFreeze)
                    Monitor.Log($"Manual freeze changed from {_manualFreeze?.ToString() ?? "null"} to {value?.ToString() ?? "null"}.");
                _manualFreeze = value;
            }
        }

        /// <summary>Backing field for <see cref="AutoFreeze"/>.</summary>
        private AutoFreezeReason _autoFreeze = AutoFreezeReason.None;

        /// <summary>The reason time would be frozen automatically if applicable, regardless of <see cref="ManualFreeze"/>.</summary>
        private AutoFreezeReason AutoFreeze {
            get => _autoFreeze;
            set {
                if (value != _autoFreeze)
                    Monitor.Log($"Auto freeze changed from {_autoFreeze} to {value}.");
                _autoFreeze = value;
            }
        }

        /// <summary>Whether time should be frozen.</summary>
        private bool IsTimeFrozen =>
            ManualFreeze == true
            || (AutoFreeze != AutoFreezeReason.None && ManualFreeze != false);

        /// <summary>Whether the flow of time should be adjusted.</summary>
        private bool AdjustTime;

        /// <summary>Backing field for <see cref="ClockInterval"/>.</summary>
        private int _clockInterval;

        /// <summary>The number of milliseconds per 10 game-minutes for the current location.</summary>
        private int ClockInterval
        {
            get => _clockInterval;
            set {
                if (value != _clockInterval)
                    Monitor.VerboseLog($"Clock interval changed from {_clockInterval} to {value}.");
                _clockInterval = Math.Max(value, 0);
            }
        }


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            // read config
            this.Config = helper.ReadConfig<ModConfig>();

            // add time events
            TimeHelper.GameTimeElapsed += OnGameTimeElapsed;
            helper.Events.GameLoop.GameLaunched += (_, _) => SetupGenericModConfigMenuIntegration();
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Player.Warped += OnWarped;

            // add time freeze/unfreeze notification
            {
                bool wasPaused = false;
                helper.Events.Display.RenderingHud += (_, _) =>
                {
                    wasPaused = Game1.paused;
                    if (this.IsTimeFrozen)
                        Game1.paused = true;
                };

                helper.Events.Display.RenderedHud += (_, _) =>
                {
                    Game1.paused = wasPaused;
                };
            }
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                Monitor.Log("Disabled mod; only works for the main player in multiplayer.", LogLevel.Warn);
        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!ShouldEnable())
                return;

            ManualFreeze = null;
            UpdateScaleForDay(Game1.currentSeason, Game1.dayOfMonth);
            UpdateSettingsForLocation(Game1.currentLocation);
        }

        /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!ShouldEnable(forInput: true))
                return;

            if (Config.Keys.FreezeTime.JustPressed())
            {
                KeyboardState state = Keyboard.GetState();
                if (state.IsKeyDown(Keys.LeftShift))
                    ResetFreeze();
                else
                    ToggleFreeze();
            }
            else if (Config.Keys.IncreaseTickInterval.JustPressed())
                ChangeClockInterval(GetKeyboardModifiedAmount());
            else if (Config.Keys.DecreaseTickInterval.JustPressed())
                ChangeClockInterval(-1 * GetKeyboardModifiedAmount());
            else if (Config.Keys.ReloadConfig.JustPressed())
                ReloadConfig();
        }

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!ShouldEnable() || !e.IsLocalPlayer)
                return;

            UpdateSettingsForLocation(e.NewLocation);
        }

        /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!ShouldEnable())
                return;

            UpdateFreezeForTime(Game1.timeOfDay);
        }

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!ShouldEnable())
                return;

            TimeHelper.Update();
        }

        /// <summary>Raised after the <see cref="Framework.TimeHelper.TickProgress"/> value changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameTimeElapsed(object sender, GameTimeElapsedEventArgs e)
        {
            if (!ShouldEnable())
                return;

            if (IsTimeFrozen)
                TimeHelper.ElapsedGameTime = e.ClockChanged ? 0 : e.PreviousTime;
            else
            {
                if (!AdjustTime)
                    return;
                if (ClockInterval == 0)
                    ClockInterval = 1000;

                if (e.ClockChanged)
                    TimeHelper.ElapsedGameTime = (int) GetScaledDuration(e.NewTime);
                else
                    TimeHelper.ElapsedGameTime = e.PreviousTime + (int) GetScaledDuration(e.TimeDifference);
            }
        }

        /****
        ** Methods
        ****/

        /// <summary>Register this mod's configuration options with GenericModConfigMenu</summary>
        private void SetupGenericModConfigMenuIntegration()
        {
            GenericModConfigMenuIntegration.Register(ModManifest, Helper.ModRegistry, Monitor,
                getConfig: () => Config,
                reset: () => Config = new(),
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    if (ShouldEnable())
                        ReloadConfig();
                }
            );
        }

        /// <summary>Get whether time features should be enabled.</summary>
        /// <param name="forInput">Whether to check for input handling.</param>
        private bool ShouldEnable(bool forInput = false)
        {
            // is loaded and host player (farmhands can't change time)
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
                return false;

            // only handle keys if the player is free
            if (forInput && !Context.IsPlayerFree)
                return false;

            return true;
        }

        /// <summary>Reload <see cref="Config"/> from the config file and apply the settings with the current context.</summary>
        private void ReloadConfig()
        {
            Config = Helper.ReadConfig<ModConfig>();
            UpdateScaleForDay(Game1.currentSeason, Game1.dayOfMonth);
            UpdateSettingsForLocation(Game1.currentLocation);
            Notifier.ShortNotify(I18n.Message_ConfigReloaded());
        }

        /// <summary>Modify the clock interval by the given amount.</summary>
        /// <param name="increase">The amount to add to the clock interval.</param>
        private void ChangeClockInterval(int amount)
        {
            int minAllowed = Math.Min(ClockInterval, Math.Abs(amount));
            ClockInterval = Math.Max(minAllowed, ClockInterval + amount);

            // log change
            var formattedClockInterval = (ClockInterval / 1000d).ToString("0.##");
            Notifier.QuickNotify(I18n.Message_SpeedChanged(seconds: formattedClockInterval));
            Monitor.Log($"Clock interval set to {formattedClockInterval} seconds.", LogLevel.Info);
        }

        /// <summary>Toggle whether time is frozen.</summary>
        private void ToggleFreeze()
        {
            if (!IsTimeFrozen)
            {
                ManualFreeze = true;
                Notifier.QuickNotify(I18n.Message_TimeStopped());
                Monitor.Log("Time is frozen globally.", LogLevel.Info);
            }
            else
            {
                ManualFreeze = false;
                Notifier.QuickNotify(I18n.Message_TimeResumed());
                Monitor.Log($"Time is resumed at \"{Game1.currentLocation.Name}\".", LogLevel.Info);
            }
        }

        /// <summary>Clear the manual freeze override, and let default settings apply.</summary>
        private void ResetFreeze()
        {
            ManualFreeze = null;
            UpdateSettingsForLocation(Game1.currentLocation);
            Monitor.Log($"Time flow is reset at \"{Game1.currentLocation}\".", LogLevel.Info);
        }

        /// <summary>Update the time freeze settings for the given time of day.</summary>
        private void UpdateFreezeForTime(int timeOfDay)
        {
            bool wasFrozen = IsTimeFrozen;
            UpdateAutoFreeze();

            if (!wasFrozen && IsTimeFrozen)
            {
                Notifier.ShortNotify(I18n.Message_OnTimeChange_TimeStopped());
                Monitor.Log($"Time automatically set to frozen at {timeOfDay}.", LogLevel.Info);
            }
        }

        /// <summary>Update the time settings for the given location.</summary>
        /// <param name="location">The game location.</param>
        private void UpdateSettingsForLocation(GameLocation location)
        {
            // update time settings
            UpdateAutoFreeze();
            UpdateClockInterval();

            // notify player
            if (Config.LocationNotify)
            {
                switch (AutoFreeze)
                {
                    case AutoFreezeReason.FrozenAtTime when IsTimeFrozen:
                        Notifier.ShortNotify(I18n.Message_OnLocationChange_TimeStoppedGlobally());
                        break;

                    case AutoFreezeReason.FrozenForLocation when IsTimeFrozen:
                        Notifier.ShortNotify(I18n.Message_OnLocationChange_TimeStoppedHere());
                        break;

                    default:
                        Notifier.ShortNotify(I18n.Message_OnLocationChange_TimeSpeedHere(seconds: ClockInterval / 1000));
                        break;
                }
            }
        }

        /// <summary>Update <see cref="AutoFreeze"/> based on the current location.</summary>
        private void UpdateClockInterval() {
            ClockInterval = Config.GetMillisecondsPerMinute(Game1.currentLocation) * 10;
        }

        /// <summary>Update <see cref="AutoFreeze"/> based on the current context. Also clear the forced unfreezing if it is unneeded.</summary>
        private void UpdateAutoFreeze()
        {
            AutoFreeze = GetAutoFreezeType();

            // clear manual unfreeze if it's no longer needed
            if (ManualFreeze == false && AutoFreeze == AutoFreezeReason.None)
            {
                Monitor.Log("Clearing the manual unfreeze override.");
                ManualFreeze = null;
            }
        }

        /// <summary>Update the time settings for the given date.</summary>
        /// <param name="season">The current season.</param>
        /// <param name="dayOfMonth">The current day of month.</param>
        private void UpdateScaleForDay(string season, int dayOfMonth)
        {
            AdjustTime = Config.ShouldScale(season, dayOfMonth);
        }

        /// <summary>Get the amount of time to add or remove from the clock interval, taking into account held modifiers.</summary>
        private int GetKeyboardModifiedAmount() {
            int change = 1000;

            KeyboardState state = Keyboard.GetState();
            if (state.IsKeyDown(Keys.LeftControl))
                change *= 100;
            else if (state.IsKeyDown(Keys.LeftShift))
                change *= 10;
            else if (state.IsKeyDown(Keys.LeftAlt))
                change /= 10;
            return change;
        }

        /// <summary>Get the adjusted amount of game time used for tracking clock changes.</summary>
        /// <param name="amount">The duration amount.</param>
        private double GetScaledDuration(double amount)
        {
            return Math.Round(amount * TimeHelper.CurrentDefaultClockInterval / ClockInterval);
        }

        /// <summary>Get the freeze type which applies for the current context, ignoring overrides by the player.</summary>
        private AutoFreezeReason GetAutoFreezeType()
        {
            if (Config.ShouldFreeze(Game1.currentLocation))
                return AutoFreezeReason.FrozenForLocation;

            if (Config.ShouldFreeze(Game1.timeOfDay))
                return AutoFreezeReason.FrozenAtTime;

            return AutoFreezeReason.None;
        }
    }
}
