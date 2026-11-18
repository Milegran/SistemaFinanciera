Option Strict On
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.Text

''' <summary>
''' Genera la cotización en PDF sin librerías externas: fuentes estándar Helvetica
''' (WinAnsiEncoding, admite tildes y ñ), logo como imagen JPEG y tabla paginada.
''' </summary>
Public NotInheritable Class CotizacionPdf
    Private Const AnchoPagina As Single = 612.0F   ' Carta: 8.5 x 11 pulgadas
    Private Const AltoPagina As Single = 792.0F
    Private Const Margen As Single = 40.0F
    Private Shared ReadOnly Verde As Single() = {0.051F, 0.478F, 0.282F}
    Private Shared ReadOnly VerdeClaro As Single() = {0.902F, 0.949F, 0.922F}
    Private Shared ReadOnly Zebra As Single() = {0.965F, 0.98F, 0.973F}
    Private Shared ReadOnly Texto As Single() = {0.11F, 0.165F, 0.239F}
    Private Shared ReadOnly Gris As Single() = {0.42F, 0.478F, 0.549F}
    Private Shared ReadOnly Borde As Single() = {0.827F, 0.878F, 0.847F}
    Private Shared ReadOnly Win1252 As Encoding = Encoding.GetEncoding(1252)

    Private ReadOnly paginas As New List(Of StringBuilder)()
    Private actual As StringBuilder
    Private y As Single

    Private Sub New()
    End Sub

    Public Shared Function Generar(c As CotizacionPrestamo, rutaLogo As String) As Byte()
        Dim doc As New CotizacionPdf()
        Return doc.Construir(c, CargarLogo(rutaLogo))
    End Function

    Private Function Construir(c As CotizacionPrestamo, logo As LogoJpeg) As Byte()
        NuevaPagina()
        Encabezado(c, logo, True)
        Partes(c)
        Seccion("DATOS DE LA COTIZACIÓN")
        Pares(c.DatosCotizacion())
        Seccion("RESUMEN ECONÓMICO")
        ResumenCajas(c)
        Seccion("TABLA DE AMORTIZACIÓN")
        y -= 2
        For Each renglon In Partir(c.NotaMetodo, 8, False, AnchoPagina - 2 * Margen)
            Escribir(Margen, y, 8, False, Gris, renglon) : y -= 11
        Next
        y -= 4
        Tabla(c, logo)
        Firmas()
        ' Pie con aviso y numeración en todas las páginas.
        For idx = 0 To paginas.Count - 1
            actual = paginas(idx)
            Dim py As Single = 46
            Linea(Margen, py + 12, AnchoPagina - Margen, py + 12, Borde, 0.6F)
            For Each renglon In Partir(CotizacionPrestamo.Aviso, 7, False, AnchoPagina - 2 * Margen - 70)
                Escribir(Margen, py, 7, False, Gris, renglon) : py -= 9
            Next
            EscribirDerecha(AnchoPagina - Margen, 46, 8, True, Gris, "Página " & (idx + 1).ToString(CultureInfo.InvariantCulture) & " de " & paginas.Count.ToString(CultureInfo.InvariantCulture))
        Next
        Return Serializar(logo)
    End Function

    ' ---------- Bloques del documento ----------

    Private Sub Encabezado(c As CotizacionPrestamo, logo As LogoJpeg, completo As Boolean)
        ' Encabezado sencillo, sin recuadros: logo a la izquierda y título a la derecha.
        Dim alto As Single = If(completo, 46, 26)
        Dim top = AltoPagina - Margen
        If logo IsNot Nothing Then
            Dim anchoLogo = alto * logo.Ancho / logo.Alto
            actual.AppendFormat(CultureInfo.InvariantCulture, "q {0:0.##} 0 0 {1:0.##} {2:0.##} {3:0.##} cm /Im1 Do Q" & vbLf, anchoLogo, alto, Margen, top - alto)
        Else
            Escribir(Margen, top - alto / 2 - 5, If(completo, 18, 12), True, Verde, CotizacionPrestamo.NombreEmpresa)
        End If
        Dim derecha = AnchoPagina - Margen
        If completo Then
            EscribirDerecha(derecha, top - alto / 2 - 5, 14, True, Texto, "COTIZACIÓN DE PRÉSTAMO")
            y = top - alto - 22
        Else
            EscribirDerecha(derecha, top - 10, 10, True, Texto, "COTIZACIÓN DE PRÉSTAMO")
            EscribirDerecha(derecha, top - 23, 8.5F, False, Gris, "N.º " & c.Numero)
            y = top - alto - 8
            Linea(Margen, y, AnchoPagina - Margen, y, Borde, 0.6F)
            y -= 16
        End If
    End Sub

    ''' <summary>Bloque "Cotización para" / "Empresa" en dos columnas, como texto plano.</summary>
    Private Sub Partes(c As CotizacionPrestamo)
        Dim derecha = AnchoPagina - Margen
        Dim mitad = (AnchoPagina - 2 * Margen) / 2 - 10
        If c.TieneCliente Then Escribir(Margen, y, 8, True, Texto, "COTIZACIÓN PARA")
        EscribirDerecha(derecha, y, 8, True, Texto, "EMPRESA")
        y -= 18
        If c.TieneCliente Then Escribir(Margen, y, 14, True, Verde, Recortar(c.ClienteNombre.Trim(), 14, True, mitad))
        EscribirDerecha(derecha, y, 14, True, Verde, CotizacionPrestamo.NombreEmpresa)
        y -= 16
        Dim izq = If(c.TieneCliente, c.LineasCliente(), New List(Of String)()), der = c.LineasEmpresa()
        For i = 0 To Math.Max(izq.Count, der.Count) - 1
            If i < izq.Count Then Escribir(Margen, y, 9.5F, False, Texto, Recortar(izq(i), 9.5F, False, mitad))
            If i < der.Count Then EscribirDerecha(derecha, y, 9.5F, False, Texto, Recortar(der(i), 9.5F, False, mitad))
            y -= 13
        Next
        y -= 8
    End Sub

    Private Sub Seccion(titulo As String)
        y -= 6
        Rect(Margen, y - 5, AnchoPagina - 2 * Margen, 18, Verde, Nothing)
        Escribir(Margen + 8, y, 9.5F, True, New Single() {1, 1, 1}, titulo)
        y -= 22
    End Sub

    Private Sub Pares(datos As List(Of KeyValuePair(Of String, String)))
        ' Dos columnas de etiqueta/valor.
        Dim anchoCol = (AnchoPagina - 2 * Margen) / 2
        For idx = 0 To datos.Count - 1 Step 2
            For col = 0 To 1
                If idx + col >= datos.Count Then Exit For
                Dim x = Margen + col * anchoCol
                Escribir(x + 4, y, 7.5F, True, Gris, datos(idx + col).Key.ToUpperInvariant())
                Dim valor = Recortar(datos(idx + col).Value, 10, False, anchoCol - 12)
                Escribir(x + 4, y - 12, 10, False, Texto, valor)
            Next
            y -= 26
            Linea(Margen, y + 8, AnchoPagina - Margen, y + 8, Borde, 0.5F)
        Next
        y -= 4
    End Sub

    Private Sub ResumenCajas(c As CotizacionPrestamo)
        Dim datos = c.Resumen()
        Dim columnas = 4
        Dim separacion As Single = 8
        Dim ancho = (AnchoPagina - 2 * Margen - separacion * (columnas - 1)) / columnas
        Dim alto As Single = 38
        For idx = 0 To datos.Count - 1
            Dim fila = idx \ columnas, col = idx Mod columnas
            Dim x = Margen + col * (ancho + separacion)
            Dim top = y - fila * (alto + separacion)
            Dim clave = datos(idx).Key = "Total a pagar" OrElse datos(idx).Key = "Neto estimado a recibir"
            Rect(x, top - alto + 8, ancho, alto, If(clave, VerdeClaro, Zebra), Borde)
            Escribir(x + 8, top - 4, 7.5F, True, Gris, datos(idx).Key.ToUpperInvariant())
            Escribir(x + 8, top - 20, 12, True, If(clave, Verde, Texto), CotizacionPrestamo.Dinero(datos(idx).Value))
        Next
        y -= CSng(Math.Ceiling(datos.Count / columnas)) * (alto + separacion) + 4
    End Sub

    Private Shared ReadOnly Columnas As Single() = {40, 80, 98, 98, 98, 118}
    Private Shared ReadOnly Titulos As String() = {"N.º", "Fecha", "Capital (B/.)", "Interés (B/.)", "Cuota (B/.)", "Saldo (B/.)"}

    Private Sub CabeceraTabla()
        Rect(Margen, y - 6, AnchoPagina - 2 * Margen, 19, VerdeClaro, Borde)
        Dim x = Margen
        For col = 0 To Columnas.Length - 1
            If col < 2 Then
                EscribirCentro(x + Columnas(col) / 2, y, 8.5F, True, Verde, Titulos(col))
            Else
                EscribirDerecha(x + Columnas(col) - 6, y, 8.5F, True, Verde, Titulos(col))
            End If
            x += Columnas(col)
        Next
        y -= 19
    End Sub

    Private Sub Tabla(c As CotizacionPrestamo, logo As LogoJpeg)
        Const altoFila As Single = 15
        CabeceraTabla()
        Dim n = 0
        For Each q In c.Plan
            ' La última cuota necesita espacio también para la fila de totales: así nunca quedan solos en otra hoja.
            Dim limite As Single = If(n = c.Plan.Count - 1, 92 + 20, 92)
            If y < limite Then
                NuevaPagina()
                Encabezado(c, logo, False)
                CabeceraTabla()
            End If
            If n Mod 2 = 1 Then Rect(Margen, y - 4.5F, AnchoPagina - 2 * Margen, altoFila, Zebra, Nothing)
            Dim valores = {q.Numero.ToString(CultureInfo.InvariantCulture), CotizacionPrestamo.Fecha(q.Fecha), CotizacionPrestamo.Numero2(q.Capital), CotizacionPrestamo.Numero2(q.Interes), CotizacionPrestamo.Numero2(q.Cuota), CotizacionPrestamo.Numero2(q.Saldo)}
            FilaTabla(valores, False, Texto)
            y -= altoFila
            n += 1
        Next
        If y < 92 Then NuevaPagina() : Encabezado(c, logo, False)
        Rect(Margen, y - 5, AnchoPagina - 2 * Margen, 17, VerdeClaro, Borde)
        FilaTabla({"Totales", "", CotizacionPrestamo.Numero2(c.Plan.Sum(Function(q) q.Capital)), CotizacionPrestamo.Numero2(c.TotalIntereses), CotizacionPrestamo.Numero2(c.TotalPagar), ""}, True, Verde)
        y -= 20
    End Sub

    Private Sub FilaTabla(valores As String(), negrita As Boolean, color As Single())
        Dim x = Margen
        For col = 0 To Columnas.Length - 1
            If valores(col) <> "" Then
                If col < 2 Then
                    EscribirCentro(x + Columnas(col) / 2, y, 8.5F, negrita, color, valores(col))
                Else
                    EscribirDerecha(x + Columnas(col) - 6, y, 8.5F, negrita, color, valores(col))
                End If
            End If
            x += Columnas(col)
        Next
    End Sub

    Private Sub Firmas()
        If y < 150 Then
            NuevaPagina()
            y = AltoPagina - Margen - 40
        End If
        y -= 40
        Dim ancho As Single = 200
        Dim x1 = Margen + 20, x2 = AnchoPagina - Margen - 20 - ancho
        Linea(x1, y, x1 + ancho, y, Texto, 0.7F)
        Linea(x2, y, x2 + ancho, y, Texto, 0.7F)
        EscribirCentro(x1 + ancho / 2, y - 12, 8.5F, False, Gris, "Firma del cliente")
        EscribirCentro(x2 + ancho / 2, y - 12, 8.5F, False, Gris, "Firma del asesor")
    End Sub

    ' ---------- Primitivas ----------

    Private Sub NuevaPagina()
        actual = New StringBuilder()
        paginas.Add(actual)
        y = AltoPagina - Margen
    End Sub

    Private Sub Color(c As Single(), relleno As Boolean)
        actual.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} {3}" & vbLf, c(0), c(1), c(2), If(relleno, "rg", "RG"))
    End Sub

    Private Sub Rect(x As Single, yy As Single, w As Single, h As Single, relleno As Single(), trazo As Single())
        If relleno IsNot Nothing Then Color(relleno, True)
        If trazo IsNot Nothing Then Color(trazo, False) : actual.Append("0.6 w" & vbLf)
        actual.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} {3:0.##} re {4}" & vbLf, x, yy, w, h, If(relleno IsNot Nothing AndAlso trazo IsNot Nothing, "B", If(relleno IsNot Nothing, "f", "S")))
    End Sub

    Private Sub Linea(x1 As Single, y1 As Single, x2 As Single, y2 As Single, c As Single(), grosor As Single)
        Color(c, False)
        actual.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##} w {1:0.##} {2:0.##} m {3:0.##} {4:0.##} l S" & vbLf, grosor, x1, y1, x2, y2)
    End Sub

    Private Sub Escribir(x As Single, yy As Single, tam As Single, negrita As Boolean, c As Single(), texto As String)
        If String.IsNullOrEmpty(texto) Then Return
        Color(c, True)
        actual.AppendFormat(CultureInfo.InvariantCulture, "BT /{0} {1:0.##} Tf {2:0.##} {3:0.##} Td (", If(negrita, "F2", "F1"), tam, x, yy)
        actual.Append(Escapar(texto)).Append(") Tj ET" & vbLf)
    End Sub

    Private Sub EscribirDerecha(xDerecha As Single, yy As Single, tam As Single, negrita As Boolean, c As Single(), texto As String)
        Escribir(xDerecha - Medir(texto, tam, negrita), yy, tam, negrita, c, texto)
    End Sub

    Private Sub EscribirCentro(xCentro As Single, yy As Single, tam As Single, negrita As Boolean, c As Single(), texto As String)
        Escribir(xCentro - Medir(texto, tam, negrita) / 2, yy, tam, negrita, c, texto)
    End Sub

    ''' <summary>Escapa el texto para un literal PDF; los caracteres se convierten a Windows-1252.</summary>
    Private Shared Function Escapar(texto As String) As String
        Dim sb As New StringBuilder()
        For Each ch In texto
            Dim c = If(Win1252.GetBytes(ch.ToString()).Length = 1 AndAlso Win1252.GetString(Win1252.GetBytes(ch.ToString())) = ch.ToString(), ch, "?"c)
            If c = "\"c OrElse c = "("c OrElse c = ")"c Then sb.Append("\"c)
            sb.Append(c)
        Next
        Return sb.ToString()
    End Function

    Private Shared Function Recortar(texto As String, tam As Single, negrita As Boolean, ancho As Single) As String
        If Medir(texto, tam, negrita) <= ancho Then Return texto
        Dim t = texto
        While t.Length > 1 AndAlso Medir(t & "…", tam, negrita) > ancho
            t = t.Substring(0, t.Length - 1)
        End While
        Return t.TrimEnd() & "…"
    End Function

    Private Shared Function Partir(texto As String, tam As Single, negrita As Boolean, ancho As Single) As List(Of String)
        Dim lineas As New List(Of String)()
        Dim actualLinea = ""
        For Each palabra In If(texto, "").Split(" "c)
            Dim prueba = If(actualLinea = "", palabra, actualLinea & " " & palabra)
            If Medir(prueba, tam, negrita) > ancho AndAlso actualLinea <> "" Then
                lineas.Add(actualLinea) : actualLinea = palabra
            Else
                actualLinea = prueba
            End If
        Next
        If actualLinea <> "" Then lineas.Add(actualLinea)
        Return lineas
    End Function

    ' Anchos AFM (1/1000 em) de Helvetica y Helvetica-Bold para los caracteres 32..126.
    Private Shared ReadOnly AnchoNormal As Integer() = {278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584}
    Private Shared ReadOnly AnchoNegrita As Integer() = {278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584}

    Private Shared Function Medir(texto As String, tam As Single, negrita As Boolean) As Single
        Dim tabla = If(negrita, AnchoNegrita, AnchoNormal)
        Dim total = 0
        For Each ch In If(texto, "")
            Dim c = ch
            Select Case c
                Case "·"c : total += 278 : Continue For
                Case "–"c : total += 556 : Continue For
                Case "…"c : total += 1000 : Continue For
                Case "º"c : total += 365 : Continue For
                Case "¿"c : total += 611 : Continue For
                Case "¡"c : total += 333 : Continue For
            End Select
            If AscW(c) > 126 Then
                Dim baseLetra = c.ToString().Normalize(NormalizationForm.FormD)(0)
                c = If(AscW(baseLetra) <= 126, baseLetra, "n"c)
            End If
            Dim code = AscW(c)
            total += If(code >= 32 AndAlso code <= 126, tabla(code - 32), 556)
        Next
        Return total * tam / 1000.0F
    End Function

    ' ---------- Logo y serialización ----------

    Private NotInheritable Class LogoJpeg
        Public Bytes As Byte()
        Public Ancho As Integer
        Public Alto As Integer
    End Class

    ''' <summary>Convierte el PNG (con transparencia) a JPEG sobre fondo blanco.</summary>
    Private Shared Function CargarLogo(ruta As String) As LogoJpeg
        If String.IsNullOrEmpty(ruta) OrElse Not File.Exists(ruta) Then Return Nothing
        Using original = System.Drawing.Image.FromFile(ruta)
            Using lienzo As New Bitmap(original.Width, original.Height)
                Using g = Graphics.FromImage(lienzo)
                    g.Clear(System.Drawing.Color.White)
                    g.DrawImage(original, 0, 0, original.Width, original.Height)
                End Using
                Dim codec = ImageCodecInfo.GetImageEncoders().First(Function(x) x.MimeType = "image/jpeg")
                Using parametros As New EncoderParameters(1), ms As New MemoryStream()
                    parametros.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L)
                    lienzo.Save(ms, codec, parametros)
                    Return New LogoJpeg With {.Bytes = ms.ToArray(), .Ancho = original.Width, .Alto = original.Height}
                End Using
            End Using
        End Using
    End Function

    Private Function Serializar(logo As LogoJpeg) As Byte()
        Using ms As New MemoryStream()
            Dim offsets As New List(Of Long)()
            Dim escribirAscii = Sub(t As String)
                                    Dim b = Win1252.GetBytes(t)
                                    ms.Write(b, 0, b.Length)
                                End Sub
            Dim iniciarObjeto = Sub(n As Integer)
                                    offsets.Add(ms.Position)
                                    escribirAscii(n.ToString(CultureInfo.InvariantCulture) & " 0 obj" & vbLf)
                                End Sub
            escribirAscii("%PDF-1.4" & vbLf)
            ms.Write(New Byte() {37, 226, 227, 207, 211, 10}, 0, 6)
            ' 1 catálogo, 2 páginas, 3 Helvetica, 4 Helvetica-Bold, 5 logo, 6.. página/contenido.
            Dim primeraPagina = 6
            Dim kids = String.Join(" ", Enumerable.Range(0, paginas.Count).Select(Function(i) (primeraPagina + i * 2).ToString(CultureInfo.InvariantCulture) & " 0 R"))
            iniciarObjeto(1) : escribirAscii("<< /Type /Catalog /Pages 2 0 R >>" & vbLf & "endobj" & vbLf)
            iniciarObjeto(2) : escribirAscii("<< /Type /Pages /Kids [" & kids & "] /Count " & paginas.Count.ToString(CultureInfo.InvariantCulture) & " >>" & vbLf & "endobj" & vbLf)
            iniciarObjeto(3) : escribirAscii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)
            iniciarObjeto(4) : escribirAscii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)
            iniciarObjeto(5)
            If logo IsNot Nothing Then
                escribirAscii(String.Format(CultureInfo.InvariantCulture, "<< /Type /XObject /Subtype /Image /Width {0} /Height {1} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {2} >>" & vbLf & "stream" & vbLf, logo.Ancho, logo.Alto, logo.Bytes.Length))
                ms.Write(logo.Bytes, 0, logo.Bytes.Length)
                escribirAscii(vbLf & "endstream" & vbLf & "endobj" & vbLf)
            Else
                escribirAscii("null" & vbLf & "endobj" & vbLf)
            End If
            For i = 0 To paginas.Count - 1
                Dim pag = primeraPagina + i * 2
                iniciarObjeto(pag)
                escribirAscii(String.Format(CultureInfo.InvariantCulture, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0} {1}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >>{2} >> /Contents {3} 0 R >>" & vbLf & "endobj" & vbLf,
                                            AnchoPagina, AltoPagina, If(logo IsNot Nothing, " /XObject << /Im1 5 0 R >>", ""), pag + 1))
                Dim contenido = Win1252.GetBytes(paginas(i).ToString())
                iniciarObjeto(pag + 1)
                escribirAscii("<< /Length " & contenido.Length.ToString(CultureInfo.InvariantCulture) & " >>" & vbLf & "stream" & vbLf)
                ms.Write(contenido, 0, contenido.Length)
                escribirAscii(vbLf & "endstream" & vbLf & "endobj" & vbLf)
            Next
            Dim inicioXref = ms.Position
            escribirAscii("xref" & vbLf & "0 " & (offsets.Count + 1).ToString(CultureInfo.InvariantCulture) & vbLf & "0000000000 65535 f " & vbLf)
            For Each o In offsets
                escribirAscii(o.ToString("0000000000", CultureInfo.InvariantCulture) & " 00000 n " & vbLf)
            Next
            escribirAscii("trailer" & vbLf & "<< /Size " & (offsets.Count + 1).ToString(CultureInfo.InvariantCulture) & " /Root 1 0 R /Info << /Title (Cotizacion de prestamo - CapitalWest) /Producer (CapitalWest) >> >>" & vbLf & "startxref" & vbLf & inicioXref.ToString(CultureInfo.InvariantCulture) & vbLf & "%%EOF")
            Return ms.ToArray()
        End Using
    End Function
End Class
