using System;
using System.Linq;

namespace UberText.Geocoding
{
    public enum LocationType
    {
        Rooftop,
        RangeInterpolated,
        GeometricCenter,
        Approximate,
        // in case the server returns back something unknown
        Unknown,
    }
}
