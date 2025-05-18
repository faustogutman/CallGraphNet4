using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;

public static class DotToHtmlGenerator
{
    public static void GenerateHtmlFromDot(string dotFilePath, string outputHtmlPath)
    {
        string dotContent = File.ReadAllText(dotFilePath);

        string escapedDot = dotContent.Replace("`", "\\`").Replace("\"", "\\\"");
        string htmlTemplate = "<!DOCTYPE html>";
        //string htmlTemplate = "<!DOCTYPE html>" +
        //                            @"<html lang=\"en\">
        //                            < head >
        //                                < meta charset =\"UTF-8\">
        //                                < title > Graphviz DAG Viewer</ title >
        //                                < script src =\"https://unpkg.com/viz.js@2.1.2/viz.js\"></script>
        //                                < script src =\"https://unpkg.com/viz.js@2.1.2/full.render.js\"></script>
        //                                < style >
        //                                    body {
        //                                        font - family: sans - serif; background: #f4f4f4; padding: 20px; }
        //                                    #graph { text-align: center; }
        //                                    svg { width: 100 %; height: auto; }
        //                                </ style >
        //                            </ head >
        //                            < body >
        //                                < h2 > Visualización de Grafo desde DOT</ h2 >
        //                                < div id =\"graph\"></div>
        //                                < script >
        //                                    const dot = `" + escapedDot + "`;
        //                                        const viz = new Viz();
        //                                        viz.renderSVGElement(dot)
        //                                            .then(function(element) {
        //                                            document.getElementById(\"graph\").appendChild(element);
        //                                            })
        //                                        .catch(function(error) {
        //                                            document.getElementById(\"graph\").innerHTML = \"<p>Error al renderizar el gráfico.</p>\";
        //                                            console.error(error);
        //                                        });
        //                                </ script >
        //                            </ body >
        //                            </ html > ";

        File.WriteAllText(outputHtmlPath, htmlTemplate);
        }
    }

// Uso:
// DotToHtmlGenerator.GenerateHtmlFromDot("dag.dot", "graph.html");
