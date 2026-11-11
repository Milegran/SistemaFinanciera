Option Strict On
Imports System.Data
Imports MySqlConnector
Imports SistemaFinanciera.Entities

Public Class FinanzasRepositorio
    Private ReadOnly cadena As String
    Public Sub New(conexion As String)
        cadena = conexion
    End Sub
    Private Function Abrir() As MySqlConnection
        Dim cn As New MySqlConnection(cadena)
        cn.Open()
        Using cmd As New MySqlCommand("SET time_zone='+00:00'", cn)
            cmd.ExecuteNonQuery()
        End Using
        Return cn
    End Function
    Private Shared Function Comando(cn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray valores As Object()) As MySqlCommand
        Dim cmd As New MySqlCommand(sql, cn, tx)
        For i = 0 To valores.Length - 1
            cmd.Parameters.AddWithValue("@p" & i, If(valores(i), DBNull.Value))
        Next
        Return cmd
    End Function
    Private Shared Function Tabla(cn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray valores As Object()) As DataTable
        Using cmd = Comando(cn, tx, sql, valores), lector = cmd.ExecuteReader()
            Dim t As New DataTable()
            t.Load(lector)
            Return t
        End Using
    End Function
    Private Shared Function Escribir(cn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray valores As Object()) As Integer
        Using cmd = Comando(cn, tx, sql, valores)
            Return cmd.ExecuteNonQuery()
        End Using
    End Function
    Private Shared Function Insertar(cn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray valores As Object()) As Integer
        Using cmd = Comando(cn, tx, sql, valores)
            cmd.ExecuteNonQuery()
            Return CInt(cmd.LastInsertedId)
        End Using
    End Function
    Private Function Transaccion(Of T)(accion As Func(Of MySqlConnection, MySqlTransaction, T)) As T
        Using cn = Abrir(), tx = cn.BeginTransaction(IsolationLevel.ReadCommitted)
            Try
                Dim resultado = accion(cn, tx)
                tx.Commit()
                Return resultado
            Catch ex As MySqlException When ex.Number = 1062
                tx.Rollback()
                Throw New ArgumentException("Ya existe un registro con ese código o la operación ya fue registrada.", ex)
            Catch
                tx.Rollback()
                Throw
            End Try
        End Using
    End Function
    Private Shared Function Usuario(cn As MySqlConnection, tx As MySqlTransaction, id As Integer) As UsuarioFinanzas
        Dim t = Tabla(cn, tx, "SELECT u.Id,u.NombreCompleto,u.DebeCambiarClave,COALESCE((SELECT CAST(Valor AS UNSIGNED) FROM ParametrosSistema WHERE Codigo='INACTIVIDAD_MINUTOS'),20) AS MinutosInactividad,r.Codigo FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Id=@p0 AND u.Activo=1 AND r.Activo=1", id)
        If t.Rows.Count = 0 Then Throw New UnauthorizedAccessException("Inicie sesión con un usuario activo y un rol asignado.")
        Return New UsuarioFinanzas With {.Id = id, .Nombre = CStr(t.Rows(0)("NombreCompleto")), .Roles = t.AsEnumerable().Select(Function(r) CStr(r("Codigo"))).ToArray(), .DebeCambiarClave = CBool(t.Rows(0)("DebeCambiarClave")), .MinutosInactividad = Math.Max(1,CInt(t.Rows(0)("MinutosInactividad")))}
    End Function
    Private Shared Sub Autorizar(cn As MySqlConnection, tx As MySqlTransaction, id As Integer, ParamArray roles As String())
        If Not Usuario(cn, tx, id).Puede(roles) Then Throw New UnauthorizedAccessException("Su rol no permite realizar esta operación.")
    End Sub
    Public Function ObtenerUsuario(id As Integer) As UsuarioFinanzas
        Using cn = Abrir()
            Return Usuario(cn, Nothing, id)
        End Using
    End Function

    Public Function RolesAdministracion(usuarioId As Integer) As DataTable
        Using cn = Abrir()
            Autorizar(cn, Nothing, usuarioId, "ADMINISTRADOR")
            Return Tabla(cn, Nothing, "SELECT Id,Codigo,Nombre,EsSistema,Activo FROM Roles WHERE Activo=1 ORDER BY EsSistema DESC,Nombre")
        End Using
    End Function

    Public Function CatalogoAdministracion(usuarioId As Integer,codigo As String) As DataTable
        Using cn=Abrir()
            Autorizar(cn,Nothing,usuarioId,"ADMINISTRADOR")
            Select Case If(codigo,"").ToUpperInvariant()
                Case "ESTADOS" : Return Tabla(cn,Nothing,"SELECT Codigo,Nombre,Activo,EsSistema FROM Estados ORDER BY Nombre")
                Case "ESTADOS_CIVILES" : Return Tabla(cn,Nothing,"SELECT Codigo,Nombre,Activo FROM EstadosCiviles ORDER BY Nombre")
                Case "TIPOS_TASA" : Return Tabla(cn,Nothing,"SELECT Codigo,Nombre,Activo FROM TiposTasa ORDER BY Nombre")
                Case "FORMAS_PAGO" : Return Tabla(cn,Nothing,"SELECT Codigo,Nombre,Activo FROM FormasPago ORDER BY Nombre")
                Case "PERIODICIDADES" : Return Tabla(cn,Nothing,"SELECT Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio,Activo FROM Periodicidades ORDER BY Nombre")
                Case "PROVINCIAS" : Return Tabla(cn,Nothing,"SELECT codigo_provincia AS Codigo,nombre_provincia AS Nombre FROM provincia ORDER BY codigo_provincia")
                Case "DISTRITOS" : Return Tabla(cn,Nothing,"SELECT codigo_distrito AS Codigo,nombre_distrito AS Nombre,codigo_provincia AS Provincia FROM distrito ORDER BY codigo_provincia,codigo_distrito")
                Case "CORREGIMIENTOS" : Return Tabla(cn,Nothing,"SELECT codigo_corregimiento AS Codigo,nombre_corregimiento AS Nombre,codigo_distrito AS Distrito FROM corregimiento ORDER BY codigo_distrito,codigo_corregimiento")
                Case Else : Throw New ArgumentException("Seleccione un catálogo disponible.")
            End Select
        End Using
    End Function

    Public Sub GuardarEstadosCatalogoAdministracion(usuarioId As Integer,codigo As String,estados As IDictionary(Of String,Boolean))
        If estados Is Nothing OrElse estados.Count=0 Then Throw New ArgumentException("No se recibieron estados para guardar.")
        Dim nombreTabla As String
        Select Case If(codigo,"").ToUpperInvariant()
            Case "ESTADOS" : nombreTabla="Estados"
            Case "ESTADOS_CIVILES" : nombreTabla="EstadosCiviles"
            Case "TIPOS_TASA" : nombreTabla="TiposTasa"
            Case "FORMAS_PAGO" : nombreTabla="FormasPago"
            Case "PERIODICIDADES" : nombreTabla="Periodicidades"
            Case Else : Throw New ArgumentException("Este catálogo no permite editar estados.")
        End Select
        Transaccion(Function(cn,tx)
            Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
            Dim actuales=Tabla(cn,tx,"SELECT Codigo,Activo,Nombre FROM " & nombreTabla & " ORDER BY Codigo FOR UPDATE")
            If actuales.Rows.Count<>estados.Count Then Throw New ArgumentException("El catálogo cambió. Actualice la pantalla e intente de nuevo.")
            If estados.Values.All(Function(estado) Not estado) Then Throw New ArgumentException("Debe quedar al menos un valor activo en el catálogo.")
            Dim encontrados As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim cambios As New List(Of String)()
            For Each fila As DataRow In actuales.Rows
                Dim clave=CStr(fila("Codigo")), nuevo As Boolean
                If Not estados.TryGetValue(clave,nuevo) Then Throw New ArgumentException("El catálogo cambió. Actualice la pantalla e intente de nuevo.")
                encontrados.Add(clave)
                If CBool(fila("Activo"))<>nuevo Then
                    Escribir(cn,tx,"UPDATE " & nombreTabla & " SET Activo=@p0 WHERE Codigo=@p1",nuevo,clave)
                    cambios.Add(clave & "=" & If(nuevo,"Activo","Inactivo"))
                End If
            Next
            If encontrados.Count<>estados.Count Then Throw New ArgumentException("El catálogo cambió. Actualice la pantalla e intente de nuevo.")
            If cambios.Count>0 Then Auditar(cn,tx,usuarioId,"CATALOGO_ESTADOS_ACTUALIZADOS",nombreTabla,0,String.Join("; ",cambios))
            Return 0
        End Function)
    End Sub

    Public Function MetodosAdministracion(usuarioId As Integer) As DataTable
        Using cn=Abrir()
            Autorizar(cn,Nothing,usuarioId,"ADMINISTRADOR")
            Return Tabla(cn,Nothing,"SELECT Id,Codigo,Nombre,Descripcion,ClaseImplementacion,Formula,Variables,Algoritmo,EjemploManual,Activo,(SELECT COUNT(*) FROM Prestamos p WHERE p.MetodoId=MetodosCalculo.Id) AS PrestamosAsociados FROM MetodosCalculo ORDER BY Id")
        End Using
    End Function

    Public Sub GuardarMetodoAdministracion(usuarioId As Integer,id As Integer,nombre As String,descripcion As String,formula As String,variables As String,algoritmo As String,ejemplo As String)
        If id<=0 Then Throw New ArgumentException("Seleccione un método de cálculo.")
        Transaccion(Function(cn,tx)
                        Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
                        Dim actual=Tabla(cn,tx,"SELECT Codigo,Nombre FROM MetodosCalculo WHERE Id=@p0 FOR UPDATE",id)
                        If actual.Rows.Count<>1 Then Throw New ArgumentException("El método ya no está disponible.")
                        Escribir(cn,tx,"UPDATE MetodosCalculo SET Nombre=@p0,Descripcion=@p1,Formula=@p2,Variables=@p3,Algoritmo=@p4,EjemploManual=@p5 WHERE Id=@p6",nombre,descripcion,formula,variables,algoritmo,ejemplo,id)
                        Auditar(cn,tx,usuarioId,"METODO_ACTUALIZADO","MetodosCalculo",id,"Documentación del método " & CStr(actual.Rows(0)("Codigo")) & " actualizada")
                        Return 0
                    End Function)
    End Sub

    Public Sub CambiarEstadoMetodoAdministracion(usuarioId As Integer,id As Integer,activo As Boolean)
        If id<=0 Then Throw New ArgumentException("Seleccione un método de cálculo.")
        Transaccion(Function(cn,tx)
                        Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
                        Dim t=Tabla(cn,tx,"SELECT Activo,Nombre FROM MetodosCalculo WHERE Id=@p0 FOR UPDATE",id)
                        If t.Rows.Count<>1 Then Throw New ArgumentException("El método ya no está disponible.")
                        If Not activo AndAlso CBool(t.Rows(0)("Activo")) AndAlso CInt(Tabla(cn,tx,"SELECT COUNT(*) Total FROM MetodosCalculo WHERE Activo=1 AND Id<>@p0",id).Rows(0)("Total"))=0 Then Throw New InvalidOperationException("Debe quedar al menos un método de cálculo activo.")
                        Escribir(cn,tx,"UPDATE MetodosCalculo SET Activo=@p0 WHERE Id=@p1",activo,id)
                        Auditar(cn,tx,usuarioId,If(activo,"METODO_ACTIVADO","METODO_DESACTIVADO"),"MetodosCalculo",id,"Método " & CStr(t.Rows(0)("Nombre")) & If(activo," activado"," desactivado"))
                        Return 0
                    End Function)
    End Sub

    Public Function ParametrosAdministracion(usuarioId As Integer) As DataTable
        Using cn=Abrir()
            Autorizar(cn,Nothing,usuarioId,"ADMINISTRADOR")
            Return Tabla(cn,Nothing,"SELECT Codigo,Valor,TipoDato,Descripcion,EsPropuesta,DATE_FORMAT(CONVERT_TZ(ModificadoUtc,'+00:00','-05:00'),'%d/%m/%Y %H:%i') AS Modificado FROM ParametrosSistema ORDER BY Codigo")
        End Using
    End Function

    Public Sub GuardarParametrosAdministracion(usuarioId As Integer,valores As IDictionary(Of String,String))
        Transaccion(Function(cn,tx)
                        Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
                        For Each par In valores
                            Dim actual=Tabla(cn,tx,"SELECT Valor FROM ParametrosSistema WHERE Codigo=@p0 FOR UPDATE",par.Key)
                            If actual.Rows.Count<>1 Then Throw New ArgumentException("El parámetro " & par.Key & " no existe en la base de datos.")
                            Dim anterior=CStr(actual.Rows(0)("Valor"))
                            If anterior<>par.Value Then
                                Escribir(cn,tx,"UPDATE ParametrosSistema SET Valor=@p0,ModificadoPor=@p1 WHERE Codigo=@p2",par.Value,usuarioId,par.Key)
                                Escribir(cn,tx,"INSERT INTO Auditoria(UsuarioId,RolCodigo,Modulo,Accion,Entidad,RegistroId,Detalle,Descripcion,ValorAnterior,ValorNuevo) SELECT @p0,(SELECT r.Codigo FROM UsuarioRoles ur JOIN Roles r ON r.Id=ur.RolId WHERE ur.UsuarioId=@p0 ORDER BY r.Id LIMIT 1),'SEGURIDAD','PARAMETRO_ACTUALIZADO','ParametrosSistema',0,@p1,@p2,@p3,@p4",usuarioId,"Cambio de parámetro del sistema",par.Key & " actualizado",anterior,par.Value)
                            End If
                        Next
                        Return 0
                    End Function)
    End Sub

    Private Function ParametroEntero(codigo As String,valorPredeterminado As Integer) As Integer
        Using cn=Abrir()
            Dim t=Tabla(cn,Nothing,"SELECT Valor FROM ParametrosSistema WHERE Codigo=@p0",codigo)
            Dim valor As Integer
            If t.Rows.Count=0 OrElse Not Integer.TryParse(Convert.ToString(t.Rows(0)("Valor")),Globalization.NumberStyles.Integer,Globalization.CultureInfo.InvariantCulture,valor) Then Return valorPredeterminado
            Return valor
        End Using
    End Function

    Public Function PoliticaClave() As DataTable
        Using cn=Abrir()
            Return Tabla(cn,Nothing,"SELECT Codigo,Valor FROM ParametrosSistema WHERE Codigo IN ('CLAVE_LONGITUD_MINIMA','CLAVE_MAYUSCULA','CLAVE_NUMERO')")
        End Using
    End Function

    Public Function ConsultarUsuariosAdministracion(usuarioId As Integer, busqueda As String, rolId As Integer, estado As String) As DataTable
        Using cn = Abrir()
            Autorizar(cn, Nothing, usuarioId, "ADMINISTRADOR")
            Return Tabla(cn, Nothing, "SELECT u.Id,u.NombreUsuario,u.NombreCompleto,u.Correo,u.Activo,u.UltimoAccesoUtc,DATE_FORMAT(CONVERT_TZ(u.UltimoAccesoUtc,'+00:00','-05:00'),'%d/%m/%Y %H:%i') AS UltimoAcceso,COALESCE(GROUP_CONCAT(DISTINCT r.Nombre ORDER BY r.Nombre SEPARATOR ', '),'Sin rol') AS Rol,COALESCE(GROUP_CONCAT(DISTINCT r.Id ORDER BY r.Id),'') AS RolIds FROM Usuarios u LEFT JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id LEFT JOIN Roles r ON r.Id=ur.RolId WHERE (@p0='' OR u.NombreCompleto LIKE CONCAT('%',@p0,'%') OR u.NombreUsuario LIKE CONCAT('%',@p0,'%') OR u.Correo LIKE CONCAT('%',@p0,'%')) AND (@p1=0 OR EXISTS(SELECT 1 FROM UsuarioRoles urf WHERE urf.UsuarioId=u.Id AND urf.RolId=@p1)) AND (@p2='TODOS' OR (@p2='ACTIVO' AND u.Activo=1) OR (@p2='INACTIVO' AND u.Activo=0)) GROUP BY u.Id,u.NombreUsuario,u.NombreCompleto,u.Correo,u.Activo,u.UltimoAccesoUtc ORDER BY u.Activo DESC,u.NombreCompleto,u.Id", If(busqueda, "").Trim(), rolId, estado)
        End Using
    End Function

    Public Function ResumenUsuariosAdministracion(usuarioId As Integer) As DataTable
        Using cn = Abrir()
            Autorizar(cn, Nothing, usuarioId, "ADMINISTRADOR")
            Return Tabla(cn, Nothing, "SELECT COUNT(*) AS Total,COALESCE(SUM(Activo=1),0) AS Activos,COALESCE(SUM(Activo=0),0) AS Inactivos,(SELECT COUNT(*) FROM Roles WHERE Activo=1) AS Roles FROM Usuarios")
        End Using
    End Function

    Public Sub GuardarUsuarioAdministracion(usuarioId As Integer, id As Integer, nombreUsuario As String, nombreCompleto As String, correo As String, rolId As Integer, activo As Boolean, hash As String)
        Transaccion(Function(cn, tx)
                        Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                        If rolId <= 0 OrElse Tabla(cn, tx, "SELECT Id FROM Roles WHERE Id=@p0 AND Activo=1", rolId).Rows.Count <> 1 Then Throw New ArgumentException("Seleccione un rol activo.")
                        If id = usuarioId AndAlso Not activo Then Throw New InvalidOperationException("No puedes desactivar tu propia cuenta.")
                        If id = 0 Then
                            Dim nuevoId = Insertar(cn, tx, "INSERT INTO Usuarios(NombreUsuario,NombreCompleto,Correo,ContrasenaHash,Activo,DebeCambiarClave) VALUES(@p0,@p1,@p2,@p3,@p4,1)", nombreUsuario, nombreCompleto, correo, hash, activo)
                            Escribir(cn, tx, "INSERT INTO UsuarioRoles(UsuarioId,RolId) VALUES(@p0,@p1)", nuevoId, rolId)
                            Auditar(cn, tx, usuarioId, "USUARIO_CREADO", "Usuarios", nuevoId, "Alta de " & nombreUsuario & " con rol " & rolId.ToString())
                            Return 0
                        End If
                        Dim actual = Tabla(cn, tx, "SELECT u.Activo,EXISTS(SELECT 1 FROM UsuarioRoles ur JOIN Roles r ON r.Id=ur.RolId WHERE ur.UsuarioId=u.Id AND r.Codigo='ADMINISTRADOR') AS EsAdmin FROM Usuarios u WHERE u.Id=@p0 FOR UPDATE", id)
                        If actual.Rows.Count <> 1 Then Throw New ArgumentException("El usuario ya no está disponible.")
                        Dim eraAdmin = CBool(actual.Rows(0)("EsAdmin")), estabaActivo = CBool(actual.Rows(0)("Activo"))
                        Dim seraAdmin = CStr(Tabla(cn, tx, "SELECT Codigo FROM Roles WHERE Id=@p0", rolId).Rows(0)("Codigo")) = "ADMINISTRADOR"
                        If estabaActivo AndAlso eraAdmin AndAlso (Not activo OrElse Not seraAdmin) Then
                            Dim restantes = CInt(Tabla(cn, tx, "SELECT COUNT(*) Total FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Activo=1 AND r.Codigo='ADMINISTRADOR' AND u.Id<>@p0", id).Rows(0)("Total"))
                            If restantes = 0 Then Throw New InvalidOperationException("Debe permanecer al menos un administrador activo.")
                        End If
                        Escribir(cn, tx, "UPDATE Usuarios SET NombreCompleto=@p0,Correo=@p1,Activo=@p2 WHERE Id=@p3", nombreCompleto, correo, activo, id)
                        Escribir(cn, tx, "DELETE FROM UsuarioRoles WHERE UsuarioId=@p0", id)
                        Escribir(cn, tx, "INSERT INTO UsuarioRoles(UsuarioId,RolId) VALUES(@p0,@p1)", id, rolId)
                        If Not String.IsNullOrEmpty(hash) Then Escribir(cn, tx, "UPDATE Usuarios SET ContrasenaHash=@p0,DebeCambiarClave=1,VersionSesion=VersionSesion+1 WHERE Id=@p1", hash, id)
                        Auditar(cn, tx, usuarioId, "USUARIO_ACTUALIZADO", "Usuarios", id, "Perfil, rol y estado actualizados para " & nombreUsuario)
                        Return 0
                    End Function)
    End Sub

    Public Sub CambiarEstadoUsuarioAdministracion(usuarioId As Integer, id As Integer, activo As Boolean)
        If id <= 0 Then Throw New ArgumentException("Seleccione un usuario.")
        Transaccion(Function(cn, tx)
                        Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                        If id = usuarioId AndAlso Not activo Then Throw New InvalidOperationException("No puedes desactivar tu propia cuenta.")
                        Dim t = Tabla(cn, tx, "SELECT u.Activo,EXISTS(SELECT 1 FROM UsuarioRoles ur JOIN Roles r ON r.Id=ur.RolId WHERE ur.UsuarioId=u.Id AND r.Codigo='ADMINISTRADOR') EsAdmin FROM Usuarios u WHERE u.Id=@p0 FOR UPDATE", id)
                        If t.Rows.Count <> 1 Then Throw New ArgumentException("El usuario ya no está disponible.")
                        If Not activo AndAlso CBool(t.Rows(0)("Activo")) AndAlso CBool(t.Rows(0)("EsAdmin")) Then
                            Dim restantes=CInt(Tabla(cn,tx,"SELECT COUNT(*) Total FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Activo=1 AND r.Codigo='ADMINISTRADOR' AND u.Id<>@p0",id).Rows(0)("Total"))
                            If restantes=0 Then Throw New InvalidOperationException("Debe permanecer al menos un administrador activo.")
                        End If
                        Escribir(cn,tx,"UPDATE Usuarios SET Activo=@p0,VersionSesion=VersionSesion+1 WHERE Id=@p1",activo,id)
                        Auditar(cn,tx,usuarioId,If(activo,"USUARIO_ACTIVADO","USUARIO_DESACTIVADO"),"Usuarios",id,"Cambio de acceso de cuenta")
                        Return 0
                    End Function)
    End Sub

    Public Sub RestablecerClaveUsuarioAdministracion(usuarioId As Integer, id As Integer, hash As String)
        Transaccion(Function(cn,tx)
                        Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
                        If Tabla(cn,tx,"SELECT Id FROM Usuarios WHERE Id=@p0 AND Activo=1 FOR UPDATE",id).Rows.Count<>1 Then Throw New ArgumentException("El usuario activo ya no está disponible.")
                        Escribir(cn,tx,"UPDATE Usuarios SET ContrasenaHash=@p0,DebeCambiarClave=1,VersionSesion=VersionSesion+1,IntentosFallidos=0,BloqueadoHastaUtc=NULL WHERE Id=@p1",hash,id)
                        Auditar(cn,tx,usuarioId,"CLAVE_RESTAURADA","Usuarios",id,"Restablecimiento de clave temporal; requiere cambio al ingresar")
                        Return 0
                    End Function)
    End Sub

    Public Sub RestablecerClaveManualUsuarioAdministracion(usuarioId As Integer,id As Integer,hash As String)
        Transaccion(Function(cn,tx)
                        Autorizar(cn,tx,usuarioId,"ADMINISTRADOR")
                        If Tabla(cn,tx,"SELECT Id FROM Usuarios WHERE Id=@p0 AND Activo=1 FOR UPDATE",id).Rows.Count<>1 Then Throw New ArgumentException("El usuario activo ya no está disponible.")
                        Escribir(cn,tx,"UPDATE Usuarios SET ContrasenaHash=@p0,DebeCambiarClave=1,VersionSesion=VersionSesion+1,IntentosFallidos=0,BloqueadoHastaUtc=NULL WHERE Id=@p1",hash,id)
                        Auditar(cn,tx,usuarioId,"CLAVE_RESTAURADA","Usuarios",id,"Asignación manual de clave; requiere cambio al ingresar")
                        Return 0
                    End Function)
    End Sub

    Public Sub CambiarClavePropia(usuarioId As Integer, hash As String)
        Transaccion(Function(cn,tx)
                        Dim t=Tabla(cn,tx,"SELECT Id FROM Usuarios WHERE Id=@p0 AND Activo=1 FOR UPDATE",usuarioId)
                        If t.Rows.Count<>1 Then Throw New UnauthorizedAccessException("La cuenta ya no está activa.")
                        Escribir(cn,tx,"UPDATE Usuarios SET ContrasenaHash=@p0,DebeCambiarClave=0,VersionSesion=VersionSesion+1,IntentosFallidos=0,BloqueadoHastaUtc=NULL WHERE Id=@p1",hash,usuarioId)
                        Auditar(cn,tx,usuarioId,"CLAVE_CAMBIADA","Usuarios",usuarioId,"Cambio de contraseña propia")
                        Return 0
                    End Function)
    End Sub

    Public Function ConsultarAuditoria(usuarioId As Integer, usuarioFiltro As Integer, modulo As String, accion As String, desde As Nullable(Of Date), hasta As Nullable(Of Date)) As DataTable
        Using cn=Abrir()
            Autorizar(cn,Nothing,usuarioId,"ADMINISTRADOR")
            Return Tabla(cn,Nothing,"SELECT a.Id,a.FechaUtc,DATE_FORMAT(CONVERT_TZ(a.FechaUtc,'+00:00','-05:00'),'%d/%m/%Y %H:%i:%s') AS Fecha,u.NombreCompleto AS Usuario,COALESCE(a.RolCodigo,'—') AS Rol,a.Modulo,a.Accion,a.Entidad,a.RegistroId,COALESCE(a.Descripcion,a.Detalle,'') AS Descripcion,COALESCE(a.ValorAnterior,'') AS ValorAnterior,COALESCE(a.ValorNuevo,'') AS ValorNuevo,a.DireccionIp FROM Auditoria a LEFT JOIN Usuarios u ON u.Id=a.UsuarioId WHERE (@p0=0 OR a.UsuarioId=@p0) AND (@p1='' OR a.Modulo=@p1) AND (@p2='' OR a.Accion=@p2) AND (@p3 IS NULL OR a.FechaUtc>=@p3) AND (@p4 IS NULL OR a.FechaUtc<@p4) ORDER BY a.FechaUtc DESC,a.Id DESC LIMIT 2000",usuarioFiltro,If(modulo,""),If(accion,""),If(desde.HasValue,CType(desde.Value.Date,Object),DBNull.Value),If(hasta.HasValue,CType(hasta.Value.Date.AddDays(1),Object),DBNull.Value))
        End Using
    End Function

    Public Function DetalleAuditoria(usuarioId As Integer,id As Long) As DataTable
        Using cn=Abrir()
            Autorizar(cn,Nothing,usuarioId,"ADMINISTRADOR")
            Return Tabla(cn,Nothing,"SELECT a.Id,DATE_FORMAT(CONVERT_TZ(a.FechaUtc,'+00:00','-05:00'),'%d/%m/%Y %H:%i:%s') AS Fecha,u.NombreCompleto AS Usuario,COALESCE(a.RolCodigo,'—') AS Rol,a.Modulo,a.Accion,a.Entidad,a.RegistroId,COALESCE(a.Descripcion,a.Detalle,'') AS Descripcion,COALESCE(a.ValorAnterior,'') AS ValorAnterior,COALESCE(a.ValorNuevo,'') AS ValorNuevo,a.DireccionIp FROM Auditoria a LEFT JOIN Usuarios u ON u.Id=a.UsuarioId WHERE a.Id=@p0 LIMIT 1",id)
        End Using
    End Function
    Private Shared Sub Auditar(cn As MySqlConnection, tx As MySqlTransaction, usuarioId As Integer, accion As String, entidad As String, id As Integer, detalle As String)
        Escribir(cn,tx,"INSERT INTO Auditoria(UsuarioId,RolCodigo,Modulo,Accion,Entidad,RegistroId,Detalle,Descripcion) SELECT @p0,(SELECT r.Codigo FROM UsuarioRoles ur JOIN Roles r ON r.Id=ur.RolId WHERE ur.UsuarioId=@p0 ORDER BY r.Id LIMIT 1),CASE WHEN @p2='Usuarios' THEN 'SEGURIDAD' WHEN @p2='Productos' OR @p2='Tasas' THEN 'PRODUCTOS' WHEN @p2='Clientes' THEN 'CLIENTES' WHEN @p2='Solicitudes' THEN 'SOLICITUDES' WHEN @p2='Prestamos' THEN 'PRÉSTAMOS' WHEN @p2='Pagos' THEN 'PAGOS' WHEN @p2='MetodosCalculo' THEN 'METODOS' ELSE 'FINANZAS' END,@p1,@p2,@p3,@p4,@p4",usuarioId,accion,entidad,id,detalle)
    End Sub
    Public Function HayUsuarios() As Boolean
        Using cn = Abrir()
            Return Tabla(cn, Nothing, "SELECT Id FROM Usuarios LIMIT 1").Rows.Count > 0
        End Using
    End Function
    Public Function Credencial(nombre As String) As DataTable
        Using cn = Abrir()
            Dim sql = If(nombre.Contains("@"), "SELECT Id,ContrasenaHash,Activo,IntentosFallidos,BloqueadoHastaUtc FROM Usuarios WHERE Correo=@p0 LIMIT 1", "SELECT Id,ContrasenaHash,Activo,IntentosFallidos,BloqueadoHastaUtc FROM Usuarios WHERE NombreUsuario=@p0 LIMIT 1")
            Return Tabla(cn, Nothing, sql, nombre)
        End Using
    End Function
    Public Sub ResultadoLogin(id As Integer, correcto As Boolean)
        Dim maxIntentos=ParametroEntero("INTENTOS_MAXIMOS",5)
        Dim minutosBloqueo=ParametroEntero("BLOQUEO_MINUTOS",15)
        Using cn = Abrir(), tx = cn.BeginTransaction()
            Dim actualizar = If(correcto,
                "UPDATE Usuarios SET IntentosFallidos=0,BloqueadoHastaUtc=NULL,UltimoAccesoUtc=UTC_TIMESTAMP() WHERE Id=@p0",
                "UPDATE Usuarios SET IntentosFallidos=IF(BloqueadoHastaUtc IS NOT NULL AND BloqueadoHastaUtc<=UTC_TIMESTAMP(),1,IntentosFallidos+1),BloqueadoHastaUtc=IF(BloqueadoHastaUtc IS NOT NULL AND BloqueadoHastaUtc<=UTC_TIMESTAMP(),NULL,IF(IntentosFallidos>=@p1,DATE_ADD(UTC_TIMESTAMP(),INTERVAL @p2 MINUTE),BloqueadoHastaUtc)) WHERE Id=@p0")
            If correcto Then
                Escribir(cn,tx,actualizar,id)
            Else
                Escribir(cn,tx,actualizar,id,maxIntentos,minutosBloqueo)
            End If
            Dim estado = Tabla(cn, tx, "SELECT IntentosFallidos,BloqueadoHastaUtc FROM Usuarios WHERE Id=@p0", id)
            Dim bloqueado = estado.Rows.Count = 1 AndAlso Not IsDBNull(estado.Rows(0)("BloqueadoHastaUtc")) AndAlso CDate(estado.Rows(0)("BloqueadoHastaUtc")) > Date.UtcNow
            Dim accion = If(correcto, "INICIO_SESION", If(bloqueado, "BLOQUEO_TEMPORAL", "INTENTO_FALLIDO"))
            Dim detalle = If(correcto, "Acceso concedido", If(bloqueado, "Se alcanzó el límite de intentos fallidos", "Credenciales no válidas"))
            Auditar(cn, tx, id, accion, "Usuarios", id, detalle)
            tx.Commit()
        End Using
    End Sub
    Public Sub RegistrarAccesoBloqueado(id As Integer)
        Using cn = Abrir()
            Auditar(cn, Nothing, id, "ACCESO_BLOQUEADO", "Usuarios", id, "Intento de acceso durante el bloqueo temporal")
        End Using
    End Sub
    Public Function CrearAdministrador(nombre As String, completo As String, correo As String, hash As String) As Integer
        Using cn = Abrir()
            Using bloqueo = Comando(cn, Nothing, "SELECT GET_LOCK('capitalwest_primer_administrador',10)")
                If Convert.ToInt32(bloqueo.ExecuteScalar()) <> 1 Then Throw New InvalidOperationException("Otra inicialización está en curso.")
            End Using
            Try
                Using tx = cn.BeginTransaction()
                    If Tabla(cn, tx, "SELECT Id FROM Usuarios LIMIT 1 FOR UPDATE").Rows.Count > 0 Then Throw New InvalidOperationException("Ya existe un usuario. Inicie sesión.")
                    Dim id = Insertar(cn, tx, "INSERT INTO Usuarios(NombreUsuario,NombreCompleto,Correo,ContrasenaHash) VALUES(@p0,@p1,@p2,@p3)", nombre, completo, correo, hash)
                    Escribir(cn, tx, "INSERT INTO UsuarioRoles(UsuarioId,RolId) SELECT @p0,Id FROM Roles WHERE Codigo='ADMINISTRADOR'", id)
                    Auditar(cn, tx, id, "CREAR", "Usuarios", id, "Alta local del primer administrador")
                    tx.Commit()
                    Return id
                End Using
            Finally
                Using liberar = Comando(cn, Nothing, "SELECT RELEASE_LOCK('capitalwest_primer_administrador')")
                    liberar.ExecuteScalar()
                End Using
            End Try
        End Using
    End Function
    Public Function Consultar(tipo As String, usuarioId As Integer, Optional id As Integer = 0) As DataTable
        Using cn = Abrir()
            Usuario(cn, Nothing, usuarioId)
            Select Case tipo
                Case "REPORTES_CLIENTES" : Return Tabla(cn, Nothing, "SELECT Id,CONCAT(Identificacion,' · ',NombreCompleto) Nombre FROM v_Clientes ORDER BY NombreCompleto")
                Case "REPORTES_PRODUCTOS" : Return Tabla(cn, Nothing, "SELECT Id,Nombre FROM ProductosFinancieros ORDER BY Nombre")
                Case "PRODUCTOS"
                    Return Tabla(cn, Nothing, "SELECT p.*,t.Porcentaje TasaAnual,t.TipoTasa,t.CapitalizacionId FROM ProductosFinancieros p LEFT JOIN Tasas t ON t.Id=(SELECT t2.Id FROM Tasas t2 WHERE t2.ProductoId=p.Id AND t2.Activo=1 AND t2.EsPredeterminada=1 ORDER BY t2.Id DESC LIMIT 1) WHERE (@p0=0 OR p.Id=@p0) ORDER BY p.Nombre", id)
                Case "TASAS"
                    Return Tabla(cn, Nothing, "SELECT t.Id,t.ProductoId,p.Nombre Producto,t.Porcentaje,t.TipoTasa,t.VigenteDesde,t.VigenteHasta,t.Activo,t.EsPredeterminada,t.CapitalizacionId FROM Tasas t INNER JOIN ProductosFinancieros p ON p.Id=t.ProductoId WHERE NOT EXISTS (SELECT 1 FROM Tasas td WHERE td.ProductoId=t.ProductoId AND td.Porcentaje=t.Porcentaje AND td.TipoTasa=t.TipoTasa AND td.CapitalizacionId=t.CapitalizacionId AND td.VigenteDesde=t.VigenteDesde AND (td.EsPredeterminada>t.EsPredeterminada OR (td.EsPredeterminada=t.EsPredeterminada AND td.Activo>t.Activo) OR (td.EsPredeterminada=t.EsPredeterminada AND td.Activo=t.Activo AND td.Id>t.Id))) ORDER BY p.Nombre,t.VigenteDesde DESC,t.Id DESC", id)
                Case "PRODUCTO_SIMULADOR"
                    Return Tabla(cn, Nothing, "SELECT pr.*,t.Porcentaje TasaAnual,t.TipoTasa,t.CapitalizacionId FROM ProductosFinancieros pr LEFT JOIN Tasas t ON t.Id=(SELECT t2.Id FROM Tasas t2 JOIN TiposTasa tt2 ON tt2.Codigo=t2.TipoTasa AND tt2.Activo=1 WHERE t2.ProductoId=pr.Id AND t2.Activo=1 AND t2.EsPredeterminada=1 AND t2.VigenteDesde<=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR) AND (t2.VigenteHasta IS NULL OR t2.VigenteHasta>=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) ORDER BY t2.Id DESC LIMIT 1) WHERE pr.Id=@p0 AND pr.Activo=1", id)
                Case "METODOS" : Return Tabla(cn, Nothing, "SELECT Id,Nombre,Codigo FROM MetodosCalculo WHERE (@p0=0 AND Activo=1) OR Id=@p0 ORDER BY Id", id)
                Case "TIPOS_TASA_ACTIVOS" : Return Tabla(cn, Nothing, "SELECT Codigo,Nombre FROM TiposTasa WHERE Activo=1 ORDER BY Nombre")
                Case "PERIODICIDADES_PAGO" : Return Tabla(cn, Nothing, "SELECT p.* FROM Periodicidades p WHERE p.Activo=1 AND p.Unidad='MESES' AND p.Intervalo=1 AND p.PeriodosPorAnio=12 ORDER BY p.Id")
                Case "PERIODICIDADES" : Return Tabla(cn, Nothing, "SELECT * FROM Periodicidades WHERE (@p0=0 AND Activo=1) OR Id=@p0 ORDER BY Id", id)
                Case "CLIENTES" : Return Tabla(cn, Nothing, "SELECT Id,CONCAT(Identificacion,' · ',Nombres,' ',Apellidos) Nombre FROM Clientes WHERE Activo=1 ORDER BY Nombres")
                Case "PRODUCTOS_ACTIVOS" : Return Tabla(cn, Nothing, "SELECT Id,Nombre FROM ProductosFinancieros WHERE Activo=1 ORDER BY Nombre")
                Case "PRODUCTOS_OPERABLES" : Return Tabla(cn, Nothing, "SELECT pr.Id,pr.Nombre FROM ProductosFinancieros pr WHERE pr.Activo=1 AND EXISTS (SELECT 1 FROM Tasas t JOIN TiposTasa tt ON tt.Codigo=t.TipoTasa AND tt.Activo=1 WHERE t.ProductoId=pr.Id AND t.Activo=1 AND t.EsPredeterminada=1 AND t.VigenteDesde<=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR) AND (t.VigenteHasta IS NULL OR t.VigenteHasta>=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR))) ORDER BY pr.Nombre")
                Case "PRODUCTO_METODOS" : Return Tabla(cn, Nothing, "SELECT m.Id,m.Nombre FROM ProductoMetodos pm JOIN MetodosCalculo m ON m.Id=pm.MetodoId WHERE pm.ProductoId=@p0 AND m.Activo=1", id)
                Case "PRODUCTO_PERIODICIDADES" : Return Tabla(cn, Nothing, "SELECT p.Id,p.Nombre FROM ProductoPeriodicidades pp JOIN Periodicidades p ON p.Id=pp.PeriodicidadId WHERE pp.ProductoId=@p0 AND p.Activo=1 AND p.Unidad='MESES' AND p.Intervalo=1 AND p.PeriodosPorAnio=12", id)
                Case "SOLICITUDES"
                    Return Tabla(cn, Nothing, "SELECT s.*,CONCAT(c.Nombres,' ',c.Apellidos) Cliente,p.Nombre Producto,m.Nombre Metodo FROM Solicitudes s JOIN Clientes c ON c.Id=s.ClienteId JOIN ProductosFinancieros p ON p.Id=s.ProductoId JOIN MetodosCalculo m ON m.Id=s.MetodoId WHERE (@p0=0 OR s.Id=@p0) ORDER BY s.Id DESC", id)
                Case "PRESTAMOS"
                    Return Tabla(cn, Nothing, "SELECT p.*,CASE WHEN p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') AND p.CuotasVencidas>0 THEN 'EN_MORA' ELSE p.EstadoCodigo END EstadoActual FROM v_PrestamosResumen p WHERE (@p0=0 OR p.Id=@p0) ORDER BY p.Id DESC", id)
                Case "RESUMEN_CARTERA"
                    Return Tabla(cn, Nothing, "SELECT COALESCE(SUM(MontoAprobado),0) MontoDesembolsado,COUNT(*) PrestamosVigentes FROM Prestamos WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')")
                Case "CUOTAS_CLIENTE"
                    Return Tabla(cn, Nothing, "SELECT q.NumeroCuota,q.FechaVencimiento,q.CapitalPendiente,q.InteresPendiente,q.MoraPendiente,q.CargosPendientes,q.TotalPendiente,q.EstadoCuota,CONCAT('PRE-',LPAD(pr.Id,GREATEST(6,CHAR_LENGTH(pr.Id)),'0')) AS NumeroPrestamo FROM v_SaldosCuota q JOIN Prestamos pr ON pr.Id=q.PrestamoId WHERE pr.ClienteId=@p0 AND pr.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') AND q.TotalPendiente>0 ORDER BY q.FechaVencimiento,pr.Id,q.NumeroCuota", id)
                Case "CUOTAS" : Return Tabla(cn, Nothing, "SELECT * FROM v_SaldosCuota WHERE PrestamoId=@p0 ORDER BY NumeroCuota", id)
                Case "PAGOS"
                    Return AplicarFormatoFactura(Tabla(cn, Nothing, "SELECT p.*,CONCAT('PRE-',LPAD(pr.Id,GREATEST(6,CHAR_LENGTH(pr.Id)),'0')) AS NumeroPrestamo,CONCAT(c.Nombres,' ',c.Apellidos) Cliente,u.NombreCompleto Usuario FROM v_PagosDesglosados p JOIN Prestamos pr ON pr.Id=p.PrestamoId JOIN Clientes c ON c.Id=p.ClienteId JOIN Usuarios u ON u.Id=p.RegistradoPor WHERE (@p0=0 OR p.PrestamoId=@p0) ORDER BY p.Id DESC", id))
                Case "MOVIMIENTOS" : Return Tabla(cn, Nothing, "SELECT * FROM v_Reporte07EstadoCuenta WHERE PrestamoId=@p0 ORDER BY Fecha,Id", id)
                Case "FORMAS" : Return Tabla(cn, Nothing, "SELECT Codigo,Nombre FROM FormasPago WHERE Activo=1 ORDER BY Codigo")
                Case Else : Throw New ArgumentException("Consulta inválida.")
            End Select
        End Using
    End Function
    Private Shared Function AplicarFormatoFactura(pagos As DataTable) As DataTable
        If pagos Is Nothing OrElse Not pagos.Columns.Contains("NumeroRecibo") OrElse Not pagos.Columns.Contains("Id") Then Return pagos
        For Each fila As DataRow In pagos.Rows
            Dim numero = Convert.ToString(fila("NumeroRecibo"), Globalization.CultureInfo.InvariantCulture)
            If numero.StartsWith("REC-", StringComparison.OrdinalIgnoreCase) Then
                fila("NumeroRecibo") = "FAC-" & Convert.ToInt32(fila("Id"), Globalization.CultureInfo.InvariantCulture).ToString("D4", Globalization.CultureInfo.InvariantCulture)
            End If
        Next
        Return pagos
    End Function
    Public Function ObtenerResumenDashboard(usuarioId As Integer) As DataSet
        Using cn = Abrir()
            Usuario(cn, Nothing, usuarioId)
            Dim ds As New DataSet("ResumenDashboard")
            Dim fechas = "(SELECT DATE(UTC_TIMESTAMP() - INTERVAL 5 HOUR) Hoy, DATE_SUB(DATE(UTC_TIMESTAMP() - INTERVAL 5 HOUR), INTERVAL WEEKDAY(DATE(UTC_TIMESTAMP() - INTERVAL 5 HOUR)) DAY) InicioSemana) f"
            Dim indicadores = Tabla(cn, Nothing,
                "SELECT " &
                "COALESCE((SELECT SUM(MontoAprobado) FROM Prestamos WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) MontoDesembolsado," &
                "(SELECT COUNT(*) FROM Prestamos WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')) PrestamosVigentes," &
                "COALESCE((SELECT SUM(MontoPagado) FROM Pagos WHERE Estado='APLICADO' AND FechaPago>=f.InicioSemana AND FechaPago<DATE_ADD(f.Hoy,INTERVAL 1 DAY)),0) PagosSemana," &
                "COALESCE((SELECT SUM(MontoPagado) FROM Pagos WHERE Estado='APLICADO' AND FechaPago>=DATE_SUB(f.InicioSemana,INTERVAL 7 DAY) AND FechaPago<f.InicioSemana),0) PagosSemanaAnterior," &
                "(SELECT COUNT(*) FROM Pagos WHERE Estado='APLICADO' AND FechaPago>=f.InicioSemana AND FechaPago<DATE_ADD(f.Hoy,INTERVAL 1 DAY)) TransaccionesSemana," &
                "(SELECT COUNT(DISTINCT q.PrestamoId) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.EstadoCuota='VENCIDA' AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')) PrestamosEnMora," &
                "COALESCE((SELECT SUM(q.TotalPendiente) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.EstadoCuota='VENCIDA' AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) MontoVencido," &
                "(SELECT COUNT(*) FROM Solicitudes WHERE EstadoCodigo IN ('SOLICITUD','EN_EVALUACION')) SolicitudesPendientes," &
                "COALESCE((SELECT SUM(MontoSolicitado) FROM Solicitudes WHERE EstadoCodigo IN ('SOLICITUD','EN_EVALUACION')),0) MontoSolicitudesPendientes," &
                "COALESCE((SELECT SUM(SaldoCapital) FROM Prestamos WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) CapitalPendiente," &
                "COALESCE((SELECT SUM(q.InteresPendiente) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.FechaVencimiento<=f.Hoy AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) InteresExigible," &
                "COALESCE((SELECT SUM(i.Interes) FROM v_PagosDesglosados i WHERE i.Estado='APLICADO' AND i.FechaPago>=DATE_FORMAT(f.Hoy,'%Y-%m-01') AND i.FechaPago<DATE_ADD(f.Hoy,INTERVAL 1 DAY)),0) InteresesCobradosMes," &
                "(SELECT COUNT(*) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.FechaVencimiento>=f.Hoy AND q.FechaVencimiento<DATE_ADD(f.Hoy,INTERVAL 3 DAY) AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')) CuotasProximas72Horas," &
                "COALESCE((SELECT SUM(q.TotalPendiente) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.FechaVencimiento>=f.Hoy AND q.FechaVencimiento<DATE_ADD(f.Hoy,INTERVAL 3 DAY) AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) MontoCuotasProximas72Horas " &
                ",COALESCE((SELECT SUM(q.TotalPendiente) FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId WHERE q.FechaVencimiento>=f.Hoy AND q.FechaVencimiento<DATE_ADD(f.Hoy,INTERVAL 10 DAY) AND q.TotalPendiente>0 AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')),0) MontoCuotasProximas10Dias " &
                "FROM " & fechas)
            indicadores.TableName = "Indicadores" : ds.Tables.Add(indicadores)

            Dim pagosMeses = Tabla(cn, Nothing,
                "SELECT YEAR(FechaPago) Anio,MONTH(FechaPago) Mes,SUM(MontoPagado) Importe FROM Pagos " &
                "WHERE Estado='APLICADO' AND FechaPago>=DATE_SUB(DATE_FORMAT(DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR),'%Y-01-01'),INTERVAL 4 YEAR) " &
                "AND FechaPago<DATE_ADD(DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR),INTERVAL 1 DAY) GROUP BY YEAR(FechaPago),MONTH(FechaPago) ORDER BY Anio,Mes")
            pagosMeses.TableName = "PagosPorMes" : ds.Tables.Add(pagosMeses)

            Dim pagosRecientes = Tabla(cn, Nothing,
                "SELECT p.Id,p.NumeroRecibo,p.FechaPago,p.MontoPagado,CONCAT('PRE-',LPAD(pr.Id,GREATEST(6,CHAR_LENGTH(pr.Id)),'0')) AS NumeroPrestamo,c.NombreCompleto,prod.Nombre Producto " &
                "FROM Pagos p JOIN Prestamos pr ON pr.Id=p.PrestamoId JOIN v_Clientes c ON c.Id=p.ClienteId " &
                "JOIN ProductosFinancieros prod ON prod.Id=pr.ProductoId WHERE p.Estado='APLICADO' " &
                "ORDER BY p.FechaPago DESC,p.Id DESC LIMIT 5")
            pagosRecientes.TableName = "PagosRecientes" : ds.Tables.Add(pagosRecientes)

            Dim cuotasProximas = Tabla(cn, Nothing,
                "SELECT q.Id,q.NumeroCuota,q.FechaVencimiento,q.TotalPendiente,DATEDIFF(q.FechaVencimiento,DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) DiasRestantes," &
                "CONCAT('PRE-',LPAD(pr.Id,GREATEST(6,CHAR_LENGTH(pr.Id)),'0')) AS NumeroPrestamo,c.NombreCompleto,c.Identificacion FROM v_SaldosCuota q JOIN Prestamos pr ON pr.Id=q.PrestamoId " &
                "JOIN v_Clientes c ON c.Id=pr.ClienteId CROSS JOIN " & fechas & " " &
                "WHERE q.FechaVencimiento>=f.Hoy AND q.FechaVencimiento<DATE_ADD(f.Hoy,INTERVAL 10 DAY) AND q.TotalPendiente>0 " &
                "AND pr.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') ORDER BY q.FechaVencimiento,q.Id LIMIT 6")
            cuotasProximas.TableName = "CuotasProximas" : ds.Tables.Add(cuotasProximas)
            Return ds
        End Using
    End Function
    Public Function ObtenerPanelClientes(usuarioId As Integer) As DataSet
        Using cn = Abrir()
            Usuario(cn, Nothing, usuarioId)
            Dim ds As New DataSet("PanelClientes")
            Dim indicadores = Tabla(cn, Nothing,
                "SELECT COUNT(DISTINCT ClienteId) ClientesConPrestamo,COALESCE(SUM(SaldoCapital),0) CarteraActiva " &
                "FROM Prestamos WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')")
            indicadores.TableName = "Indicadores" : ds.Tables.Add(indicadores)
            Dim clientesConPrestamo = Tabla(cn, Nothing,
                "SELECT ClienteId,COUNT(*) PrestamosActivos FROM Prestamos " &
                "WHERE FechaDesembolso IS NOT NULL AND EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') " &
                "GROUP BY ClienteId")
            clientesConPrestamo.TableName = "ClientesConPrestamo" : ds.Tables.Add(clientesConPrestamo)
            Return ds
        End Using
    End Function
    Public Function ObtenerPerfilCliente(usuarioId As Integer, clienteId As Integer) As DataSet
        If clienteId <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(clienteId))
        Using cn = Abrir()
            Usuario(cn, Nothing, usuarioId)
            Dim ds As New DataSet("PerfilCliente")
            Dim indicadores = Tabla(cn, Nothing,
                "SELECT COUNT(CASE WHEN p.FechaDesembolso IS NOT NULL AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') THEN 1 END) PrestamosActivos," &
                "COALESCE(SUM(CASE WHEN p.FechaDesembolso IS NOT NULL AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') THEN p.SaldoCapital ELSE 0 END),0) SaldoPendiente," &
                "(SELECT COUNT(*) FROM v_SaldosCuota q JOIN Prestamos pm ON pm.Id=q.PrestamoId WHERE pm.ClienteId=@p0 AND q.EstadoCuota='VENCIDA' AND q.TotalPendiente>0 AND pm.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')) CuotasEnMora," &
                "COALESCE((SELECT SUM(pg.MontoPagado) FROM Pagos pg WHERE pg.ClienteId=@p0 AND pg.Estado='APLICADO'),0) HistoricoPagado " &
                "FROM Prestamos p WHERE p.ClienteId=@p0", clienteId)
            indicadores.TableName = "Indicadores" : ds.Tables.Add(indicadores)
            Dim prestamos = Tabla(cn, Nothing,
                "SELECT p.Id,CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS NumeroPrestamo,prod.Nombre Producto,p.MontoAprobado,p.SaldoCapital,m.Nombre Metodo,per.Nombre Periodicidad," &
                "p.CantidadCuotas,p.PlazoMeses,CASE WHEN p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') AND p.CuotasVencidas>0 THEN 'EN_MORA' ELSE p.EstadoCodigo END EstadoActual " &
                "FROM v_PrestamosResumen p JOIN ProductosFinancieros prod ON prod.Id=p.ProductoId " &
                "JOIN MetodosCalculo m ON m.Id=p.MetodoId JOIN Periodicidades per ON per.Id=p.PeriodicidadId " &
                "WHERE p.ClienteId=@p0 ORDER BY p.Id DESC", clienteId)
            prestamos.TableName = "Prestamos" : ds.Tables.Add(prestamos)
            Dim pagos = Tabla(cn, Nothing,
                "SELECT pg.FechaPago,pg.NumeroRecibo,pg.TipoPago,COALESCE(fp.Nombre,pg.FormaPagoCodigo) Canal,pg.MontoPagado,pg.Estado " &
                "FROM Pagos pg LEFT JOIN FormasPago fp ON fp.Codigo=pg.FormaPagoCodigo " &
                "WHERE pg.ClienteId=@p0 ORDER BY pg.FechaPago DESC,pg.Id DESC LIMIT 25", clienteId)
            pagos.TableName = "Pagos" : ds.Tables.Add(pagos)
            Dim referencias = Tabla(cn, Nothing,
                "SELECT Nombre,Parentesco,Telefono FROM ReferenciasCliente WHERE ClienteId=@p0 ORDER BY Nombre,Id", clienteId)
            referencias.TableName = "Referencias" : ds.Tables.Add(referencias)
            Return ds
        End Using
    End Function
    Public Function CambiarEstadoProducto(id As Integer, activo As Boolean, usuarioId As Integer) As Integer
        Return Transaccion(Function(cn, tx)
            Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
            If Tabla(cn, tx, "SELECT Id FROM ProductosFinancieros WHERE Id=@p0 FOR UPDATE", id).Rows.Count = 0 Then Throw New ArgumentException("Producto inexistente.")
            Escribir(cn, tx, "UPDATE ProductosFinancieros SET Activo=@p1 WHERE Id=@p0", id, activo)
            Auditar(cn, tx, usuarioId, "CAMBIAR_ESTADO", "ProductosFinancieros", id, If(activo, "Activo", "Inactivo"))
            Return id
        End Function)
    End Function
    Public Function EliminarProducto(id As Integer, usuarioId As Integer) As Integer
        Try
            Return Transaccion(Function(cn, tx)
                Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                If Tabla(cn, tx, "SELECT Id FROM ProductosFinancieros WHERE Id=@p0 FOR UPDATE", id).Rows.Count = 0 Then Throw New ArgumentException("Producto inexistente.")
                If Tabla(cn, tx, "SELECT Id FROM Solicitudes WHERE ProductoId=@p0 LIMIT 1", id).Rows.Count > 0 OrElse Tabla(cn, tx, "SELECT Id FROM Prestamos WHERE ProductoId=@p0 LIMIT 1", id).Rows.Count > 0 Then Throw New ArgumentException("Este producto tiene solicitudes o préstamos. Puede desactivarlo, pero no eliminarlo.")
                Escribir(cn, tx, "DELETE FROM ProductoMetodos WHERE ProductoId=@p0", id)
                Escribir(cn, tx, "DELETE FROM ProductoPeriodicidades WHERE ProductoId=@p0", id)
                Escribir(cn, tx, "DELETE FROM Tasas WHERE ProductoId=@p0", id)
                Escribir(cn, tx, "DELETE FROM ProductosFinancieros WHERE Id=@p0", id)
                Auditar(cn, tx, usuarioId, "ELIMINAR", "ProductosFinancieros", id, "Producto sin operaciones eliminado")
                Return id
            End Function)
        Catch ex As MySqlException When ex.Number = 1451
            Throw New ArgumentException("El producto tiene información relacionada y no puede eliminarse. Puede desactivarlo.")
        End Try
    End Function
    Public Shared Function CalcularComision(tipo As String, valor As Decimal, monto As Decimal) As Decimal
        If valor < 0 OrElse monto < 0 Then Throw New ArgumentException("Comisión o monto inválidos.")
        If tipo = "PORCENTAJE" Then
            If valor > 100 Then Throw New ArgumentException("La comisión porcentual no puede superar 100 %.")
            Return Decimal.Round(monto * valor / 100D, 2, MidpointRounding.AwayFromZero)
        End If
        If tipo <> "FIJO" Then Throw New ArgumentException("Tipo de comisión inválido.")
        Return Decimal.Round(valor, 2, MidpointRounding.AwayFromZero)
    End Function
    Public Function GuardarProducto(p As ProductoFinanciero, usuarioId As Integer) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                               If Tabla(cn, tx, "SELECT Codigo FROM TiposTasa WHERE Codigo=@p0 AND Activo=1", p.TipoTasa).Rows.Count <> 1 Then Throw New ArgumentException("El tipo de tasa ya no está disponible. Seleccione una opción activa.")
                               If p.Periodicidades.Count <> 1 Then Throw New ArgumentException("La periodicidad de pago debe ser únicamente mensual.")
                               If Tabla(cn, tx, "SELECT Id FROM Periodicidades WHERE Id=@p0 AND Activo=1 AND Unidad='MESES' AND Intervalo=1 AND PeriodosPorAnio=12", p.Periodicidades(0)).Rows.Count <> 1 Then Throw New ArgumentException("Solo se permiten pagos mensuales.")
                               If Tabla(cn, tx, "SELECT Id FROM Periodicidades WHERE Id=@p0 AND Activo=1 AND Unidad='MESES' AND Intervalo=1 AND PeriodosPorAnio=12", p.CapitalizacionId).Rows.Count <> 1 Then Throw New ArgumentException("La capitalización debe ser mensual.")
                               Dim id = p.Id
                               If id = 0 Then
                                   id = Insertar(cn, tx, "INSERT INTO ProductosFinancieros(Codigo,Nombre,Descripcion,MontoMinimo,MontoMaximo,PlazoMinimo,PlazoMaximo,PlazoMinimoMeses,PlazoMaximoMeses,ValorComision,ValorGastos,Activo) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11)", p.Codigo, p.Nombre, p.Descripcion, p.MontoMinimo, p.MontoMaximo, p.CuotasMinimas, p.CuotasMaximas, p.MesesMinimos, p.MesesMaximos, p.Comision, p.Gastos, p.Activo)
                               Else
                                   If Tabla(cn, tx, "SELECT Id FROM ProductosFinancieros WHERE Id=@p0 FOR UPDATE", id).Rows.Count = 0 Then Throw New ArgumentException("Producto inexistente.")
                                   Escribir(cn, tx, "UPDATE ProductosFinancieros SET Codigo=@p1,Nombre=@p2,Descripcion=@p3,MontoMinimo=@p4,MontoMaximo=@p5,PlazoMinimo=@p6,PlazoMaximo=@p7,PlazoMinimoMeses=@p8,PlazoMaximoMeses=@p9,TipoComision='FIJO',TipoGastos='FIJO',ValorComision=@p10,ValorGastos=@p11,Activo=@p12 WHERE Id=@p0", id, p.Codigo, p.Nombre, p.Descripcion, p.MontoMinimo, p.MontoMaximo, p.CuotasMinimas, p.CuotasMaximas, p.MesesMinimos, p.MesesMaximos, p.Comision, p.Gastos, p.Activo)
                               End If
                               CalcularComision(p.TipoComision, p.Comision, p.MontoMinimo)
                               Escribir(cn, tx, "UPDATE ProductosFinancieros SET TipoComision=@p1 WHERE Id=@p0", id, p.TipoComision)
                               For Each existente As DataRow In Tabla(cn, tx, "SELECT MetodoId FROM ProductoMetodos WHERE ProductoId=@p0", id).Rows
                                   Dim metodoId = CInt(existente("MetodoId"))
                                   If Not p.Metodos.Contains(metodoId) Then
                                       If Tabla(cn, tx, "SELECT Id FROM Solicitudes WHERE ProductoId=@p0 AND MetodoId=@p1 LIMIT 1", id, metodoId).Rows.Count > 0 Then Throw New ArgumentException("No puede retirar un método utilizado por una solicitud o contrato. Cree otro producto para condiciones diferentes.")
                                       Escribir(cn, tx, "DELETE FROM ProductoMetodos WHERE ProductoId=@p0 AND MetodoId=@p1", id, metodoId)
                                   End If
                               Next
                               For Each existente As DataRow In Tabla(cn, tx, "SELECT PeriodicidadId FROM ProductoPeriodicidades WHERE ProductoId=@p0", id).Rows
                                   Dim periodoId = CInt(existente("PeriodicidadId"))
                                   If Not p.Periodicidades.Contains(periodoId) Then
                                       If Tabla(cn, tx, "SELECT Id FROM Solicitudes WHERE ProductoId=@p0 AND PeriodicidadId=@p1 LIMIT 1", id, periodoId).Rows.Count > 0 Then Throw New ArgumentException("No puede retirar una periodicidad utilizada por solicitudes o contratos.")
                                       Escribir(cn, tx, "DELETE FROM ProductoPeriodicidades WHERE ProductoId=@p0 AND PeriodicidadId=@p1", id, periodoId)
                                   End If
                               Next
                               ' No borrar asociaciones usadas por contratos: se conserva la integridad histórica.
                               For Each metodo In p.Metodos.Distinct()
                                   If Tabla(cn, tx, "SELECT Id FROM MetodosCalculo WHERE Id=@p0 AND Activo=1", metodo).Rows.Count = 0 Then Throw New ArgumentException("Método inválido.")
                                   Escribir(cn, tx, "INSERT IGNORE INTO ProductoMetodos VALUES(@p0,@p1)", id, metodo)
                               Next
                               For Each periodo In p.Periodicidades.Distinct()
                                   If Tabla(cn, tx, "SELECT Id FROM Periodicidades WHERE Id=@p0 AND Activo=1", periodo).Rows.Count = 0 Then Throw New ArgumentException("Periodicidad inválida.")
                                   Escribir(cn, tx, "INSERT IGNORE INTO ProductoPeriodicidades VALUES(@p0,@p1)", id, periodo)
                               Next
                               Dim tasasVigentes = Tabla(cn, tx, "SELECT Id,Porcentaje,TipoTasa,CapitalizacionId FROM Tasas WHERE ProductoId=@p0 AND Activo=1 AND EsPredeterminada=1 ORDER BY Id DESC FOR UPDATE", id)
                               Dim crearNuevaTasa = tasasVigentes.Rows.Count = 0
                               Dim tasaVigenteId As Integer = 0
                               If tasasVigentes.Rows.Count > 0 Then
                                   Dim tasaVigente = tasasVigentes.Rows(0)
                                   tasaVigenteId = CInt(tasaVigente("Id"))
                                   crearNuevaTasa = CDec(tasaVigente("Porcentaje")) <> p.TasaAnual OrElse CStr(tasaVigente("TipoTasa")) <> p.TipoTasa OrElse CInt(tasaVigente("CapitalizacionId")) <> p.CapitalizacionId
                               End If
                               If crearNuevaTasa Then
                                   Escribir(cn, tx, "UPDATE Tasas SET Activo=0,EsPredeterminada=0,VigenteHasta=GREATEST(VigenteDesde,DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) WHERE ProductoId=@p0 AND EsPredeterminada=1", id)
                                   Insertar(cn, tx, "INSERT INTO Tasas(ProductoId,Porcentaje,TipoTasa,CapitalizacionId,VigenteDesde,EsPredeterminada,CreadoPor) VALUES(@p0,@p1,@p2,@p3,DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR),1,@p4)", id, p.TasaAnual, p.TipoTasa, p.CapitalizacionId, usuarioId)
                               ElseIf tasasVigentes.Rows.Count > 1 Then
                                   For Each tasaDuplicada As DataRow In tasasVigentes.Rows
                                       If CInt(tasaDuplicada("Id")) = tasaVigenteId Then Continue For
                                       Escribir(cn, tx, "UPDATE Tasas SET Activo=0,EsPredeterminada=0,VigenteHasta=GREATEST(VigenteDesde,DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) WHERE Id=@p0 AND ProductoId=@p1", CInt(tasaDuplicada("Id")), id)
                                   Next
                               End If
                               Auditar(cn, tx, usuarioId, "GUARDAR", "ProductosFinancieros", id, p.Codigo & " · " & p.Nombre)
                               Return id
                           End Function)
    End Function
    Public Function GuardarTasa(productoId As Integer, porcentaje As Decimal, tipoTasa As String, fechaDesde As Date, usuarioId As Integer) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                               If Tabla(cn, tx, "SELECT Codigo FROM TiposTasa WHERE Codigo=@p0 AND Activo=1", tipoTasa).Rows.Count <> 1 Then Throw New ArgumentException("El tipo de tasa ya no está disponible. Seleccione una opción activa.")
                               If Tabla(cn, tx, "SELECT Id FROM ProductosFinancieros WHERE Id=@p0 FOR UPDATE", productoId).Rows.Count = 0 Then Throw New ArgumentException("Producto inexistente.")
                               Dim actual = Tabla(cn, tx, "SELECT Id,CapitalizacionId,Porcentaje,TipoTasa,VigenteDesde,VigenteHasta,Activo FROM Tasas WHERE ProductoId=@p0 AND EsPredeterminada=1 ORDER BY Id DESC FOR UPDATE", productoId)
                               If actual.Rows.Count = 0 Then Throw New ArgumentException("El producto no tiene una tasa base para heredar la capitalización. Edite el producto primero.")
                               Dim tasaActual = actual.Rows(0)
                               Dim tasaActualId = CInt(tasaActual("Id"))
                               Dim capitalizacionId = CInt(tasaActual("CapitalizacionId"))
                               If actual.Rows.Count > 1 Then
                                   For i = 1 To actual.Rows.Count - 1
                                       Escribir(cn, tx, "UPDATE Tasas SET Activo=0,EsPredeterminada=0,VigenteHasta=GREATEST(VigenteDesde,@p1) WHERE Id=@p0 AND ProductoId=@p2", CInt(actual.Rows(i)("Id")), fechaDesde, productoId)
                                   Next
                               End If
                               If CBool(tasaActual("Activo")) AndAlso IsDBNull(tasaActual("VigenteHasta")) AndAlso CDec(tasaActual("Porcentaje")) = porcentaje AndAlso CStr(tasaActual("TipoTasa")) = tipoTasa AndAlso CDate(tasaActual("VigenteDesde")).Date = fechaDesde.Date Then Return tasaActualId
                               Escribir(cn, tx, "UPDATE Tasas SET Activo=0,EsPredeterminada=0,VigenteHasta=GREATEST(VigenteDesde,@p1) WHERE ProductoId=@p0 AND EsPredeterminada=1", productoId, fechaDesde)
                               Dim id = Insertar(cn, tx, "INSERT INTO Tasas(ProductoId,Porcentaje,TipoTasa,CapitalizacionId,VigenteDesde,EsPredeterminada,CreadoPor) VALUES(@p0,@p1,@p2,@p3,@p4,1,@p5)", productoId, porcentaje, tipoTasa, capitalizacionId, fechaDesde, usuarioId)
                               Auditar(cn, tx, usuarioId, "GUARDAR", "Tasas", id, "Producto " & productoId & " · tasa " & porcentaje.ToString(Globalization.CultureInfo.InvariantCulture) & "% · " & tipoTasa)
                               Return id
                           End Function)
    End Function
    Private Shared Function Contexto(cn As MySqlConnection, tx As MySqlTransaction, s As SolicitudFinanciera) As DataRow
        Dim t = Tabla(cn, tx, "SELECT pr.*,t.Id TasaId,t.Porcentaje,t.TipoTasa,t.CapitalizacionId,cap.PeriodosPorAnio Capitalizaciones,p.PeriodosPorAnio,p.Unidad,p.Intervalo,m.Codigo Metodo,c.Ingresos FROM ProductosFinancieros pr JOIN Tasas t ON t.Id=(SELECT t2.Id FROM Tasas t2 JOIN TiposTasa tt2 ON tt2.Codigo=t2.TipoTasa AND tt2.Activo=1 WHERE t2.ProductoId=pr.Id AND t2.Activo=1 AND t2.EsPredeterminada=1 AND t2.VigenteDesde<=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR) AND (t2.VigenteHasta IS NULL OR t2.VigenteHasta>=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) ORDER BY t2.Id DESC LIMIT 1) JOIN TiposTasa tt ON tt.Codigo=t.TipoTasa AND tt.Activo=1 JOIN Periodicidades cap ON cap.Id=t.CapitalizacionId AND cap.Activo=1 JOIN ProductoMetodos pm ON pm.ProductoId=pr.Id JOIN MetodosCalculo m ON m.Id=pm.MetodoId AND m.Activo=1 JOIN ProductoPeriodicidades pp ON pp.ProductoId=pr.Id JOIN Periodicidades p ON p.Id=pp.PeriodicidadId AND p.Activo=1 AND p.Unidad='MESES' AND p.Intervalo=1 AND p.PeriodosPorAnio=12 JOIN Clientes c ON c.Id=@p3 AND c.Activo=1 WHERE pr.Id=@p0 AND pr.Activo=1 AND m.Id=@p1 AND p.Id=@p2 FOR UPDATE", s.ProductoId, s.MetodoId, s.PeriodicidadId, s.ClienteId)
        If t.Rows.Count <> 1 Then Throw New ArgumentException("Seleccione cliente/producto activos y condiciones permitidas con tasa vigente.")
        Dim r = t.Rows(0), meses = Decimal.Round(s.CantidadCuotas * 12D / CDec(r("PeriodosPorAnio")), 2)
        If s.Monto < CDec(r("MontoMinimo")) OrElse s.Monto > CDec(r("MontoMaximo")) OrElse s.CantidadCuotas < CInt(r("PlazoMinimo")) OrElse s.CantidadCuotas > CInt(r("PlazoMaximo")) OrElse meses < CDec(r("PlazoMinimoMeses")) OrElse meses > CDec(r("PlazoMaximoMeses")) Then Throw New ArgumentException("Monto o plazo fuera de los límites del producto.")
        Return r
    End Function
    Private Shared Function Condiciones(r As DataRow, monto As Decimal, cuotas As Integer, fecha As Date) As CondicionesPrestamo
        Return New CondicionesPrestamo With {.Monto = monto, .CantidadCuotas = cuotas, .FechaPrimerPago = fecha, .TasaAnual = CDec(r("Porcentaje")), .TipoTasa = CStr(r("TipoTasa")), .CapitalizacionesAnuales = CDec(r("Capitalizaciones")), .PeriodosAnuales = CDec(r("PeriodosPorAnio")), .Unidad = CStr(r("Unidad")), .Intervalo = CInt(r("Intervalo")), .Metodo = CStr(r("Metodo"))}
    End Function
    Public Function CrearSolicitud(s As SolicitudFinanciera, usuarioId As Integer, calcular As Func(Of CondicionesPrestamo, List(Of CuotaPlan))) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR", "OFICIAL_CREDITO")
                               Dim r = Contexto(cn, tx, s), condicionesPlan = Condiciones(r, s.Monto, s.CantidadCuotas, s.FechaPrimerPago), plan = calcular(condicionesPlan)
                               Dim numeroSolicitud = If(String.IsNullOrEmpty(s.Clave), Numero("SOL"), "SOL-" & s.Clave)
                               Dim existente = Tabla(cn, tx, "SELECT * FROM Solicitudes WHERE NumeroSolicitud=@p0", numeroSolicitud)
                               If existente.Rows.Count > 0 Then
                                   Dim previa = existente.Rows(0)
                                   If CInt(previa("CreadoPor")) <> usuarioId OrElse CInt(previa("ClienteId")) <> s.ClienteId OrElse CInt(previa("ProductoId")) <> s.ProductoId OrElse CInt(previa("MetodoId")) <> s.MetodoId OrElse CInt(previa("PeriodicidadId")) <> s.PeriodicidadId OrElse CDec(previa("MontoSolicitado")) <> s.Monto OrElse CInt(previa("CantidadCuotas")) <> s.CantidadCuotas OrElse CDate(previa("FechaPrimerPago")).Date <> s.FechaPrimerPago.Date OrElse Convert.ToString(previa("Observaciones")) <> If(s.Observaciones, "") Then Throw New InvalidOperationException("Este formulario ya registró otra solicitud. Inicie una nueva.")
                                   Return CInt(previa("Id"))
                               End If
                               Dim id = Insertar(cn, tx, "INSERT INTO Solicitudes(NumeroSolicitud,ClienteId,ProductoId,MetodoId,TasaId,PeriodicidadId,MontoSolicitado,CantidadCuotas,CreadoPor,Observaciones,PlazoMeses,TasaPactada,TipoTasa,CapitalizacionId,FechaPrimerPago,Comision,GastosAsociados,CuotaEstimada,IngresosEvaluados) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17,@p18)", numeroSolicitud, s.ClienteId, s.ProductoId, s.MetodoId, CInt(r("TasaId")), s.PeriodicidadId, s.Monto, s.CantidadCuotas, usuarioId, s.Observaciones, Decimal.Round(s.CantidadCuotas * 12D / condicionesPlan.PeriodosAnuales, 2), condicionesPlan.TasaAnual, condicionesPlan.TipoTasa, CInt(r("CapitalizacionId")), s.FechaPrimerPago, CalcularComision(CStr(r("TipoComision")), CDec(r("ValorComision")), s.Monto), CDec(r("ValorGastos")), plan.Max(Function(q) q.Cuota), CDec(r("Ingresos")))
                               Auditar(cn, tx, usuarioId, "CREAR", "Solicitudes", id, "Estado: SOLICITUD")
                               Return id
                           End Function)
    End Function
    Public Function Simular(s As SolicitudFinanciera, usuarioId As Integer, calcular As Func(Of CondicionesPrestamo, List(Of CuotaPlan))) As List(Of CuotaPlan)
        Return Transaccion(Function(cn, tx)
                               Usuario(cn, tx, usuarioId)
                               Dim r = Contexto(cn, tx, s)
                               Return calcular(Condiciones(r, s.Monto, s.CantidadCuotas, s.FechaPrimerPago))
                           End Function)
    End Function
    Public Function SimularProducto(s As SolicitudFinanciera, usuarioId As Integer, calcular As Func(Of CondicionesPrestamo, List(Of CuotaPlan))) As List(Of CuotaPlan)
        Return Transaccion(Function(cn, tx)
                               Usuario(cn, tx, usuarioId)
                               Dim t = Tabla(cn, tx,
                                   "SELECT pr.MontoMinimo,pr.MontoMaximo,pr.PlazoMinimo,pr.PlazoMaximo,pr.PlazoMinimoMeses,pr.PlazoMaximoMeses," &
                                   "t.Porcentaje,t.TipoTasa,cap.PeriodosPorAnio Capitalizaciones,p.PeriodosPorAnio,p.Unidad,p.Intervalo,m.Codigo Metodo " &
                                   "FROM ProductosFinancieros pr " &
                                   "JOIN Tasas t ON t.Id=(SELECT t2.Id FROM Tasas t2 JOIN TiposTasa tt2 ON tt2.Codigo=t2.TipoTasa AND tt2.Activo=1 WHERE t2.ProductoId=pr.Id AND t2.Activo=1 AND t2.EsPredeterminada=1 AND t2.VigenteDesde<=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR) AND (t2.VigenteHasta IS NULL OR t2.VigenteHasta>=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)) ORDER BY t2.Id DESC LIMIT 1) " &
                                   "JOIN TiposTasa tt ON tt.Codigo=t.TipoTasa AND tt.Activo=1 " &
                                   "JOIN Periodicidades cap ON cap.Id=t.CapitalizacionId AND cap.Activo=1 " &
                                   "JOIN ProductoMetodos pm ON pm.ProductoId=pr.Id " &
                                   "JOIN MetodosCalculo m ON m.Id=pm.MetodoId AND m.Activo=1 " &
                                   "JOIN ProductoPeriodicidades pp ON pp.ProductoId=pr.Id " &
                                   "JOIN Periodicidades p ON p.Id=pp.PeriodicidadId AND p.Activo=1 AND p.Unidad='MESES' AND p.Intervalo=1 AND p.PeriodosPorAnio=12 " &
                                   "WHERE pr.Id=@p0 AND m.Id=@p1 AND p.Id=@p2 AND pr.Activo=1 " &
                                   "FOR UPDATE",
                                   s.ProductoId, s.MetodoId, s.PeriodicidadId)
                               If t.Rows.Count <> 1 Then Throw New ArgumentException("Seleccione un producto activo con tasa vigente, método y periodicidad permitidos.")
                               Dim r = t.Rows(0)
                               Dim meses = Decimal.Round(s.CantidadCuotas * 12D / CDec(r("PeriodosPorAnio")), 2)
                               If s.Monto < CDec(r("MontoMinimo")) OrElse s.Monto > CDec(r("MontoMaximo")) Then Throw New ArgumentException("El monto está fuera de los límites definidos para el producto.")
                               If s.CantidadCuotas < CInt(r("PlazoMinimo")) OrElse s.CantidadCuotas > CInt(r("PlazoMaximo")) OrElse meses < CDec(r("PlazoMinimoMeses")) OrElse meses > CDec(r("PlazoMaximoMeses")) Then
                                   Throw New ArgumentException("El monto o plazo está fuera de los límites definidos para el producto.")
                               End If
                               Return calcular(Condiciones(r, s.Monto, s.CantidadCuotas, s.FechaPrimerPago))
                           End Function)
    End Function
    Public Function Evaluar(id As Integer, version As ULong, accion As String, monto As Decimal, motivo As String, usuarioId As Integer) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                               Dim t = Tabla(cn, tx, "SELECT * FROM Solicitudes WHERE Id=@p0 FOR UPDATE", id)
                               If t.Rows.Count = 0 Then Throw New ArgumentException("Solicitud inexistente.")
                               Dim r = t.Rows(0), anterior = CStr(r("EstadoCodigo"))
                               If CULng(r("Version")) <> version Then Throw New InvalidOperationException("La solicitud cambió. Actualice antes de continuar.")
                               If (accion = "EN_EVALUACION" AndAlso anterior <> "SOLICITUD") OrElse ({"APROBADO", "RECHAZADO"}.Contains(accion) AndAlso anterior <> "EN_EVALUACION") OrElse Not {"EN_EVALUACION", "APROBADO", "RECHAZADO"}.Contains(accion) Then Throw New ArgumentException("Transición de solicitud no permitida.")
                               If accion = "APROBADO" AndAlso (monto <= 0 OrElse monto > CDec(r("MontoSolicitado"))) Then Throw New ArgumentException("El monto aprobado debe ser positivo y no superar el solicitado.")
                               If accion = "APROBADO" Then
                                   Dim producto = Tabla(cn, tx, "SELECT MontoMinimo,MontoMaximo,Activo FROM ProductosFinancieros WHERE Id=@p0 FOR UPDATE", r("ProductoId")).Rows(0)
                                   If Not CBool(producto("Activo")) OrElse monto < CDec(producto("MontoMinimo")) OrElse monto > CDec(producto("MontoMaximo")) OrElse monto < CDec(r("Comision")) + CDec(r("GastosAsociados")) Then Throw New ArgumentException("El monto aprobado no cumple los límites y costos del producto activo.")
                               End If
                               If accion = "RECHAZADO" AndAlso String.IsNullOrWhiteSpace(motivo) Then Throw New ArgumentException("Indique el motivo del rechazo.")
                               Escribir(cn, tx, "UPDATE Solicitudes SET EstadoCodigo=@p1,EvaluadoPor=@p2,FechaEvaluacion=UTC_TIMESTAMP(),MontoAprobado=@p3,FechaAprobacion=@p4,MotivoRechazo=@p5,Version=Version+1 WHERE Id=@p0", id, accion, usuarioId, If(accion = "APROBADO", CType(monto, Object), Nothing), If(accion = "APROBADO", CType(Date.UtcNow, Object), Nothing), If(accion = "RECHAZADO", motivo, Nothing))
                               Auditar(cn, tx, usuarioId, accion, "Solicitudes", id, anterior & " → " & accion & ". " & motivo)
                               Return id
                           End Function)
    End Function
    Public Shared Function Numero(prefijo As String) As String
        Return prefijo & "-" & Guid.NewGuid().ToString("N")
    End Function
    Public Function Formalizar(solicitudId As Integer, version As ULong, fechaPrimerPago As Date, usuarioId As Integer, calcular As Func(Of CondicionesPrestamo, List(Of CuotaPlan))) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR")
                               Dim t = Tabla(cn, tx, "SELECT s.*,m.Codigo Metodo,p.PeriodosPorAnio,p.Unidad,p.Intervalo,cap.PeriodosPorAnio Capitalizaciones,s.TasaPactada Porcentaje,c.Activo ClienteActivo,pr.Activo ProductoActivo,pr.MontoMinimo,pr.MontoMaximo FROM Solicitudes s JOIN Clientes c ON c.Id=s.ClienteId JOIN ProductosFinancieros pr ON pr.Id=s.ProductoId JOIN MetodosCalculo m ON m.Id=s.MetodoId JOIN Periodicidades p ON p.Id=s.PeriodicidadId JOIN Periodicidades cap ON cap.Id=s.CapitalizacionId WHERE s.Id=@p0 AND m.Activo=1 AND p.Activo=1 AND cap.Activo=1 FOR UPDATE", solicitudId)
                               If t.Rows.Count <> 1 Then Throw New ArgumentException("Solicitud inexistente.")
                               Dim r = t.Rows(0)
                               If CULng(r("Version")) <> version OrElse CStr(r("EstadoCodigo")) <> "APROBADO" Then Throw New InvalidOperationException("La solicitud debe estar aprobada y actualizada.")
                               If Not CBool(r("ClienteActivo")) OrElse Not CBool(r("ProductoActivo")) Then Throw New ArgumentException("Cliente o producto inactivo.")
                               If Tabla(cn, tx, "SELECT Id FROM Prestamos WHERE SolicitudId=@p0", solicitudId).Rows.Count > 0 Then Throw New InvalidOperationException("La solicitud ya fue formalizada.")
                               Dim monto = CDec(r("MontoAprobado")), costos = CDec(r("Comision")) + CDec(r("GastosAsociados"))
                               If monto < CDec(r("MontoMinimo")) OrElse monto > CDec(r("MontoMaximo")) OrElse costos > monto Then Throw New ArgumentException("Monto aprobado o costos fuera del producto.")
                               Dim condicionesPlan = Condiciones(r, monto, CInt(r("CantidadCuotas")), fechaPrimerPago)
                               Dim minimo = If(condicionesPlan.Unidad = "MESES", Date.UtcNow.AddHours(-5).Date.AddMonths(condicionesPlan.Intervalo), Date.UtcNow.AddHours(-5).Date.AddDays(condicionesPlan.Intervalo))
                               If fechaPrimerPago <> minimo Then Throw New ArgumentException("Para períodos completos, el primer pago debe ser " & minimo.ToString("dd/MM/yyyy") & ".")
                               Dim plan = calcular(condicionesPlan)
                               Dim id = Insertar(cn, tx, "INSERT INTO Prestamos(NumeroPrestamo,SolicitudId,ClienteId,ProductoId,MetodoId,PeriodicidadId,CapitalizacionId,FechaAprobacion,FechaDesembolso,FechaPrimerPago,MontoAprobado,TasaInteres,TipoTasa,CantidadCuotas,Comision,GastosAsociados,SaldoCapital,EstadoCodigo,AprobadoPor,DesembolsadoPor,PlazoMeses) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,UTC_TIMESTAMP(),@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p9,'VIGENTE',@p15,@p16,@p17)", Numero("PRE"), solicitudId, r("ClienteId"), r("ProductoId"), r("MetodoId"), r("PeriodicidadId"), r("CapitalizacionId"), r("FechaAprobacion"), fechaPrimerPago, monto, r("TasaPactada"), r("TipoTasa"), r("CantidadCuotas"), r("Comision"), r("GastosAsociados"), r("EvaluadoPor"), usuarioId, r("PlazoMeses"))
                               For Each q In plan
                                   Insertar(cn, tx, "INSERT INTO Cuotas(PrestamoId,NumeroCuota,FechaVencimiento,Capital,Interes,SaldoProyectado) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", id, q.Numero, q.Fecha, q.Capital, q.Interes, q.Saldo)
                               Next
                               Movimiento(cn, tx, id, Nothing, "DESEMBOLSO", "Capital contractual; costos retenidos: " & costos.ToString(Globalization.CultureInfo.InvariantCulture), monto, 0D, usuarioId, Date.UtcNow.AddHours(-5))
                               Auditar(cn, tx, usuarioId, "DESEMBOLSAR", "Prestamos", id, "Solicitud " & solicitudId & "; neto entregado=" & (monto - costos).ToString(Globalization.CultureInfo.InvariantCulture))
                               Return id
                           End Function)
    End Function
    Private Shared Sub Movimiento(cn As MySqlConnection, tx As MySqlTransaction, prestamo As Integer, pago As Object, tipo As String, concepto As String, debito As Decimal, credito As Decimal, usuarioId As Integer, fecha As DateTime)
        Escribir(cn, tx, "INSERT INTO MovimientosPrestamo(PrestamoId,PagoId,Fecha,Tipo,Concepto,Debito,Credito,UsuarioId) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)", prestamo, pago, fecha, tipo, concepto, debito, credito, usuarioId)
    End Sub
    Public Function RegistrarPago(p As PagoFinanciero, usuarioId As Integer) As Integer
        Return Transaccion(Function(cn, tx)
                               Autorizar(cn, tx, usuarioId, "ADMINISTRADOR", "CAJERO")
                               Dim t = Tabla(cn, tx, "SELECT * FROM Prestamos WHERE Id=@p0 FOR UPDATE", p.PrestamoId)
                               If t.Rows.Count <> 1 Then Throw New ArgumentException("No se encontró el préstamo seleccionado. Actualiza la lista e inténtalo nuevamente.")
                               Dim prestamo = t.Rows(0), repetido = Tabla(cn, tx, "SELECT * FROM Pagos WHERE ClaveIdempotencia=@p0", p.Clave)
                               If repetido.Rows.Count > 0 Then
                                   Dim previo = repetido.Rows(0)
                                   If CInt(previo("PrestamoId")) <> p.PrestamoId OrElse CDec(previo("MontoPagado")) <> p.Monto OrElse CDate(previo("FechaPago")).Date <> p.Fecha.Date OrElse CStr(previo("FormaPagoCodigo")) <> p.Forma OrElse Convert.ToString(previo("ReferenciaExterna")) <> If(p.Referencia, "") OrElse CInt(previo("RegistradoPor")) <> usuarioId Then Throw New InvalidOperationException("La clave del pago ya se utilizó con otros datos.")
                                   Return CInt(previo("Id"))
                               End If
                               If Not {"VIGENTE", "EN_MORA", "DESEMBOLSADO"}.Contains(CStr(prestamo("EstadoCodigo"))) Then Throw New ArgumentException("No se puede registrar el pago: el préstamo está " & CStr(prestamo("EstadoCodigo")).Replace("_", " ").ToLowerInvariant() & " y ya no admite cobros.")
                               Dim instante = Date.UtcNow.AddHours(-5)
                               Dim hoy = instante.Date
                               If p.Fecha.Date < hoy Then Throw New ArgumentException("No se puede registrar un pago con una fecha anterior a la fecha operativa de hoy (" & hoy.ToString("dd/MM/yyyy") & "). Los pagos retroactivos no están permitidos; actualiza la pantalla para cargar la fecha vigente.")
                               If p.Fecha.Date > hoy Then Throw New ArgumentException("No se puede adelantar la fecha del pago. Solo se permiten registros con la fecha operativa de hoy (" & hoy.ToString("dd/MM/yyyy") & "); los pagos con fecha futura no están permitidos.")
                               If Tabla(cn, tx, "SELECT Codigo FROM FormasPago WHERE Codigo=@p0 AND Activo=1", p.Forma).Rows.Count = 0 Then Throw New ArgumentException("La forma de pago seleccionada ya no está activa. Actualiza la pantalla y elige otra forma disponible.")
                               Dim cuotas = Tabla(cn, tx, "SELECT * FROM v_SaldosCuota WHERE PrestamoId=@p0 ORDER BY NumeroCuota", p.PrestamoId)
                               Dim finMes = New Date(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month))
                               Dim cuotasAplicables = cuotas.AsEnumerable().Where(Function(q) CDate(q("FechaVencimiento")).Date <= finMes AndAlso CDec(q("TotalPendiente")) > 0).ToList()
                               Dim totalMes = cuotasAplicables.Sum(Function(q) CDec(q("TotalPendiente")))
                               If totalMes <= 0D Then
                                   Dim proximaCuota = cuotas.AsEnumerable().Where(Function(q) CDec(q("TotalPendiente")) > 0).OrderBy(Function(q) CDate(q("FechaVencimiento"))).FirstOrDefault()
                                   Dim detalleProximaCuota = If(proximaCuota Is Nothing, String.Empty, " La próxima cuota vence el " & CDate(proximaCuota("FechaVencimiento")).ToString("dd/MM/yyyy", Globalization.CultureInfo.GetCultureInfo("es-PA")) & "; no se pueden adelantar pagos de períodos futuros.")
                                   Throw New ArgumentException("No se puede registrar un pago ahora: no hay cuotas vencidas ni programadas para este mes con saldo pendiente." & detalleProximaCuota)
                               End If
                               If p.Monto > totalMes Then Throw New ArgumentException("El monto excede el total exigible de este mes (B/. " & totalMes.ToString("0.00") & "). Reduce el monto; no se permiten sobrepagos ni pagos de cuotas futuras.")
                               Dim capitalPagado As Decimal = 0D, restante = p.Monto
                               Dim id = Insertar(cn, tx, "INSERT INTO Pagos(NumeroRecibo,PrestamoId,ClienteId,FechaPago,MontoPagado,SaldoCapitalPosterior,RegistradoPor,FormaPagoCodigo,ReferenciaExterna,ClaveIdempotencia) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)", Numero("TMP"), p.PrestamoId, prestamo("ClienteId"), instante, p.Monto, prestamo("SaldoCapital"), usuarioId, p.Forma, p.Referencia, p.Clave)
                               Escribir(cn, tx, "UPDATE Pagos SET NumeroRecibo=@p1 WHERE Id=@p0", id, "FAC-" & id.ToString("D4", Globalization.CultureInfo.InvariantCulture))
                               For Each q In cuotasAplicables
                                   ' Registrar el desglose contractual una sola vez al aplicar la cuota del período.
                                   For Each cargo In New String()() {New String() {"Interes", "INTERES", "Interés cuota "}, New String() {"Mora", "MORA", "Mora cuota "}, New String() {"OtrosCargos", "CARGO", "Cargos cuota "}}
                                       Dim concepto = cargo(2) & CStr(q("NumeroCuota"))
                                       If CDec(q(cargo(0))) > 0 AndAlso Tabla(cn, tx, "SELECT Id FROM MovimientosPrestamo WHERE PrestamoId=@p0 AND Tipo=@p1 AND Concepto=@p2", p.PrestamoId, cargo(1), concepto).Rows.Count = 0 Then Movimiento(cn, tx, p.PrestamoId, Nothing, cargo(1), concepto, CDec(q(cargo(0))), 0D, usuarioId, instante)
                                   Next
                                   Dim partes As New List(Of Decimal)
                                   For Each columna In {"MoraPendiente", "CargosPendientes", "InteresPendiente", "CapitalPendiente"}
                                       Dim aplicado = Math.Min(restante, CDec(q(columna)))
                                       partes.Add(aplicado) : restante -= aplicado
                                   Next
                                   If partes.Sum() > 0 Then
                                       Escribir(cn, tx, "INSERT INTO AplicacionesPago(PagoId,CuotaId,PrestamoId,Mora,OtrosCargos,Interes,Capital) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6)", id, q("Id"), p.PrestamoId, partes(0), partes(1), partes(2), partes(3))
                                       capitalPagado += partes(3)
                                   End If
                                   If restante = 0 Then Exit For
                               Next
                               If restante <> 0 Then Throw New InvalidOperationException("El desglose del pago no coincide.")
                               Dim saldo = CDec(prestamo("SaldoCapital")) - capitalPagado
                               If saldo < 0 Then Throw New InvalidOperationException("El capital aplicado supera el saldo.")
                               Dim pendiente = cuotas.AsEnumerable().Sum(Function(q) CDec(q("TotalPendiente"))) - p.Monto
                               Dim estado = If(pendiente = 0, "CANCELADO", "VIGENTE")
                               ' Volver a consultar el desglose ya aplicado para determinar mora residual.
                               If estado <> "CANCELADO" Then estado = If(Tabla(cn, tx, "SELECT Id FROM v_SaldosCuota WHERE PrestamoId=@p0 AND FechaVencimiento<@p1 AND TotalPendiente>0 LIMIT 1", p.PrestamoId, hoy).Rows.Count > 0, "EN_MORA", "VIGENTE")
                               Escribir(cn, tx, "UPDATE Prestamos SET SaldoCapital=@p1,EstadoCodigo=@p2,FechaCancelacion=@p3,TipoCancelacion=@p4,Version=Version+1 WHERE Id=@p0", p.PrestamoId, saldo, estado, If(estado = "CANCELADO", CType(Date.UtcNow, Object), Nothing), If(estado = "CANCELADO", "NORMAL", Nothing))
                               Escribir(cn, tx, "UPDATE Pagos SET SaldoCapitalPosterior=@p1 WHERE Id=@p0", id, saldo)
                               Movimiento(cn, tx, p.PrestamoId, id, "PAGO", "Recibo " & id, 0D, p.Monto, usuarioId, instante)
                               Auditar(cn, tx, usuarioId, "PAGAR", "Pagos", id, "Préstamo " & p.PrestamoId & "; capital=" & capitalPagado.ToString(Globalization.CultureInfo.InvariantCulture))
                               Return id
                           End Function)
    End Function

    Public Function ConsultarReporte(numero As Integer, usuarioId As Integer, desde As Nullable(Of Date), hasta As Nullable(Of Date), clienteId As Integer, productoId As Integer, prestamoId As Integer) As DataTable
        Using cn = Abrir()
            Autorizar(cn, Nothing, usuarioId, "ADMINISTRADOR", "OFICIAL_CREDITO", "CAJERO", "CONSULTA")
            Dim sql As String, fecha As String, orden As String, grupo As String = ""
            Select Case numero
                Case 1
                    sql = "SELECT c.Identificacion AS Identificación,c.NombreCompleto AS Cliente,c.Telefono AS Teléfono,c.Correo,c.Provincia,IF(c.TipoPersona='NATURAL','Natural','Jurídica') AS Tipo,IF(c.Activo=1,'Activo','Inactivo') AS Estado,DATE(c.CreadoUtc-INTERVAL 5 HOUR) AS Registro FROM v_Clientes c WHERE 1=1"
                    fecha = "DATE(c.CreadoUtc-INTERVAL 5 HOUR)" : orden = "c.NombreCompleto,c.Id"
                Case 2, 3, 6
                    sql = "SELECT CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS Préstamo,p.NombreCompleto AS Cliente,p.Producto,p.Metodo AS Método,p.FechaDesembolso AS Desembolso,p.FechaCancelacion AS Cancelación,p.MontoAprobado AS Capital,p.SaldoCapital AS Saldo,p.InteresesProgramadosPendientes AS Intereses,p.MoraPendiente AS Mora,p.CargosPendientes AS Cargos,CASE WHEN p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') AND p.CuotasVencidas>0 THEN 'EN_MORA' ELSE p.EstadoCodigo END AS Estado FROM v_PrestamosResumen p WHERE " & If(numero=3,"p.EstadoCodigo='CANCELADO'","p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO')")
                    fecha = If(numero=3,"p.FechaCancelacion","p.FechaDesembolso") : orden = "p.Id DESC"
                Case 4
                    sql = "SELECT CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS Préstamo,r.NombreCompleto AS Cliente,r.Telefono AS Teléfono,pr.Nombre AS Producto,r.NumeroCuota AS Cuota,r.FechaVencimiento AS Vencimiento,r.DiasAtraso AS Días,r.MontoVencido AS Vencido,r.MoraPendiente AS Mora FROM v_Reporte04PrestamosMora r JOIN Prestamos p ON p.Id=r.PrestamoId JOIN ProductosFinancieros pr ON pr.Id=p.ProductoId WHERE 1=1"
                    fecha = "r.FechaVencimiento" : orden = "r.FechaVencimiento,p.Id,r.NumeroCuota"
                Case 5
                    sql = "SELECT r.Id AS PagoId,CONCAT('FAC-',LPAD(r.Id,4,'0')) AS Recibo,CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS Préstamo,r.NombreCompleto AS Cliente,pr.Nombre AS Producto,r.FechaPago AS Fecha,r.MontoPagado AS Pagado,r.Capital,r.Interes AS Intereses,r.Mora,r.OtrosCargos AS Cargos,r.NombreUsuario AS Cajero FROM v_Reporte05PagosPeriodo r JOIN Prestamos p ON p.Id=r.PrestamoId JOIN ProductosFinancieros pr ON pr.Id=p.ProductoId WHERE 1=1"
                    fecha = "r.FechaPago" : orden = "r.FechaPago DESC,r.Id DESC"
                Case 7
                    ' El saldo se calcula en la vista ANTES del filtro de fechas para incluir movimientos anteriores.
                    sql = "SELECT CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS Préstamo,r.NombreCompleto AS Cliente,r.Fecha,r.Tipo,r.Concepto,r.Debito AS Débito,r.Credito AS Crédito,r.SaldoAcumulado AS Saldo FROM v_Reporte07EstadoCuenta r JOIN Prestamos p ON p.Id=r.PrestamoId WHERE 1=1"
                    fecha = "r.Fecha" : orden = "r.Fecha,r.Id"
                Case 8
                    sql = "SELECT CONCAT('PRE-',LPAD(p.Id,GREATEST(6,CHAR_LENGTH(p.Id)),'0')) AS Préstamo,c.NombreCompleto AS Cliente,r.NumeroCuota AS Cuota,r.FechaVencimiento AS Vencimiento,r.Capital,r.Interes AS Intereses,r.InteresCapitalizado AS Capitalizado,r.CuotaProgramada AS Programado,r.Mora,r.OtrosCargos AS Cargos,r.TotalPendiente AS Pendiente,r.SaldoProyectado AS Proyectado,r.EstadoCuota AS Estado FROM v_Reporte08Amortizacion r JOIN Prestamos p ON p.Id=r.PrestamoId JOIN v_Clientes c ON c.Id=p.ClienteId WHERE 1=1"
                    fecha = "r.FechaVencimiento" : orden = "r.NumeroCuota,r.Id"
                Case 9
                    sql = "SELECT p.Producto,CASE WHEN p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO') AND p.CuotasVencidas>0 THEN 'EN_MORA' ELSE p.EstadoCodigo END AS Estado,COUNT(*) AS Préstamos,SUM(p.MontoAprobado) AS Capital,SUM(p.SaldoCapital) AS Saldo,SUM(p.InteresesProgramadosPendientes) AS Intereses,SUM(p.MoraPendiente) AS Mora FROM v_PrestamosResumen p WHERE p.FechaDesembolso IS NOT NULL"
                    fecha = "p.FechaDesembolso" : grupo = " GROUP BY p.ProductoId,p.Producto,Estado" : orden = "p.Producto,Estado"
                Case 10
                    sql = "SELECT m.Nombre AS Método,COUNT(*) AS Préstamos,SUM(p.MontoAprobado) AS Capital,SUM(p.SaldoCapital) AS Saldo,SUM(COALESCE(q.Intereses,0)) AS Intereses,SUM(COALESCE(q.Mora,0)) AS Mora FROM Prestamos p JOIN MetodosCalculo m ON m.Id=p.MetodoId LEFT JOIN (SELECT PrestamoId,SUM(Interes+InteresCapitalizado) AS Intereses,SUM(Mora) AS Mora FROM Cuotas GROUP BY PrestamoId) q ON q.PrestamoId=p.Id WHERE p.FechaDesembolso IS NOT NULL"
                    fecha = "p.FechaDesembolso" : grupo = " GROUP BY p.MetodoId,m.Nombre" : orden = "m.Nombre"
                Case Else
                    Throw New ArgumentException("Seleccione uno de los reportes disponibles.")
            End Select
            sql &= If(numero=1," AND (@p2=0 OR c.Id=@p2)"," AND (@p2=0 OR p.ClienteId=@p2) AND (@p3=0 OR p.ProductoId=@p3) AND (@p4=0 OR p.Id=@p4)")
            sql &= " AND (@p0 IS NULL OR " & fecha & ">=@p0) AND (@p1 IS NULL OR " & fecha & "<@p1)" & grupo & " ORDER BY " & orden
            Dim inicio As Object = If(desde.HasValue,CType(desde.GetValueOrDefault().Date,Object),DBNull.Value)
            Dim fin As Object = If(hasta.HasValue,CType(hasta.GetValueOrDefault().Date.AddDays(1),Object),DBNull.Value)
            Return Tabla(cn, Nothing, sql, inicio, fin, clienteId, productoId, prestamoId)
        End Using
    End Function
End Class
