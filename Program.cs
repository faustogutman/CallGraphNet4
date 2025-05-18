using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        // Paso 1: Obtener ruta del archivo CSV de salida
        string csvPath = GetSaveFilePath("Guardar archivo CSV", "CSV Files (*.csv)|*.csv");
        if (string.IsNullOrEmpty(csvPath))
        {
            Console.WriteLine("Operación cancelada.");
            return;
        }

        // Paso 2: Seleccionar archivos de entrada
        var fileSelector = new openFiles.FileTextExtractorMulti();
        List<string> selectedFiles = fileSelector.SelectFiles();

        if (selectedFiles == null || selectedFiles.Count == 0)
        {
            Console.WriteLine("No se seleccionaron archivos.");
            return;
        }

        // Paso 3: Extraer rutinas de todos los archivos
        List<RoutineInfo> allRoutines = new List<RoutineInfo>();

        foreach (var filePath in selectedFiles)
        {
            string content = File.ReadAllText(filePath);
            var routines = PLParser.ExtractRoutines(content);
            allRoutines.AddRange(routines);
        }

        // Paso 4: Exportar CSV
        PLParser.ExportCallsToCsv(allRoutines, csvPath);
        Console.WriteLine($"CSV exportado a: {csvPath}");

        /// Paso 5: Crear el archivo .dot desde las rutinas
        string dotBasePath = Path.ChangeExtension(csvPath, null); // mismo nombre sin .csv
        var graph = DAGBuilder.BuildGraph(allRoutines);
        DAGBuilder.ExportToDot(graph, dotBasePath);
        Console.WriteLine($"Archivo .dot generado en: {dotBasePath}.dot");

        //// Paso 6 (opcional): Renderizar imagen PNG
        //string pngPath = dotBasePath + ".png";
        //try
        //{
        //    DAGBuilder.RenderDotToPng(dotBasePath + ".dot", pngPath);
        //    Console.WriteLine($"Imagen PNG generada en: {pngPath}");
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("No se pudo renderizar la imagen PNG. Verifica si Graphviz está instalado y accesible.");
        //    Console.WriteLine(ex.Message);
        //}
    }

    private static string GetSaveFilePath(string title, string filter)
    {
        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
        {
            saveFileDialog.Title = title;
            saveFileDialog.Filter = filter;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog.FileName;
            }
        }
        return null;
    }
}
