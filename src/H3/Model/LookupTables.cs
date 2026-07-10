using System.Runtime.CompilerServices;
using static H3.Constants;

#nullable enable

namespace H3.Model; 

public static partial class LookupTables {

    #region basecells

    public const int INVALID_BASE_CELL = 127;

    // TODO build BaseFace or something; anyway, it should have rotations etc
    // TODO link basecell to its BaseFace

    /// <summary>
    /// Resolution 0 base cell lookup table for each face.
    ///
    /// Given the face number and a resolution 0 ijk+ coordinate in that face's
    /// face-centered ijk coordinate system, gives the base cell located at that
    /// coordinate and the number of 60 ccw rotations to rotate into that base
    /// cell's orientation.
    ///
    /// Valid lookup coordinates are from(0, 0, 0) to(2, 2, 2).
    /// </summary>
    private static BaseCellRotation[,,,]? _faceIjkBaseCells;

    /// <summary>
    /// Resolution 0 base cell lookup table for each face.
    ///
    /// Given the face number and a resolution 0 ijk+ coordinate in that face's
    /// face-centered ijk coordinate system, gives the base cell located at that
    /// coordinate and the number of 60 ccw rotations to rotate into that base
    /// cell's orientation.
    ///
    /// Valid lookup coordinates are from (0, 0, 0) to (2, 2, 2).
    /// </summary>
    public static BaseCellRotation[,,,] FaceIjkBaseCells {
        get {
            var cells = _faceIjkBaseCells;
            if (cells != null) return cells;

            cells = new BaseCellRotation[NUM_ICOSA_FACES, 3, 3, 3];
            for (var face = 0; face < NUM_ICOSA_FACES; face += 1) {
                for (var i = 0; i < 3; i += 1) {
                    for (var j = 0; j < 3; j += 1) {
                        for (var k = 0; k < 3; k += 1) {
                            var flat = FlatFaceIjkIndex(face, i, j, k);
                            cells[face, i, j, k] = (FaceIjkBaseCellTable[flat], FaceIjkBaseCellRotationTable[flat]);
                        }
                    }
                }
            }

            _faceIjkBaseCells = cells;
            return cells;
        }
    }

    /// <summary>
    /// Index into <see cref="FaceIjkBaseCellTable"/> and
    /// <see cref="FaceIjkBaseCellRotationTable"/> for the given face and
    /// resolution 0 ijk+ coordinate components (0 &lt;= i, j, k &lt;= 2).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int FlatFaceIjkIndex(int face, int i, int j, int k) =>
        face * 27 + i * 9 + j * 3 + k;

    #endregion basecells

    #region coordinates + unit vectors
    public static readonly CoordIJK[] UnitVectors = {
        new(0, 0, 0),  // Center
        new(0, 0, 1),  // K
        new(0, 1, 0),  // J
        new(0, 1, 1),  // JK
        new(1, 0, 0),  // I
        new(1, 0, 1),  // IK
        new(1, 1, 0)   // IJ
    };

    /// <summary>
    /// The vertexes of an origin-centered cell in a Class II resolution on a
    /// substrate grid with aperture sequence 33r. The aperture 3 gets us the
    /// vertices, and the 3r gets us back to Class II.  vertices listed ccw
    /// from the i-axes
    /// </summary>
    public static readonly CoordIJK[] Class2HexVertices = {
        new(2, 1, 0),
        new(1, 2, 0),
        new(0, 2, 1),
        new(0, 1, 2),
        new(1, 0, 2),
        new(2, 0, 1)
    };

    /// <summary>
    /// the vertexes of an origin-centered cell in a Class III resolution on a
    /// substrate grid with aperture sequence 33r7r. The aperture 3 gets us the
    /// vertices, and the 3r7r gets us to Class II.  vertices listed ccw from
    /// the i-axes
    /// </summary>
    public static readonly CoordIJK[] Class3HexVertices = {
        new(5, 4, 0),
        new(1, 5, 0),
        new(0, 5, 4),
        new(0, 1, 5),
        new(4, 0, 5),
        new(5, 0, 1)
    };

    /// <summary>
    /// the vertexes of an origin-centered pentagon in a Class II resolution on a
    /// substrate grid with aperture sequence 33r. The aperture 3 gets us the
    /// vertices, and the 3r gets us back to Class II.  vertices listed ccw from
    /// the i-axes
    /// </summary>
    public static readonly CoordIJK[] Class2PentagonVertices = {
        new(2, 1, 0),
        new(1, 2, 0),
        new(0, 2, 1),
        new(0, 1, 2),
        new(1, 0, 2)
    };

    /// <summary>
    /// the vertexes of an origin-centered pentagon in a Class III resolution on
    /// a substrate grid with aperture sequence 33r7r. The aperture 3 gets us the
    /// vertices, and the 3r7r gets us to Class II. vertices listed ccw from the
    /// i-axes
    /// </summary>
    public static readonly CoordIJK[] Class3PentagonVertices = {
        new(5, 4, 0),
        new(1, 5, 0),
        new(0, 5, 4),
        new(0, 1, 5),
        new(4, 0, 5)
    };

    #endregion coordinates + unit vectors

    #region faces

    public static readonly double[] AxisAzimuths = {
        5.619958268523939882,  // face  0
        5.760339081714187279,  // face  1
        0.780213654393430055,  // face  2
        0.430469363979999913,  // face  3
        6.130269123335111400,  // face  4
        2.692877706530642877,  // face  5
        2.982963003477243874,  // face  6
        3.532912002790141181,  // face  7
        3.494305004259568154,  // face  8
        3.003214169499538391,  // face  9
        5.930472956509811562,  // face 10
        0.138378484090254847,  // face 11
        0.448714947059150361,  // face 12
        0.158629650112549365,  // face 13
        5.891865957979238535,  // face 14
        2.711123289609793325,  // face 15
        3.294508837434268316,  // face 16
        3.804819692245439833,  // face 17
        3.664438879055192436,  // face 18
        2.361378999196363184,  // face 19
    };

    private const int KI = FaceIJK.KI;
    private const int JK = FaceIJK.JK;
    private const int IJ = FaceIJK.IJ;

    public static readonly int[,] AdjacentFaceDirections = {
        {0,  KI, -1, -1, IJ, JK, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 0
        {IJ, 0,  KI, -1, -1, -1, JK, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 1
        {-1, IJ, 0,  KI, -1, -1, -1, JK, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 2
        {-1, -1, IJ, 0,  KI, -1, -1, -1, JK, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 3
        {KI, -1, -1, IJ, 0,  -1, -1, -1, -1, JK,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 4
        {JK, -1, -1, -1, -1, 0,  -1, -1, -1, -1,
            IJ, -1, -1, -1, KI, -1, -1, -1, -1, -1},  // face 5
        {-1, JK, -1, -1, -1, -1, 0,  -1, -1, -1,
            KI, IJ, -1, -1, -1, -1, -1, -1, -1, -1},  // face 6
        {-1, -1, JK, -1, -1, -1, -1, 0,  -1, -1,
            -1, KI, IJ, -1, -1, -1, -1, -1, -1, -1},  // face 7
        {-1, -1, -1, JK, -1, -1, -1, -1, 0,  -1,
            -1, -1, KI, IJ, -1, -1, -1, -1, -1, -1},  // face 8
        {-1, -1, -1, -1, JK, -1, -1, -1, -1, 0,
            -1, -1, -1, KI, IJ, -1, -1, -1, -1, -1},  // face 9
        {-1, -1, -1, -1, -1, IJ, KI, -1, -1, -1,
            0,  -1, -1, -1, -1, JK, -1, -1, -1, -1},  // face 10
        {-1, -1, -1, -1, -1, -1, IJ, KI, -1, -1,
            -1, 0,  -1, -1, -1, -1, JK, -1, -1, -1},  // face 11
        {-1, -1, -1, -1, -1, -1, -1, IJ, KI, -1,
            -1, -1, 0,  -1, -1, -1, -1, JK, -1, -1},  // face 12
        {-1, -1, -1, -1, -1, -1, -1, -1, IJ, KI,
            -1, -1, -1, 0,  -1, -1, -1, -1, JK, -1},  // face 13
        {-1, -1, -1, -1, -1, KI, -1, -1, -1, IJ,
            -1, -1, -1, -1, 0,  -1, -1, -1, -1, JK},  // face 14
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            JK, -1, -1, -1, -1, 0,  IJ, -1, -1, KI},  // face 15
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, JK, -1, -1, -1, KI, 0,  IJ, -1, -1},  // face 16
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, JK, -1, -1, -1, KI, 0,  IJ, -1},  // face 17
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, JK, -1, -1, -1, KI, 0,  IJ},  // face 18
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, JK, IJ, -1, -1, KI, 0}    // face 19
    };

    public static readonly FaceOrientIJK[,] OrientedFaceNeighbours = {
        {
            // face 0
            (0, (0, 0, 0), 0),  // central face
            (4, (2, 0, 2), 1),  // ij quadrant
            (1, (2, 2, 0), 5),  // ki quadrant
            (5, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 1
            (1, (0, 0, 0), 0),  // central face
            (0, (2, 0, 2), 1),  // ij quadrant
            (2, (2, 2, 0), 5),  // ki quadrant
            (6, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 2
            (2, (0, 0, 0), 0),  // central face
            (1, (2, 0, 2), 1),  // ij quadrant
            (3, (2, 2, 0), 5),  // ki quadrant
            (7, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 3
            (3, (0, 0, 0), 0),  // central face
            (2, (2, 0, 2), 1),  // ij quadrant
            (4, (2, 2, 0), 5),  // ki quadrant
            (8, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 4
            (4, (0, 0, 0), 0),  // central face
            (3, (2, 0, 2), 1),  // ij quadrant
            (0, (2, 2, 0), 5),  // ki quadrant
            (9, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 5
            (5, (0, 0, 0), 0),   // central face
            (10, (2, 2, 0), 3),  // ij quadrant
            (14, (2, 0, 2), 3),  // ki quadrant
            (0, (0, 2, 2), 3)    // jk quadrant
        },
        {
            // face 6
            (6, (0, 0, 0), 0),   // central face
            (11, (2, 2, 0), 3),  // ij quadrant
            (10, (2, 0, 2), 3),  // ki quadrant
            (1, (0, 2, 2), 3)    // jk quadrant
        },
        {
            // face 7
            (7, (0, 0, 0), 0),   // central face
            (12, (2, 2, 0), 3),  // ij quadrant
            (11, (2, 0, 2), 3),  // ki quadrant
            (2, (0, 2, 2), 3)    // jk quadrant
        },
        {
            // face 8
            (8, (0, 0, 0), 0),   // central face
            (13, (2, 2, 0), 3),  // ij quadrant
            (12, (2, 0, 2), 3),  // ki quadrant
            (3, (0, 2, 2), 3)    // jk quadrant
        },
        {
            // face 9
            (9, (0, 0, 0), 0),   // central face
            (14, (2, 2, 0), 3),  // ij quadrant
            (13, (2, 0, 2), 3),  // ki quadrant
            (4, (0, 2, 2), 3)    // jk quadrant
        },
        {
            // face 10
            (10, (0, 0, 0), 0),  // central face
            (5, (2, 2, 0), 3),   // ij quadrant
            (6, (2, 0, 2), 3),   // ki quadrant
            (15, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 11
            (11, (0, 0, 0), 0),  // central face
            (6, (2, 2, 0), 3),   // ij quadrant
            (7, (2, 0, 2), 3),   // ki quadrant
            (16, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 12
            (12, (0, 0, 0), 0),  // central face
            (7, (2, 2, 0), 3),   // ij quadrant
            (8, (2, 0, 2), 3),   // ki quadrant
            (17, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 13
            (13, (0, 0, 0), 0),  // central face
            (8, (2, 2, 0), 3),   // ij quadrant
            (9, (2, 0, 2), 3),   // ki quadrant
            (18, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 14
            (14, (0, 0, 0), 0),  // central face
            (9, (2, 2, 0), 3),   // ij quadrant
            (5, (2, 0, 2), 3),   // ki quadrant
            (19, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 15
            (15, (0, 0, 0), 0),  // central face
            (16, (2, 0, 2), 1),  // ij quadrant
            (19, (2, 2, 0), 5),  // ki quadrant
            (10, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 16
            (16, (0, 0, 0), 0),  // central face
            (17, (2, 0, 2), 1),  // ij quadrant
            (15, (2, 2, 0), 5),  // ki quadrant
            (11, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 17
            (17, (0, 0, 0), 0),  // central face
            (18, (2, 0, 2), 1),  // ij quadrant
            (16, (2, 2, 0), 5),  // ki quadrant
            (12, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 18
            (18, (0, 0, 0), 0),  // central face
            (19, (2, 0, 2), 1),  // ij quadrant
            (17, (2, 2, 0), 5),  // ki quadrant
            (13, (0, 2, 2), 3)   // jk quadrant
        },
        {
            // face 19
            (19, (0, 0, 0), 0),  // central face
            (15, (2, 0, 2), 1),  // ij quadrant
            (18, (2, 2, 0), 5),  // ki quadrant
            (14, (0, 2, 2), 3)   // jk quadrant
        }
    };

    public static readonly LatLng[] GeoFaceCenters = {
        new(0.803582649718989942, 1.248397419617396099),    // face  0
        new(1.307747883455638156, 2.536945009877921159),    // face  1
        new(1.054751253523952054, -1.347517358900396623),   // face  2
        new(0.600191595538186799, -0.450603909469755746),   // face  3
        new(0.491715428198773866, 0.401988202911306943),    // face  4
        new(0.172745327415618701, 1.678146885280433686),    // face  5
        new(0.605929321571350690, 2.953923329812411617),    // face  6
        new(0.427370518328979641, -1.888876200336285401),   // face  7
        new(-0.079066118549212831, -0.733429513380867741),  // face  8
        new(-0.230961644455383637, 0.506495587332349035),   // face  9
        new(0.079066118549212831, 2.408163140208925497),    // face 10
        new(0.230961644455383637, -2.635097066257444203),   // face 11
        new(-0.172745327415618701, -1.463445768309359553),  // face 12
        new(-0.605929321571350690, -0.187669323777381622),  // face 13
        new(-0.427370518328979641, 1.252716453253507838),   // face 14
        new(-0.600191595538186799, 2.690988744120037492),   // face 15
        new(-0.491715428198773866, -2.739604450678486295),  // face 16
        new(-0.803582649718989942, -1.893195233972397139),  // face 17
        new(-1.307747883455638156, -0.604647643711872080),  // face 18
        new(-1.054751253523952054, 1.794075294689396615),   // face 19
    };

    public static readonly Vec3d[] FaceCenters = {
        new(0.2199307791404606, 0.6583691780274996, 0.7198475378926182),     // face  0
        new(-0.2139234834501421, 0.1478171829550703, 0.9656017935214205),    // face  1
        new(0.1092625278784797, -0.4811951572873210, 0.8697775121287253),    // face  2
        new(0.7428567301586791, -0.3593941678278028, 0.5648005936517033),    // face  3
        new(0.8112534709140969, 0.3448953237639384, 0.4721387736413930),     // face  4
        new(-0.1055498149613921, 0.9794457296411413, 0.1718874610009365),    // face  5
        new(-0.8075407579970092, 0.1533552485898818, 0.5695261994882688),    // face  6
        new(-0.2846148069787907, -0.8644080972654206, 0.4144792552473539),   // face  7
        new(0.7405621473854482, -0.6673299564565524, -0.0789837646326737),   // face  8
        new(0.8512303986474293, 0.4722343788582681, -0.2289137388687808),    // face  9
        new(-0.7405621473854481, 0.6673299564565524, 0.0789837646326737),    // face 10
        new(-0.8512303986474292, -0.4722343788582682, 0.2289137388687808),   // face 11
        new(0.1055498149613919, -0.9794457296411413, -0.1718874610009365),   // face 12
        new(0.8075407579970092, -0.1533552485898819, -0.5695261994882688),   // face 13
        new(0.2846148069787908, 0.8644080972654204, -0.4144792552473539),    // face 14
        new(-0.7428567301586791, 0.3593941678278027, -0.5648005936517033),   // face 15
        new(-0.8112534709140971, -0.3448953237639382, -0.4721387736413930),  // face 16
        new(-0.2199307791404607, -0.6583691780274996, -0.7198475378926182),  // face 17
        new(0.2139234834501420, -0.1478171829550704, -0.9656017935214205),   // face 18
        new(-0.1092625278784796, 0.4811951572873210, -0.8697775121287253),   // face 19
    };

    /// <summary>
    /// Table of direction-to-face mapping for each pentagon.   Note that
    /// faces are in directional order, starting at J_AXES_DIGIT.
    /// </summary>
    public static readonly PentagonDirectionToFaceMapping[] PentagonDirectionFaces = {
        (4, (4, 0, 2, 1, 3)),
        (14, (6, 11, 2, 7, 1)),
        (24, (5, 10, 1, 6, 0)),
        (38, (7, 12, 3, 8, 2)),
        (49, (9, 14, 0, 5, 4)),
        (58, (8, 13, 4, 9, 3)),
        (63, (11, 6, 15, 10, 16)),
        (72, (12, 7, 16, 11, 17)),
        (83, (10, 5, 19, 14, 15)),
        (97, (13, 8, 17, 12, 18)),
        (107, (14, 9, 18, 13, 19)),
        (117, (15, 19, 17, 18, 16))
    };


    #endregion faces

    #region other
    public static readonly int[] MaxDistanceByClass2Res = {
        2,        // res  0
        -1,       // res  1
        14,       // res  2
        -1,       // res  3
        98,       // res  4
        -1,       // res  5
        686,      // res  6
        -1,       // res  7
        4802,     // res  8
        -1,       // res  9
        33614,    // res 10
        -1,       // res 11
        235298,   // res 12
        -1,       // res 13
        1647086,  // res 14
        -1,       // res 15
        11529602  // res 16
    };

    public static readonly int[] UnitScaleByClass2Res = {
        1,       // res  0
        -1,      // res  1
        7,       // res  2
        -1,      // res  3
        49,      // res  4
        -1,      // res  5
        343,     // res  6
        -1,      // res  7
        2401,    // res  8
        -1,      // res  9
        16807,   // res 10
        -1,      // res 11
        117649,  // res 12
        -1,      // res 13
        823543,  // res 14
        -1,      // res 15
        5764801  // res 16
    };

    /// <summary>
    /// Directions used for traversing a hexagonal ring counterclockwise around
    /// {1, 0, 0}.
    ///
    /// <pre>
    ///       _
    ///     _/ \\_
    ///    / \\5/ \\
    ///    \\0/ \\4/
    ///    / \\_/ \\
    ///    \\1/ \\3/
    ///      \\2/
    /// </pre>
    /// </summary>
    public static readonly Direction[] CounterClockwiseDirections = {
        Direction.J,
        Direction.JK,
        Direction.K,
        Direction.IK,
        Direction.I,
        Direction.IJ
    };

    /// <summary>
    /// Direction used for traversing to the next outward hexagonal ring.
    /// </summary>
    public const Direction NextRingDirection = Direction.I;

    /// <summary>
    /// Origin leading digit -> index leading digit -> rotations 60 cw
    /// Either being 1 (K axis) is invalid.
    /// No good default at 0.
    /// </summary>
    public static readonly int[,] PentagonRotations = {
        {0, -1, 0, 0, 0, 0, 0},        // 0
        {-1, -1, -1, -1, -1, -1, -1},  // 1
        {0, -1, 0, 0, 0, 1, 0},        // 2
        {0, -1, 0, 0, 1, 1, 0},        // 3
        {0, -1, 0, 5, 0, 0, 0},        // 4
        {0, -1, 5, 5, 0, 0, 0},        // 5
        {0, -1, 0, 0, 0, 0, 0},        // 6
    };

    /// <summary>
    /// Reverse base cell direction -> leading index digit -> rotations 60 ccw.
    /// For reversing the rotation introduced in PnetagonRotations when
    /// the origin is on a pentagon (regardless of the base cell of the index.)
    /// </summary>
    public static readonly int[,] PentagonRotationsInReverse = {
        {0, 0, 0, 0, 0, 0, 0},         // 0
        {-1, -1, -1, -1, -1, -1, -1},  // 1
        {0, 1, 0, 0, 0, 0, 0},         // 2
        {0, 1, 0, 0, 0, 1, 0},         // 3
        {0, 5, 0, 0, 0, 0, 0},         // 4
        {0, 5, 0, 5, 0, 0, 0},         // 5
        {0, 0, 0, 0, 0, 0, 0},         // 6
    };

    /// <summary>
    /// Reverse base cell direction -> leading index digit -> rotations 60 ccw.
    /// For reversing the rotation introduced in PentagonRotations when the index is
    /// on a pentagon and the origin is not.
    /// </summary>
    public static readonly int[,] NonPolarPentagonRotationsInReverse = {
        {0, 0, 0, 0, 0, 0, 0},         // 0
        {-1, -1, -1, -1, -1, -1, -1},  // 1
        {0, 1, 0, 0, 0, 0, 0},         // 2
        {0, 1, 0, 0, 0, 1, 0},         // 3
        {0, 5, 0, 0, 0, 0, 0},         // 4
        {0, 1, 0, 5, 1, 1, 0},         // 5
        {0, 0, 0, 0, 0, 0, 0},         // 6
    };

    /// <summary>
    /// Reverse base cell direction -> leading index digit -> rotations 60 ccw.
    /// For reversing the rotation introduced in PentagonRotations when the index is
    /// on a polar pentagon and the origin is not.
    /// </summary>
    public static readonly int[,] PolarPentagonRotationsInReverse = {
        {0, 0, 0, 0, 0, 0, 0},         // 0
        {-1, -1, -1, -1, -1, -1, -1},  // 1
        {0, 1, 1, 1, 1, 1, 1},         // 2
        {0, 1, 0, 0, 0, 1, 0},         // 3
        {0, 1, 0, 0, 1, 1, 1},         // 4
        {0, 1, 0, 5, 1, 1, 0},         // 5
        {0, 1, 1, 0, 1, 1, 1},         // 6
    };

    /// <summary>
    /// Prohibited directions when unfolding a pentagon.
    /// </summary>
    /// <remarks>
    /// Indexes by two directions, both relative to the pentagon base cell. The first
    /// is the direction of the origin index and the second is the direction of the
    /// index to unfold. Direction refers to the direction from base cell to base
    /// cell if the indexes are on different base cells, or the leading digit if
    /// within the pentagon base cell.
    ///
    /// This previously included a Class II/Class III check but these were removed
    /// due to failure cases. It's possible this could be restricted to a narrower
    /// set of a failure cases. Currently, the logic is any unfolding across more
    /// than one icosahedron face is not permitted.
    /// </remarks>
    public static readonly bool[,] UnfoldableDirections = {
        {false, false, false, false, false, false, false},  // 0
        {false, false, false, false, false, false, false},  // 1
        {false, false, false, false, true, true, false},    // 2
        {false, false, false, false, true, false, true},    // 3
        {false, false, true, true, false, false, false},    // 4
        {false, false, true, false, false, false, true},    // 5
        {false, false, false, true, false, true, false},    // 6
    };

    /// <summary>
    /// The average area of a hexagon cell at each resolution, in km^2.  Excludes
    /// the 12 pentagon cells per resolution.
    /// </summary>
    public static readonly double[] HexgonAreasInKm2 = {
        4.357449416078383e+06, 6.097884417941332e+05, 8.680178039899720e+04,
        1.239343465508816e+04, 1.770347654491307e+03, 2.529038581819449e+02,
        3.612906216441245e+01, 5.161293359717191e+00, 7.373275975944177e-01,
        1.053325134272067e-01, 1.504750190766435e-02, 2.149643129451879e-03,
        3.070918756316060e-04, 4.387026794728296e-05, 6.267181135324313e-06,
        8.953115907605790e-07
    };

    /// <summary>
    /// The average area of a hexagon cell at each resolution, in m^2.  Excludes
    /// the 12 pentagon cells per resolution.
    /// </summary>
    public static readonly double[] HexagonAreasInM2 = {
        4.357449416078390e+12, 6.097884417941339e+11, 8.680178039899731e+10,
        1.239343465508818e+10, 1.770347654491309e+09, 2.529038581819452e+08,
        3.612906216441250e+07, 5.161293359717198e+06, 7.373275975944188e+05,
        1.053325134272069e+05, 1.504750190766437e+04, 2.149643129451882e+03,
        3.070918756316063e+02, 4.387026794728301e+01, 6.267181135324322e+00,
        8.953115907605802e-01
    };

    /// <summary>
    /// The average edge length of a hexagon cell at each resolution, in km.
    /// Excludes the 12 pentagon cells per resolution.
    /// </summary>
    public static readonly double[] EdgeLengthsInKm = {
        1281.256011, 483.0568391, 182.5129565, 68.97922179,
        26.07175968, 9.854090990, 3.724532667, 1.406475763,
        0.531414010, 0.200786148, 0.075863783, 0.028663897,
        0.010830188, 0.004092010, 0.001546100, 0.000584169
    };

    /// <summary>
    /// The average edge length of a hexagon cell at each resolution, in m.
    /// Excludes the 12 pentagon cells per resolution.
    /// </summary>
    public static readonly double[] EdgeLengthsInM = {
        1281256.011, 483056.8391, 182512.9565, 68979.22179,
        26071.75968, 9854.090990, 3724.532667, 1406.475763,
        531.4140101, 200.7861476, 75.86378287, 28.66389748,
        10.83018784, 4.092010473, 1.546099657, 0.584168630
    };

    #endregion other

}