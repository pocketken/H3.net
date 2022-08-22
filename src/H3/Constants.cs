namespace H3; 

public class Constants {
    public const double M_PI = 3.14159265358979323846;
    public const double M_PI_2 = 1.5707963267948966;
    public const double M_2PI = 6.28318530717958647692528676655900576839433; // 2 * Math.PI;

    public const double M_PI_180 = 0.0174532925199432957692369076848861271111; // pi / 180
    public const double M_180_PI = 57.29577951308232087679815481410517033240547; // pi * 180

    public const double EPSILON = 0.0000000000000001; // threshold epsilon
    public const double M_SQRT3_2 = 0.8660254037844386467637231707529361834714; // sqrt(3) / 2.0
    public const double M_SIN60 = M_SQRT3_2; // sin(60')
    public const double M_SQRT7 = 2.6457513110645905905016157536392604257102;

    // rotation angle between Class II and Class III resolution axes
    public const double M_AP7_ROT_RADS = 0.333473172251832115336090755351601070065900389; // (asin(sqrt(3.0 / 28.0)))

    // earth radius in kilometers using WGS84 authalic radius
    public const double EARTH_RADIUS_KM = 6371.007180918475;

    /* scaling factor from hex2d resolution 0 unit length
     * (or distance between adjacent cell center points
     * on the plane) to gnomonic unit length. */
    public const double RES0_U_GNOMONIC = 0.38196601125010500003;

    // max H3 resolution; H3 version 1 has 16 resolutions, numbered 0 through 15
    public const int MAX_H3_RES = 15;

    // The number of faces on an icosahedron
    public const int NUM_ICOSA_FACES = 20;

    // The number of H3 base cells
    public const int NUM_BASE_CELLS = 122;

    // The number of pentagon cells
    public const int NUM_PENTAGONS = 12;

    // The number of vertices in a hexagon
    public const int NUM_HEX_VERTS = 6;

    // The number of vertices in a pentagon
    public const int NUM_PENT_VERTS = 5;

    /** epsilon of ~0.1mm in degrees */
    public const double EPSILON_DEG = .000000001;

    /** epsilon of ~0.1mm in radians */
    public const double EPSILON_RAD = EPSILON_DEG * M_PI_180;

    // maximum number of face coordinates
    public const int MAX_FACE_COORD = 2;
}