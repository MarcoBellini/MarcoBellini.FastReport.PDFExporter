using SkiaSharp;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Maps GDI+ state semantics (Save/Restore, transform get/set, ResetClip) onto a Skia canvas.
/// The transform exposed to FastReport excludes the page base matrix (pixel to point scaling).
/// Skia clips can only shrink: resetting the clip restores the canvas to the most recent Save
/// (or to the page start) and re-applies the current transform.
/// </summary>
internal sealed class CanvasStateManager
{
    private readonly SKCanvas canvas;
    private readonly SKMatrix baseMatrix;
    private readonly SKMatrix inverseBaseMatrix;
    private readonly int pageSaveCount;
    private readonly Stack<int> saveCounts = new();

    public CanvasStateManager(SKCanvas canvas)
    {
        this.canvas = canvas;

        baseMatrix = canvas.TotalMatrix;
        inverseBaseMatrix = baseMatrix.TryInvert(out var inverse) ? inverse : SKMatrix.Identity;
        pageSaveCount = canvas.Save();
    }

    /// <summary>
    /// Current transform in FastReport pixel space.
    /// </summary>
    public SKMatrix Transform
    {
        get => inverseBaseMatrix.PreConcat(canvas.TotalMatrix);
        set
        {
            var total = baseMatrix.PreConcat(value);
            canvas.SetMatrix(in total);
        }
    }

    public int Save()
    {
        var count = canvas.Save();
        saveCounts.Push(count);
        return count;
    }

    public void Restore(int saveCount)
    {
        canvas.RestoreToCount(saveCount);

        while (saveCounts.Count > 0 && saveCounts.Peek() >= saveCount)
            saveCounts.Pop();
    }

    /// <summary>
    /// Multiply the current transform. <paramref name="prepend"/> applies <paramref name="matrix"/> before the current transform (GDI+ default).
    /// </summary>
    public void MultiplyTransform(SKMatrix matrix, bool prepend)
    {
        Transform = prepend ? Transform.PreConcat(matrix) : Transform.PostConcat(matrix);
    }

    public void ResetClip()
    {
        var current = canvas.TotalMatrix;
        var restoreTo = saveCounts.Count > 0 ? saveCounts.Peek() : pageSaveCount;

        canvas.RestoreToCount(restoreTo);
        canvas.Save();
        canvas.SetMatrix(in current);
    }

    /// <summary>
    /// Restore the canvas to the state it had when the manager was created.
    /// </summary>
    public void RestorePage()
    {
        canvas.RestoreToCount(pageSaveCount);
        saveCounts.Clear();
    }
}
