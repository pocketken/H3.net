using System;
using System.Linq;
using H3.Extensions;
using static H3.Constants;

#nullable enable

namespace H3.Model {

    public class BBox {
        public double North { get; set; }
        public double South { get; set; }
        public double East { get; set; }
        public double West { get; set; }
        public bool IsTransmeridian => East < West;

        public BBox() { }

        public BBox(double north, double south, double east, double west) {
            North = north;
            South = south;
            East = east;
            West = west;
        }

        public GeoCoord Center() => new GeoCoord {
            Latitude = (North + South) / 2.0,
            Longitude = (IsTransmeridian ? East + M_2PI : East) + West / 2.0
        };

        public bool Contains(GeoCoord point) =>
            point.Latitude >= South && point.Latitude <= North &&
                IsTransmeridian
                    ? point.Longitude >= West || point.Longitude <= East
                    : point.Longitude >= West && point.Longitude <= East;

        /// <summary>
        /// Returns an estimated number of hexagons that fit within the
        /// cartesian-projected bounding box at the specified resolution.
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public int GetHexagonEstimateForResolution(int resolution) {
            // Get the area of the pentagon as the maximally-distorted area possible
            H3Index firstPentagon = LookupTables.PentagonIndexesPerResolution[resolution].First();
            double pentagonRadiusKm = firstPentagon.GetRadiusInKm();

            // Area of a regular hexagon is 3/2*sqrt(3) * r * r
            // The pentagon has the most distortion (smallest edges) and shares its
            // edges with hexagons, so the most-distorted hexagons have this area,
            // shrunk by 20% off chance that the bounding box perfectly bounds a
            // pentagon.
            double pentagonAreaKm2 = 0.8 * (2.59807621135 * pentagonRadiusKm * pentagonRadiusKm);

            // Then get the area of the bounding box of the geofence in question
            GeoCoord p1 = new GeoCoord(North, East);
            GeoCoord p2 = new GeoCoord(South, West);
            double d = p1.GetPointDistanceInKm(p2);

            // Derived constant based on: https://math.stackexchange.com/a/1921940
            // Clamped to 3 as higher values tend to rapidly drag the estimate to zero.
            double a = d * d / Math.Min(3.0, Math.Abs((p1.Longitude - p2.Longitude) / (p1.Latitude - p2.Latitude)));

            // Divide the two to get an estimate of the number of hexagons needed
            int estimate = (int)Math.Ceiling(a / pentagonAreaKm2);
            return estimate == 0 ? 1 : estimate;
        }

        public static bool operator ==(BBox a, BBox b) =>
            a.North == b.North && a.South == b.South && a.East == b.East && a.West == b.West;

        public static bool operator !=(BBox a, BBox b) =>
            a.North != b.North || a.South != b.South || a.East != b.East || a.West != b.West;

        public override bool Equals(object? other) =>
            other is BBox b && North == b.North && South == b.South && East == b.East && West == b.West;

        public override int GetHashCode() => HashCode.Combine(North, South, East, West);
    }

}
