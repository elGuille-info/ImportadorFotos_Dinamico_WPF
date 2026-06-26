'
' Módulo para las extensiones                                   (26/jun/26 02.45)
'
'

Imports System
Imports System.Runtime.CompilerServices

Module Extensiones

    ' Versión simple para pluralizar                            (26/jun/26 02.58)

    ''' <summary>
    ''' Devuelve el plural del texto indicado, según el valor sea distinto de 1.
    ''' </summary>
    ''' <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
    ''' <param name="singular">La palabra a pluralizar.</param>
    ''' <returns>La cadena pluralizada o la indicada si no es plural.</returns>
    ''' <remarks>Si la palabra en singular es en mayúsculas se devuelve en mayúsculas.</remarks>
    <Extension>
    Public Function Plural(singular As String, n As Integer) As String
        Return Plural(n, singular)
    End Function

    ''' <summary>
    ''' Devuelve el plural del texto indicado, según el valor sea distinto de 1.
    ''' </summary>
    ''' <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
    ''' <param name="singular">La palabra a pluralizar.</param>
    ''' <returns>La cadena pluralizada o la indicada si no es plural.</returns>
    ''' <remarks>Si la palabra en singular es en mayúsculas se devuelve en mayúsculas.</remarks>
    <Extension>
    Public Function Plural(n As Integer, singular As String) As String
        Dim mayusculas = (singular = singular.ToUpper())

        If n <> 1 Then
            singular &= "s"
        End If
        If mayusculas Then
            Return singular.ToUpper()
        End If
        Return singular
    End Function

End Module
