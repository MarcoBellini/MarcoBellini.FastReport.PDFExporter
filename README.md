# FastReport.PdfExporter

[![NuGet](https://img.shields.io/nuget/v/FastReport.PdfExporter.svg)](https://www.nuget.org/packages/FastReport.PdfExporter)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-lightgrey.svg)]()

Improved **PDF Exporter** for **FastReport Open Source** using **PDFSharp-GDI** as the rendering engine.

This project aims to improve PDF generation quality and reduce output file size compared to the default PDF exporter shipped with the FastReport Open Source edition.

> **Note:** This project is not intended to replace FastReport's commercial offerings. If you need advanced or enterprise-grade features, consider the [official commercial products](https://www.fast-report.com).

---

## Key Features

- Higher-quality PDF rendering compared to the default FastReport Open Source exporter
- Typically **smaller PDF output files**
- Rendering based on **PDFSharp-GDI** (GDI-based font resolver)

---

## Requirements

- .NET 8 or later
- Windows OS (see [Platform Support](#platform-support-windows-only))
- FastReport Open Source (compatible versions: **>=2026.1.4**)

---

## Installation

Install via NuGet Package Manager:

```shell
dotnet add package FastReport.PdfExporter
```
---

## Usage

### Basic export to file

```csharp
using FastReport.Export.PdfExporter;

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
using FastReport.Export.PdfExporter;

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
using FastReport.Export.PdfExporter;

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

---

## Platform Support (Windows Only)

This exporter is **Windows-only** due to the following dependencies:

- **FastReport Open Source** relies on `System.Drawing.Common`, which Microsoft has restricted to Windows starting with .NET 6 ([see docs](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only))
- **PDFSharp-GDI** uses GDI for font resolving and rendering

It can be used in:

- Windows desktop applications (WinForms, WPF, console)
- **ASP.NET Core**, as long as it runs on a **Windows host**

---

## Limitations

### Unsupported Brush Types

The following brush types are not compatible with PDFSharp and will not render correctly:

- `PathGradientBrush`
- `HatchBrush`
- `TextureBrush`

> **Why?** PDFSharp does not have a direct equivalent for these GDI+ brush types, so they cannot be translated into PDF drawing instructions.

### Pen LineCap Limitations

- Pens with **different start and end caps** are not supported
- `LineCap.Triangle` is not supported

---

## Dependencies / Related Projects

- [FastReport Open Source](https://github.com/FastReports/FastReport)
- [PDFsharp by empira](https://github.com/empira/PDFsharp)

---

## Contributing

Contributions, bug reports, and feature requests are welcome! Feel free to open an [issue](../../issues) or submit a [pull request](../../pulls).

Please make sure to:

- Describe the problem or improvement clearly
- Include a minimal reproducible example if reporting a bug
- Follow the existing code style

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.
