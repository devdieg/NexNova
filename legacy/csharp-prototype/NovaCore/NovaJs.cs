using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaJs
    {
        #region Campos
        private Dictionary<string, object> globalVariables = new Dictionary<string, object>();
        private Dictionary<string, Func<object[], object>> globalFunctions = new Dictionary<string, Func<object[], object>>();
        private NovaDom currentDom;
        #endregion

        #region Eventos
        public event EventHandler<ScriptExecutedEventArgs> ScriptExecuted;
        #endregion

        #region Constructor
        public NovaJs()
        {
            InitializeGlobalObjects();
        }
        #endregion

        #region Inicialización
        private void InitializeGlobalObjects()
        {
            // Objeto console
            globalFunctions["console.log"] = args =>
            {
                string message = string.Join(" ", args.Select(a => a?.ToString() ?? "null"));
                Console.WriteLine($"[JS Console] {message}");
                return null;
            };

            globalFunctions["console.error"] = args =>
            {
                string message = string.Join(" ", args.Select(a => a?.ToString() ?? "null"));
                Console.WriteLine($"[JS Error] {message}");
                return null;
            };

            globalFunctions["console.warn"] = args =>
            {
                string message = string.Join(" ", args.Select(a => a?.ToString() ?? "null"));
                Console.WriteLine($"[JS Warn] {message}");
                return null;
            };

            // Objeto document
            globalFunctions["document.getElementById"] = args =>
            {
                if (args.Length == 0 || currentDom == null)
                    return null;

                string id = args[0]?.ToString();
                if (string.IsNullOrEmpty(id))
                    return null;

                var element = currentDom.GetElementById(id);
                return CreateJsElement(element);
            };

            globalFunctions["document.querySelector"] = args =>
            {
                if (args.Length == 0 || currentDom == null)
                    return null;

                string selector = args[0]?.ToString();
                if (string.IsNullOrEmpty(selector))
                    return null;

                var element = currentDom.QuerySelector(selector);
                return CreateJsElement(element);
            };

            globalFunctions["document.write"] = args =>
            {
                if (args.Length == 0 || currentDom == null)
                    return null;

                string content = args[0]?.ToString() ?? "";
                // Nota: En un navegador real, esto reemplaza todo el documento
                // Aquí solo lo registramos
                Console.WriteLine($"[JS document.write] {content}");
                return null;
            };

            // Objeto window
            globalFunctions["window.alert"] = args =>
            {
                if (args.Length == 0)
                    return null;

                string message = args[0]?.ToString() ?? "";
                Console.WriteLine($"[JS Alert] {message}");
                // En una implementación real, esto mostraría un diálogo modal
                return null;
            };

            globalFunctions["window.setTimeout"] = args =>
            {
                if (args.Length < 2)
                    return null;

                var function = args[0] as Func<object[], object>;
                int delay = Convert.ToInt32(args[1]);

                if (function != null)
                {
                    Task.Delay(delay).ContinueWith(_ =>
                    {
                        var functionArgs = args.Length > 2 ? args.Skip(2).ToArray() : new object[0];
                        function(functionArgs);
                    });
                }

                return null;
            };

            globalFunctions["window.location.href"] = args =>
            {
                // Getter
                if (args.Length == 0)
                    return currentDom?.CurrentUrl ?? "";

                // Setter - navegar a nueva URL
                if (args.Length == 1 && currentDom != null)
                {
                    string url = args[0]?.ToString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        // Disparar evento de navegación
                        // En una implementación completa, esto navegaría realmente
                        Console.WriteLine($"[JS Navigation] {url}");
                    }
                }

                return null;
            };

            // Objeto Math
            globalFunctions["Math.random"] = args => new Random().NextDouble();
            globalFunctions["Math.floor"] = args => args.Length > 0 ? Math.Floor(Convert.ToDouble(args[0])) : 0.0;
            globalFunctions["Math.ceil"] = args => args.Length > 0 ? Math.Ceiling(Convert.ToDouble(args[0])) : 0.0;
            globalFunctions["Math.round"] = args => args.Length > 0 ? Math.Round(Convert.ToDouble(args[0])) : 0.0;
            globalFunctions["Math.max"] = args => args.Length > 0 ? args.Max(a => Convert.ToDouble(a)) : double.NaN;
            globalFunctions["Math.min"] = args => args.Length > 0 ? args.Min(a => Convert.ToDouble(a)) : double.NaN;

            // Objeto Date
            globalFunctions["Date.now"] = args => DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // Funciones globales
            globalFunctions["parseInt"] = args => args.Length > 0 ? int.Parse(args[0].ToString()) : 0;
            globalFunctions["parseFloat"] = args => args.Length > 0 ? float.Parse(args[0].ToString()) : 0f;
            globalFunctions["isNaN"] = args => args.Length > 0 && double.IsNaN(Convert.ToDouble(args[0]));
            globalFunctions["encodeURIComponent"] = args => args.Length > 0 ? Uri.EscapeDataString(args[0].ToString()) : "";
            globalFunctions["decodeURIComponent"] = args => args.Length > 0 ? Uri.UnescapeDataString(args[0].ToString()) : "";
        }
        #endregion

        #region Métodos Públicos
        public async Task<string> ExecuteAsync(string script, NovaDom dom)
        {
            try
            {
                if (string.IsNullOrEmpty(script))
                    return "";

                currentDom = dom;

                // Limpiar script
                script = script.Trim();

                // Remover etiquetas <script> si existen
                if (script.StartsWith("<script"))
                {
                    script = Regex.Replace(script, @"<script[^>]*>", "", RegexOptions.IgnoreCase);
                    script = Regex.Replace(script, @"</script>", "", RegexOptions.IgnoreCase);
                }

                // Ejecutar script
                var result = ExecuteScript(script);

                // Disparar evento
                string resultStr = result?.ToString() ?? "";
                ScriptExecuted?.Invoke(this, new ScriptExecutedEventArgs(script, resultStr));

                return resultStr;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JS Execution error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
            finally
            {
                currentDom = null;
            }
        }

        public void SetGlobalVariable(string name, object value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                globalVariables[name] = value;
            }
        }

        public object GetGlobalVariable(string name)
        {
            return globalVariables.ContainsKey(name) ? globalVariables[name] : null;
        }

        public void RegisterFunction(string name, Func<object[], object> function)
        {
            if (!string.IsNullOrEmpty(name) && function != null)
            {
                globalFunctions[name] = function;
            }
        }
        #endregion

        #region Ejecución de Scripts
        private object ExecuteScript(string script)
        {
            // Análisis muy básico de JavaScript
            // Esto es un intérprete extremadamente simplificado

            // Variables locales
            var localVariables = new Dictionary<string, object>();

            // Dividir en líneas
            var lines = script.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);

            object lastResult = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                lastResult = ExecuteLine(trimmedLine, localVariables);
            }

            return lastResult;
        }

        private object ExecuteLine(string line, Dictionary<string, object> localVariables)
        {
            // Comentarios
            if (line.StartsWith("//") || line.StartsWith("/*"))
                return null;

            // Declaración de variable (var, let, const)
            if (Regex.IsMatch(line, @"^\s*(var|let|const)\s+"))
            {
                return ExecuteVariableDeclaration(line, localVariables);
            }

            // Asignación
            if (line.Contains("="))
            {
                return ExecuteAssignment(line, localVariables);
            }

            // Llamada a función
            if (line.Contains("(") && line.Contains(")"))
            {
                return ExecuteFunctionCall(line, localVariables);
            }

            // Declaración de función
            if (line.StartsWith("function "))
            {
                return ExecuteFunctionDeclaration(line, localVariables);
            }

            // Retorno
            if (line.StartsWith("return "))
            {
                return ExecuteReturn(line.Substring(7).Trim(), localVariables);
            }

            // If statement
            if (line.StartsWith("if "))
            {
                return ExecuteIfStatement(line, localVariables);
            }

            // For loop
            if (line.StartsWith("for "))
            {
                return ExecuteForLoop(line, localVariables);
            }

            // While loop
            if (line.StartsWith("while "))
            {
                return ExecuteWhileLoop(line, localVariables);
            }

            // Expresión simple
            return EvaluateExpression(line, localVariables);
        }

        private object ExecuteVariableDeclaration(string line, Dictionary<string, object> localVariables)
        {
            // Extraer nombre y valor
            var match = Regex.Match(line, @"(var|let|const)\s+(\w+)(?:\s*=\s*(.*))?");
            if (match.Success)
            {
                string varName = match.Groups[2].Value;
                string valueExpr = match.Groups[3].Value;

                object value = null;
                if (!string.IsNullOrEmpty(valueExpr))
                {
                    value = EvaluateExpression(valueExpr, localVariables);
                }

                localVariables[varName] = value;
                return value;
            }

            return null;
        }

        private object ExecuteAssignment(string line, Dictionary<string, object> localVariables)
        {
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                return null;

            string left = parts[0].Trim();
            string right = parts[1].Trim();

            object value = EvaluateExpression(right, localVariables);

            // Asignar a variable
            if (IsValidVariableName(left))
            {
                if (localVariables.ContainsKey(left))
                    localVariables[left] = value;
                else if (globalVariables.ContainsKey(left))
                    globalVariables[left] = value;
                else
                    localVariables[left] = value;
            }

            return value;
        }

        private object ExecuteFunctionCall(string line, Dictionary<string, object> localVariables)
        {
            // Extraer nombre de función y argumentos
            var match = Regex.Match(line, @"(\w+(?:\.\w+)*)\s*\(([^)]*)\)");
            if (!match.Success)
                return null;

            string functionName = match.Groups[1].Value;
            string argsString = match.Groups[2].Value;

            // Parsear argumentos
            var args = ParseArguments(argsString, localVariables);

            // Buscar función
            if (globalFunctions.ContainsKey(functionName))
            {
                return globalFunctions[functionName](args);
            }

            // Buscar en variables locales (funciones definidas por el usuario)
            if (localVariables.ContainsKey(functionName) &&
                localVariables[functionName] is Func<object[], object> userFunction)
            {
                return userFunction(args);
            }

            Console.WriteLine($"Function not found: {functionName}");
            return null;
        }

        private object ExecuteFunctionDeclaration(string line, Dictionary<string, object> localVariables)
        {
            // Extraer nombre y parámetros
            var match = Regex.Match(line, @"function\s+(\w+)\s*\(([^)]*)\)\s*\{?");
            if (!match.Success)
                return null;

            string functionName = match.Groups[1].Value;
            string paramsString = match.Groups[2].Value;

            // Registrar función
            globalFunctions[functionName] = args =>
            {
                // Crear nuevo contexto para la función
                var funcLocals = new Dictionary<string, object>();

                // Asignar parámetros
                var paramNames = paramsString.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
                for (int i = 0; i < Math.Min(paramNames.Length, args.Length); i++)
                {
                    funcLocals[paramNames[i]] = args[i];
                }

                // Ejecutar cuerpo (simplificado)
                // En una implementación real, se necesitaría capturar el cuerpo de la función
                return null;
            };

            return null;
        }

        private object ExecuteReturn(string expression, Dictionary<string, object> localVariables)
        {
            return EvaluateExpression(expression, localVariables);
        }

        private object ExecuteIfStatement(string line, Dictionary<string, object> localVariables)
        {
            // Simplificado - solo evaluar condición
            var match = Regex.Match(line, @"if\s*\(([^)]+)\)");
            if (match.Success)
            {
                string condition = match.Groups[1].Value;
                bool result = EvaluateCondition(condition, localVariables);
                return result;
            }

            return false;
        }

        private object ExecuteForLoop(string line, Dictionary<string, object> localVariables)
        {
            // Simplificado - solo ejecutar cuerpo una vez
            var match = Regex.Match(line, @"for\s*\(([^;]+);([^;]+);([^)]+)\)");
            if (match.Success)
            {
                string init = match.Groups[1].Value;
                string condition = match.Groups[2].Value;
                string increment = match.Groups[3].Value;

                // Ejecutar inicialización
                ExecuteLine(init, localVariables);

                // Evaluar condición
                if (EvaluateCondition(condition, localVariables))
                {
                    // Ejecutar cuerpo (simplificado)
                    // En una implementación real, se necesitaría capturar el cuerpo del loop
                    ExecuteLine(increment, localVariables);
                }
            }

            return null;
        }

        private object ExecuteWhileLoop(string line, Dictionary<string, object> localVariables)
        {
            // Simplificado - solo evaluar condición
            var match = Regex.Match(line, @"while\s*\(([^)]+)\)");
            if (match.Success)
            {
                string condition = match.Groups[1].Value;
                bool result = EvaluateCondition(condition, localVariables);
                return result;
            }

            return false;
        }
        #endregion

        #region Evaluación de Expresiones
        private object EvaluateExpression(string expression, Dictionary<string, object> localVariables)
        {
            expression = expression.Trim();

            // Literales
            if (string.IsNullOrEmpty(expression))
                return null;

            // String literal
            if (expression.StartsWith("\"") && expression.EndsWith("\""))
                return expression.Substring(1, expression.Length - 2);

            if (expression.StartsWith("'") && expression.EndsWith("'"))
                return expression.Substring(1, expression.Length - 2);

            // Number literal
            if (double.TryParse(expression, out double number))
                return number;

            // Boolean literal
            if (expression == "true") return true;
            if (expression == "false") return false;
            if (expression == "null") return null;
            if (expression == "undefined") return null;

            // Variable
            if (IsValidVariableName(expression))
            {
                if (localVariables.ContainsKey(expression))
                    return localVariables[expression];

                if (globalVariables.ContainsKey(expression))
                    return globalVariables[expression];

                return null;
            }

            // Operaciones aritméticas
            if (expression.Contains("+") || expression.Contains("-") ||
                expression.Contains("*") || expression.Contains("/"))
            {
                return EvaluateArithmetic(expression, localVariables);
            }

            // Llamada a función
            if (expression.Contains("(") && expression.Contains(")"))
            {
                return ExecuteFunctionCall(expression, localVariables);
            }

            return expression;
        }

        private double EvaluateArithmetic(string expression, Dictionary<string, object> localVariables)
        {
            try
            {
                // Simplificado - solo operaciones básicas
                expression = expression.Replace(" ", "");

                // Buscar operadores en orden de precedencia
                if (expression.Contains("*"))
                {
                    var parts = expression.Split('*');
                    var left = Convert.ToDouble(EvaluateExpression(parts[0], localVariables));
                    var right = Convert.ToDouble(EvaluateExpression(parts[1], localVariables));
                    return left * right;
                }

                if (expression.Contains("/"))
                {
                    var parts = expression.Split('/');
                    var left = Convert.ToDouble(EvaluateExpression(parts[0], localVariables));
                    var right = Convert.ToDouble(EvaluateExpression(parts[1], localVariables));
                    return left / right;
                }

                if (expression.Contains("+"))
                {
                    var parts = expression.Split('+');
                    var left = EvaluateExpression(parts[0], localVariables);
                    var right = EvaluateExpression(parts[1], localVariables);

                    // Concatenación de strings o suma de números
                    if (left is string || right is string)
                        return Convert.ToDouble($"{left}{right}");

                    return Convert.ToDouble(left) + Convert.ToDouble(right);
                }

                if (expression.Contains("-"))
                {
                    var parts = expression.Split('-');
                    var left = Convert.ToDouble(EvaluateExpression(parts[0], localVariables));
                    var right = Convert.ToDouble(EvaluateExpression(parts[1], localVariables));
                    return left - right;
                }

                return Convert.ToDouble(EvaluateExpression(expression, localVariables));
            }
            catch
            {
                return double.NaN;
            }
        }

        private bool EvaluateCondition(string condition, Dictionary<string, object> localVariables)
        {
            condition = condition.Trim();

            // Operadores de comparación
            if (condition.Contains("=="))
            {
                var parts = condition.Split(new[] { "==" }, 2, StringSplitOptions.RemoveEmptyEntries);
                var left = EvaluateExpression(parts[0].Trim(), localVariables);
                var right = EvaluateExpression(parts[1].Trim(), localVariables);
                return object.Equals(left, right);
            }

            if (condition.Contains("!="))
            {
                var parts = condition.Split(new[] { "!=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                var left = EvaluateExpression(parts[0].Trim(), localVariables);
                var right = EvaluateExpression(parts[1].Trim(), localVariables);
                return !object.Equals(left, right);
            }

            if (condition.Contains(">="))
            {
                var parts = condition.Split(new[] { ">=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                var left = Convert.ToDouble(EvaluateExpression(parts[0].Trim(), localVariables));
                var right = Convert.ToDouble(EvaluateExpression(parts[1].Trim(), localVariables));
                return left >= right;
            }

            if (condition.Contains("<="))
            {
                var parts = condition.Split(new[] { "<=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                var left = Convert.ToDouble(EvaluateExpression(parts[0].Trim(), localVariables));
                var right = Convert.ToDouble(EvaluateExpression(parts[1].Trim(), localVariables));
                return left <= right;
            }

            if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                var left = Convert.ToDouble(EvaluateExpression(parts[0].Trim(), localVariables));
                var right = Convert.ToDouble(EvaluateExpression(parts[1].Trim(), localVariables));
                return left > right;
            }

            if (condition.Contains("<"))
            {
                var parts = condition.Split('<');
                var left = Convert.ToDouble(EvaluateExpression(parts[0].Trim(), localVariables));
                var right = Convert.ToDouble(EvaluateExpression(parts[1].Trim(), localVariables));
                return left < right;
            }

            // Evaluar como expresión booleana
            var result = EvaluateExpression(condition, localVariables);
            return Convert.ToBoolean(result);
        }

        private object[] ParseArguments(string argsString, Dictionary<string, object> localVariables)
        {
            if (string.IsNullOrEmpty(argsString))
                return new object[0];

            var args = new List<object>();
            var currentArg = new System.Text.StringBuilder();
            int parenDepth = 0;
            bool inString = false;
            char stringChar = '\0';

            foreach (char c in argsString)
            {
                if (c == '"' || c == '\'')
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (c == stringChar)
                    {
                        inString = false;
                    }
                }
                else if (c == '(' && !inString)
                {
                    parenDepth++;
                }
                else if (c == ')' && !inString)
                {
                    parenDepth--;
                }
                else if (c == ',' && !inString && parenDepth == 0)
                {
                    args.Add(EvaluateExpression(currentArg.ToString().Trim(), localVariables));
                    currentArg.Clear();
                    continue;
                }

                currentArg.Append(c);
            }

            if (currentArg.Length > 0)
            {
                args.Add(EvaluateExpression(currentArg.ToString().Trim(), localVariables));
            }

            return args.ToArray();
        }
        #endregion

        #region Métodos de Ayuda
        private bool IsValidVariableName(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z_$][a-zA-Z0-9_$]*$");
        }

        private object CreateJsElement(HtmlDocument element)
        {
            if (element == null)
                return null;

            // Crear un objeto simple que represente el elemento
            var jsElement = new Dictionary<string, object>
            {
                ["tagName"] = element.TagName,
                ["innerText"] = element.InnerText,
                ["innerHTML"] = element.InnerText, // Simplificado
                ["style"] = new Dictionary<string, object>(),
                ["attributes"] = element.Attributes ?? new Dictionary<string, string>()
            };

            // Agregar métodos
            globalFunctions[$"element_{element.Id}.setAttribute"] = args =>
            {
                if (args.Length >= 2)
                {
                    string name = args[0]?.ToString();
                    string value = args[1]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (element.Attributes == null)
                            element.Attributes = new Dictionary<string, string>();

                        element.Attributes[name] = value;
                    }
                }
                return null;
            };

            globalFunctions[$"element_{element.Id}.getAttribute"] = args =>
            {
                if (args.Length > 0 && element.Attributes != null)
                {
                    string name = args[0]?.ToString();
                    if (!string.IsNullOrEmpty(name) && element.Attributes.ContainsKey(name))
                        return element.Attributes[name];
                }
                return null;
            };

            globalFunctions[$"element_{element.Id}.addEventListener"] = args =>
            {
                // Simplificado - solo registrar
                return null;
            };

            return jsElement;
        }
        #endregion
    }

    public class ScriptExecutedEventArgs : EventArgs
    {
        public string Script { get; }
        public string Result { get; }

        public ScriptExecutedEventArgs(string script, string result)
        {
            Script = script;
            Result = result;
        }
    }
}