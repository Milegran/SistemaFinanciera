Option Strict On
Imports MySqlConnector
Imports SistemaFinanciera.Entities

' Acceso parametrizado; la restricción UNIQUE también protege contra altas simultáneas.
Public Class ClienteRepositorio
    Implements IClienteRepositorio
    Private ReadOnly conexion As String
    Public Sub New(cadenaConexion As String)
        If String.IsNullOrWhiteSpace(cadenaConexion) Then Throw New ArgumentException("Configure la conexión SistemaFinanciera.")
        conexion = cadenaConexion
    End Sub
    Public Function Listar(busqueda As String, activo As Nullable(Of Boolean)) As List(Of Cliente) Implements IClienteRepositorio.Listar
        Dim resultado As New List(Of Cliente)
        Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand("SELECT * FROM Clientes WHERE (@Estado IS NULL OR Activo=@Estado) AND (@Busqueda='' OR LOCATE(@Busqueda,Identificacion)>0 OR LOCATE(@Busqueda,CONCAT(Nombres,' ',Apellidos))>0 OR LOCATE(@Busqueda,Correo)>0 OR LOCATE(@Busqueda,Telefono)>0) ORDER BY Nombres,Apellidos,Id", cn)
            cmd.Parameters.Add("@Estado", MySqlDbType.Bool).Value = If(activo.HasValue, CType(activo.GetValueOrDefault(), Object), DBNull.Value)
            cmd.Parameters.Add("@Busqueda", MySqlDbType.VarChar, 254).Value = busqueda
            Abrir(cn)
            Using lector = cmd.ExecuteReader()
                While lector.Read()
                    resultado.Add(Leer(lector))
                End While
            End Using
        End Using
        Return resultado
    End Function
    Public Function Obtener(id As Integer) As Cliente Implements IClienteRepositorio.Obtener
        Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand("SELECT * FROM Clientes WHERE Id=@Id", cn)
            cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = id
            Abrir(cn)
            Using lector = cmd.ExecuteReader()
                If lector.Read() Then Return Leer(lector)
            End Using
        End Using
        Throw New InvalidOperationException("El cliente ya no está disponible. Actualice el listado.")
    End Function
    Public Function Guardar(c As Cliente) As Integer Implements IClienteRepositorio.Guardar
        ResolverUbicacion(c)
        Dim sql = If(c.Id = 0, "INSERT INTO Clientes (TipoPersona, TipoIdentificacion, Identificacion, Nombres, Apellidos, FechaNacimiento, Sexo, EstadoCivil, Telefono, Correo, Direccion, CodigoProvincia, CodigoDistrito, CodigoCorregimiento, Provincia, Distrito, Corregimiento, Ocupacion, Ingresos, Referencias) VALUES (@TipoPersona, @TipoIdentificacion, @Identificacion, @Nombres, @Apellidos, @FechaNacimiento, @Sexo, @EstadoCivil, @Telefono, @Correo, @Direccion, @CodigoProvincia, @CodigoDistrito, @CodigoCorregimiento, @Provincia, @Distrito, @Corregimiento, @Ocupacion, @Ingresos, @Referencias)", "UPDATE Clientes SET TipoPersona=@TipoPersona, TipoIdentificacion=@TipoIdentificacion, Identificacion=@Identificacion, Nombres=@Nombres, Apellidos=@Apellidos, FechaNacimiento=@FechaNacimiento, Sexo=@Sexo, EstadoCivil=@EstadoCivil, Telefono=@Telefono, Correo=@Correo, Direccion=@Direccion, CodigoProvincia=@CodigoProvincia, CodigoDistrito=@CodigoDistrito, CodigoCorregimiento=@CodigoCorregimiento, Provincia=@Provincia, Distrito=@Distrito, Corregimiento=@Corregimiento, Ocupacion=@Ocupacion, Ingresos=@Ingresos, Referencias=@Referencias, Version=Version+1, ModificadoUtc=UTC_TIMESTAMP() WHERE Id=@Id AND Version=@Version")
        Try
            Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand(sql, cn)
                Parametros(cmd, c)
                If c.Id <> 0 Then
                    cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = c.Id
                    cmd.Parameters.Add("@Version", MySqlDbType.UInt64).Value = NumeroVersion(c.Version)
                End If
                Abrir(cn)
                Dim afectados = cmd.ExecuteNonQuery()
                If afectados <> 1 Then Throw New InvalidOperationException("Otro usuario modificó el cliente. Vuelva a abrirlo antes de guardar.")
                Return If(c.Id = 0, Convert.ToInt32(cmd.LastInsertedId), c.Id)
            End Using
        Catch ex As MySqlException When ex.Number = 1062
            Throw New ArgumentException("Ya existe un cliente con esa identificación, incluso si está inactivo.", ex)
        End Try
    End Function
    Private Sub ResolverUbicacion(c As Cliente)
        Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand("SELECT p.nombre_provincia,d.nombre_distrito,r.nombre_corregimiento FROM provincia p JOIN distrito d ON d.codigo_provincia=p.codigo_provincia JOIN corregimiento r ON r.codigo_distrito=d.codigo_distrito AND r.codigo_provincia=p.codigo_provincia WHERE p.codigo_provincia=@p AND d.codigo_distrito=@d AND r.codigo_corregimiento=@r", cn)
            cmd.Parameters.AddWithValue("@p", If(c.CodigoProvincia, ""))
            cmd.Parameters.AddWithValue("@d", If(c.CodigoDistrito, ""))
            cmd.Parameters.AddWithValue("@r", If(c.CodigoCorregimiento, ""))
            Abrir(cn)
            Using lector = cmd.ExecuteReader()
                If Not lector.Read() Then Throw New ArgumentException("La ubicación no pertenece a una provincia, distrito y corregimiento válidos del catálogo.")
                c.Provincia = lector.GetString(0)
                c.Distrito = lector.GetString(1)
                c.Corregimiento = lector.GetString(2)
            End Using
        End Using
    End Sub
    Public Function Ubicaciones(nivel As Integer, Optional provincia As String = "", Optional distrito As String = "") As List(Of KeyValuePair(Of String, String))
        Dim sql As String
        Select Case nivel
            Case 1 : sql = "SELECT codigo_provincia,nombre_provincia FROM provincia ORDER BY nombre_provincia"
            Case 2 : sql = "SELECT codigo_distrito,nombre_distrito FROM distrito WHERE codigo_provincia=@p ORDER BY nombre_distrito"
            Case 3 : sql = "SELECT r.codigo_corregimiento,r.nombre_corregimiento FROM corregimiento r JOIN distrito d ON d.codigo_distrito=r.codigo_distrito AND d.codigo_provincia=r.codigo_provincia WHERE r.codigo_provincia=@p AND r.codigo_distrito=@d ORDER BY r.nombre_corregimiento"
            Case Else : Throw New ArgumentException("Nivel de ubicación inválido.")
        End Select
        Dim datos As New List(Of KeyValuePair(Of String, String))
        Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand(sql, cn)
            cmd.Parameters.AddWithValue("@p", provincia)
            cmd.Parameters.AddWithValue("@d", distrito)
            Abrir(cn)
            Using lector = cmd.ExecuteReader()
                While lector.Read()
                    datos.Add(New KeyValuePair(Of String, String)(lector.GetString(0), lector.GetString(1)))
                End While
            End Using
        End Using
        Return datos
    End Function
    Public Sub CambiarEstado(id As Integer, activo As Boolean, version As Byte()) Implements IClienteRepositorio.CambiarEstado
        Using cn As New MySqlConnection(conexion), cmd As New MySqlCommand("UPDATE Clientes SET Activo=@Activo, Version=Version+1, ModificadoUtc=UTC_TIMESTAMP() WHERE Id=@Id AND Version=@Version", cn)
            cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = id
            cmd.Parameters.Add("@Activo", MySqlDbType.Bool).Value = activo
            cmd.Parameters.Add("@Version", MySqlDbType.UInt64).Value = NumeroVersion(version)
            Abrir(cn)
            If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Otro usuario modificó el cliente. Actualice el listado.")
        End Using
    End Sub
    ' La UI conserva su token opaco de ocho bytes; en MariaDB se almacena como BIGINT UNSIGNED.
    Private Shared Function NumeroVersion(version As Byte()) As ULong
        If version Is Nothing OrElse version.Length <> 8 Then Throw New InvalidOperationException("Vuelva a abrir el cliente antes de modificarlo.")
        Return BitConverter.ToUInt64(version, 0)
    End Function
    Private Shared Sub Abrir(cn As MySqlConnection)
        cn.Open()
        Using zona As New MySqlCommand("SET time_zone = '+00:00'", cn)
            zona.ExecuteNonQuery()
        End Using
    End Sub
    Private Shared Sub Parametros(cmd As MySqlCommand, c As Cliente)
        cmd.Parameters.Add("@TipoPersona", MySqlDbType.VarChar, 10).Value = If(c.TipoPersona, "").Trim()
        cmd.Parameters.Add("@TipoIdentificacion", MySqlDbType.VarChar, 10).Value = If(c.TipoIdentificacion, "").Trim()
        cmd.Parameters.Add("@Identificacion", MySqlDbType.VarChar, 30).Value = If(c.Identificacion, "").Trim()
        cmd.Parameters.Add("@Nombres", MySqlDbType.VarChar, 120).Value = If(c.Nombres, "").Trim()
        cmd.Parameters.Add("@Apellidos", MySqlDbType.VarChar, 120).Value = If(c.Apellidos, "").Trim()
        cmd.Parameters.Add("@FechaNacimiento", MySqlDbType.Date).Value = If(c.FechaNacimiento.HasValue, CType(c.FechaNacimiento.GetValueOrDefault().Date, Object), DBNull.Value)
        cmd.Parameters.Add("@Sexo", MySqlDbType.VarChar, 10).Value = If(c.Sexo, "").Trim()
        cmd.Parameters.Add("@EstadoCivil", MySqlDbType.VarChar, 15).Value = If(c.EstadoCivil, "").Trim()
        cmd.Parameters.Add("@Telefono", MySqlDbType.VarChar, 8).Value = If(c.Telefono, "").Trim()
        cmd.Parameters.Add("@Correo", MySqlDbType.VarChar, 254).Value = If(c.Correo, "").Trim()
        cmd.Parameters.Add("@Direccion", MySqlDbType.VarChar, 300).Value = If(c.Direccion, "").Trim()
        cmd.Parameters.Add("@CodigoProvincia", MySqlDbType.VarChar, 2).Value = c.CodigoProvincia
        cmd.Parameters.Add("@CodigoDistrito", MySqlDbType.VarChar, 4).Value = c.CodigoDistrito
        cmd.Parameters.Add("@CodigoCorregimiento", MySqlDbType.VarChar, 6).Value = c.CodigoCorregimiento
        cmd.Parameters.Add("@Provincia", MySqlDbType.VarChar, 80).Value = If(c.Provincia, "").Trim()
        cmd.Parameters.Add("@Distrito", MySqlDbType.VarChar, 150).Value = If(c.Distrito, "").Trim()
        cmd.Parameters.Add("@Corregimiento", MySqlDbType.VarChar, 150).Value = If(c.Corregimiento, "").Trim()
        cmd.Parameters.Add("@Ocupacion", MySqlDbType.VarChar, 200).Value = If(c.Ocupacion, "").Trim()
        Dim ingresos = cmd.Parameters.Add("@Ingresos", MySqlDbType.Decimal)
        ingresos.Precision = 14
        ingresos.Scale = 2
        ingresos.Value = c.Ingresos
        cmd.Parameters.Add("@Referencias", MySqlDbType.VarChar, 1000).Value = If(c.Referencias, "").Trim()
    End Sub
    Private Shared Function Leer(r As MySqlDataReader) As Cliente
        Dim c As New Cliente With {.CreadoUtc = DateTime.SpecifyKind(CDate(r("CreadoUtc")), DateTimeKind.Utc), .Id = CInt(r("Id")), .Activo = CBool(r("Activo")), .Version = BitConverter.GetBytes(Convert.ToUInt64(r("Version")))}
        c.TipoPersona = CStr(r("TipoPersona"))
        c.TipoIdentificacion = CStr(r("TipoIdentificacion"))
        c.Identificacion = CStr(r("Identificacion"))
        c.Nombres = CStr(r("Nombres"))
        c.Apellidos = CStr(r("Apellidos"))
        c.FechaNacimiento = If(r.IsDBNull(r.GetOrdinal("FechaNacimiento")), CType(Nothing, Nullable(Of Date)), CDate(r("FechaNacimiento")))
        c.Sexo = CStr(r("Sexo"))
        c.EstadoCivil = CStr(r("EstadoCivil"))
        c.Telefono = CStr(r("Telefono"))
        c.Correo = CStr(r("Correo"))
        c.Direccion = CStr(r("Direccion"))
        c.CodigoProvincia = If(IsDBNull(r("CodigoProvincia")), "", CStr(r("CodigoProvincia")))
        c.CodigoDistrito = If(IsDBNull(r("CodigoDistrito")), "", CStr(r("CodigoDistrito")))
        c.CodigoCorregimiento = If(IsDBNull(r("CodigoCorregimiento")), "", CStr(r("CodigoCorregimiento")))
        c.Provincia = CStr(r("Provincia"))
        c.Distrito = CStr(r("Distrito"))
        c.Corregimiento = CStr(r("Corregimiento"))
        c.Ocupacion = CStr(r("Ocupacion"))
        c.Ingresos = CDec(r("Ingresos"))
        c.Referencias = CStr(r("Referencias"))
        Return c
    End Function
End Class
