Option Strict On
Imports SistemaFinanciera.Entities
Imports System.Text.RegularExpressions
Imports System.Net.Mail

' Las reglas se aplican en el servidor, incluso cuando se omite la validación del navegador.
Public Class ClienteServicio
    Public Const PatronNombre As String = "^[\p{L}\p{M}]+\.?(?:[ '\u2019.\-][\p{L}\p{M}]+\.?)*$"
    Public Const PatronCedula As String = "^(?:[0-9]{1,2}(?:AV|PI)?|PE|E|N|AV|PI)-[0-9]{1,6}-[0-9]{1,6}$"
    Public Const PatronPasaporte As String = "^[A-Z0-9]{5,30}$"
    Public Const PatronRuc As String = "^[0-9]{1,10}-[0-9]{1,6}-[0-9]{1,10}$"
    Public Const PatronTelefono As String = "^(?!0+$)[0-9]{7,8}$"
    Public Shared ReadOnly Property HoyPanama As Date
        Get
            Return Date.UtcNow.AddHours(-5).Date
        End Get
    End Property
    Private ReadOnly repositorio As IClienteRepositorio
    Public Sub New(repositorio As IClienteRepositorio)
        If repositorio Is Nothing Then Throw New ArgumentNullException(NameOf(repositorio))
        Me.repositorio = repositorio
    End Sub
    Public Function Listar(busqueda As String, activo As Nullable(Of Boolean), Optional provincia As String = "", Optional tipoPersona As String = "") As List(Of Cliente)
        Dim datos = repositorio.Listar(If(busqueda, "").Trim(), activo)
        Return datos.Where(Function(c) (String.IsNullOrEmpty(provincia) OrElse String.Equals(c.Provincia, provincia, StringComparison.OrdinalIgnoreCase)) AndAlso (String.IsNullOrEmpty(tipoPersona) OrElse c.TipoPersona = tipoPersona)).ToList()
    End Function
    Public Function Obtener(id As Integer) As Cliente
        Return repositorio.Obtener(id)
    End Function
    Public Function EstadosCivilesActivos() As List(Of KeyValuePair(Of String, String))
        Return repositorio.EstadosCivilesActivos()
    End Function
    Public Function Guardar(cliente As Cliente) As Integer
        Dim errores = Validar(cliente)
        If errores.Count > 0 Then Throw New ArgumentException(String.Join(Environment.NewLine, errores))
        If cliente.TipoPersona = "NATURAL" AndAlso Not repositorio.EstadoCivilActivo(cliente.EstadoCivil) Then
            Dim estadoAnterior As Cliente = Nothing
            If cliente.Id > 0 Then estadoAnterior = repositorio.Obtener(cliente.Id)
            If estadoAnterior Is Nothing OrElse estadoAnterior.EstadoCivil <> cliente.EstadoCivil Then Throw New ArgumentException("El estado civil seleccionado ya no está disponible. Elija una opción activa del catálogo.")
        End If
        Return repositorio.Guardar(cliente)
    End Function
    Public Sub CambiarEstado(id As Integer, activo As Boolean, version As Byte())
        If id <= 0 OrElse version Is Nothing Then Throw New ArgumentException("Cliente inválido.")
        repositorio.CambiarEstado(id, activo, version)
    End Sub
    Public Shared Function Validar(c As Cliente) As List(Of String)
        Dim errores As New List(Of String)
        If c Is Nothing Then
            errores.Add("Debe indicar el cliente.")
            Return errores
        End If
        c.TipoPersona = If(c.TipoPersona, "").Trim().ToUpperInvariant()
        c.TipoIdentificacion = If(c.TipoIdentificacion, "").Trim().ToUpperInvariant()
        c.Identificacion = Regex.Replace(If(c.Identificacion, "").Trim().ToUpperInvariant(), "\s", "")
        c.Telefono = Regex.Replace(If(c.Telefono, ""), "[\s()+-]", "")
        If c.Telefono.StartsWith("507") AndAlso (c.Telefono.Length = 10 OrElse c.Telefono.Length = 11) Then c.Telefono = c.Telefono.Substring(3)
        c.Correo = If(c.Correo, "").Trim()
        c.Nombres = If(c.Nombres, "").Trim().Normalize()
        c.Apellidos = If(c.Apellidos, "").Trim().Normalize()
        If c.TipoPersona <> "NATURAL" AndAlso c.TipoPersona <> "JURIDICA" Then errores.Add("Seleccione el tipo de persona.")
        If Not {"CEDULA", "PASAPORTE", "RUC"}.Contains(c.TipoIdentificacion) Then errores.Add("Seleccione el tipo de identificación.")
        If c.TipoPersona = "JURIDICA" AndAlso c.TipoIdentificacion <> "RUC" Then errores.Add("Para persona jurídica indique el RUC.")
        If c.TipoPersona = "NATURAL" AndAlso Not {"CEDULA", "PASAPORTE"}.Contains(c.TipoIdentificacion) Then errores.Add("Para persona natural seleccione cédula o pasaporte.")
        If c.TipoIdentificacion = "CEDULA" AndAlso Not Regex.IsMatch(c.Identificacion, PatronCedula) Then errores.Add("Indique la cédula con prefijo admitido y tres grupos (ej. 8-123-456 o E-123-456).")
        If c.TipoIdentificacion = "PASAPORTE" AndAlso Not Regex.IsMatch(c.Identificacion, PatronPasaporte) Then errores.Add("El pasaporte debe contener entre 5 y 30 letras o números.")
        If c.TipoIdentificacion = "RUC" AndAlso Not Regex.IsMatch(c.Identificacion, PatronRuc) Then errores.Add("Indique el RUC en tres grupos numéricos separados por guiones.")
        ComprobarTexto(c.Identificacion, "Identificación", 30, True, errores)
        ComprobarTexto(c.Nombres, "Nombres o razón social", If(c.TipoPersona = "JURIDICA", 100, 50), True, errores)
        ComprobarTexto(c.Apellidos, "Apellidos", 50, c.TipoPersona = "NATURAL", errores)
        If c.TipoPersona = "NATURAL" Then
            If Not Regex.IsMatch(c.Nombres, PatronNombre) Then errores.Add("Los nombres solo admiten letras, acentos, espacios, apóstrofos, puntos y guiones entre nombres.")
            If Not Regex.IsMatch(c.Apellidos, PatronNombre) Then errores.Add("Los apellidos solo admiten letras, acentos, espacios, apóstrofos, puntos y guiones entre apellidos.")
            If Not c.FechaNacimiento.HasValue OrElse c.FechaNacimiento.Value.Date > HoyPanama OrElse c.FechaNacimiento.Value.Date < HoyPanama.AddYears(-120) Then errores.Add("Indique una fecha de nacimiento válida, no futura.")
            If Not {"F", "M", "OTRO"}.Contains(c.Sexo) Then errores.Add("Seleccione el sexo.")
            If Not {"SOLTERO", "CASADO", "UNIDO", "DIVORCIADO", "VIUDO"}.Contains(c.EstadoCivil) Then errores.Add("Seleccione el estado civil.")
        ElseIf c.TipoPersona = "JURIDICA" Then
            If Not Regex.IsMatch(c.Nombres, "\p{L}") Then errores.Add("La razón social debe contener al menos una letra.")
            c.Apellidos = ""
            c.FechaNacimiento = Nothing
            c.Sexo = "NO_APLICA"
            c.EstadoCivil = "NO_APLICA"
        End If
        If Not Regex.IsMatch(c.Telefono, PatronTelefono) Then errores.Add("El teléfono debe tener 7 u 8 dígitos y no puede contener solo ceros; puede incluir +507.")
        ComprobarTexto(c.Correo, "Correo", 35, True, errores)
        Try
            Dim email As New MailAddress(c.Correo)
            If email.Address <> c.Correo OrElse Not email.Host.Contains(".") Then errores.Add("Indique un correo válido.")
        Catch ex As FormatException
            errores.Add("Indique un correo válido.")
        Catch ex As ArgumentException
            errores.Add("Indique un correo válido.")
        End Try
        ComprobarTexto(c.Direccion, "Dirección", 200, True, errores)
        If Not Regex.IsMatch(If(c.Direccion, ""), "\p{L}") Then errores.Add("La dirección debe incluir una descripción con letras.")
        If Not Regex.IsMatch(If(c.CodigoProvincia, ""), "^[0-9]{2}$") OrElse Not Regex.IsMatch(If(c.CodigoDistrito, ""), "^[0-9]{4}$") OrElse Not Regex.IsMatch(If(c.CodigoCorregimiento, ""), "^[0-9]{6}$") Then errores.Add("Seleccione provincia, distrito y corregimiento del catálogo.")
        ComprobarTexto(c.Provincia, "Provincia o comarca", 80, True, errores)
        ComprobarTexto(c.Distrito, "Distrito", 150, True, errores)
        ComprobarTexto(c.Corregimiento, "Corregimiento", 150, True, errores)
        ComprobarTexto(c.Ocupacion, "Información laboral o actividad económica", 80, True, errores)
        If Not Regex.IsMatch(If(c.Ocupacion, ""), "\p{L}") Then errores.Add("Describa la ocupación o actividad económica con letras.")
        ComprobarTexto(c.Referencias, "Referencias", 300, True, errores)
        If Not ReferenciasValidas(c.Referencias) Then errores.Add("Incluya el nombre de la referencia y un teléfono válido de 7 u 8 dígitos. Puede usar texto libre y saltos de línea.")
        If c.Ingresos < 0D OrElse c.Ingresos > 999999999999.99D OrElse Decimal.Round(c.Ingresos, 2) <> c.Ingresos Then errores.Add("Los ingresos deben ser no negativos y tener como máximo dos decimales.")
        Return errores
    End Function
    Private Shared Sub ComprobarTexto(valor As String, nombre As String, limite As Integer, obligatorio As Boolean, errores As List(Of String))
        If obligatorio AndAlso String.IsNullOrWhiteSpace(valor) Then errores.Add(nombre & " es obligatorio.")
        If If(valor, "").Length > limite Then errores.Add(nombre & " supera " & limite.ToString() & " caracteres.")
        If Regex.IsMatch(If(valor, ""), "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F\p{Cf}<>]") Then errores.Add(nombre & " contiene caracteres no admitidos.")
    End Sub
    Public Shared Function ReferenciasValidas(valor As String) As Boolean
        If String.IsNullOrWhiteSpace(valor) Then Return False
        ' Es texto libre, no un CSV: no imponer orden, separadores ni una línea por persona.
        If Not Regex.IsMatch(valor, "[\p{L}\p{M}]{2,}") Then Return False
        For Each candidato As Match In Regex.Matches(valor, "\+?[0-9](?:[0-9 \t()+-]*[0-9])?")
            Dim telefono = Regex.Replace(candidato.Value, "[\s()+-]", "")
            If telefono.StartsWith("507") AndAlso (telefono.Length = 10 OrElse telefono.Length = 11) Then telefono = telefono.Substring(3)
            If Regex.IsMatch(telefono, PatronTelefono) Then Return True
        Next
        Return False
    End Function
End Class
