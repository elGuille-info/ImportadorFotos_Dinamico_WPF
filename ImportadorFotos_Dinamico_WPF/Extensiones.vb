' Comentarios originales, que no sé porqué los quita.           (27/jun/26 22.29)

﻿'
' Módulo para las extensiones                                   (26/jun/26 02.45)
'
' Versión simple para pluralizar                                (26/jun/26 02.58)
'


' Módulo para las extensiones — versión mejorada de pluralización
' Soporta reglas simples:
'  - palabras que terminan en vocal: añade "s"  (foto -> fotos)
'  - palabras que terminan en "z": cambia "z" por "ces" (luz -> luces)
'  - palabras que terminan en consonante (no z): añade "es" (balón -> balones)
' Conserva comportamiento cuando n = 1 y preserva MAYÚSCULAS completas.
'
Imports System
Imports System.Runtime.CompilerServices
Imports System.Globalization
Imports System.Text

Module Extensiones

    ''' <summary>
    ''' Devuelve el plural del texto indicado, según el valor sea distinto de 1.
    ''' Ejemplo: "foto".Plural(2) -> "fotos"
    ''' </summary>
    ''' <param name="singular">La palabra a pluralizar (puede ser Nothing).</param>
    ''' <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
    ''' <returns>La cadena pluralizada o la indicada si no es plural; devuelve Nothing si input es Nothing.</returns>
    <Extension>
    Public Function Plural(singular As String, n As Integer) As String
        ' Reusar la implementación centrada en el entero
        Return Plural(n, singular)
    End Function

    ''' <summary>
    ''' Devuelve el plural del texto indicado, según el valor sea distinto de 1.
    ''' Permite llamar: 2.Plural("foto")
    ''' </summary>
    ''' <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
    ''' <param name="singular">La palabra a pluralizar (puede ser Nothing).</param>
    ''' <returns>La cadena pluralizada o la indicada si no es plural; devuelve Nothing si input es Nothing.</returns>
    <Extension>
    Public Function Plural(n As Integer, singular As String) As String
        ' Esta comparación no me gusta, no quiero que devuelva Nothing.
        'If singular Is Nothing Then
        '    Return Nothing
        'End If

        ' Mi propuesta:                                         (27/jun/26 22.27)
        If String.IsNullOrWhiteSpace(singular) Then
            Return ""
        End If

        ' Mantener espacios exteriores (si el usuario los usa)
        Dim leading = String.Empty
        Dim trailing = String.Empty
        Dim core = singular

        ' Extraer espacios prefijo/sufijo para devolverlos igual
        Dim iStart = 0
        While iStart < core.Length AndAlso Char.IsWhiteSpace(core(iStart))
            iStart += 1
        End While
        Dim iEnd = core.Length - 1
        While iEnd >= 0 AndAlso Char.IsWhiteSpace(core(iEnd))
            iEnd -= 1
        End While

        If iStart > 0 Then
            leading = core.Substring(0, iStart)
        End If
        If iEnd < core.Length - 1 Then
            trailing = core.Substring(iEnd + 1)
        End If

        If iEnd < iStart Then
            ' La cadena es sólo espacios
            Return singular
        End If

        core = core.Substring(iStart, iEnd - iStart + 1)

        ' Si no es plural, devolver la original (manteniendo espacios)
        If n = 1 Then
            Return $"{leading}{core}{trailing}"
        End If

        ' Detectar si la palabra está en MAYÚSCULAS completas
        Dim isAllUpper = core.Equals(core.ToUpperInvariant(), StringComparison.Ordinal)

        ' Regla simple de pluralización
        Dim pluralCore As String
        If core.Length = 0 Then
            pluralCore = core
        Else
            Dim lastChar As Char = core(core.Length - 1)
            ' Vocales incluyendo tildes
            Dim vowels As String = "aeiouáéíóúAEIOUÁÉÍÓÚ"
            If vowels.IndexOf(lastChar) >= 0 Then
                pluralCore = core & "s"
            ElseIf lastChar = "z"c OrElse lastChar = "Z"c Then
                ' luz -> luces  (z -> ces)
                pluralCore = core.Substring(0, core.Length - 1) & "ces"
            Else
                ' consonante -> añadir "es"
                pluralCore = core & "es"
            End If
        End If

        If isAllUpper Then
            pluralCore = pluralCore.ToUpperInvariant()
        End If

        Return $"{leading}{pluralCore}{trailing}"
    End Function

End Module