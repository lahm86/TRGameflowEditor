﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace TRGE.Core
{
    internal class TRAudioTrack
    {
        [JsonProperty]
        internal ushort ID;
        [JsonProperty]
        internal string Name;
        [JsonProperty]
        internal uint Offset;
        [JsonProperty]
        internal uint Length;
        [JsonProperty]
        internal List<TRAudioCategory> Categories;

        internal TRAudioTrack()
        {
            Categories = new List<TRAudioCategory>();
        }

        public override bool Equals(object obj)
        {
            return obj is TRAudioTrack track &&
                   ID == track.ID;
        }

        public override int GetHashCode()
        {
            return 1213502048 + ID.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", ID.ToString().PadLeft(3, '0'), Name);
        }
    }
}