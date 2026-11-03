Option Strict On
Imports System.Data
Imports System.Collections.Generic
Imports System.Globalization

Public Class Parametros
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False : Notificar("No tienes permiso para modificar los parámetros.",True) : Return
        End If
        If Not IsPostBack Then Ejecutar(Sub() CargarValores())
    End Sub
    Private Sub CargarValores()
        Dim datos=ServicioFinanzas.ParametrosAdministracion(Actor.Id)
        Dim valores As New Dictionary(Of String,String)(StringComparer.Ordinal)
        For Each fila As DataRow In datos.Rows
            valores(CStr(fila("Codigo")))=CStr(fila("Valor"))
        Next
        txtIntentos.Text=Valor(valores,"INTENTOS_MAXIMOS","5")
        txtBloqueo.Text=Valor(valores,"BLOQUEO_MINUTOS","15")
        txtInactividad.Text=Valor(valores,"INACTIVIDAD_MINUTOS","20")
        txtLongitud.Text=Valor(valores,"CLAVE_LONGITUD_MINIMA","8")
        chkMayuscula.Checked=Valor(valores,"CLAVE_MAYUSCULA","1")="1"
        chkNumero.Checked=Valor(valores,"CLAVE_NUMERO","1")="1"
        txtPorcentaje.Text=Valor(valores,"PORCENTAJE_MAXIMO_CUOTA_INGRESOS","35")
        txtTasaMora.Text=Valor(valores,"TASA_MORA_ANUAL","0")
        txtDiasGracia.Text=Valor(valores,"DIAS_GRACIA_MORA","0")
    End Sub
    Private Shared Function Valor(valores As IDictionary(Of String,String),codigo As String,defecto As String) As String
        Return If(valores.ContainsKey(codigo),valores(codigo),defecto)
    End Function
    Protected Sub Guardar(sender As Object,e As EventArgs) Handles btnGuardar.Click
        Ejecutar(Sub()
            Dim valores As New Dictionary(Of String,String)(StringComparer.Ordinal) From {
                {"INTENTOS_MAXIMOS",txtIntentos.Text.Trim()},{"BLOQUEO_MINUTOS",txtBloqueo.Text.Trim()},{"INACTIVIDAD_MINUTOS",txtInactividad.Text.Trim()},
                {"CLAVE_LONGITUD_MINIMA",txtLongitud.Text.Trim()},{"CLAVE_MAYUSCULA",If(chkMayuscula.Checked,"1","0")},{"CLAVE_NUMERO",If(chkNumero.Checked,"1","0")},
                {"PORCENTAJE_MAXIMO_CUOTA_INGRESOS",txtPorcentaje.Text.Trim()},{"TASA_MORA_ANUAL",txtTasaMora.Text.Trim()},{"DIAS_GRACIA_MORA",txtDiasGracia.Text.Trim()}}
            ServicioFinanzas.GuardarParametrosAdministracion(Actor.Id,valores)
            Notificar("Parámetros guardados y registrados en la auditoría.")
            CargarValores()
        End Sub)
    End Sub
End Class
