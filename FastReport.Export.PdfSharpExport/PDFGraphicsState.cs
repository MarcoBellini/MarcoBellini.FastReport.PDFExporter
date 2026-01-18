using PdfSharp.Drawing;

namespace FastReport.Export.PDFSharpExport;

/// <summary>
/// Class used to store PDFSharp graphics state.
/// </summary>
internal class PdfGraphicsState : IGraphicsState
{

    private XGraphicsState _XGraphicsState;

    /// <summary>
    /// Create new PDFGraphicsState
    /// </summary>
    /// <param name="state">PDFSharp state returned from <c>save()</c></param>
    public PdfGraphicsState(XGraphicsState state)
    {
        _XGraphicsState = state;    
    }

    /// <summary>
    /// Get Saved state
    /// </summary>
    public XGraphicsState GetState { get => _XGraphicsState; }


}
