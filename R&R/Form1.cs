using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml;

namespace R_R
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dgvFiles.Columns.Add("FileName", "File Name");
            dgvFiles.Columns.Add("FilePath", "File Path");
            dgvFiles.Columns.Add("FileExtension", "File Extension");
            dgvFiles.Columns.Add("CreationDate", "Last Modified Date");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    dgvFiles.Rows.Clear();

                    string selectedPath = folderDialog.SelectedPath;
                    string extension = txtExtension.Text;
                    DateTime selectedDate = dtpDate.Value.Date;

                    var files = Directory.GetFiles(selectedPath, $"*{extension}")
                        .Where(f => File.GetLastWriteTime(f).Date == selectedDate)
                        .ToArray();

                    foreach (var file in files)
                    {
                        dgvFiles.Rows.Add(Path.GetFileName(file), Path.GetDirectoryName(file), Path.GetExtension(file), File.GetLastWriteTime(file));
                    }

                    if (files.Length == 0)
                    {
                        MessageBox.Show("No files found matching the criteria.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProcessedFiles");

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string excelFilePath = Path.Combine(targetFolder, "Resultados.xlsx");

            using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Resultados");

                worksheet.Cells[1, 1].Value = "Serial Number";
                worksheet.Cells[1, 2].Value = "CRD";
                worksheet.Cells[1, 3].Value = "Result";

                int row = 2;

                foreach (DataGridViewRow dgvRow in dgvFiles.Rows)
                {
                    if (dgvRow.Cells[0].Value != null)
                    {
                        string fileName = dgvRow.Cells[0].Value.ToString();
                        string filePath = Path.Combine(dgvRow.Cells[1].Value.ToString(), fileName);

                        // Copiar el archivo al directorio de destino
                        string destFilePath = Path.Combine(targetFolder, fileName);
                        File.Copy(filePath, destFilePath, true);

                        string[] lines = File.ReadAllLines(destFilePath);

                        // Obtener el serial, CRDs y resultados usando la función recursiva
                        ProcessLogLines(lines, ref row, worksheet);
                    }
                }

                package.Save();
            }

            MessageBox.Show("Files processed and results saved in Excel.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ProcessLogLines(string[] lines, ref int row, ExcelWorksheet worksheet)
        {
            string serialNumber = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("{@BTEST|"))
                {
                    int startSerial = line.IndexOf("{@BTEST|") + "{@BTEST|".Length;
                    int endSerial = line.IndexOf("|", startSerial);
                    serialNumber = line.Substring(startSerial, endSerial - startSerial);
                }

                if (line.Contains("{@BLOCK|") && line.Contains("%"))
                {
                    // Extraer el CRD
                    int startCRD = line.IndexOf('%') + 1;
                    int endCRD = line.IndexOf('|', startCRD);
                    string crd = line.Substring(startCRD, endCRD - startCRD);

                    // Buscar el resultado en la siguiente línea
                    if (i + 1 < lines.Length && lines[i + 1].Contains("{@A-JUM|"))
                    {
                        string nextLine = lines[i + 1];
                        int startResult = nextLine.IndexOf("|", nextLine.IndexOf("|") + 1) + 1;
                        int endResult = nextLine.IndexOf("{", startResult);

                        // Verifica que los índices sean válidos
                        if (startResult >= 0 && endResult > startResult)
                        {
                            string result = nextLine.Substring(startResult, endResult - startResult);

                            // Guardar el Serial, CRD y Resultado en el Excel
                            worksheet.Cells[row, 1].Value = serialNumber;
                            worksheet.Cells[row, 2].Value = crd;
                            worksheet.Cells[row, 3].Value = result;

                            row++;
                        }
                    }
                }
            }
        }

    }
}

