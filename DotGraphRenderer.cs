using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using SysColor = System.Drawing.Color;


public class DotGraphRenderer
{
    public string DotFilePath { get; set; }
    public string OutputImagePath { get; set; } = "grafo_coloreado.png";

    private HashSet<string> sources = new HashSet<string>();
    private HashSet<string> targets = new HashSet<string>();

    public DotGraphRenderer(string dotFilePath, string outputImagePath = null)
    {
        DotFilePath = dotFilePath;
        if (!string.IsNullOrEmpty(outputImagePath))
            OutputImagePath = outputImagePath;
    }

    public void GenerateGraphImage()
    {
        if (!File.Exists(DotFilePath))
            throw new FileNotFoundException("Archivo DOT no encontrado: " + DotFilePath);

        var graph = new Graph("G") { Directed = true };
        var lines = File.ReadAllLines(DotFilePath);
        var edgePattern = new Regex("\"(.*?)\"\\s*->\\s*\"(.*?)\";");

        foreach (string line in lines)
        {
            Match match = edgePattern.Match(line);
            if (match.Success)
            {
                string from = match.Groups[1].Value;
                string to = match.Groups[2].Value;

                graph.AddEdge(from, to);
                sources.Add(from);
                targets.Add(to);
            }
        }

        ApplyNodeStyles(graph);
        RenderToPng(graph);
    }

    private void ApplyNodeStyles(Graph graph)
    {
        var allNodes = new HashSet<string>(sources);
        allNodes.UnionWith(targets);

        foreach (string nodeName in allNodes)
        {
            Node node = graph.FindNode(nodeName);
            if (node == null) continue;

            node.Attr.Shape = Shape.Box;
            node.Label.FontSize = 10;

            bool isSource = sources.Contains(nodeName);
            bool isTarget = targets.Contains(nodeName);

            if (isSource && !isTarget)
                node.Attr.FillColor = ToMsaglColor(System.Drawing.Color.LightGreen);
            else if (!isSource && isTarget)
                node.Attr.FillColor = ToMsaglColor(System.Drawing.Color.IndianRed);
            else
                node.Attr.FillColor = ToMsaglColor(System.Drawing.Color.LightBlue);
        }
    }

    private Microsoft.Msagl.Drawing.Color ToMsaglColor(System.Drawing.Color color)
    {
        return new Microsoft.Msagl.Drawing.Color(color.A, color.R, color.G, color.B);
    }


    private void RenderToPng(Graph graph)
    {
        var viewer = new GViewer { Graph = graph };
        var form = new Form { Width = 1000, Height = 800 };
        viewer.Dock = DockStyle.Fill;
        form.Controls.Add(viewer);

        form.Load += (s, e) =>
        {
            using (var bmp = new Bitmap(form.Width, form.Height))
            {
                viewer.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(OutputImagePath, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show("Imagen guardada en: " + OutputImagePath);
                form.Close();
            }
        };

        Application.Run(form);
    }
}
