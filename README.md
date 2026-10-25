# MarcoBellini.FastReport.PDFExporter

[![NuGet](https://img.shields.io/nuget/v/MarcoBellini.FastReport.PDFExporter.svg)](https://www.nuget.org/packages/MarcoBellini.FastReport.PDFExporter)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-lightgrey.svg)]()

Improved **PDF Exporter** for **FastReport Open Source** using **SkiaSharp** (Skia PDF backend, `SKDocument.CreatePdf`) as the rendering engine.

This project aims to improve PDF generation quality and reduce output file size compared to the default PDF exporter shipped with the FastReport Open Source edition.

> **Note:** This project is not intended to replace FastReport's commercial offerings. If you need advanced or enterprise-grade features, consider the [official commercial products](https://www.fast-report.com).

---

## Breaking Changes

> [!WARNING]
> **Rendering engine switched from PDFsharp to SkiaSharp.**
> Starting from the release following **2027.1.0**, this package no longer uses **PDFsharp-GDI v6.2.4** and is now built on **SkiaSharp 4.153.1** (MIT).
> PDFsharp versions later than **v6.2.4** ship under a [different license](https://github.com/empira/pdfsharp?tab=License-1-ov-file) that mainly affects **closed-source and commercial** projects. To keep this exporter free to use in any project, the PDFsharp dependency has been replaced.

What changes for consumers:

- **The `PDFsharp-GDI` package is no longer referenced.** If your application used PDFsharp types through this package as a transitive dependency, add an explicit reference to PDFsharp yourself, or remove that usage.
- **New native dependency:** `SkiaSharp` and `SkiaSharp.NativeAssets.Win32` are referenced automatically. The native `libSkiaSharp.dll` must be deployed with your application (this happens automatically with a standard `dotnet publish`).
- **No font resolver is needed:** system fonts are resolved by name, so any PDFsharp `GlobalFontSettings` / font resolver setup can be removed.
- **The output may differ slightly** from previous versions (text layout, font embedding/subsetting, gradients). Check critical reports after upgrading.
- The public API (`PDFExport`) is **unchanged**.

---

## Key Features

- Higher-quality, **vector** PDF rendering compared to the default FastReport Open Source exporter
- Typically **smaller PDF output files**: embedded TrueType fonts are **subset** to the glyphs actually used, and images drawn several times (e.g. watermarks) are embedded once
- Rendering based on **SkiaSharp**, with native support for:
  - **transparency** (alpha colors, semi-transparent images and watermarks)
  - **clipping** and **transformations** (rotated text and images, arbitrary matrices)
  - **gradients** with multiple stops (FastReport linear gradients, including focus/contrast blends)
  - **hatch** and **texture** brushes (approximated path gradients)
  - dashed, custom-dashed and joined strokes
- System fonts are resolved automatically by name (no font resolver to configure)
- Text layout that emulates GDI+ `DrawString` (word wrap, alignment, line limit, trimming, underline / strikeout)

---

## Requirements

- .NET 8 or later
- Windows OS (see [Platform Support](#platform-support-windows-only))
- FastReport Open Source (compatible versions: **>=2026.1.4**)

---

## Installation

Install via NuGet Package Manager:

```shell
dotnet add package MarcoBellini.FastReport.PDFExporter
```

The SkiaSharp native library for Windows (`SkiaSharp.NativeAssets.Win32`) is referenced automatically.

---

## Usage

### Basic export to file

```csharp
using MarcoBellini.FastReport.PDFExporter;

void ExportReport()
{
    using var report = new Report();
    using var pdfExport = new PDFExport();

    report.Load("Reports/MyReport.frx");
    report.Prepare();

    var outputPath = Path.Combine(Path.GetTempPath(), "Report.pdf");
    report.Export(pdfExport, outputPath);
}
```

### Export to a memory stream (e.g. for ASP.NET Core responses)

```csharp
using MarcoBellini.FastReport.PDFExporter;

byte[] ExportReportToBytes()
{
    using var report = new Report();
    using var pdfExport = new PDFExport();
    using var stream = new MemoryStream();

    report.Load("Reports/MyReport.frx");
    report.Prepare();
    report.Export(pdfExport, stream);

    return stream.ToArray();
}
```

### Export with data source

```csharp
using MarcoBellini.FastReport.PDFExporter;

void ExportReportWithData(IEnumerable<MyRecord> data)
{
    using var report = new Report();
    using var pdfExport = new PDFExport();

    report.Load("Reports/MyReport.frx");
    report.RegisterData(data, "MyDataSource");
    report.Prepare();

    var outputPath = Path.Combine(Path.GetTempPath(), "Report.pdf");
    report.Export(pdfExport, outputPath);
}
```

The report information (`ReportInfo.Name`, `Author`, `Description`) is written to the PDF metadata (Title, Author, Subject).

---

## Platform Support (Windows Only)

This exporter is currently **Windows-only**:

- **FastReport Open Source** relies on `System.Drawing.Common`, which Microsoft has restricted to Windows starting with .NET 6 ([see docs](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only))
- The package ships only the Windows SkiaSharp native assets (`SkiaSharp.NativeAssets.Win32`)

It can be used in:

- Windows desktop applications (WinForms, WPF, console)
- **ASP.NET Core**, as long as it runs on a **Windows host**

> **Roadmap:** Linux support is planned. The rendering engine (SkiaSharp) is cross-platform and the
> GDI+-specific parts (string measurement, image conversion) are isolated behind interfaces.

---

## Architecture

| Component | Responsibility |
|---|---|
| `PDFExport` | FastReport export lifecycle: document, pages, bands, watermarks |
| `PDFGraphicsAdapter` | FastReport `IGraphics` implementation on top of a Skia PDF page canvas |
| `Skia/SkiaPaintFactory` + `Skia/Brushes/*` | GDI+ pens and brushes to Skia paints (one strategy per brush type) |
| `Skia/PathConverter`, `Skia/SkiaConvert` | GDI+ geometry, matrices and enums to Skia |
| `Skia/CanvasStateManager` | GDI+ Save / Restore / Transform / ResetClip semantics |
| `Skia/SkiaFontProvider`, `Skia/GdiImageConverter` | Cached fonts and images (document scoped) |
| `Text/TextLayoutEngine`, `Text/SkiaTextRenderer` | GDI+ compatible text layout and drawing |
| `Text/GdiStringMeasurer` | GDI+ measurements exposed to FastReport (`MeasureString`, `MeasureCharacterRanges`) |
| `Pdf/FontSubsetPostProcessor` | TrueType font subsetting of the generated PDF |

---

## Limitations

- **Different start and end caps** on the same pen are not supported: the start cap is used for both ends. Anchor and arrow caps are drawn as flat caps.
- **`PathGradientBrush`** is approximated with a radial gradient from the center color to the first surround color.
- **Clipping**: `CombineMode.Union`, `Xor` and `Complement` cannot be expressed and leave the current clip unchanged. `ResetClip` resets the clip to the last saved graphics state.
- **Complex text shaping** (right-to-left scripts, ligatures) is not performed; characters missing from the selected font are not replaced with a fallback font.
- **Ellipsis trimming** is applied at character level for every `StringTrimming.Ellipsis*` mode, and tabs are expanded to spaces.
- Font subsetting applies to TrueType fonts (`.ttf`); other font formats are embedded as produced by Skia.

---

## Building and testing

```shell
dotnet build -c Release
```

The tests use xUnit v3 and can be run from Visual Studio or by launching the test executable:

```shell
dotnet build MarcoBellini.FastReport.PDFExporter.Tests
MarcoBellini.FastReport.PDFExporter.Tests\bin\Debug\net10.0-windows\MarcoBellini.FastReport.PDFExporter.Tests.exe
```

---

## Dependencies / Related Projects

- [FastReport Open Source](https://github.com/FastReports/FastReport)
- [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT) and [Skia](https://skia.org) (BSD-3-Clause)

---

## Contributing

Contributions, bug reports, and feature requests are welcome! Feel free to open an [issue](../../issues) or submit a [pull request](../../pulls).

Please make sure to:

- Describe the problem or improvement clearly
- Include a minimal reproducible example if reporting a bug
- Follow the existing code style

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE.txt) file for details, including the third-party notices.
