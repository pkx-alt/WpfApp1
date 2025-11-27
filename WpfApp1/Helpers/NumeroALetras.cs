// --- Helpers/NumeroALetras.cs ---

using System;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace OrySiPOS.Helpers
{
    public static class NumeroALetras
    {
        // Sobrecargas públicas: acepta decimal (para compatibilidad) y string (para cantidades muy largas)
        public static string Convertir(decimal numero)
        {
            // Convertimos a string con punto como separador
            string s = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Convertir(s);
        }

        public static string Convertir(string numeroStr)
        {
            if (string.IsNullOrWhiteSpace(numeroStr))
                throw new ArgumentException("El número no puede ser vacío.");

            // Normalizar entrada: quitar espacios, aceptar coma o punto como separador decimal.
            numeroStr = numeroStr.Trim();
            numeroStr = numeroStr.Replace(" ", "");

            // Si viene con símbolo de moneda, quitarlo
            numeroStr = Regex.Replace(numeroStr, @"[^\d\.,\-]", "");

            // Detectar signo
            bool negativo = numeroStr.StartsWith("-");
            if (negativo) numeroStr = numeroStr.Substring(1);

            // Reemplazar comas por punto (solo la última coma/punto es separador decimal)
            int lastComma = numeroStr.LastIndexOf(',');
            int lastDot = numeroStr.LastIndexOf('.');
            int sepIndex = Math.Max(lastComma, lastDot);

            string intPartStr;
            string fracPartStr = "";

            if (sepIndex >= 0)
            {
                intPartStr = numeroStr.Substring(0, sepIndex);
                fracPartStr = numeroStr.Substring(sepIndex + 1);
            }
            else
            {
                intPartStr = numeroStr;
            }

            // Eliminar separadores de miles dentro de la parte entera (puntos o comas usados como miles)
            intPartStr = intPartStr.Replace(".", "").Replace(",", "");

            if (string.IsNullOrEmpty(intPartStr)) intPartStr = "0";

            // Redondeo de centavos con la parte fraccional como string (2 decimales)
            int centavos = ObtenerCentavosConRedondeo(ref intPartStr, fracPartStr);

            // Parsear la parte entera a BigInteger
            BigInteger parteEntera;
            if (!BigInteger.TryParse(intPartStr, out parteEntera))
                throw new ArgumentException("Número entero inválido.");

            if (parteEntera < 0) // seguridad
                throw new ArgumentException("Número inválido.");

            // Construir literal
            string literal;
            if (parteEntera == 0)
                literal = "CERO";
            else
                literal = ConvertirBigIntegerAletras(parteEntera);

            // Moneda
            string moneda = (parteEntera == 1) ? "PESO" : "PESOS";

            // Centavos formato 00/100 M.N.
            string parteDecimal = $" {centavos:00}/100 M.N.";

            // Signo negativo
            string prefijo = negativo ? "MENOS " : "";

            return $"({prefijo}{literal} {moneda}{parteDecimal})";
        }

        // --- Helpers internos ---

        // Convierte parte entera (BigInteger) a palabras en español (mayúsculas)
        private static string ConvertirBigIntegerAletras(BigInteger numero)
        {
            if (numero == 0) return "CERO";

            // Arrays de nombres para las potencias (escala larga). Index 0 -> unidades, 1 -> miles, 2 -> millones (10^6), 3 -> mil millones (10^9), 4 -> billones (10^12), ...
            string[] nombresEvenSingular = {
                "", "", "MILLÓN", "BILLÓN", "TRILLÓN", "CUATRILLÓN", "QUINTILLÓN", "SEXTILLÓN",
                "SEPTILLÓN", "OCTILLÓN", "NONILLÓN", "DECILLÓN", "UNDECILLÓN", "DUODECILLÓN"
            };
            string[] nombresEvenPlural = {
                "", "", "MILLONES", "BILLONES", "TRILLONES", "CUATRILLONES", "QUINTILLONES", "SEXTILLONES",
                "SEPTILLONES", "OCTILLONES", "NONILLONES", "DECILLONES", "UNDECILLONES", "DUODECILLONES"
            };

            // Extraer grupos de 3 dígitos (de derecha a izquierda)
            var grupos = new System.Collections.Generic.List<int>();
            BigInteger temp = numero;
            while (temp > 0)
            {
                BigInteger rem;
                temp = BigInteger.DivRem(temp, 1000, out rem);
                grupos.Add((int)rem); // rem está en 0..999
            }

            var sb = new StringBuilder();
            for (int idx = grupos.Count - 1; idx >= 0; idx--)
            {
                int valor = grupos[idx];
                if (valor == 0) continue;

                if (idx == 0)
                {
                    // unidades
                    sb.Append(ConvertirCentenas(valor));
                }
                else if (idx == 1)
                {
                    // miles
                    if (valor == 1)
                        sb.Append("MIL ");
                    else
                        sb.Append($"{ConvertirCentenas(valor)}MIL ");
                }
                else if (idx >= 2)
                {
                    if (idx % 2 == 0)
                    {
                        // índice par: corresponde a MILLÓN, BILLÓN, TRILLÓN, ...
                        if (idx / 2 < nombresEvenSingular.Length)
                        {
                            if (valor == 1)
                                sb.Append($"UN {nombresEvenSingular[idx / 2]} ");
                            else
                                sb.Append($"{ConvertirCentenas(valor)}{nombresEvenPlural[idx / 2]} ");
                        }
                        else
                        {
                            // Si se sale del array, construir con patrón genérico
                            if (valor == 1)
                                sb.Append($"UN 10^{idx * 3} ");
                            else
                                sb.Append($"{ConvertirCentenas(valor)}10^{idx * 3} ");
                        }
                    }
                    else
                    {
                        // índice impar >=3: "X MIL <nombrePluralEvenPrev>"
                        int evenIndex = (idx - 1) / 1; // índice par previo = idx-1
                        int evenArrayIndex = (idx - 1); // corresponde a potencia anterior (par)
                        int namesIndex = idx - 1; // we want the even index (idx-1)
                        int evenPos = idx - 1; // but for our nombresEvenPlural mapping: mapping is (idx even -> (idx)/2 index)
                        int mapping = (idx - 1) / 2;
                        if (mapping < nombresEvenPlural.Length)
                        {
                            if (valor == 1)
                                sb.Append($"MIL {nombresEvenPlural[mapping]} ");
                            else
                                sb.Append($"{ConvertirCentenas(valor)}MIL {nombresEvenPlural[mapping]} ");
                        }
                        else
                        {
                            if (valor == 1)
                                sb.Append($"MIL 10^{idx * 3} ");
                            else
                                sb.Append($"{ConvertirCentenas(valor)}MIL 10^{idx * 3} ");
                        }
                    }
                }
            }

            return sb.ToString().Trim();
        }

        // Convierte 1..999 a letras (termina con espacio)
        private static string ConvertirCentenas(int n)
        {
            if (n == 0) return "";
            if (n < 0 || n > 999) return "";

            string[] Centenas = {
                "", "CIENTO ", "DOSCIENTOS ", "TRESCIENTOS ", "CUATROCIENTOS ",
                "QUINIENTOS ", "SEISCIENTOS ", "SETECIENTOS ", "OCHOCIENTOS ", "NOVECIENTOS "
            };

            if (n == 100) return "CIEN ";

            int centenas = n / 100;
            int resto = n % 100;

            string resultado = "";
            if (centenas > 0)
                resultado += Centenas[centenas];

            resultado += ConvertirDecenas(resto);

            return resultado;
        }

        private static string ConvertirDecenas(int n)
        {
            string[] Unidades = {
                "", "UN ", "DOS ", "TRES ", "CUATRO ", "CINCO ", "SEIS ", "SIETE ", "OCHO ", "NUEVE "
            };
            string[] DiezADiecinueve = {
                "DIEZ ", "ONCE ", "DOCE ", "TRECE ", "CATORCE ", "QUINCE ", "DIECISEIS ", "DIECISIETE ", "DIECIOCHO ", "DIECINUEVE "
            };
            string[] Decenas = {
                "", "", "VEINTE ", "TREINTA ", "CUARENTA ", "CINCUENTA ", "SESENTA ", "SETENTA ", "OCHENTA ", "NOVENTA "
            };

            if (n == 0) return "";
            if (n < 10) return Unidades[n];
            if (n < 20) return DiezADiecinueve[n - 10];
            if (n < 30)
            {
                // 20..29 -> VEINTI + unidad (sin espacio si hay unidad)
                if (n == 20) return "VEINTE ";
                int unidad = n % 10;
                // Muchos estilos escriben "VEINTIUNO" sin espacio; aquí usamos "VEINTI" + "UN " -> "VEINTIUN "
                return "VEINTI" + Unidades[unidad].TrimEnd() + " ";
            }

            int decena = n / 10;
            int unidad2 = n % 10;

            if (unidad2 == 0)
                return Decenas[decena];

            return Decenas[decena] + "Y " + Unidades[unidad2];
        }

        // Extrae centavos de la parte fraccional con redondeo (modifica intPartStr si hay acarreo)
        private static int ObtenerCentavosConRedondeo(ref string intPartStr, string fracPartStr)
        {
            // Normalizar
            if (fracPartStr == null) fracPartStr = "";
            fracPartStr = Regex.Replace(fracPartStr, @"\s+", "");

            // Solo dígitos
            fracPartStr = Regex.Replace(fracPartStr, @"[^\d]", "");

            if (fracPartStr.Length == 0) return 0;

            // Queremos redondear a 2 decimales
            // Si hay menos de 2 digitos, rellenar
            if (fracPartStr.Length == 1) fracPartStr = fracPartStr + "0";

            if (fracPartStr.Length == 2)
            {
                return int.Parse(fracPartStr.Substring(0, 2));
            }
            else
            {
                // >=3 dígitos: mirar tercer dígito para redondeo
                int tercer = int.Parse(fracPartStr.Substring(2, 1));
                int dos = int.Parse(fracPartStr.Substring(0, 2));
                if (tercer >= 5)
                {
                    dos += 1;
                    if (dos == 100)
                    {
                        // acarreo a la parte entera: incrementar intPartStr en 1
                        BigInteger partsInt;
                        if (!BigInteger.TryParse(intPartStr, out partsInt)) partsInt = 0;
                        partsInt += 1;
                        intPartStr = partsInt.ToString();
                        dos = 0;
                    }
                }
                return dos;
            }
        }
    }
}
