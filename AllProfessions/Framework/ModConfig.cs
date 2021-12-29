using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AllProfessions.Framework
{
    /// <summary>The mod configuration.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The profession names or IDs which shouldn't be added.</summary>
        public HashSet<string> IgnoreProfessions { get; set; } = new();


        /*********
        ** Public methods
        *********/
        /// <summary>Whether a given profession shouldn't be added automatically.</summary>
        /// <param name="profession">The profession to check.</param>
        public bool ShouldIgnore(Profession profession)
        {
            return
                this.IgnoreProfessions.Contains(profession.ToString())
                || this.IgnoreProfessions.Contains(((int)profession).ToString());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method called after the config file is deserialized.</summary>
        /// <param name="context">The deserialization context.</param>
        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            this.IgnoreProfessions = new(this.IgnoreProfessions ?? new(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
