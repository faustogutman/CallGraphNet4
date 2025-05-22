using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

public  class openFiles
{
    public class FileTextExtractor
    {
        public string FilePath { get; private set; }

        public string SelectFileAndReadText()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select a text file";
                openFileDialog.Filter = "Text Files (*.sql|*.txt)|*.txt|All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FilePath = openFileDialog.FileName;
                    return File.ReadAllText(FilePath);
                }

                return null;
            }
        }
    }

    public class FileTextExtractorMulti
    {
        public string[] SelectedFiles { get; private set; }

        public List<string> SelectFiles()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select one or more text files";
                openFileDialog.Filter = "Files PL/SQL (*.sql, *.pkb, *.pks, *.txt)|*.sql;*.pkb;*.pks;*.txt|Todos los archivos (*.*)|*.*";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileNames.Length > 0)
                {
                    SelectedFiles = openFileDialog.FileNames;
                    return new List<string>(SelectedFiles);
                }

                return null;
            }
        }
    }

    public class FolderSelector
    {
        public string SelectedFolderPath { get; private set; }

        public string SelectFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    SelectedFolderPath = folderDialog.SelectedPath;
                    return SelectedFolderPath;
                }

                return null;
            }
        }
    }
}