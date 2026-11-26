using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;
using ClosedXML.Excel;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using Microsoft.VisualBasic;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace EventosPeruIA.CORE.Infrastructure.Reportes;

public static class ReporteExportador
{
    private static readonly CultureInfo CulturaInforme = CultureInfo.GetCultureInfo("es-PE");
    private static readonly TimeZoneInfo ZonaLima = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
    private const string MimeExcel =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static ArchivoExportadoResponseDto CrearExcel(ReporteDashboardOrganizadorDto reporte)
    {
        using var libro = new XLWorkbook();
        var datos = libro.AddWorksheet("Informe");
        datos.Cell(1, 1).Value = "Reporte de ventas";
        datos.Cell(1, 1).Style.Font.Bold = true;

        var metadatos = Metadatos(reporte);
        for (int index = 0; index < metadatos.Length; index++)
        {
            var (etiqueta, valor) = metadatos[index];
            datos.Cell(index + 3, 1).Value = etiqueta;
            EscribirCelda(datos.Cell(index + 3, 2), valor);
        }
        datos.Column(1).Width = 32;
        datos.Column(2).Width = 60;

        foreach (var tabla in Tablas(reporte))
        {
            var hoja = libro.AddWorksheet(tabla.Titulo);
            for (int columna = 0; columna < tabla.Columnas.Length; columna++)
            {
                var encabezado = tabla.Columnas[columna];
                hoja.Cell(1, columna + 1).Value = encabezado.Titulo;
                hoja.Column(columna + 1).Width = Math.Max(
                    encabezado.Ancho * 5, encabezado.Titulo.Length + 2);
            }

            hoja.Row(1).Style.Font.Bold = true;
            hoja.SheetView.FreezeRows(1);
            for (int fila = 0; fila < tabla.Filas.Length; fila++)
            {
                for (int columna = 0; columna < tabla.Columnas.Length; columna++)
                    EscribirCelda(hoja.Cell(fila + 2, columna + 1), tabla.Filas[fila][columna]);
            }
        }

        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return Archivo(reporte, "xlsx", MimeExcel, salida.ToArray());
    }

    public static ArchivoExportadoResponseDto CrearPdf(ReporteDashboardOrganizadorDto reporte)
    {
        FuentesPdf.AsegurarConfiguracion();
        var documento = new global::MigraDoc.DocumentObjectModel.Document();
        documento.Info.Title = "Reporte de ventas";
        var estilo = documento.Styles[StyleNames.Normal]!;
        estilo.Font.Name = "Open Sans";
        estilo.Font.Size = 8.5;

        var seccion = documento.AddSection();
        seccion.PageSetup.PageFormat = PageFormat.A4;
        seccion.PageSetup.Orientation = Orientation.Landscape;
        seccion.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
        seccion.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        seccion.AddParagraph("Reporte de ventas", StyleNames.Heading1);

        foreach (var (etiqueta, valor) in Metadatos(reporte))
        {
            var parrafo = seccion.AddParagraph();
            parrafo.AddFormattedText(etiqueta + "  ", TextFormat.Bold);
            parrafo.AddText(ComoTexto(valor));
        }

        foreach (var tabla in Tablas(reporte))
        {
            var titulo = seccion.AddParagraph(tabla.Titulo, StyleNames.Heading2);
            titulo.Format.KeepWithNext = true;
            var tablaPdf = seccion.AddTable();
            tablaPdf.Borders.Width = 0.3;
            tablaPdf.Borders.Color = Colors.LightGray;
            tablaPdf.TopPadding = Unit.FromMillimeter(1.5);
            tablaPdf.BottomPadding = Unit.FromMillimeter(1.5);

            foreach (var columna in tabla.Columnas)
                tablaPdf.AddColumn(Unit.FromCentimeter(columna.Ancho));

            var cabecera = tablaPdf.AddRow();
            cabecera.HeadingFormat = true;
            cabecera.Shading.Color = Colors.LightGray;
            cabecera.Format.Font.Bold = true;
            for (int columna = 0; columna < tabla.Columnas.Length; columna++)
                cabecera[columna].AddParagraph(tabla.Columnas[columna].Titulo);

            foreach (var valores in tabla.Filas)
            {
                var fila = tablaPdf.AddRow();
                fila.VerticalAlignment = VerticalAlignment.Center;
                for (int columna = 0; columna < valores.Length; columna++)
                {
                    var celda = fila[columna].AddParagraph(ComoTexto(valores[columna]));
                    if (valores[columna] is int or decimal)
                        celda.Format.Alignment = ParagraphAlignment.Right;
                }
            }

            if (tabla.Filas.Length == 0)
                seccion.AddParagraph("Sin registros.");
        }

        var renderer = new PdfDocumentRenderer { Document = documento };
        renderer.RenderDocument();
        using var salida = new MemoryStream();
        renderer.PdfDocument.Save(salida, false);
        return Archivo(reporte, "pdf", "application/pdf", salida.ToArray());
    }

    private static (string Etiqueta, object Valor)[] Metadatos(ReporteDashboardOrganizadorDto reporte)
    {
        var filtro = reporte.FiltrosAplicados;
        var evento = filtro.EventoId.HasValue
            ? (reporte.ResumenEventos.Find(e => e.EventoId == filtro.EventoId)?.NombreEvento ?? "Evento")
                + $" (ID {filtro.EventoId.Value})"
            : "Todos los eventos";

        return
        [
            ("Fecha de generación (Lima)", FechaGeneracionLima(reporte)
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            ("Evento", evento),
            ("Desde (Lima)", filtro.FechaInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Sin límite"),
            ("Hasta (Lima)", filtro.FechaFin?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Sin límite"),
            ("Entradas vendidas", reporte.GranTotalEntradasActivas),
            ("Ingresos (S/)", reporte.GranTotalIngresosSoles),
            ("Ingresos (US$)", reporte.GranTotalIngresosUsd),
            ("Resultado", reporte.MensajeInformativo)
        ];
    }

    private static Tabla[] Tablas(ReporteDashboardOrganizadorDto reporte) =>
    [
        new("Entradas por evento",
        [
            new("ID", 1.25), new("Evento", 7), new("Estado", 2.8), new("Válidas", 2),
            new("Utilizadas", 2.2), new("Vendidas", 2.2), new("Disponibles actuales", 2.4),
            new("Anuladas", 2.2), new("Pendientes de reembolso", 3.15)
        ],
        reporte.ResumenEventos.Select(e => new object[]
        {
            e.EventoId, e.NombreEvento, e.EstadoEvento, e.EntradasValidasVendidas,
            e.EntradasUtilizadas, e.TotalEntradasActivas, e.CapacidadDisponible,
            e.EntradasAnuladasCount, e.EntradasPendientesReembolso
        }).ToArray()),
        new("Ingresos y reembolsos",
        [
            new("ID", 1.25), new("Evento", 7), new("Ingresos (S/)", 4.25), new("Ingresos (US$)", 4.25),
            new("Reembolso pendiente (S/)", 4.25), new("Reembolso pendiente (US$)", 4.25)
        ],
        reporte.ResumenEventos.Select(e => new object[]
        {
            e.EventoId, e.NombreEvento, e.IngresosTotalesSoles, e.IngresosTotalesUsd,
            e.MontoPendienteReembolsoSoles, e.MontoPendienteReembolsoUsd
        }).ToArray()),
        new("Ventas diarias",
        [
            new("Fecha (Lima)", 4), new("Entradas vendidas", 4.25),
            new("Ingresos (S/)", 4.25), new("Ingresos (US$)", 4.25)
        ],
        reporte.EvolucionVentasDiarias.Select(d => new object[]
        {
            d.Fecha, d.EntradasVendidas, d.MontoTotalSoles, d.MontoTotalUsd
        }).ToArray())
    ];

    private static void EscribirCelda(IXLCell celda, object valor)
    {
        switch (valor)
        {
            case int cantidad:
                celda.Value = cantidad;
                break;
            case decimal importe:
                celda.Value = (double)importe;
                celda.Style.NumberFormat.Format = "#,##0.00";
                break;
            case DateOnly fecha:
                celda.Value = fecha.ToDateTime(TimeOnly.MinValue);
                celda.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            default:
                celda.Value = ComoTexto(valor);
                break;
        }
    }

    private static string ComoTexto(object valor) => valor switch
    {
        DateOnly fecha => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        decimal importe => importe.ToString("N2", CulturaInforme),
        _ => Convert.ToString(valor, CulturaInforme) ?? string.Empty
    };

    private static ArchivoExportadoResponseDto Archivo(
        ReporteDashboardOrganizadorDto reporte,
        string extension,
        string contentType,
        byte[] contenido) => new()
        {
            NombreArchivo = $"Reporte_Ventas_{FechaGeneracionLima(reporte):yyyyMMdd_HHmmss}.{extension}",
            ContentType = contentType,
            ContenidoBytes = contenido
        };

    private static DateTime FechaGeneracionLima(ReporteDashboardOrganizadorDto reporte) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(reporte.FechaGeneracion, DateTimeKind.Utc), ZonaLima);

    private sealed record Columna(string Titulo, double Ancho);
    private sealed record Tabla(string Titulo, Columna[] Columnas, object[][] Filas);

    private sealed class FuentesPdf : IFontResolver
    {
        private const string Regular = "OpenSans-Regular";
        private const string Negrita = "OpenSans-Bold";

        static FuentesPdf() => GlobalFontSettings.FontResolver = new FuentesPdf();

        public static void AsegurarConfiguracion() { }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? Negrita : Regular);

        public byte[] GetFont(string faceName)
        {
            var recurso = $"{typeof(ReporteExportador).Namespace}.Fonts.{faceName}.ttf";
            using var fuente = typeof(ReporteExportador).Assembly.GetManifestResourceStream(recurso)
                ?? throw new InvalidOperationException($"Falta la fuente del informe: {faceName}.");
            using var memoria = new MemoryStream();
            fuente.CopyTo(memoria);
            return memoria.ToArray();
        }
    }
}