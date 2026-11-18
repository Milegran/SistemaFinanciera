Option Strict On
Imports SistemaFinanciera.Entities

''' <summary>
''' Matriz única de acceso a pantallas por rol. La usan el guardia de FinanzasPagina
''' (bloquea la página en el servidor) y los menús (ocultan lo que el rol no puede abrir).
''' Las operaciones siguen validándose además en la capa de datos.
''' </summary>
Public NotInheritable Class AccesoPantallas
    Public Const Administrador As String = "ADMINISTRADOR"
    Public Const Oficial As String = "OFICIAL_CREDITO"
    Public Const Cajero As String = "CAJERO"
    Public Const Consulta As String = "CONSULTA"
    Private Shared ReadOnly Todos As String() = {Administrador, Oficial, Cajero, Consulta}

    ' Ruta relativa a la aplicación (sin "~/") → roles que pueden abrirla.
    Private Shared ReadOnly Reglas As New Dictionary(Of String, String())(StringComparer.OrdinalIgnoreCase) From {
        {"Default.aspx", Todos},
        {"Clientes/Clientes.aspx", {Administrador, Oficial, Consulta}},
        {"Productos/Productos.aspx", {Administrador, Oficial, Consulta}},
        {"Simulador/Simulador.aspx", {Administrador, Oficial}},
        {"Solicitudes/Solicitudes.aspx", {Administrador, Oficial, Consulta}},
        {"Prestamos/Prestamos.aspx", Todos},
        {"Prestamos/DetallePrestamo.aspx", Todos},
        {"Pagos/Pagos.aspx", {Administrador, Cajero, Consulta}},
        {"Pagos/Factura.aspx", {Administrador, Cajero}},
        {"Reportes/Reportes.aspx", {Administrador, Consulta}},
        {"Administracion/CambiarClave.aspx", Todos}}

    Private Sub New()
    End Sub

    Private Shared Function Normalizar(ruta As String) As String
        Dim r = If(ruta, "").Trim()
        Dim corte = r.IndexOfAny({"?"c, "#"c})
        If corte >= 0 Then r = r.Substring(0, corte)
        If r.StartsWith("~/", StringComparison.Ordinal) Then r = r.Substring(2)
        Return r.TrimStart("/"c)
    End Function

    ''' <summary>Roles permitidos para la ruta; Nothing si la ruta no está regulada.</summary>
    Public Shared Function RolesDe(ruta As String) As String()
        Dim r = Normalizar(ruta)
        Dim roles As String() = Nothing
        If Reglas.TryGetValue(r, roles) Then Return roles
        ' Toda la carpeta de administración es exclusiva del administrador.
        If r.StartsWith("Administracion/", StringComparison.OrdinalIgnoreCase) Then Return {Administrador}
        Return Nothing
    End Function

    Public Shared Function Permite(usuario As UsuarioFinanzas, ruta As String) As Boolean
        If usuario Is Nothing Then Return False
        Dim roles = RolesDe(ruta)
        Return roles Is Nothing OrElse usuario.Puede(roles)
    End Function

    ''' <summary>Quién puede crear, editar o desactivar clientes.</summary>
    Public Shared Function GestionaClientes(usuario As UsuarioFinanzas) As Boolean
        Return usuario IsNot Nothing AndAlso usuario.Puede(Administrador, Oficial)
    End Function
End Class
