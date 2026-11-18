Option Strict On
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Security
Imports System.Text

''' <summary>
''' Genera la cotización como libro de Excel (.xlsx, Office Open XML) sin librerías externas:
''' logo, estilos formales, celdas combinadas, importes numéricos y fechas reales.
''' </summary>
Public NotInheritable Class CotizacionExcel
    ' Índices de estilo (cellXfs en styles.xml).
    Private Const EstTitulo As Integer = 1, EstSubtitulo As Integer = 2, EstSeccion As Integer = 3, EstEtiqueta As Integer = 4
    Private Const EstValor As Integer = 5, EstValorDinero As Integer = 6, EstCabecera As Integer = 7, EstEntero As Integer = 8
    Private Const EstFecha As Integer = 9, EstDinero As Integer = 10, EstEnteroZ As Integer = 11, EstFechaZ As Integer = 12
    Private Const EstDineroZ As Integer = 13, EstTotalEtq As Integer = 14, EstTotalDinero As Integer = 15, EstAviso As Integer = 16
    Private Const EstValorClave As Integer = 17, EstNumeroDoc As Integer = 18, EstFirma As Integer = 19
    Private Const EstParteEtqIzq As Integer = 20, EstParteEtqDer As Integer = 21, EstParteNomIzq As Integer = 22, EstParteNomDer As Integer = 23
    Private Const EstParteIzq As Integer = 24, EstParteDer As Integer = 25, EstTituloSimple As Integer = 26

    Private ReadOnly filas As New StringBuilder()
    Private ReadOnly combinadas As New List(Of String)()
    Private fila As Integer

    Private Sub New()
    End Sub

    Public Shared Function Generar(c As CotizacionPrestamo, rutaLogo As String) As Byte()
        Dim libro As New CotizacionExcel()
        Dim hoja = libro.Hoja(c)
        Dim logo = If(Not String.IsNullOrEmpty(rutaLogo) AndAlso File.Exists(rutaLogo), File.ReadAllBytes(rutaLogo), Nothing)
        Using ms As New MemoryStream()
            Using zip As New ZipArchive(ms, ZipArchiveMode.Create, True)
                Agregar(zip, "[Content_Types].xml", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Default Extension=""png"" ContentType=""image/png""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/><Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>" & If(logo IsNot Nothing, "<Override PartName=""/xl/drawings/drawing1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.drawing+xml""/>", "") & "<Override PartName=""/docProps/core.xml"" ContentType=""application/vnd.openxmlformats-package.core-properties+xml""/></Types>")
                Agregar(zip, "_rels/.rels", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/><Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"" Target=""docProps/core.xml""/></Relationships>")
                Agregar(zip, "docProps/core.xml", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><cp:coreProperties xmlns:cp=""http://schemas.openxmlformats.org/package/2006/metadata/core-properties"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:dcterms=""http://purl.org/dc/terms/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><dc:title>Cotización de préstamo " & X(c.Numero) & "</dc:title><dc:creator>CapitalWest</dc:creator></cp:coreProperties>")
                Agregar(zip, "xl/workbook.xml", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets><sheet name=""Cotización"" sheetId=""1"" r:id=""rId1""/></sheets><definedNames><definedName name=""_xlnm.Print_Titles"" localSheetId=""0"">'Cotización'!$" & libro.filaCabeceraTabla.ToString(CultureInfo.InvariantCulture) & ":$" & libro.filaCabeceraTabla.ToString(CultureInfo.InvariantCulture) & "</definedName></definedNames></workbook>")
                Agregar(zip, "xl/_rels/workbook.xml.rels", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/><Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/></Relationships>")
                Agregar(zip, "xl/styles.xml", Estilos())
                Agregar(zip, "xl/worksheets/sheet1.xml", hoja.Replace("{DIBUJO}", If(logo IsNot Nothing, "<drawing r:id=""rId1""/>", "")))
                If logo IsNot Nothing Then
                    Agregar(zip, "xl/worksheets/_rels/sheet1.xml.rels", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing"" Target=""../drawings/drawing1.xml""/></Relationships>")
                    Agregar(zip, "xl/drawings/_rels/drawing1.xml.rels", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"" Target=""../media/logo.png""/></Relationships>")
                    ' Logo de 280x154 px mostrado a ~118x65 px (1 px = 9525 EMU).
                    Agregar(zip, "xl/drawings/drawing1.xml", "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><xdr:wsDr xmlns:xdr=""http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing"" xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><xdr:oneCellAnchor><xdr:from><xdr:col>0</xdr:col><xdr:colOff>76200</xdr:colOff><xdr:row>0</xdr:row><xdr:rowOff>57150</xdr:rowOff></xdr:from><xdr:ext cx=""1123950"" cy=""619125""/><xdr:pic><xdr:nvPicPr><xdr:cNvPr id=""2"" name=""Logo CapitalWest"" descr=""CapitalWest""/><xdr:cNvPicPr><a:picLocks noChangeAspect=""1""/></xdr:cNvPicPr></xdr:nvPicPr><xdr:blipFill><a:blip r:embed=""rId1""/><a:stretch><a:fillRect/></a:stretch></xdr:blipFill><xdr:spPr><a:xfrm><a:off x=""0"" y=""0""/><a:ext cx=""1123950"" cy=""619125""/></a:xfrm><a:prstGeom prst=""rect""><a:avLst/></a:prstGeom></xdr:spPr></xdr:pic><xdr:clientData/></xdr:oneCellAnchor></xdr:wsDr>")
                    Dim entrada = zip.CreateEntry("xl/media/logo.png", CompressionLevel.Optimal)
                    Using s = entrada.Open()
                        s.Write(logo, 0, logo.Length)
                    End Using
                End If
            End Using
            Return ms.ToArray()
        End Using
    End Function

    Private filaCabeceraTabla As Integer

    Private Function Hoja(c As CotizacionPrestamo) As String
        ' Encabezado sencillo, sin recuadros: logo en A1 y título a la derecha.
        fila = 1
        Iniciar(54) : Celda("D", "COTIZACIÓN DE PRÉSTAMO", EstTituloSimple) : Combinar("D1:F1") : Cerrar()
        fila += 1
        ' "Cotización para" (izquierda) y "Empresa" (derecha), como texto plano.
        Iniciar(16) : Celda("A", If(c.TieneCliente, "COTIZACIÓN PARA", ""), EstParteEtqIzq) : Combinar("A" & fila & ":C" & fila) : Celda("D", "EMPRESA", EstParteEtqDer) : Combinar("D" & fila & ":F" & fila) : Cerrar()
        Iniciar(22) : Celda("A", If(c.TieneCliente, c.ClienteNombre.Trim(), ""), EstParteNomIzq) : Combinar("A" & fila & ":C" & fila) : Celda("D", CotizacionPrestamo.NombreEmpresa, EstParteNomDer) : Combinar("D" & fila & ":F" & fila) : Cerrar()
        Dim izq = If(c.TieneCliente, c.LineasCliente(), New List(Of String)()), der = c.LineasEmpresa()
        For i = 0 To Math.Max(izq.Count, der.Count) - 1
            Iniciar(15)
            Celda("A", If(i < izq.Count, izq(i), ""), EstParteIzq) : Combinar("A" & fila & ":C" & fila)
            Celda("D", If(i < der.Count, der(i), ""), EstParteDer) : Combinar("D" & fila & ":F" & fila)
            Cerrar()
        Next
        fila += 1
        Seccion("Datos de la cotización")
        For Each dato In c.DatosCotizacion()
            Par(dato.Key, dato.Value)
        Next
        fila += 1
        Seccion("Resumen económico")
        For Each dato In c.Resumen()
            Iniciar(18)
            Celda("A", dato.Key, EstEtiqueta) : Celda("B", "", EstEtiqueta) : Combinar("A" & fila & ":B" & fila)
            Dim clave = dato.Key = "Total a pagar" OrElse dato.Key = "Neto estimado a recibir"
            Numero("C", dato.Value, If(clave, EstValorClave, EstValorDinero))
            For Each col In {"D", "E", "F"}
                Celda(col, "", If(clave, EstValorClave, EstValorDinero))
            Next
            Combinar("C" & fila & ":F" & fila)
            Cerrar()
        Next
        fila += 1
        Seccion("Tabla de amortización")
        Iniciar(30) : Celda("A", c.NotaMetodo, EstAviso) : Combinar("A" & fila & ":F" & fila) : Cerrar()
        filaCabeceraTabla = fila
        Iniciar(20)
        Dim cols = {"A", "B", "C", "D", "E", "F"}
        Dim titulos = {"N.º", "Fecha", "Capital (B/.)", "Interés (B/.)", "Cuota (B/.)", "Saldo (B/.)"}
        For i = 0 To 5
            Celda(cols(i), titulos(i), EstCabecera)
        Next
        Cerrar()
        Dim primera = fila, n = 0
        For Each q In c.Plan
            Dim z = n Mod 2 = 1
            Iniciar(16)
            Numero("A", q.Numero, If(z, EstEnteroZ, EstEntero))
            Numero("B", CDec(q.Fecha.ToOADate()), If(z, EstFechaZ, EstFecha))
            Numero("C", q.Capital, If(z, EstDineroZ, EstDinero))
            Numero("D", q.Interes, If(z, EstDineroZ, EstDinero))
            Numero("E", q.Cuota, If(z, EstDineroZ, EstDinero))
            Numero("F", q.Saldo, If(z, EstDineroZ, EstDinero))
            Cerrar()
            n += 1
        Next
        Dim ultima = fila - 1
        Iniciar(20)
        Celda("A", "Totales", EstTotalEtq) : Celda("B", "", EstTotalEtq) : Combinar("A" & fila & ":B" & fila)
        Formula("C", "SUM(C" & primera & ":C" & ultima & ")", c.Plan.Sum(Function(q) q.Capital), EstTotalDinero)
        Formula("D", "SUM(D" & primera & ":D" & ultima & ")", c.TotalIntereses, EstTotalDinero)
        Formula("E", "SUM(E" & primera & ":E" & ultima & ")", c.TotalPagar, EstTotalDinero)
        Celda("F", "", EstTotalDinero)
        Cerrar()
        fila += 1
        Iniciar(54) : Celda("A", CotizacionPrestamo.Aviso, EstAviso) : Combinar("A" & fila & ":F" & fila) : Cerrar()
        fila += 2
        Iniciar(18) : Celda("A", "______________________________", EstFirma) : Combinar("A" & fila & ":C" & fila) : Celda("D", "______________________________", EstFirma) : Combinar("D" & fila & ":F" & fila) : Cerrar()
        Iniciar(16) : Celda("A", "Firma del cliente", EstFirma) : Combinar("A" & fila & ":C" & fila) : Celda("D", "Firma del asesor", EstFirma) : Combinar("D" & fila & ":F" & fila) : Cerrar()

        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">")
        sb.Append("<sheetPr><pageSetUpPr fitToPage=""1""/></sheetPr>")
        sb.Append("<sheetViews><sheetView workbookViewId=""0"" showGridLines=""0"" zoomScale=""100""/></sheetViews>")
        sb.Append("<cols><col min=""1"" max=""1"" width=""11"" customWidth=""1""/><col min=""2"" max=""2"" width=""15"" customWidth=""1""/><col min=""3"" max=""6"" width=""19"" customWidth=""1""/></cols>")
        sb.Append("<sheetData>").Append(filas.ToString()).Append("</sheetData>")
        If combinadas.Count > 0 Then
            sb.Append("<mergeCells count=""").Append(combinadas.Count).Append(""">")
            For Each rango In combinadas
                sb.Append("<mergeCell ref=""").Append(rango).Append("""/>")
            Next
            sb.Append("</mergeCells>")
        End If
        sb.Append("<printOptions horizontalCentered=""1""/><pageMargins left=""0.5"" right=""0.5"" top=""0.6"" bottom=""0.6"" header=""0.3"" footer=""0.3""/>")
        sb.Append("<pageSetup paperSize=""1"" orientation=""portrait"" fitToWidth=""1"" fitToHeight=""0""/>")
        sb.Append("<headerFooter><oddFooter>&amp;L&amp;8CapitalWest · Cotización ").Append(X(c.Numero)).Append("&amp;R&amp;8Página &amp;P de &amp;N</oddFooter></headerFooter>")
        sb.Append("{DIBUJO}</worksheet>")
        Return sb.ToString()
    End Function

    ' ---------- Celdas ----------

    Private Sub Iniciar(altoPuntos As Integer)
        filas.Append("<row r=""").Append(fila).Append(""" ht=""").Append(altoPuntos).Append(""" customHeight=""1"">")
    End Sub
    Private Sub Cerrar()
        filas.Append("</row>")
        fila += 1
    End Sub
    Private Sub Combinar(rango As String)
        combinadas.Add(rango)
    End Sub
    Private Sub Celda(col As String, texto As String, estilo As Integer)
        filas.Append("<c r=""").Append(col).Append(fila).Append(""" s=""").Append(estilo).Append("""")
        If String.IsNullOrEmpty(texto) Then
            filas.Append("/>")
        Else
            filas.Append(" t=""inlineStr""><is><t xml:space=""preserve"">").Append(X(texto)).Append("</t></is></c>")
        End If
    End Sub
    Private Sub Numero(col As String, valor As Decimal, estilo As Integer)
        filas.Append("<c r=""").Append(col).Append(fila).Append(""" s=""").Append(estilo).Append("""><v>").Append(valor.ToString(CultureInfo.InvariantCulture)).Append("</v></c>")
    End Sub
    Private Sub Formula(col As String, expresion As String, valor As Decimal, estilo As Integer)
        filas.Append("<c r=""").Append(col).Append(fila).Append(""" s=""").Append(estilo).Append("""><f>").Append(expresion).Append("</f><v>").Append(valor.ToString(CultureInfo.InvariantCulture)).Append("</v></c>")
    End Sub
    Private Sub Seccion(titulo As String)
        Iniciar(20)
        Celda("A", titulo.ToUpper(CotizacionPrestamo.Cultura), EstSeccion)
        For Each col In {"B", "C", "D", "E", "F"}
            Celda(col, "", EstSeccion)
        Next
        Combinar("A" & fila & ":F" & fila)
        Cerrar()
    End Sub
    Private Sub Par(etiqueta As String, valor As String)
        Iniciar(18)
        Celda("A", etiqueta, EstEtiqueta) : Celda("B", "", EstEtiqueta) : Combinar("A" & fila & ":B" & fila)
        Celda("C", valor, EstValor)
        For Each col In {"D", "E", "F"}
            Celda(col, "", EstValor)
        Next
        Combinar("C" & fila & ":F" & fila)
        Cerrar()
    End Sub

    Private Shared Function X(texto As String) As String
        Return SecurityElement.Escape(If(texto, ""))
    End Function

    Private Shared Sub Agregar(zip As ZipArchive, ruta As String, contenido As String)
        Dim entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal)
        Using s = entrada.Open(), w As New StreamWriter(s, New UTF8Encoding(False))
            w.Write(contenido)
        End Using
    End Sub

    Private Shared Function Estilos() As String
        ' Fuentes: 0 normal, 1 título, 2 gris, 3 sección (blanca), 4 etiqueta, 5 valor, 6 cabecera tabla, 7 aviso, 8 clave verde, 9 número documento
        Dim fuentes = "<fonts count=""13"">" &
            "<font><sz val=""10""/><color rgb=""FF1C2A3D""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""18""/><color rgb=""FF0D7A48""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><sz val=""10""/><color rgb=""FF6B7A8C""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""10""/><color rgb=""FF3B6A52""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><sz val=""10""/><color rgb=""FF1C2A3D""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""10""/><color rgb=""FF0D7A48""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><i/><sz val=""9""/><color rgb=""FF6B7A8C""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""11""/><color rgb=""FF0D7A48""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""11""/><color rgb=""FF1C2A3D""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""8""/><color rgb=""FF1C2A3D""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""14""/><color rgb=""FF0D7A48""/><name val=""Calibri""/><family val=""2""/></font>" &
            "<font><b/><sz val=""14""/><color rgb=""FF1C2A3D""/><name val=""Calibri""/><family val=""2""/></font></fonts>"
        ' Rellenos: 0 none, 1 gray125 (obligatorio), 2 verde, 3 etiqueta, 4 cabecera, 5 zebra
        Dim rellenos = "<fills count=""6""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill>" &
            "<fill><patternFill patternType=""solid""><fgColor rgb=""FF0D7A48""/><bgColor indexed=""64""/></patternFill></fill>" &
            "<fill><patternFill patternType=""solid""><fgColor rgb=""FFF3F8F5""/><bgColor indexed=""64""/></patternFill></fill>" &
            "<fill><patternFill patternType=""solid""><fgColor rgb=""FFE6F2EB""/><bgColor indexed=""64""/></patternFill></fill>" &
            "<fill><patternFill patternType=""solid""><fgColor rgb=""FFF6FAF8""/><bgColor indexed=""64""/></patternFill></fill></fills>"
        Dim bordes = "<borders count=""2""><border><left/><right/><top/><bottom/><diagonal/></border>" &
            "<border><left style=""thin""><color rgb=""FFD3E0D8""/></left><right style=""thin""><color rgb=""FFD3E0D8""/></right><top style=""thin""><color rgb=""FFD3E0D8""/></top><bottom style=""thin""><color rgb=""FFD3E0D8""/></bottom><diagonal/></border></borders>"
        Dim formatos = "<numFmts count=""2""><numFmt numFmtId=""164"" formatCode=""&quot;B/. &quot;#,##0.00""/><numFmt numFmtId=""165"" formatCode=""dd/mm/yyyy""/></numFmts>"
        Dim alV = "<alignment vertical=""center""/>"
        Dim xf = Function(numFmt As Integer, font As Integer, fill As Integer, border As Integer, alineacion As String) _
            "<xf numFmtId=""" & numFmt & """ fontId=""" & font & """ fillId=""" & fill & """ borderId=""" & border & """ xfId=""0""" &
            If(numFmt <> 0, " applyNumberFormat=""1""", "") & " applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1"">" & alineacion & "</xf>"
        Dim centro = "<alignment horizontal=""center"" vertical=""center""/>"
        Dim derecha = "<alignment horizontal=""right"" vertical=""center""/>"
        Dim celdas = "<cellXfs count=""27"">" &
            "<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>" &
            xf(0, 1, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") &   ' 1 título
            xf(0, 2, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") &   ' 2 subtítulo
            xf(0, 3, 2, 0, "<alignment horizontal=""left"" vertical=""center"" indent=""1""/>") & ' 3 sección
            xf(0, 4, 3, 1, "<alignment horizontal=""left"" vertical=""center"" indent=""1""/>") & ' 4 etiqueta
            xf(0, 5, 0, 1, "<alignment horizontal=""left"" vertical=""center"" indent=""1"" wrapText=""1""/>") & ' 5 valor
            xf(164, 5, 0, 1, "<alignment horizontal=""left"" vertical=""center"" indent=""1""/>") & ' 6 valor dinero
            xf(0, 6, 4, 1, centro.Replace("/>", " wrapText=""1""/>")) &   ' 7 cabecera tabla
            xf(0, 5, 0, 1, centro) &      ' 8 entero
            xf(165, 5, 0, 1, centro) &    ' 9 fecha
            xf(4, 5, 0, 1, derecha) &     ' 10 dinero
            xf(0, 5, 5, 1, centro) &      ' 11 entero zebra
            xf(165, 5, 5, 1, centro) &    ' 12 fecha zebra
            xf(4, 5, 5, 1, derecha) &     ' 13 dinero zebra
            xf(0, 6, 4, 1, "<alignment horizontal=""left"" vertical=""center"" indent=""1""/>") & ' 14 total etiqueta
            xf(4, 6, 4, 1, derecha) &     ' 15 total dinero
            xf(0, 7, 0, 0, "<alignment horizontal=""left"" vertical=""top"" wrapText=""1""/>") & ' 16 aviso
            xf(164, 8, 4, 1, "<alignment horizontal=""left"" vertical=""center"" indent=""1""/>") & ' 17 valor clave
            xf(0, 9, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") &  ' 18 número documento
            xf(0, 2, 0, 0, centro) &     ' 19 firma
            xf(0, 10, 0, 0, "<alignment horizontal=""left"" vertical=""bottom""/>") &  ' 20 etiqueta parte izq
            xf(0, 10, 0, 0, "<alignment horizontal=""right"" vertical=""bottom""/>") & ' 21 etiqueta parte der
            xf(0, 11, 0, 0, "<alignment horizontal=""left"" vertical=""center""/>") &  ' 22 nombre izq
            xf(0, 11, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") & ' 23 nombre der
            xf(0, 5, 0, 0, "<alignment horizontal=""left"" vertical=""center""/>") &   ' 24 línea izq
            xf(0, 5, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") &  ' 25 línea der
            xf(0, 12, 0, 0, "<alignment horizontal=""right"" vertical=""center""/>") & ' 26 título sencillo
            "</cellXfs>"
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">" &
            formatos & fuentes & rellenos & bordes &
            "<cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>" & celdas &
            "<cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles></styleSheet>"
    End Function
End Class
