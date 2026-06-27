//
// Clase para las extensiones                                   (26/jun/26 02.45)
//
// Convertida de VB a C# con Code Converter (VB - C#) v10.0.1.0
//

using System;

namespace ImportadorFotos_Dinamico_WPF
{
    static public class Extensiones
    {

        // Versión simple para pluralizar                            (26/jun/26 02.58)

        /// <summary>
        /// Devuelve el plural del texto indicado, según el valor sea distinto de 1.
        /// </summary>
        /// <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
        /// <param name="singular">La palabra a pluralizar.</param>
        /// <returns>La cadena pluralizada o la indicada si no es plural.</returns>
        /// <remarks>Si la palabra en singular es en mayúsculas se devuelve en mayúsculas.</remarks>
        public static string Plural(this string singular, int n)
        {
            return n.Plural(singular);
        }

        /// <summary>
        /// Devuelve el plural del texto indicado, según el valor sea distinto de 1.
        /// </summary>
        /// <param name="n">El valor a tener en cuenta (será plural si es distinto de 1).</param>
        /// <param name="singular">La palabra a pluralizar.</param>
        /// <returns>La cadena pluralizada o la indicada si no es plural.</returns>
        /// <remarks>Si la palabra en singular es en mayúsculas se devuelve en mayúsculas.</remarks>
        public static string Plural(this int n, string singular)
        {
            bool mayusculas = (singular == singular.ToUpper());

            if (n != 1)
            {
                singular += "s";
            }
            if (mayusculas)
            {
                return singular.ToUpper();
            }
            return singular;
        }
    }
}