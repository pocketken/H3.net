using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace H3.Data {

    // TODO use templating for the actual code bits, just feed in vars

    [Generator]
    public class BaseCellsGenerator : ISourceGenerator {

        private static readonly string _indent = string.Empty.PadLeft(12, ' ');

        private const string Template = @"using System.Collections.Generic;

namespace H3.Model {{

    /// <summary>
    /// All of the 122 base cells that comprise the H3 indexing scheme.
    /// </summary>
    public sealed class BaseCells {{

        private static readonly sbyte[] NotAPentagonOffsets = {{ 0, 0 }};
{0}
        public static readonly BaseCell[] Cells = {{
{1}
        }};

    }}

}}
";

        public void Execute(GeneratorExecutionContext context) {
            var cells = new StringBuilder();
            var cellNames = new StringBuilder();

            for (var c = 0; c < Constants.NUM_BASE_CELLS; c += 1) {
                var cell = LookupTables.BaseCells[c];
                cellNames.Append($"{_indent}BaseCell{c}{(c < 121 ? ",\n" : "")}");

                var neighbouringCells = new List<int>();
                var neighbourRotations = new List<int>();
                var neighbourDirections = new Dictionary<sbyte, Direction>();

                for (var d = 0; d < 7; d += 1) {
                    var n = (sbyte)LookupTables.Neighbours[c, d];
                    neighbouringCells.Add(n);
                    neighbourRotations.Add(LookupTables.NeighbourCounterClockwiseRotations[c, d]);
                    neighbourDirections[n] = (Direction)d;
                }

                cells.Append($@"
        private static readonly BaseCell BaseCell{c} = new() {{
            Cell = {c},
            Home = new FaceIJK {{
                Face = {cell.Home.Face},
                Coord = new CoordIJK {{ I = {cell.Home.I}, J = {cell.Home.J}, K = {cell.Home.K} }}
            }},
            IsPentagon = {(cell.IsPentagon ? "true" : "false")},
            IsPolarPentagon = {(c == 4 || c == 117 ? "true" : "false")},
            ClockwiseOffsetPent = {(cell.IsPentagon ? $"new sbyte[] {{ {cell.ClockwiseOffsetPent[0]}, {cell.ClockwiseOffsetPent[1]} }}" : "NotAPentagonOffsets")},
            NeighbouringCells = new sbyte[] {{ {string.Join(", ", neighbouringCells)} }},
            NeighbourRotations = new sbyte[] {{ {string.Join(", ", neighbourRotations)} }},
            NeighbourDirections = new Dictionary<sbyte, Direction> {{
                {string.Join(", ", neighbourDirections.Select(e => $"{{ {e.Key}, Direction.{e.Value} }}"))}
            }}
        }};
");
            }

            context.AddSource("BaseCells.cs", string.Format(Template, cells, cellNames));
        }

        public void Initialize(GeneratorInitializationContext context) { }

    }

}