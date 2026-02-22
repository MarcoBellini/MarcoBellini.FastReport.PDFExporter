# FastReport.PdfExporter

Improved **PDF Exporter** for **FastReport Open Source** using **PDFSharp-GDI** as the rendering engine.

This project aims to improve PDF generation quality and reduce output file size compared to the default PDF exporter shipped with the FastReport Open Source edition.  

It is **not intended to compete with FastReport’s commercial offerings**—if you need advanced or enterprise-grade features, consider the official commercial products.

## Key features

- Higher-quality PDF rendering compared to the default exporter (FastReport Open Source)
- Typically **smaller PDF files**
- Rendering based on **PDFSharp-GDI** (GDI-based font resolver)

## Dependencies / Related projects

- FastReport Open Source: https://github.com/FastReports/FastReport
- PDFsharp (empira): https://github.com/empira/PDFsharp

## Platform support (Windows only)

This exporter is **Windows-only** due to dependencies on:

- **FastReport Open Source** relying on `System.Drawing.Common`
- **PDFSharp-GDI** using **GDI** for font resolving/rendering

Microsoft documents that `System.Drawing.Common` is **Windows-only** starting with .NET 6:  
https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only

It can be used in:
- Windows desktop applications
- **ASP.NET Core**, as long as it runs on a **Windows machine**

## Limitations / Not supported

### Brushes not supported

The following brush types are **not compatible**:

- Path Gradient brushes
- Hatch brushes
- Glass brushes
- Texture brushes

### Pen LineCaps limitations

- LineCaps with different start and end caps are **not supported**
- Triangular caps are **not supported** (`LineCap.Triangle`)

## Usage example

Below is a minimal example showing how to prepare and export a report:

```csharp

using FastReport.Export.PdfExporter;

// ...

void ExportReport()
{
  using var report = new Report();
  using var pdfExport = new PDFExport();
  
  report.Load("Reports/MyReport.frx");
  
  var OutputPath = Path.Combine(Path.GetTempPath(), "Report.pdf");
  
  report.Prepare();  
  
  report.Export(pdfExport, OutputPath);
}

// ...

```

## License

This project is open source and licensed under the **MIT License**.
