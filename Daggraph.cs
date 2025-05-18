// קוד C# ליצירת גרף מכוון אציקליקלי (DAG) ובדיקת מחזורים
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DAGBuilder
{
    public static Dictionary<string, List<string>> BuildGraph(List<RoutineInfo> routines)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var routine in routines)
        {
            if (!graph.ContainsKey(routine.Name))
                graph[routine.Name] = new List<string>();

            foreach (var call in routine.AllCalls.Concat(routine.ExtCalls))
            {
                graph[routine.Name].Add(call);
            }
        }

        return graph;
    }

    public static bool HasCycle(Dictionary<string, List<string>> graph)
    {
        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (IsCyclicUtil(node, visited, recStack, graph))
                return true;
        }

        return false;
    }

    private static bool IsCyclicUtil(string node, HashSet<string> visited, HashSet<string> recStack, Dictionary<string, List<string>> graph)
    {
        if (recStack.Contains(node))
            return true;

        if (visited.Contains(node))
            return false;

        visited.Add(node);
        recStack.Add(node);

        List<string> neighbors;
        if (graph.TryGetValue(node, out neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (IsCyclicUtil(neighbor, visited, recStack, graph))
                    return true;
            }
        }

        recStack.Remove(node);
        return false;
    }

    public static void ExportToDot(Dictionary<string, List<string>> graph, string dotFilePath)
    {
        using (var writer = new StreamWriter(dotFilePath + ".dot"))
        {
            writer.WriteLine("digraph G {");
            writer.WriteLine("  rankdir=LR;");

            foreach (var kvp in graph)
            {
                foreach (var target in kvp.Value)
                {
                    writer.WriteLine($"  \"{kvp.Key}\" -> \"{target}\";");
                }
            }

            writer.WriteLine("}");
        }
    }

    public static void RenderDotToPng(string dotFilePath, string outputPngPath)
    {
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "dot"; // נדרש Graphviz מותקן
        process.StartInfo.Arguments = $"-Tpng \"{dotFilePath}\" -o \"{outputPngPath}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }
}

// שימוש:
// var graph = DAGBuilder.BuildGraph(routines);
// bool hasCycle = DAGBuilder.HasCycle(graph);
// if (!hasCycle) { DAGBuilder.ExportToDot(graph, "dag.dot"); DAGBuilder.RenderDotToPng("dag.dot", "dag.png"); }
