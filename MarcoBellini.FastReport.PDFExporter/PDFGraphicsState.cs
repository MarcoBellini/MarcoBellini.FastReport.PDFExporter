using FastReport;
using PdfSharp.Drawing;

namespace MarcoBellini.FastReport.PDFExporter;

/// <summary>
/// Class used to store PDFSharp graphics state.
/// </summary>
internal class PdfGraphicsState : IGraphicsState
{

    private XGraphicsState state;

    /// <summary>
    /// Create new PDFGraphicsState
    /// </summary>
    /// <param name="state">PDFSharp state returned from <c>save()</c></param>
    public PdfGraphicsState(XGraphicsState state)
    {
        this.state = state;    
    }

    /// <summary>
    /// Get Saved state
    /// </summary>
    public XGraphicsState GetState { get => state; }


}
