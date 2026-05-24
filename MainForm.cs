using System.Diagnostics;
using System.Text;

namespace Lab31_ProcessManager;

public class MainForm : Form
{
    private readonly DataGridView grid = new();
    private readonly Button refreshButton = new();
    private readonly Button exportButton = new();
    private readonly Label countLabel = new();
    private readonly ContextMenuStrip contextMenu = new();

    public MainForm()
    {
        Text = "ЛР31 — Менеджер процесів";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        LoadProcesses();
    }

    private void InitializeControls()
    {
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            Padding = new Padding(10)
        };

        refreshButton.Text = "Оновити список";
        refreshButton.Width = 150;
        refreshButton.Height = 32;
        refreshButton.Left = 10;
        refreshButton.Top = 10;
        refreshButton.Click += (_, _) => LoadProcesses();

        exportButton.Text = "Експорт у TXT";
        exportButton.Width = 150;
        exportButton.Height = 32;
        exportButton.Left = 170;
        exportButton.Top = 10;
        exportButton.Click += (_, _) => ExportToTxt();

        countLabel.AutoSize = true;
        countLabel.Left = 340;
        countLabel.Top = 17;

        topPanel.Controls.Add(refreshButton);
        topPanel.Controls.Add(exportButton);
        topPanel.Controls.Add(countLabel);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.ContextMenuStrip = contextMenu;

        grid.Columns.Add("Id", "ID");
        grid.Columns.Add("Name", "Назва процесу");
        grid.Columns.Add("MachineName", "Комп'ютер");
        grid.Columns.Add("MemoryMb", "Пам'ять, МБ");
        grid.Columns.Add("Threads", "Потоки");
        grid.Columns.Add("StartTime", "Час запуску");

        contextMenu.Items.Add("Інформація про процес", null, (_, _) => ShowProcessInfo());
        contextMenu.Items.Add("Потоки та модулі процесу", null, (_, _) => ShowThreadsAndModules());
        contextMenu.Items.Add("Зупинити процес", null, (_, _) => KillSelectedProcess());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Оновити список", null, (_, _) => LoadProcesses());
        contextMenu.Items.Add("Експорт у TXT", null, (_, _) => ExportToTxt());

        Controls.Add(grid);
        Controls.Add(topPanel);
    }

    private void LoadProcesses()
    {
        grid.Rows.Clear();

        var processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .ToList();

        foreach (var process in processes)
        {
            try
            {
                var info = ProcessInfo.FromProcess(process);
                grid.Rows.Add(info.Id, info.Name, info.MachineName, info.MemoryMb, info.Threads, info.StartTime);
            }
            catch
            {
                // Деякі системні процеси можуть бути недоступні для читання.
            }
        }

        countLabel.Text = $"Кількість процесів: {grid.Rows.Count}";
    }

    private int? GetSelectedProcessId()
    {
        if (grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Оберіть процес у таблиці.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
    }

    private Process? GetSelectedProcess()
    {
        int? id = GetSelectedProcessId();
        if (id == null) return null;

        try { return Process.GetProcessById(id.Value); }
        catch
        {
            MessageBox.Show("Процес уже завершено або до нього немає доступу.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LoadProcesses();
            return null;
        }
    }

    private void ShowProcessInfo()
    {
        var process = GetSelectedProcess();
        if (process == null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"ID: {process.Id}");
        sb.AppendLine($"Назва: {process.ProcessName}");
        sb.AppendLine($"Комп'ютер: {process.MachineName}");

        try { sb.AppendLine($"Час запуску: {process.StartTime}"); }
        catch { sb.AppendLine("Час запуску: немає доступу"); }

        try { sb.AppendLine($"Основний модуль: {process.MainModule?.FileName}"); }
        catch { sb.AppendLine("Основний модуль: немає доступу"); }

        sb.AppendLine($"Оперативна пам'ять: {process.WorkingSet64 / 1024 / 1024} МБ");
        sb.AppendLine($"Віртуальна пам'ять: {process.VirtualMemorySize64 / 1024 / 1024} МБ");
        sb.AppendLine($"Кількість потоків: {SafeThreadsCount(process)}");

        MessageBox.Show(sb.ToString(), "Інформація про процес", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static int SafeThreadsCount(Process process)
    {
        try { return process.Threads.Count; }
        catch { return 0; }
    }

    private void ShowThreadsAndModules()
    {
        var process = GetSelectedProcess();
        if (process == null) return;

        var form = new DetailsForm(process);
        form.ShowDialog();
    }

    private void KillSelectedProcess()
    {
        var process = GetSelectedProcess();
        if (process == null) return;

        var result = MessageBox.Show(
            $"Ви дійсно хочете зупинити процес {process.ProcessName}?");

        if (result != DialogResult.OK) return;

        try
        {
            process.Kill();
            process.WaitForExit(2000);
            MessageBox.Show("Процес зупинено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadProcesses();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не вдалося зупинити процес: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportToTxt()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Текстові файли (*.txt)|*.txt",
            FileName = "processes.txt"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID\tНазва процесу\tКомп'ютер\tПам'ять, МБ\tПотоки\tЧас запуску");

            foreach (DataGridViewRow row in grid.Rows)
            {
                sb.AppendLine($"{row.Cells["Id"].Value}\t{row.Cells["Name"].Value}\t{row.Cells["MachineName"].Value}\t{row.Cells["MemoryMb"].Value}\t{row.Cells["Threads"].Value}\t{row.Cells["StartTime"].Value}");
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Список процесів експортовано у текстовий файл.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Помилка експорту: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
