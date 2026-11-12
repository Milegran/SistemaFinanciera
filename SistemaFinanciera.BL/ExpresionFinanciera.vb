Option Strict On
Imports System.Globalization
Imports SistemaFinanciera.Entities

Friend Enum TipoToken
    Numero
    Identificador
    Simbolo
    Fin
End Enum

Friend Structure Token
    Public Tipo As TipoToken
    Public Texto As String
    Public Posicion As Integer
End Structure

Friend NotInheritable Class ErroresExpresion
    Private Sub New()
    End Sub
    Public Shared Function En(posicion As Integer, esperado As String, Optional encontrado As String = Nothing) As ArgumentException
        Dim mensaje = "Sintaxis inválida en la posición " & (posicion + 1).ToString(CultureInfo.InvariantCulture) & ": " & esperado
        If encontrado IsNot Nothing Then mensaje &= " (se encontró '" & encontrado & "')"
        Return New ArgumentException(mensaje & ".")
    End Function
End Class

Friend NotInheritable Class Tokenizador
    Private Sub New()
    End Sub
    Public Shared Function Tokenizar(texto As String) As List(Of Token)
        If texto.Length > ExpresionFinanciera.MaximoCaracteres Then Throw New ArgumentException("Fórmula demasiado extensa: máximo " & ExpresionFinanciera.MaximoCaracteres.ToString(CultureInfo.InvariantCulture) & " caracteres.")
        Dim tokens As New List(Of Token)()
        Dim p As Integer = 0
        While p < texto.Length
            Dim ch = texto(p)
            If Char.IsWhiteSpace(ch) Then
                p += 1
                Continue While
            End If
            If ch = "#"c Then
                While p < texto.Length AndAlso texto(p) <> vbLf(0) AndAlso texto(p) <> vbCr(0)
                    p += 1
                End While
                Continue While
            End If
            If Char.IsDigit(ch) Then
                Dim inicio = p
                While p < texto.Length AndAlso Char.IsDigit(texto(p))
                    p += 1
                End While
                If p < texto.Length AndAlso texto(p) = "."c Then
                    p += 1
                    If p >= texto.Length OrElse Not Char.IsDigit(texto(p)) Then Throw ErroresExpresion.En(p, "dígitos después del punto decimal", ".")
                    While p < texto.Length AndAlso Char.IsDigit(texto(p))
                        p += 1
                    End While
                End If
                Dim literal = texto.Substring(inicio, p - inicio)
                Dim valor As Decimal
                If Not Decimal.TryParse(literal, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, valor) Then Throw ErroresExpresion.En(inicio, "número dentro del rango permitido", literal)
                tokens.Add(New Token With {.Tipo = TipoToken.Numero, .Texto = literal, .Posicion = inicio})
                Continue While
            End If
            If Char.IsLetter(ch) OrElse ch = "_"c Then
                Dim inicio = p
                While p < texto.Length AndAlso (Char.IsLetterOrDigit(texto(p)) OrElse texto(p) = "_"c)
                    p += 1
                End While
                tokens.Add(New Token With {.Tipo = TipoToken.Identificador, .Texto = texto.Substring(inicio, p - inicio).ToLowerInvariant(), .Posicion = inicio})
                Continue While
            End If
            If ch = "("c OrElse ch = ")"c OrElse ch = ","c Then
                tokens.Add(New Token With {.Tipo = TipoToken.Simbolo, .Texto = ch.ToString(), .Posicion = p})
                p += 1
                Continue While
            End If
            If p + 1 < texto.Length Then
                Dim par = texto.Substring(p, 2)
                If par = "<=" OrElse par = ">=" OrElse par = "<>" OrElse par = "!=" Then
                    tokens.Add(New Token With {.Tipo = TipoToken.Simbolo, .Texto = par, .Posicion = p})
                    p += 2
                    Continue While
                End If
            End If
            If "+-*/^=<>".IndexOf(ch) >= 0 Then
                tokens.Add(New Token With {.Tipo = TipoToken.Simbolo, .Texto = ch.ToString(), .Posicion = p})
                p += 1
                Continue While
            End If
            Throw ErroresExpresion.En(p, "carácter permitido", ch.ToString())
        End While
        If tokens.Count > ExpresionFinanciera.MaximoTokens Then Throw New ArgumentException("Fórmula demasiado extensa: máximo " & ExpresionFinanciera.MaximoTokens.ToString(CultureInfo.InvariantCulture) & " elementos.")
        tokens.Add(New Token With {.Tipo = TipoToken.Fin, .Texto = "", .Posicion = texto.Length})
        Return tokens
    End Function
End Class

Friend MustInherit Class NodoExpresion
    Public MustOverride Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
End Class

Friend NotInheritable Class NodoLiteral
    Inherits NodoExpresion
    Private ReadOnly valor As Decimal
    Public Sub New(valor As Decimal)
        Me.valor = valor
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Return valor
    End Function
End Class

Friend NotInheritable Class NodoVariable
    Inherits NodoExpresion
    Private ReadOnly nombre As String
    Public Sub New(nombre As String)
        Me.nombre = nombre
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Dim valor As Decimal
        If ctx Is Nothing OrElse Not ctx.TryGetValue(nombre, valor) Then Throw New ArgumentException("La variable '" & nombre & "' no tiene valor en este punto de la fórmula.")
        Return valor
    End Function
End Class

Friend NotInheritable Class NodoAritmetico
    Inherits NodoExpresion
    Private ReadOnly operador As Char
    Private ReadOnly izquierdo As NodoExpresion
    Private ReadOnly derecho As NodoExpresion
    Public Sub New(operador As Char, izquierdo As NodoExpresion, derecho As NodoExpresion)
        Me.operador = operador
        Me.izquierdo = izquierdo
        Me.derecho = derecho
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Dim a = izquierdo.Evaluar(ctx)
        Dim b = derecho.Evaluar(ctx)
        Select Case operador
            Case "+"c
                Return a + b
            Case "-"c
                Return a - b
            Case "*"c
                Return a * b
            Case Else
                If b = 0D Then Throw New ArgumentException("División por cero en la fórmula.")
                Return a / b
        End Select
    End Function
End Class

Friend NotInheritable Class NodoPotencia
    Inherits NodoExpresion
    Private ReadOnly base As NodoExpresion
    Private ReadOnly exponente As NodoExpresion
    Public Sub New(base As NodoExpresion, exponente As NodoExpresion)
        Me.base = base
        Me.exponente = exponente
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Return CDec(Math.Pow(CDbl(base.Evaluar(ctx)), CDbl(exponente.Evaluar(ctx))))
    End Function
End Class

Friend NotInheritable Class NodoComparacion
    Inherits NodoExpresion
    Private ReadOnly operador As String
    Private ReadOnly izquierdo As NodoExpresion
    Private ReadOnly derecho As NodoExpresion
    Public Sub New(operador As String, izquierdo As NodoExpresion, derecho As NodoExpresion)
        Me.operador = operador
        Me.izquierdo = izquierdo
        Me.derecho = derecho
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Dim a = izquierdo.Evaluar(ctx)
        Dim b = derecho.Evaluar(ctx)
        Select Case operador
            Case "="
                Return If(a = b, 1D, 0D)
            Case "<>", "!="
                Return If(a <> b, 1D, 0D)
            Case "<"
                Return If(a < b, 1D, 0D)
            Case ">"
                Return If(a > b, 1D, 0D)
            Case "<="
                Return If(a <= b, 1D, 0D)
            Case Else
                Return If(a >= b, 1D, 0D)
        End Select
    End Function
End Class

Friend NotInheritable Class NodoUnario
    Inherits NodoExpresion
    Private ReadOnly operador As Char
    Private ReadOnly hijo As NodoExpresion
    Public Sub New(operador As Char, hijo As NodoExpresion)
        Me.operador = operador
        Me.hijo = hijo
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Dim valor = hijo.Evaluar(ctx)
        Return If(operador = "-"c, -valor, valor)
    End Function
End Class

Friend NotInheritable Class NodoNo
    Inherits NodoExpresion
    Private ReadOnly hijo As NodoExpresion
    Public Sub New(hijo As NodoExpresion)
        Me.hijo = hijo
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Return If(hijo.Evaluar(ctx) = 0D, 1D, 0D)
    End Function
End Class

Friend NotInheritable Class NodoY
    Inherits NodoExpresion
    Private ReadOnly izquierdo As NodoExpresion
    Private ReadOnly derecho As NodoExpresion
    Public Sub New(izquierdo As NodoExpresion, derecho As NodoExpresion)
        Me.izquierdo = izquierdo
        Me.derecho = derecho
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        If izquierdo.Evaluar(ctx) = 0D Then Return 0D
        Return If(derecho.Evaluar(ctx) = 0D, 0D, 1D)
    End Function
End Class

Friend NotInheritable Class NodoO
    Inherits NodoExpresion
    Private ReadOnly izquierdo As NodoExpresion
    Private ReadOnly derecho As NodoExpresion
    Public Sub New(izquierdo As NodoExpresion, derecho As NodoExpresion)
        Me.izquierdo = izquierdo
        Me.derecho = derecho
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        If izquierdo.Evaluar(ctx) <> 0D Then Return 1D
        Return If(derecho.Evaluar(ctx) <> 0D, 1D, 0D)
    End Function
End Class

Friend NotInheritable Class NodoSi
    Inherits NodoExpresion
    Private ReadOnly condicion As NodoExpresion
    Private ReadOnly entonces As NodoExpresion
    Private ReadOnly sino As NodoExpresion
    Public Sub New(condicion As NodoExpresion, entonces As NodoExpresion, sino As NodoExpresion)
        Me.condicion = condicion
        Me.entonces = entonces
        Me.sino = sino
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Return If(condicion.Evaluar(ctx) <> 0D, entonces.Evaluar(ctx), sino.Evaluar(ctx))
    End Function
End Class

Friend NotInheritable Class NodoFuncion
    Inherits NodoExpresion
    Private ReadOnly nombre As String
    Private ReadOnly argumentos As List(Of NodoExpresion)
    Public Sub New(nombre As String, argumentos As List(Of NodoExpresion))
        Me.nombre = nombre
        Me.argumentos = argumentos
    End Sub
    Public Overrides Function Evaluar(ctx As IDictionary(Of String, Decimal)) As Decimal
        Select Case nombre
            Case "abs"
                Return Math.Abs(argumentos(0).Evaluar(ctx))
            Case "round"
                Return MotorPrestamos.Dinero(argumentos(0).Evaluar(ctx))
            Case "floor"
                Return Math.Floor(argumentos(0).Evaluar(ctx))
            Case "ceil"
                Return Math.Ceiling(argumentos(0).Evaluar(ctx))
            Case "min"
                Return Math.Min(argumentos(0).Evaluar(ctx), argumentos(1).Evaluar(ctx))
            Case "max"
                Return Math.Max(argumentos(0).Evaluar(ctx), argumentos(1).Evaluar(ctx))
            Case Else
                Return CDec(Math.Pow(CDbl(argumentos(0).Evaluar(ctx)), CDbl(argumentos(1).Evaluar(ctx))))
        End Select
    End Function
End Class

Public NotInheritable Class Expresion
    Private ReadOnly raiz As NodoExpresion
    Friend Sub New(formula As String, raiz As NodoExpresion)
        Me.Formula = formula
        Me.raiz = raiz
    End Sub
    Public ReadOnly Property Formula As String
    Public Function Evaluar(valores As IDictionary(Of String, Decimal)) As Decimal
        If valores Is Nothing Then Throw New ArgumentException("Falta el contexto de cálculo de la fórmula '" & Formula & "'.")
        Try
            Return raiz.Evaluar(valores)
        Catch ex As DivideByZeroException
            Throw New ArgumentException("División por cero al evaluar la fórmula '" & Formula & "'.")
        Catch ex As OverflowException
            Throw New ArgumentException("La fórmula '" & Formula & "' produjo un resultado fuera de rango.")
        End Try
    End Function
End Class

Public NotInheritable Class SistemaExpresiones
    Private ReadOnly orden As List(Of String)
    Private ReadOnly definiciones As Dictionary(Of String, Expresion)
    Friend ReadOnly ExpresionInteres As Expresion
    Friend ReadOnly ExpresionCapital As Expresion
    Friend ReadOnly ExpresionSaldo As Expresion
    Friend Sub New(orden As List(Of String), definiciones As Dictionary(Of String, Expresion), expresionInteres As Expresion, expresionCapital As Expresion, expresionSaldo As Expresion)
        Me.orden = orden
        Me.definiciones = definiciones
        Me.ExpresionInteres = expresionInteres
        Me.ExpresionCapital = expresionCapital
        Me.ExpresionSaldo = expresionSaldo
    End Sub
    Public ReadOnly Property VariablesDefinidas As String()
        Get
            Return orden.ToArray()
        End Get
    End Property
    Public Function CalcularCuota(c As CondicionesPrestamo, i As Decimal, k As Integer, saldo As Decimal, interesAcum As Decimal, capitalAcum As Decimal) As CuotaPlan
        If c Is Nothing Then Throw New ArgumentException("Condiciones de cálculo no válidas.")
        Dim ctx As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase) From {
            {"p", c.Monto}, {"i", i}, {"r", c.TasaAnual}, {"n", CDec(c.CantidadCuotas)},
            {"k", CDec(k)}, {"saldo", saldo}, {"f", c.PeriodosAnuales},
            {"interes_acum", interesAcum}, {"capital_acum", capitalAcum}}
        For Each nombre In orden
            ctx(nombre) = definiciones(nombre).Evaluar(ctx)
        Next
        Dim interes = MotorPrestamos.Dinero(ExpresionInteres.Evaluar(ctx))
        If interes < 0D Then Throw New ArgumentException("La fórmula de interes produjo un valor negativo en la cuota " & k & ".")
        ctx("interes") = interes
        Dim capital = MotorPrestamos.Dinero(ExpresionCapital.Evaluar(ctx))
        If capital < 0D Then Throw New ArgumentException("La fórmula de capital produjo un valor negativo en la cuota " & k & ".")
        ctx("capital") = capital
        Dim nuevoSaldo = If(ExpresionSaldo Is Nothing, saldo - capital, MotorPrestamos.Dinero(ExpresionSaldo.Evaluar(ctx)))
        If nuevoSaldo < 0D Then Throw New ArgumentException("La fórmula de saldo produjo un valor negativo en la cuota " & k & ".")
        Return New CuotaPlan With {.Capital = capital, .Interes = interes, .Saldo = nuevoSaldo}
    End Function
End Class

Public NotInheritable Class ExpresionFinanciera
    Public Const MaximoCaracteres As Integer = 2000
    Public Const MaximoTokens As Integer = 200
    Public Const MaximoProfundidad As Integer = 30
    Public Const MaximoDefiniciones As Integer = 30
    Public Shared ReadOnly VariablesBase As String() = {"p", "i", "r", "n", "k", "saldo", "f", "interes_acum", "capital_acum"}
    Public Shared ReadOnly Funciones As String() = {"if", "min", "max", "abs", "round", "floor", "ceil", "pow", "and", "or", "not"}
    Private Shared ReadOnly Reservadas As New HashSet(Of String)(VariablesBase.Concat(Funciones).Concat({"interes", "capital"}), StringComparer.OrdinalIgnoreCase)

    Private Sub New()
    End Sub

    Public Shared Function Analizar(texto As String, Optional permitidas As ICollection(Of String) = Nothing) As Expresion
        If String.IsNullOrWhiteSpace(texto) Then Throw New ArgumentException("La fórmula está vacía.")
        Dim raiz As NodoExpresion = New AnalizadorExpresion(texto).Parsear(permitidas)
        Return New Expresion(texto.Trim(), raiz)
    End Function

    Public Shared Function Evaluar(texto As String, valores As IDictionary(Of String, Decimal)) As Decimal
        Return Analizar(texto).Evaluar(valores)
    End Function

    Public Shared Function Construir(definiciones As String, exprInteres As String, exprCapital As String, exprSaldo As String) As SistemaExpresiones
        If String.IsNullOrWhiteSpace(exprInteres) Then Throw New ArgumentException("Debe indicar la fórmula de interes.")
        If String.IsNullOrWhiteSpace(exprCapital) Then Throw New ArgumentException("Debe indicar la fórmula de capital.")
        Dim orden As New List(Of String)()
        Dim defs As New Dictionary(Of String, Expresion)(StringComparer.OrdinalIgnoreCase)
        Dim disponibles As New HashSet(Of String)(VariablesBase, StringComparer.OrdinalIgnoreCase)
        If Not String.IsNullOrWhiteSpace(definiciones) Then
            For Each lineaCruda In definiciones.Replace(vbCr, vbLf).Split(vbLf(0))
                Dim linea = lineaCruda.Trim()
                If linea.Length = 0 OrElse linea.StartsWith("#") Then Continue For
                If orden.Count >= MaximoDefiniciones Then Throw New ArgumentException("Máximo " & MaximoDefiniciones.ToString(CultureInfo.InvariantCulture) & " definiciones de variables.")
                Dim posicion = linea.IndexOf("="c)
                If posicion <= 0 Then Throw New ArgumentException("Definición de variable inválida: use 'nombre = expresión' (ej.: cuota = P*i/(1-(1+i)^(-n))).")
                Dim nombre = linea.Substring(0, posicion).Trim().ToLowerInvariant()
                If Not NombreValido(nombre) Then Throw New ArgumentException("Nombre de variable inválido: '" & nombre & "'. Use de 1 a 30 caracteres, empiece con letra o guion bajo y continúe con letras, números o guion bajo.")
                If Reservadas.Contains(nombre) Then Throw New ArgumentException("La variable '" & nombre & "' no puede llamarse igual que una variable o función del sistema.")
                If defs.ContainsKey(nombre) Then Throw New ArgumentException("La variable '" & nombre & "' está definida dos veces.")
                defs(nombre) = Analizar(linea.Substring(posicion + 1), disponibles)
                orden.Add(nombre)
                disponibles.Add(nombre)
            Next
        End If
        Dim interes = Analizar(exprInteres, disponibles)
        disponibles.Add("interes")
        Dim capital = Analizar(exprCapital, disponibles)
        disponibles.Add("capital")
        Dim saldo = If(String.IsNullOrWhiteSpace(exprSaldo), Nothing, Analizar(exprSaldo, disponibles))
        Return New SistemaExpresiones(orden, defs, interes, capital, saldo)
    End Function

    Private Shared Function NombreValido(nombre As String) As Boolean
        If nombre.Length < 1 OrElse nombre.Length > 30 Then Return False
        Dim primero = nombre(0)
        If Not ((primero >= "a"c AndAlso primero <= "z"c) OrElse primero = "_"c) Then Return False
        For indice As Integer = 1 To nombre.Length - 1
            Dim c = nombre(indice)
            If Not ((c >= "a"c AndAlso c <= "z"c) OrElse (c >= "0"c AndAlso c <= "9"c) OrElse c = "_"c) Then Return False
        Next
        Return True
    End Function
End Class

Friend NotInheritable Class AnalizadorExpresion
    Private ReadOnly tokens As List(Of Token)
    Private indice As Integer
    Private profundidad As Integer

    Public Sub New(texto As String)
        tokens = Tokenizador.Tokenizar(If(texto, ""))
    End Sub

    Public Function Parsear(permitidas As ICollection(Of String)) As NodoExpresion
        Dim raiz = OrExpresion(permitidas)
        If Actual.Tipo <> TipoToken.Fin Then Throw ErroresExpresion.En(Actual.Posicion, "un operador o el final de la fórmula", Describir(Actual))
        Return raiz
    End Function

    Private ReadOnly Property Actual As Token
        Get
            Return tokens(indice)
        End Get
    End Property

    Private Function EsIdentificador(texto As String) As Boolean
        Return Actual.Tipo = TipoToken.Identificador AndAlso Actual.Texto = texto
    End Function

    Private Function TomarIdentificador(texto As String) As Boolean
        If Not EsIdentificador(texto) Then Return False
        indice += 1
        Return True
    End Function

    Private Function TomarSimbolo(texto As String) As Boolean
        If Actual.Tipo <> TipoToken.Simbolo OrElse Actual.Texto <> texto Then Return False
        indice += 1
        Return True
    End Function

    Private Sub Profundizar()
        profundidad += 1
        If profundidad > ExpresionFinanciera.MaximoProfundidad Then Throw New ArgumentException("Fórmula demasiado anidada: máximo " & ExpresionFinanciera.MaximoProfundidad.ToString(CultureInfo.InvariantCulture) & " niveles de paréntesis, signos menos o palabras 'not'.")
    End Sub

    Private Shared Function Describir(t As Token) As String
        If t.Tipo = TipoToken.Fin Then Return "el final de la fórmula"
        If t.Tipo = TipoToken.Simbolo AndAlso t.Texto = "," Then Return "la coma; use punto (.) como separador decimal (ej.: 0.05)"
        Return t.Texto
    End Function

    Private Function OrExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim izquierdo = YExpresion(permitidas)
        While EsIdentificador("or")
            indice += 1
            izquierdo = New NodoO(izquierdo, YExpresion(permitidas))
        End While
        Return izquierdo
    End Function

    Private Function YExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim izquierdo = NoExpresion(permitidas)
        While EsIdentificador("and")
            indice += 1
            izquierdo = New NodoY(izquierdo, NoExpresion(permitidas))
        End While
        Return izquierdo
    End Function

    Private Function NoExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        If EsIdentificador("not") Then
            indice += 1
            Profundizar()
            Dim hijo = NoExpresion(permitidas)
            profundidad -= 1
            Return New NodoNo(hijo)
        End If
        Return ComparacionExpresion(permitidas)
    End Function

    Private Function ComparacionExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim izquierdo = SumaExpresion(permitidas)
        If Actual.Tipo = TipoToken.Simbolo AndAlso {"=", "<>", "!=", "<", ">", "<=", ">="}.Contains(Actual.Texto) Then
            Dim operador = tokens(indice).Texto
            indice += 1
            Return New NodoComparacion(operador, izquierdo, SumaExpresion(permitidas))
        End If
        Return izquierdo
    End Function

    Private Function SumaExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim izquierdo = ProductoExpresion(permitidas)
        While Actual.Tipo = TipoToken.Simbolo AndAlso (Actual.Texto = "+" OrElse Actual.Texto = "-")
            Dim operador = tokens(indice).Texto(0)
            indice += 1
            izquierdo = New NodoAritmetico(operador, izquierdo, ProductoExpresion(permitidas))
        End While
        Return izquierdo
    End Function

    Private Function ProductoExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim izquierdo = UnarioExpresion(permitidas)
        While Actual.Tipo = TipoToken.Simbolo AndAlso (Actual.Texto = "*" OrElse Actual.Texto = "/")
            Dim operador = tokens(indice).Texto(0)
            indice += 1
            izquierdo = New NodoAritmetico(operador, izquierdo, UnarioExpresion(permitidas))
        End While
        Return izquierdo
    End Function

    Private Function UnarioExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        If Actual.Tipo = TipoToken.Simbolo AndAlso (Actual.Texto = "-" OrElse Actual.Texto = "+") Then
            Dim operador = tokens(indice).Texto(0)
            indice += 1
            Profundizar()
            Dim hijo = UnarioExpresion(permitidas)
            profundidad -= 1
            Return New NodoUnario(operador, hijo)
        End If
        Return PotenciaExpresion(permitidas)
    End Function

    Private Function PotenciaExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim base = PrimarioExpresion(permitidas)
        If Not TomarSimbolo("^") Then Return base
        Return New NodoPotencia(base, UnarioExpresion(permitidas))
    End Function

    Private Function PrimarioExpresion(permitidas As ICollection(Of String)) As NodoExpresion
        Dim t = Actual
        If t.Tipo = TipoToken.Numero Then
            indice += 1
            Return New NodoLiteral(Decimal.Parse(t.Texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))
        End If
        If t.Tipo = TipoToken.Simbolo AndAlso t.Texto = "(" Then
            indice += 1
            Profundizar()
            Dim interior = OrExpresion(permitidas)
            profundidad -= 1
            If Not TomarSimbolo(")") Then Throw ErroresExpresion.En(Actual.Posicion, "cerrar el paréntesis", Describir(Actual))
            Return interior
        End If
        If t.Tipo = TipoToken.Identificador Then
            indice += 1
            If TomarSimbolo("(") Then Return CrearLlamada(t, permitidas)
            If permitidas IsNot Nothing AndAlso Not permitidas.Contains(t.Texto) Then Throw New ArgumentException("Variable desconocida: '" & t.Texto & "'. Defínela antes de usarla o usa una variable del sistema (" & String.Join(", ", ExpresionFinanciera.VariablesBase) & ").")
            Return New NodoVariable(t.Texto)
        End If
        Throw ErroresExpresion.En(t.Posicion, "una expresión", Describir(t))
    End Function

    Private Function CrearLlamada(t As Token, permitidas As ICollection(Of String)) As NodoExpresion
        Dim nombre = t.Texto
        Dim argumentos As New List(Of NodoExpresion)()
        If Not TomarSimbolo(")") Then
            argumentos.Add(OrExpresion(permitidas))
            While TomarSimbolo(",")
                argumentos.Add(OrExpresion(permitidas))
            End While
            If Not TomarSimbolo(")") Then Throw ErroresExpresion.En(Actual.Posicion, "cerrar los argumentos de " & nombre, Describir(Actual))
        End If
        Select Case nombre
            Case "if"
                ExigirArgumentos(nombre, argumentos, 3)
                Return New NodoSi(argumentos(0), argumentos(1), argumentos(2))
            Case "and"
                ExigirArgumentos(nombre, argumentos, 2)
                Return New NodoY(argumentos(0), argumentos(1))
            Case "or"
                ExigirArgumentos(nombre, argumentos, 2)
                Return New NodoO(argumentos(0), argumentos(1))
            Case "not"
                ExigirArgumentos(nombre, argumentos, 1)
                Return New NodoNo(argumentos(0))
            Case "min", "max", "pow"
                ExigirArgumentos(nombre, argumentos, 2)
                Return New NodoFuncion(nombre, argumentos)
            Case "abs", "round", "floor", "ceil"
                ExigirArgumentos(nombre, argumentos, 1)
                Return New NodoFuncion(nombre, argumentos)
            Case Else
                Throw New ArgumentException("Función no reconocida: '" & nombre & "'. Funciones disponibles: " & String.Join(", ", ExpresionFinanciera.Funciones) & ".")
        End Select
    End Function

    Private Shared Sub ExigirArgumentos(nombre As String, argumentos As List(Of NodoExpresion), cantidad As Integer)
        If argumentos.Count = cantidad Then Return
        Throw New ArgumentException("La función " & nombre & " requiere " & cantidad.ToString(CultureInfo.InvariantCulture) & " argumento(s) y recibió " & argumentos.Count.ToString(CultureInfo.InvariantCulture) & ".")
    End Sub
End Class

Public NotInheritable Class MetodoExpresion
    Implements IMetodoCalculoPrestamo
    Private ReadOnly sistema As SistemaExpresiones

    Public Sub New(c As CondicionesPrestamo)
        If c Is Nothing Then Throw New ArgumentException("Condiciones de cálculo no válidas.")
        sistema = ExpresionFinanciera.Construir(c.Definiciones, c.ExprInteres, c.ExprCapital, c.ExprSaldo)
    End Sub

    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        Dim saldo = c.Monto
        Dim interesAcum As Decimal = 0D
        Dim capitalAcum As Decimal = 0D
        Dim plan As New List(Of CuotaPlan)()
        For k As Integer = 1 To c.CantidadCuotas
            Dim cuota = sistema.CalcularCuota(c, i, k, saldo, interesAcum, capitalAcum)
            plan.Add(cuota)
            saldo = cuota.Saldo
            interesAcum += cuota.Interes
            capitalAcum += cuota.Capital
        Next
        Return plan
    End Function
End Class
