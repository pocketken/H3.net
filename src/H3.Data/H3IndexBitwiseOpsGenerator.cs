using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace H3.Data {

    [Generator]
    public class H3IndexBitwiseOpsGenerator : ISourceGenerator {

        private static readonly string _indent = string.Empty.PadLeft(16, ' ');

        private const string TEMPLATE = @"using H3.Model;
using System.Runtime.CompilerServices;

namespace H3 {{

    public sealed partial class H3Index {{

        /// <summary>
        /// Perform in-place 60 degree clockwise rotation(s) of the index.
        /// </summary>
        /// <param name=""rotations"">number of rotations to perform</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateClockwise(int rotations) {{
            if (rotations <= 0) return;
            Value = Resolution switch {{{0}
                _ => Value
            }};
        }}

        /// <summary>
        /// Perform in-place 60 degree clockwise rotation(s) of the index.
        /// </summary>
        /// <param name=""rotations"">number of rotations to perform</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateCounterClockwise(int rotations) {{
            if (rotations <= 0) return;
            Value = Resolution switch {{{1}
                _ => Value
            }};
        }}

    }}

}}";

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context) {
            var rotateHexCw = new StringBuilder();
            var rotateHexCcw = new StringBuilder();

            for (var r = 1; r <= 15; r += 1) {
                var mask = ~0UL;
                mask <<= 3 * r;
                mask = ~mask;
                mask <<= 3 * (15 - r);
                mask = ~mask;

                rotateHexCw.Append($"\n{_indent}{r} => (Value & {mask}UL) |\n");
                rotateHexCcw.Append($"\n{_indent}{r} => (Value & {mask}UL) |\n");

                for (var c = 1; c <= r; c += 1) {
                    var offset = (15 - c) * 3;
                    var eol = c == r ? "," : " |\n";
                    rotateHexCw.Append($"{_indent}    ((ulong)((Direction)((Value >> {offset}) & H3_DIGIT_MASK)).RotateClockwise(rotations) << {offset}){eol}");
                    rotateHexCcw.Append($"{_indent}    ((ulong)((Direction)((Value >> {offset}) & H3_DIGIT_MASK)).RotateCounterClockwise(rotations) << {offset}){eol}");
                }
            }

            context.AddSource("H3Index.BitwiseOperations.cs", string.Format(TEMPLATE, rotateHexCw, rotateHexCcw));
        }

    }
}
