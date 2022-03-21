using System.Linq;
using static H3.Constants;

#nullable enable

namespace H3.Model; 

public class PentagonDirectionToFaceMapping {
    public int BaseCellNumber { get; init; }
    public int[] Faces { get; init; } = new int[NUM_PENT_VERTS];

    public BaseCell BaseCell => BaseCells.Cells[BaseCellNumber];

    public static PentagonDirectionToFaceMapping ForBaseCell(int baseCellNumber) =>
        LookupTables.PentagonDirectionFaces.First(df => df.BaseCellNumber == baseCellNumber);

    public static implicit operator PentagonDirectionToFaceMapping((int, (int, int, int, int, int)) data) {
        return new PentagonDirectionToFaceMapping {
            BaseCellNumber = data.Item1,
            Faces = new[] {
                data.Item2.Item1,
                data.Item2.Item2,
                data.Item2.Item3,
                data.Item2.Item4,
                data.Item2.Item5
            }
        };
    }
}