using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class RoutineCall
{
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public bool IsExternal { get; set; }
}

public class ParameterInfo
{
    public string Name { get; set; }
    public string Mode { get; set; }
    public string DataType { get; set; }
}

public class RoutineInfo
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public List<ParameterInfo> Parameters { get; set; }
    public List<string> AllCalls { get; set; }
    public List<string> ExtCalls { get; set; }
    public List<RoutineCall> Calls { get; set; }
    public string PackageName { get; set; }
    public int LineNumber { get; set; }

    public RoutineInfo()
    {
        Parameters = new List<ParameterInfo>();
        AllCalls = new List<string>();
        ExtCalls = new List<string>();
        Calls = new List<RoutineCall>();
    }
}

public static class PLParser
{
    // Regex para funciones y procedimientos (admite END; o END nombre;)
    private static readonly Regex RoutinePattern = new Regex(
        @"(FUNCTION|PROCEDURE)\s+(\w+)\s*(\((.*?)\))?\s*(RETURN\s+[\w\(\)%]+)?\s*(IS|AS)([\s\S]*?)(END\s+(\w+)?\s*;)",
        RegexOptions.IgnoreCase);

    private static readonly Regex CallPattern = new Regex(@"\b([a-zA-Z_]\w*)\s*\(", RegexOptions.IgnoreCase);
    private static readonly Regex PackageCallPattern = new Regex(@"\b(\w+)\.(\w+)\s*\(", RegexOptions.IgnoreCase);

    private static readonly HashSet<string> IgnoredKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AND", "MOD", "PUT_LINE", "DBMS_OUTPUT", "VARCHAR2", "IF", "THEN", "ELSE", "ELSIF",
        "LOOP", "FOR", "WHILE", "BEGIN", "END", "NULL", "RETURN", "RAISE", "COMMIT",
        "ROLLBACK", "DECLARE", "TYPE", "CURSOR", "RECORD"
    };

    public static List<RoutineInfo> ExtractRoutines(string packageBody)
    {
        List<RoutineInfo> routines = new List<RoutineInfo>();
        string packageName = NameFromBody(packageBody);

        MatchCollection matches = RoutinePattern.Matches(packageBody);

        foreach (Match match in matches)
        {
            string routineName = match.Groups[2].Value;

            RoutineInfo routine = new RoutineInfo
            {
                Type = match.Groups[1].Value.ToUpper(),
                Name = routineName,
                ReturnType = match.Groups[5].Success ? match.Groups[5].Value.Trim() : null,
                PackageName = packageName,
                LineNumber = GetLineNumber(packageBody, match.Index)
            };

            string parameterBlock = match.Groups[4].Value;
            string body = match.Groups[7].Value;

            ParseParameters(parameterBlock, routine);
            routines.Add(routine);

            // Detectar funciones/procedimientos anidados recursivamente
            ExtractNestedRoutines(body, routineName, packageName, routines);
        }

        // Diccionario para buscar package de rutinas internas
        var routinesDict = routines.ToDictionary(r => r.Name, r => r.PackageName, StringComparer.OrdinalIgnoreCase);

        // Set de nombres locales para detectar internas
        var localRoutineNames = routines.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var routine in routines)
        {
            string body = GetRoutineBody(packageBody, routine.Name.Split('.').Last());
            ExtractCalls(body, packageBody, packageBody.IndexOf(body), routine, localRoutineNames, routinesDict);
        }

        return routines;
    }

    private static void ExtractNestedRoutines(string body, string parentName, string packageName, List<RoutineInfo> resultList)
    {
        MatchCollection matches = RoutinePattern.Matches(body);

        foreach (Match match in matches)
        {
            string routineName = match.Groups[2].Value;
            string fullName = parentName + "." + routineName;

            RoutineInfo routine = new RoutineInfo
            {
                Type = match.Groups[1].Value.ToUpper(),
                Name = fullName,
                ReturnType = match.Groups[5].Success ? match.Groups[5].Value.Trim() : null,
                PackageName = packageName,
                LineNumber = GetLineNumber(body, match.Index)
            };

            string parameterBlock = match.Groups[4].Value;
            string routineBody = match.Groups[7].Value;

            ParseParameters(parameterBlock, routine);
            resultList.Add(routine);

            // Recurse para detectar más anidados
            ExtractNestedRoutines(routineBody, fullName, packageName, resultList);
        }
    }

    private static void ParseParameters(string parameterBlock, RoutineInfo routine)
    {
        if (string.IsNullOrWhiteSpace(parameterBlock)) return;

        string[] parameters = parameterBlock.Split(',');
        foreach (string param in parameters)
        {
            string[] parts = param.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            ParameterInfo paramInfo = new ParameterInfo();
            paramInfo.Name = parts[0];

            if (parts.Length >= 3 && (parts[1].Equals("IN", StringComparison.OrdinalIgnoreCase)
                || parts[1].Equals("OUT", StringComparison.OrdinalIgnoreCase)
                || parts[1].Equals("IN OUT", StringComparison.OrdinalIgnoreCase)))
            {
                paramInfo.Mode = parts[1].ToUpperInvariant();
                paramInfo.DataType = string.Join(" ", parts.Skip(2).ToArray());
            }
            else if (parts.Length >= 2)
            {
                paramInfo.DataType = string.Join(" ", parts.Skip(1).ToArray());
            }

            routine.Parameters.Add(paramInfo);
        }
    }

    private static void ExtractCalls(string body, string fullText, int bodyOffset, RoutineInfo routine, HashSet<string> localRoutineNames, Dictionary<string, string> routinesDict)
    {
        HashSet<string> seenCalls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Calls simples sin paquete explícito
        foreach (Match callMatch in CallPattern.Matches(body))
        {
            string name = callMatch.Groups[1].Value;
            if (IgnoredKeywords.Contains(name)) continue;

            int callLine = GetLineNumber(fullText, bodyOffset + callMatch.Index);

            string fullCallName;
            bool isInternal = localRoutineNames.Contains(name) || localRoutineNames.Contains(routine.Name + "." + name);

            if (isInternal)
            {
                if (routinesDict.TryGetValue(name, out string pkg))
                {
                    fullCallName = pkg + "." + name;
                }
                else if (routinesDict.TryGetValue(routine.Name + "." + name, out string pkgNested))
                {
                    fullCallName = pkgNested + "." + routine.Name + "." + name;
                }
                else
                {
                    fullCallName = routine.PackageName + "." + name;
                }
            }
            else
            {
                fullCallName = "UNKNOWN." + name;
            }

            if (fullCallName.StartsWith("UNKNOWN.", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!seenCalls.Contains(fullCallName))
            {
                seenCalls.Add(fullCallName);
                routine.AllCalls.Add(fullCallName);
            }

            routine.Calls.Add(new RoutineCall
            {
                Name = fullCallName,
                LineNumber = callLine,
                IsExternal = !isInternal
            });
        }

        // Calls con paquete explícito
        foreach (Match pkgCallMatch in PackageCallPattern.Matches(body))
        {
            string pkg = pkgCallMatch.Groups[1].Value;
            string func = pkgCallMatch.Groups[2].Value;
            if (pkg.Equals("DBMS_OUTPUT", StringComparison.OrdinalIgnoreCase)) continue;

            string fullName = pkg + "." + func;

            if (fullName.StartsWith("UNKNOWN.", StringComparison.OrdinalIgnoreCase))
                continue;

            int callLine = GetLineNumber(fullText, bodyOffset + pkgCallMatch.Index);

            if (!seenCalls.Contains(fullName))
            {
                seenCalls.Add(fullName);
                routine.ExtCalls.Add(fullName);
            }

            routine.Calls.Add(new RoutineCall
            {
                Name = fullName,
                LineNumber = callLine,
                IsExternal = true
            });
        }
    }

    private static int GetLineNumber(string text, int charIndex)
    {
        return text.Substring(0, charIndex).Count(c => c == '\n') + 1;
    }

    private static string NameFromBody(string packageBody)
    {
        Match nameMatch = Regex.Match(packageBody, @"PACKAGE\s+BODY\s+(?:""[^""]+""\.)?""?(\w+)""?", RegexOptions.IgnoreCase);
        return nameMatch.Success ? nameMatch.Groups[1].Value : "UNKNOWN";
    }

    private static string GetRoutineBody(string fullText, string routineName)
    {
        var match = Regex.Match(fullText,
            $@"(FUNCTION|PROCEDURE)\s+{routineName}\s*(\((.*?)\))?\s*(RETURN\s+[\w\(\)%]+)?\s*(IS|AS)([\s\S]*?)(END\s+{routineName}\s*;)",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[6].Value : "";
    }

    public static void ExportCallsToCsv(List<RoutineInfo> routines, string filePath)
    {
        List<string> lines = new List<string>
    {
        "Package,RoutineName,RoutineLineNumber,CallPackage,CallRoutineName,CallLineNumber,Type"
    };

        foreach (RoutineInfo routine in routines)
        {
            foreach (RoutineCall call in routine.Calls)
            {
                if (call.Name.StartsWith("UNKNOWN.", StringComparison.OrdinalIgnoreCase))
                    continue;

                string callPackage = "";
                string callRoutineName = call.Name;

                int lastDotIndex = call.Name.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    callPackage = call.Name.Substring(0, lastDotIndex);
                    callRoutineName = call.Name.Substring(lastDotIndex + 1);
                }

                lines.Add(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                    routine.PackageName,
                    routine.Name,
                    routine.LineNumber,
                    callPackage,
                    callRoutineName,
                    call.LineNumber,
                    call.IsExternal ? "External" : "Internal"));
            }
        }

        File.WriteAllLines(filePath, lines);
    }

}
