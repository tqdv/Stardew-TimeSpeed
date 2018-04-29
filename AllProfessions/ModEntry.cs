using System;
using System.Collections.Generic;
using System.Linq;
using AllProfessions.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AllProfessions
{
    /// <summary>The entry class called by SMAPI.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>Professions to gain for each level. Each entry represents the skill, level requirement, and profession IDs.</summary>
        private readonly Tuple<Skill, int, int[]>[] ProfessionsToGain =
        {
            Tuple.Create(Skill.Farming, 5, new[] { Farmer.rancher, Farmer.tiller }),
            Tuple.Create(Skill.Farming, 10, new[] { Farmer.butcher/*actually coopmaster*/, Farmer.shepherd, Farmer.artisan, Farmer.agriculturist }),
            Tuple.Create(Skill.Fishing, 5, new[] { Farmer.fisher, Farmer.trapper }),
            Tuple.Create(Skill.Fishing, 10, new[] { Farmer.angler, Farmer.pirate, Farmer.baitmaster, Farmer.mariner }),
            Tuple.Create(Skill.Foraging, 5, new[] { Farmer.forester, Farmer.gatherer }),
            Tuple.Create(Skill.Foraging, 10, new[] { Farmer.lumberjack, Farmer.tapper, Farmer.botanist, Farmer.tracker }),
            Tuple.Create(Skill.Mining, 5, new[] { Farmer.miner, Farmer.geologist }),
            Tuple.Create(Skill.Mining, 10, new[] { Farmer.blacksmith, Farmer.burrower/*actually prospector*/, Farmer.excavator, Farmer.gemologist }),
            Tuple.Create(Skill.Combat, 5, new[] { Farmer.fighter, Farmer.scout }),
            Tuple.Create(Skill.Combat, 10, new[] { Farmer.brute, Farmer.defender, Farmer.acrobat, Farmer.desperado })
        };


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            TimeEvents.AfterDayStarted += this.ReceiveAfterDayStarted;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ReceiveAfterDayStarted(object sender, EventArgs e)
        {
            // When the player loads a saved game, or after the overnight level screen,
            // add any professions the player should have but doesn't.
            this.AddMissingProfessions();
        }

        /****
        ** Methods
        ****/
        /// <summary>Add all missing professions.</summary>
        private void AddMissingProfessions()
        {
            // get missing professions
            List<int> expectedProfessions = new List<int>();
            foreach (var entry in this.ProfessionsToGain)
            {
                Skill skill = entry.Item1;
                int level = entry.Item2;
                int[] professions = entry.Item3;

                if (Game1.player.getEffectiveSkillLevel((int)skill) >= level)
                    expectedProfessions.AddRange(professions);
            }

            // add professions
            foreach (int professionID in expectedProfessions.Distinct().Except(Game1.player.professions))
            {
                // add profession
                Game1.player.professions.Add(professionID);

                // add health bonuses that are a special case of LevelUpMenu.getImmediateProfessionPerk
                switch (professionID)
                {
                    // fighter
                    case 24:
                        Game1.player.maxHealth += 15;
                        break;

                    // defender
                    case 27:
                        Game1.player.maxHealth += 25;
                        break;
                }
            }
        }
    }
}
