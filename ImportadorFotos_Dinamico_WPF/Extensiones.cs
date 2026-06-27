using System;
using System.Globalization;
using System.Text;

namespace ImportadorFotos_Dinamico_WPF
{
    /// <summary>
    /// Métodos de extensión auxiliares con pluralización mejorada para español.
    /// Reglas implementadas (versión simplificada):
    /// - Si termina en vocal -> añadir "s".
    /// - Si termina en consonante distinta de 'z' -> añadir "es".
    /// - Si termina en 'z' -> cambiar 'z' por 'c' y añadir "es".
    /// - Si ya termina en 's' (insensible a mayúsculas) -> se considera ya plural/invariable y no se modifica.
    /// Nota: la normalización de tildes (por ejemplo, balón -> balones) se maneja al detectar la letra base
    /// (sin diacríticos) para elegir la regla, pero no se intenta reasignar tildes automáticamente en todos los casos.
    /// </summary>
    public static class Extensiones
    {
        /// <summary>
        /// Devuelve el plural de la palabra <paramref name="singular"/> según el valor <paramref name="count"/>.
        /// Si <paramref name="count"/> != 1, se aplican reglas simplificadas de pluralización en español.
        /// Retorna null si la entrada es null.
        /// </summary>
        /// <param name="singular">Palabra en singular.</param>
        /// <param name="count">Cantidad que determina si debe usarse plural.</param>
        /// <returns>La palabra pluralizada o la original.</returns>
        public static string Plural(this string singular, int count)
        {
            if (singular is null) return null;
            if (singular.Length == 0) return singular;

            var culture = CultureInfo.CurrentCulture;

            // Detectar si la entrada es todo en mayúsculas (para preservar el estilo)
            bool wasAllUpper = string.Equals(singular, singular.ToUpper(culture), StringComparison.Ordinal);

            // Trabajamos con la palabra recortada en los extremos para aplicar las reglas
            string trimmed = singular.Trim();
            if (trimmed.Length == 0) return singular; // solo espacios

            // Si no hay que pluralizar, devolvemos la original tal cual (manteniendo espacios, si se desean preservar)
            if (count == 1) return singular;

            // Si ya termina en 's' (insensible a mayúsc/minúsc) consideramos que no añadir sufijo
            if (trimmed.EndsWith("s", true, culture))
            {
                return wasAllUpper ? singular.ToUpper(culture) : singular;
            }

            // Para decidir la regla analizamos la letra base (sin diacríticos) del último carácter
            char lastBase = GetLastBaseLetter(trimmed, culture);

            string resultBody;

            if (lastBase == 'z')
            {
                // sustituir z/Z por c/C y añadir "es"
                char lastOrig = trimmed[trimmed.Length - 1];
                bool lastIsUpper = char.IsUpper(lastOrig);
                char replacement = lastIsUpper ? 'C' : 'c';
                resultBody = trimmed.Substring(0, trimmed.Length - 1) + replacement + "es";
            }
            else if (IsVowel(lastBase))
            {
                // termina en vocal -> añadir 's'
                resultBody = trimmed + "s";
            }
            else
            {
                // consonante distinta de z -> añadir 'es'
                resultBody = trimmed + "es";
            }

            // Preservar mayúsculas totales si correspondía
            if (wasAllUpper)
            {
                resultBody = resultBody.ToUpper(culture);
            }

            // Devolvemos el resultado limpio (sin preservar espacios externos)
            return resultBody;
        }

        /// <summary>
        /// Sobrecarga para permitir la llamada desde int:  n.Plural("item")
        /// </summary>
        public static string Plural(this int count, string singular)
        {
            return singular.Plural(count);
        }

        // ---------- Helpers ----------

        // Devuelve la última letra base de la cadena (sin diacríticos), en minúscula cultural.
        private static char GetLastBaseLetter(string s, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(s)) return '\0';
            // Tomamos el último carácter visible
            char last = s[s.Length - 1];

            // Convertir el carácter a su forma sin diacríticos mediante normalización
            string lastStr = last.ToString();
            string decomp = lastStr.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char ch in decomp)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            string recomposed = sb.ToString().Normalize(NormalizationForm.FormC);
            if (recomposed.Length == 0) return char.ToLowerInvariant(last);
            return char.ToLower(recomposed[recomposed.Length - 1], culture);
        }

        private static bool IsVowel(char c)
        {
            return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u';
        }
    }
}
