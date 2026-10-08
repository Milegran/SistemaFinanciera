Option Strict On
Imports SistemaFinanciera.Entities
Imports System.Text.RegularExpressions
Imports System.Net.Mail

' Las reglas se aplican en el servidor, incluso cuando se omite la validación del navegador.
Public Class ClienteServicio
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
    Public Function Guardar(cliente As Cliente) As Integer
        Dim errores = Validar(cliente)
        If errores.Count > 0 Then Throw New ArgumentException(String.Join(Environment.NewLine, errores))
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
        If c.TipoPersona <> "NATURAL" AndAlso c.TipoPersona <> "JURIDICA" Then errores.Add("Seleccione el tipo de persona.")
        If Not {"CEDULA", "PASAPORTE", "RUC"}.Contains(c.TipoIdentificacion) Then errores.Add("Seleccione el tipo de identificación.")
        If c.TipoPersona = "JURIDICA" AndAlso c.TipoIdentificacion <> "RUC" Then errores.Add("Para persona jurídica indique el RUC.")
        If c.TipoIdentificacion = "CEDULA" AndAlso Not Regex.IsMatch(c.Identificacion, "^[A-Z0-9]+-[0-9]+-[0-9]+$") Then errores.Add("La cédula debe tener tres grupos separados por guiones (ej. 8-123-456 o E-123-456).")
        If c.TipoIdentificacion = "PASAPORTE" AndAlso Not Regex.IsMatch(c.Identificacion, "^[A-Z0-9]{5,30}$") Then errores.Add("El pasaporte debe contener entre 5 y 30 letras o números.")
        If c.TipoIdentificacion = "RUC" AndAlso Not Regex.IsMatch(c.Identificacion, "^[A-Z0-9]+(?:-[A-Z0-9]+)*$") Then errores.Add("Indique un RUC con letras, números y guiones.")
        ComprobarTexto(c.Identificacion, "Identificación", 30, True, errores)
        ComprobarTexto(c.Nombres, "Nombres o razón social", 120, True, errores)
        ComprobarTexto(c.Apellidos, "Apellidos", 120, c.TipoPersona = "NATURAL", errores)
        If c.TipoPersona = "NATURAL" Then
            If Not c.FechaNacimiento.HasValue OrElse c.FechaNacimiento.Value.Date > Date.Today OrElse c.FechaNacimiento.Value.Date < Date.Today.AddYears(-120) Then errores.Add("Indique una fecha de nacimiento válida, no futura.")
            If Not {"F", "M", "OTRO"}.Contains(c.Sexo) Then errores.Add("Seleccione el sexo.")
            If Not {"SOLTERO", "CASADO", "UNIDO", "DIVORCIADO", "VIUDO"}.Contains(c.EstadoCivil) Then errores.Add("Seleccione el estado civil.")
        ElseIf c.TipoPersona = "JURIDICA" Then
            c.FechaNacimiento = Nothing
            c.Sexo = "NO_APLICA"
            c.EstadoCivil = "NO_APLICA"
        End If
        If Not Regex.IsMatch(c.Telefono, "^[0-9]{7,8}$") Then errores.Add("El teléfono debe tener 7 u 8 dígitos; puede incluir +507.")
        ComprobarTexto(c.Correo, "Correo", 254, True, errores)
        Try
            Dim email As New MailAddress(c.Correo)
            If email.Address <> c.Correo OrElse Not email.Host.Contains(".") Then errores.Add("Indique un correo válido.")
        Catch ex As FormatException
            errores.Add("Indique un correo válido.")
        Catch ex As ArgumentException
            errores.Add("Indique un correo válido.")
        End Try
        ComprobarTexto(c.Direccion, "Dirección", 300, True, errores)
        If Not Regex.IsMatch(If(c.CodigoProvincia, ""), "^[0-9]{2}$") OrElse Not Regex.IsMatch(If(c.CodigoDistrito, ""), "^[0-9]{4}$") OrElse Not Regex.IsMatch(If(c.CodigoCorregimiento, ""), "^[0-9]{6}$") Then errores.Add("Seleccione provincia, distrito y corregimiento del catálogo.")
        ComprobarTexto(c.Provincia, "Provincia o comarca", 80, True, errores)
        ComprobarTexto(c.Distrito, "Distrito", 150, True, errores)
        ComprobarTexto(c.Corregimiento, "Corregimiento", 150, True, errores)
        ComprobarTexto(c.Ocupacion, "Información laboral o actividad económica", 200, True, errores)
        ComprobarTexto(c.Referencias, "Referencias", 1000, True, errores)
        If c.Ingresos < 0D OrElse c.Ingresos > 999999999999.99D OrElse Decimal.Round(c.Ingresos, 2) <> c.Ingresos Then errores.Add("Los ingresos deben ser no negativos y tener como máximo dos decimales.")
        Return errores
    End Function
    Private Shared Sub ComprobarTexto(valor As String, nombre As String, limite As Integer, obligatorio As Boolean, errores As List(Of String))
        If obligatorio AndAlso String.IsNullOrWhiteSpace(valor) Then errores.Add(nombre & " es obligatorio.")
        If If(valor, "").Length > limite Then errores.Add(nombre & " supera " & limite.ToString() & " caracteres.")
    End Sub
End Class
