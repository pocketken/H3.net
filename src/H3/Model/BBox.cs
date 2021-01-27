using System;
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

        public override bool Equals(object? other) =>
            other is BBox b && North == b.North && South == b.South && East == b.East && West == b.West;

        public override int GetHashCode() => HashCode.Combine(North, South, East, West);
    }

}
