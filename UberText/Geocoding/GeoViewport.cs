﻿using System;
using System.Linq;
using Newtonsoft.Json;

namespace UberText.Geocoding
{
    public class GeoViewport
    {
        [JsonProperty("northeast")]
        public GeoCoordinate Northeast { get; set; }

        [JsonProperty("southwest")]
        public GeoCoordinate Southwest { get; set; }
    }
}
