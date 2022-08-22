using System;
using System.Collections.Generic;
using System.Linq;
using static H3.Constants;

namespace H3.Model; 

public static class LookupTables {

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
    public static readonly BaseCellRotation[,,,] FaceIjkBaseCells = {
        {
            // face 0
            {
                // i 0
                {(16, 0), (18, 0), (24, 0)},  // j 0
                {(33, 0), (30, 0), (32, 3)},  // j 1
                {(49, 1), (48, 3), (50, 3)}   // j 2
            },
            {
                // i 1
                {(8, 0), (5, 5), (10, 5)},    // j 0
                {(22, 0), (16, 0), (18, 0)},  // j 1
                {(41, 1), (33, 0), (30, 0)}   // j 2
            },
            {
                // i 2
                {(4, 0), (0, 5), (2, 5)},    // j 0
                {(15, 1), (8, 0), (5, 5)},   // j 1
                {(31, 1), (22, 0), (16, 0)}  // j 2
            }
        },
        {
            // face 1
            {
                // i 0
                {(2, 0), (6, 0), (14, 0)},    // j 0
                {(10, 0), (11, 0), (17, 3)},  // j 1
                {(24, 1), (23, 3), (25, 3)}   // j 2
            },
            {
                // i 1
                {(0, 0), (1, 5), (9, 5)},    // j 0
                {(5, 0), (2, 0), (6, 0)},    // j 1
                {(18, 1), (10, 0), (11, 0)}  // j 2
            },
            {
                // i 2
                {(4, 1), (3, 5), (7, 5)},  // j 0
                {(8, 1), (0, 0), (1, 5)},  // j 1
                {(16, 1), (5, 0), (2, 0)}  // j 2
            }
        },
        {
            // face 2
            {
                // i 0
                {(7, 0), (21, 0), (38, 0)},  // j 0
                {(9, 0), (19, 0), (34, 3)},  // j 1
                {(14, 1), (20, 3), (36, 3)}  // j 2
            },
            {
                // i 1
                {(3, 0), (13, 5), (29, 5)},  // j 0
                {(1, 0), (7, 0), (21, 0)},   // j 1
                {(6, 1), (9, 0), (19, 0)}    // j 2
            },
            {
                // i 2
                {(4, 2), (12, 5), (26, 5)},  // j 0
                {(0, 1), (3, 0), (13, 5)},   // j 1
                {(2, 1), (1, 0), (7, 0)}     // j 2
            }
        },
        {
            // face 3
            {
                // i 0
                {(26, 0), (42, 0), (58, 0)},  // j 0
                {(29, 0), (43, 0), (62, 3)},  // j 1
                {(38, 1), (47, 3), (64, 3)}   // j 2
            },
            {
                // i 1
                {(12, 0), (28, 5), (44, 5)},  // j 0
                {(13, 0), (26, 0), (42, 0)},  // j 1
                {(21, 1), (29, 0), (43, 0)}   // j 2
            },
            {
                // i 2
                {(4, 3), (15, 5), (31, 5)},  // j 0
                {(3, 1), (12, 0), (28, 5)},  // j 1
                {(7, 1), (13, 0), (26, 0)}   // j 2
            }
        },
        {
            // face 4
            {
                // i 0
                {(31, 0), (41, 0), (49, 0)},  // j 0
                {(44, 0), (53, 0), (61, 3)},  // j 1
                {(58, 1), (65, 3), (75, 3)}   // j 2
            },
            {
                // i 1
                {(15, 0), (22, 5), (33, 5)},  // j 0
                {(28, 0), (31, 0), (41, 0)},  // j 1
                {(42, 1), (44, 0), (53, 0)}   // j 2
            },
            {
                // i 2
                {(4, 4), (8, 5), (16, 5)},    // j 0
                {(12, 1), (15, 0), (22, 5)},  // j 1
                {(26, 1), (28, 0), (31, 0)}   // j 2
            }
        },
        {
            // face 5
            {
                // i 0
                {(50, 0), (48, 0), (49, 3)},  // j 0
                {(32, 0), (30, 3), (33, 3)},  // j 1
                {(24, 3), (18, 3), (16, 3)}   // j 2
            },
            {
                // i 1
                {(70, 0), (67, 0), (66, 3)},  // j 0
                {(52, 3), (50, 0), (48, 0)},  // j 1
                {(37, 3), (32, 0), (30, 3)}   // j 2
            },
            {
                // i 2
                {(83, 0), (87, 3), (85, 3)},  // j 0
                {(74, 3), (70, 0), (67, 0)},  // j 1
                {(57, 1), (52, 3), (50, 0)}   // j 2
            }
        },
        {
            // face 6
            {
                // i 0
                {(25, 0), (23, 0), (24, 3)},  // j 0
                {(17, 0), (11, 3), (10, 3)},  // j 1
                {(14, 3), (6, 3), (2, 3)}     // j 2
            },
            {
                // i 1
                {(45, 0), (39, 0), (37, 3)},  // j 0
                {(35, 3), (25, 0), (23, 0)},  // j 1
                {(27, 3), (17, 0), (11, 3)}   // j 2
            },
            {
                // i 2
                {(63, 0), (59, 3), (57, 3)},  // j 0
                {(56, 3), (45, 0), (39, 0)},  // j 1
                {(46, 3), (35, 3), (25, 0)}   // j 2
            }
        },
        {
            // face 7
            {
                // i 0
                {(36, 0), (20, 0), (14, 3)},  // j 0
                {(34, 0), (19, 3), (9, 3)},   // j 1
                {(38, 3), (21, 3), (7, 3)}    // j 2
            },
            {
                // i 1
                {(55, 0), (40, 0), (27, 3)},  // j 0
                {(54, 3), (36, 0), (20, 0)},  // j 1
                {(51, 3), (34, 0), (19, 3)}   // j 2
            },
            {
                // i 2
                {(72, 0), (60, 3), (46, 3)},  // j 0
                {(73, 3), (55, 0), (40, 0)},  // j 1
                {(71, 3), (54, 3), (36, 0)}   // j 2
            }
        },
        {
            // face 8
            {
                // i 0
                {(64, 0), (47, 0), (38, 3)},  // j 0
                {(62, 0), (43, 3), (29, 3)},  // j 1
                {(58, 3), (42, 3), (26, 3)}   // j 2
            },
            {
                // i 1
                {(84, 0), (69, 0), (51, 3)},  // j 0
                {(82, 3), (64, 0), (47, 0)},  // j 1
                {(76, 3), (62, 0), (43, 3)}   // j 2
            },
            {
                // i 2
                {(97, 0), (89, 3), (71, 3)},  // j 0
                {(98, 3), (84, 0), (69, 0)},  // j 1
                {(96, 3), (82, 3), (64, 0)}   // j 2
            }
        },
        {
            // face 9
            {
                // i 0
                {(75, 0), (65, 0), (58, 3)},  // j 0
                {(61, 0), (53, 3), (44, 3)},  // j 1
                {(49, 3), (41, 3), (31, 3)}   // j 2
            },
            {
                // i 1
                {(94, 0), (86, 0), (76, 3)},  // j 0
                {(81, 3), (75, 0), (65, 0)},  // j 1
                {(66, 3), (61, 0), (53, 3)}   // j 2
            },
            {
                // i 2
                {(107, 0), (104, 3), (96, 3)},  // j 0
                {(101, 3), (94, 0), (86, 0)},   // j 1
                {(85, 3), (81, 3), (75, 0)}     // j 2
            }
        },
        {
            // face 10
            {
                // i 0
                {(57, 0), (59, 0), (63, 3)},  // j 0
                {(74, 0), (78, 3), (79, 3)},  // j 1
                {(83, 3), (92, 3), (95, 3)}   // j 2
            },
            {
                // i 1
                {(37, 0), (39, 3), (45, 3)},  // j 0
                {(52, 0), (57, 0), (59, 0)},  // j 1
                {(70, 3), (74, 0), (78, 3)}   // j 2
            },
            {
                // i 2
                {(24, 0), (23, 3), (25, 3)},  // j 0
                {(32, 3), (37, 0), (39, 3)},  // j 1
                {(50, 3), (52, 0), (57, 0)}   // j 2
            }
        },
        {
            // face 11
            {
                // i 0
                {(46, 0), (60, 0), (72, 3)},  // j 0
                {(56, 0), (68, 3), (80, 3)},  // j 1
                {(63, 3), (77, 3), (90, 3)}   // j 2
            },
            {
                // i 1
                {(27, 0), (40, 3), (55, 3)},  // j 0
                {(35, 0), (46, 0), (60, 0)},  // j 1
                {(45, 3), (56, 0), (68, 3)}   // j 2
            },
            {
                // i 2
                {(14, 0), (20, 3), (36, 3)},  // j 0
                {(17, 3), (27, 0), (40, 3)},  // j 1
                {(25, 3), (35, 0), (46, 0)}   // j 2
            }
        },
        {
            // face 12
            {
                // i 0
                {(71, 0), (89, 0), (97, 3)},   // j 0
                {(73, 0), (91, 3), (103, 3)},  // j 1
                {(72, 3), (88, 3), (105, 3)}   // j 2
            },
            {
                // i 1
                {(51, 0), (69, 3), (84, 3)},  // j 0
                {(54, 0), (71, 0), (89, 0)},  // j 1
                {(55, 3), (73, 0), (91, 3)}   // j 2
            },
            {
                // i 2
                {(38, 0), (47, 3), (64, 3)},  // j 0
                {(34, 3), (51, 0), (69, 3)},  // j 1
                {(36, 3), (54, 0), (71, 0)}   // j 2
            }
        },
        {
            // face 13
            {
                // i 0
                {(96, 0), (104, 0), (107, 3)},  // j 0
                {(98, 0), (110, 3), (115, 3)},  // j 1
                {(97, 3), (111, 3), (119, 3)}   // j 2
            },
            {
                // i 1
                {(76, 0), (86, 3), (94, 3)},   // j 0
                {(82, 0), (96, 0), (104, 0)},  // j 1
                {(84, 3), (98, 0), (110, 3)}   // j 2
            },
            {
                // i 2
                {(58, 0), (65, 3), (75, 3)},  // j 0
                {(62, 3), (76, 0), (86, 3)},  // j 1
                {(64, 3), (82, 0), (96, 0)}   // j 2
            }
        },
        {
            // face 14
            {
                // i 0
                {(85, 0), (87, 0), (83, 3)},     // j 0
                {(101, 0), (102, 3), (100, 3)},  // j 1
                {(107, 3), (112, 3), (114, 3)}   // j 2
            },
            {
                // i 1
                {(66, 0), (67, 3), (70, 3)},   // j 0
                {(81, 0), (85, 0), (87, 0)},   // j 1
                {(94, 3), (101, 0), (102, 3)}  // j 2
            },
            {
                // i 2
                {(49, 0), (48, 3), (50, 3)},  // j 0
                {(61, 3), (66, 0), (67, 3)},  // j 1
                {(75, 3), (81, 0), (85, 0)}   // j 2
            }
        },
        {
            // face 15
            {
                // i 0
                {(95, 0), (92, 0), (83, 0)},  // j 0
                {(79, 0), (78, 0), (74, 3)},  // j 1
                {(63, 1), (59, 3), (57, 3)}   // j 2
            },
            {
                // i 1
                {(109, 0), (108, 0), (100, 5)},  // j 0
                {(93, 1), (95, 0), (92, 0)},     // j 1
                {(77, 1), (79, 0), (78, 0)}      // j 2
            },
            {
                // i 2
                {(117, 4), (118, 5), (114, 5)},  // j 0
                {(106, 1), (109, 0), (108, 0)},  // j 1
                {(90, 1), (93, 1), (95, 0)}      // j 2
            }
        },
        {
            // face 16
            {
                // i 0
                {(90, 0), (77, 0), (63, 0)},  // j 0
                {(80, 0), (68, 0), (56, 3)},  // j 1
                {(72, 1), (60, 3), (46, 3)}   // j 2
            },
            {
                // i 1
                {(106, 0), (93, 0), (79, 5)},  // j 0
                {(99, 1), (90, 0), (77, 0)},   // j 1
                {(88, 1), (80, 0), (68, 0)}    // j 2
            },
            {
                // i 2
                {(117, 3), (109, 5), (95, 5)},  // j 0
                {(113, 1), (106, 0), (93, 0)},  // j 1
                {(105, 1), (99, 1), (90, 0)}    // j 2
            }
        },
        {
            // face 17
            {
                // i 0
                {(105, 0), (88, 0), (72, 0)},  // j 0
                {(103, 0), (91, 0), (73, 3)},  // j 1
                {(97, 1), (89, 3), (71, 3)}    // j 2
            },
            {
                // i 1
                {(113, 0), (99, 0), (80, 5)},   // j 0
                {(116, 1), (105, 0), (88, 0)},  // j 1
                {(111, 1), (103, 0), (91, 0)}   // j 2
            },
            {
                // i 2
                {(117, 2), (106, 5), (90, 5)},  // j 0
                {(121, 1), (113, 0), (99, 0)},  // j 1
                {(119, 1), (116, 1), (105, 0)}  // j 2
            }
        },
        {
            // face 18
            {
                // i 0
                {(119, 0), (111, 0), (97, 0)},  // j 0
                {(115, 0), (110, 0), (98, 3)},  // j 1
                {(107, 1), (104, 3), (96, 3)}   // j 2
            },
            {
                // i 1
                {(121, 0), (116, 0), (103, 5)},  // j 0
                {(120, 1), (119, 0), (111, 0)},  // j 1
                {(112, 1), (115, 0), (110, 0)}   // j 2
            },
            {
                // i 2
                {(117, 1), (113, 5), (105, 5)},  // j 0
                {(118, 1), (121, 0), (116, 0)},  // j 1
                {(114, 1), (120, 1), (119, 0)}   // j 2
            }
        },
        {
            // face 19
            {
                // i 0
                {(114, 0), (112, 0), (107, 0)},  // j 0
                {(100, 0), (102, 0), (101, 3)},  // j 1
                {(83, 1), (87, 3), (85, 3)}      // j 2
            },
            {
                // i 1
                {(118, 0), (120, 0), (115, 5)},  // j 0
                {(108, 1), (114, 0), (112, 0)},  // j 1
                {(92, 1), (100, 0), (102, 0)}    // j 2
            },
            {
                // i 2
                {(117, 0), (121, 5), (119, 5)},  // j 0
                {(109, 1), (118, 0), (120, 0)},  // j 1
                {(95, 1), (108, 1), (114, 0)}    // j 2
            }
        }
    };

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

    public static readonly Dictionary<Direction, CoordIJK> DirectionToUnitVector =
        Enum.GetValues(typeof(Direction)).Cast<Direction>().ToDictionary(e => e, e => e switch {
            Direction.Invalid => CoordIJK.InvalidIJKCoordinate,
            _ => UnitVectors[(int)e]
        });

    public static readonly Dictionary<CoordIJK, Direction> UnitVectorToDirection =
        DirectionToUnitVector.ToDictionary(e => e.Value, e => e.Key);

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
    /// New digit when traversing along class II grids.
    ///
    /// Current digit -> direction -> new digit.
    /// </summary>
    public static readonly Direction[,] NewDirectionClass2 = {
        {
            Direction.Center, Direction.K, Direction.J, Direction.JK, Direction.I,
            Direction.IK, Direction.IJ
        },
        {
            Direction.K, Direction.I, Direction.JK, Direction.IJ, Direction.IK,
            Direction.J, Direction.Center
        },
        {
            Direction.J, Direction.JK, Direction.K, Direction.I, Direction.IJ,
            Direction.Center, Direction.IK
        },
        {
            Direction.JK, Direction.IJ, Direction.I, Direction.IK, Direction.Center,
            Direction.K, Direction.J
        },
        {
            Direction.I, Direction.IK, Direction.IJ, Direction.Center, Direction.J,
            Direction.JK, Direction.K
        },
        {
            Direction.IK, Direction.J, Direction.Center, Direction.K, Direction.JK,
            Direction.IJ, Direction.I
        },
        {
            Direction.IJ, Direction.Center, Direction.IK, Direction.J, Direction.K,
            Direction.I, Direction.JK
        }
    };

    /// <summary>
    /// New traversal direction when traversing along class II grids.
    ///
    /// Current digit -> direction -> new ap7 move (at coarser level).
    /// </summary>
    public static readonly Direction[,] NewAdjustmentClass2 = {
        {
            Direction.Center, Direction.Center, Direction.Center, Direction.Center, Direction.Center,
            Direction.Center, Direction.Center
        },
        {
            Direction.Center, Direction.K, Direction.Center, Direction.K, Direction.Center,
            Direction.IK, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.J, Direction.JK, Direction.Center,
            Direction.Center, Direction.J
        },
        {
            Direction.Center, Direction.K, Direction.JK, Direction.JK, Direction.Center,
            Direction.Center, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.Center, Direction.Center, Direction.I,
            Direction.I, Direction.IJ
        },
        {
            Direction.Center, Direction.IK, Direction.Center, Direction.Center, Direction.I,
            Direction.IK, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.J, Direction.Center, Direction.IJ,
            Direction.Center, Direction.IJ
        }
    };

    /// <summary>
    /// New traversal direction when traversing along class III grids.
    ///
    /// Current digit -> direction -> new digit.
    /// </summary>
    public static readonly Direction[,] NewDirectionClass3 = {
        {
            Direction.Center, Direction.K, Direction.J, Direction.JK, Direction.I,
            Direction.IK, Direction.IJ
        },
        {
            Direction.K, Direction.J, Direction.JK, Direction.I, Direction.IK,
            Direction.IJ, Direction.Center
        },
        {
            Direction.J, Direction.JK, Direction.I, Direction.IK, Direction.IJ,
            Direction.Center, Direction.K
        },
        {
            Direction.JK, Direction.I, Direction.IK, Direction.IJ, Direction.Center,
            Direction.K, Direction.J
        },
        {
            Direction.I, Direction.IK, Direction.IJ, Direction.Center, Direction.K,
            Direction.J, Direction.JK
        },
        {
            Direction.IK, Direction.IJ, Direction.Center, Direction.K, Direction.J,
            Direction.JK, Direction.I
        },
        {
            Direction.IJ, Direction.Center, Direction.K, Direction.J, Direction.JK,
            Direction.I, Direction.IK
        }
    };

    /// <summary>
    /// New traversal direction when traversing along class III grids.
    ///
    /// Current digit -> direction -> new ap7 move (at coarser level).
    /// </summary>
    public static readonly Direction[,] NewAdjustmentClass3 = {
        {
            Direction.Center, Direction.Center, Direction.Center, Direction.Center, Direction.Center,
            Direction.Center, Direction.Center
        },
        {
            Direction.Center, Direction.K, Direction.Center, Direction.JK, Direction.Center,
            Direction.K, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.J, Direction.J, Direction.Center,
            Direction.Center, Direction.IJ
        },
        {
            Direction.Center, Direction.JK, Direction.J, Direction.JK, Direction.Center,
            Direction.Center, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.Center, Direction.Center, Direction.I,
            Direction.IK, Direction.I
        },
        {
            Direction.Center, Direction.K, Direction.Center, Direction.Center, Direction.IK,
            Direction.IK, Direction.Center
        },
        {
            Direction.Center, Direction.Center, Direction.IJ, Direction.Center, Direction.I,
            Direction.Center, Direction.IJ
        }
    };

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
    /// A list of all hex indexes that are pentagons at each resolution.
    /// </summary>
    public static readonly Dictionary<int, H3Index[]> PentagonIndexesPerResolution = Enumerable.Range(0, MAX_H3_RES + 1)
        .ToDictionary(resolution => resolution, resolution =>
            Enumerable.Range(0, NUM_BASE_CELLS - 1)
                .Where(baseCellNumber => BaseCells.Cells[baseCellNumber].IsPentagon)
                .Select(baseCellNumber => H3Index.Create(resolution, baseCellNumber, Direction.Center)).ToArray()
        );

    /// <summary>
    /// The area of hexagon cells at each resolution in km^2
    /// </summary>
    public static readonly double[] HexgonAreasInKm2 = {
        4250546.848, 607220.9782, 86745.85403, 12392.26486,
        1770.323552, 252.9033645, 36.1290521,  5.1612932,
        0.7373276,   0.1053325,   0.0150475,   0.0021496,
        0.0003071,   0.0000439,   0.0000063,   0.0000009
    };

    /// <summary>
    /// The area of hexagon cells at each resolution in m^2
    /// </summary>
    public static readonly double[] HexagonAreasInM2 = {
        4.25055E+12, 6.07221E+11, 86745854035, 12392264862,
        1770323552,  252903364.5, 36129052.1,  5161293.2,
        737327.6,    105332.5,    15047.5,     2149.6,
        307.1,       43.9,        6.3,         0.9
    };

    /// <summary>
    /// TODO figure out what these are actually used for and doc accordingly
    /// </summary>
    public static readonly double[] EdgeLengthsInKm = {
        1107.712591, 418.6760055, 158.2446558, 59.81085794,
        22.6063794,  8.544408276, 3.229482772, 1.220629759,
        0.461354684, 0.174375668, 0.065907807, 0.024910561,
        0.009415526, 0.003559893, 0.001348575, 0.000509713
    };

    /// <summary>
    /// TODO figure out what these are actually used for and doc accordingly
    /// </summary>
    public static readonly double[] EdgeLengthsInM = {
        1107712.591, 418676.0055, 158244.6558, 59810.85794,
        22606.3794,  8544.408276, 3229.482772, 1220.629759,
        461.3546837, 174.3756681, 65.90780749, 24.9105614,
        9.415526211, 3.559893033, 1.348574562, 0.509713273
    };

    #endregion other

}