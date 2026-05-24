using System.Diagnostics;
using System.Text;

namespace Lab31_ProcessManager;

public class DetailsForm : Form
{
    private readonly Process process;
    private readonly TabControl tabs = new();
    private readonly TextBox threadsBox = new();
    private readonly TextBox modulesBox = new();

    public DetailsForm(Process selectedProcess)
    {
        process = selectedProcess;
        Text = $"Потоки та модулі — {process.ProcessName}";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadThreads();
        LoadModules();
    }

    private void InitializeControls()
    {
        tabs.Dock = DockStyle.Fill;

        threadsBox.Dock = DockStyle.Fill;
        threadsBox.Multiline = true;
        threadsBox.ScrollBars = ScrollBars.Both;
        threadsBox.ReadOnly = true;
        threadsBox.Font = new Font("Consolas", 10);

        modulesBox.Dock = DockStyle.Fill;
        modulesBox.Multiline = true;
        modulesBox.ScrollBars = ScrollBars.Both;
        modulesBox.ReadOnly = true;
        modulesBox.Font = new Font("Consolas", 10);

        var threadsPage = new TabPage("Потоки");
        threadsPage.Controls.Add(threadsBox);

        var modulesPage = new TabPage("Модулі");
        modulesPage.Controls.Add(modulesBox);

        tabs.TabPages.Add(threadsPage);
        tabs.TabPages.Add(modulesPage);

        Controls.Add(tabs);
    }

    private void LoadThreads()
    {
        var sb = new StringBuilder();

        try
        {
            foreach (ProcessThread thread in process.Threads)
            {
                sb.AppendLine($"ID потоку: {thread.Id}");
                sb.AppendLine($"Поточний пріоритет: {thread.CurrentPriority}");
                sb.AppendLine($"Рівень пріоритету: {thread.PriorityLevel}");

                try { sb.AppendLine($"Час запуску: {thread.StartTime}"); }
                catch { sb.AppendLine("Час запуску: немає доступу"); }

                sb.AppendLine(new string('-', 60));
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("Не вдалося отримати потоки: " + ex.Message);
        }

        threadsBox.Text = sb.ToString();
    }

    private void LoadModules()
    {
        var sb = new StringBuilder();

        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                sb.AppendLine($"Назва модуля: {module.ModuleName}");
                sb.AppendLine($"Файл: {module.FileName}");
                sb.AppendLine($"Розмір у пам'яті: {module.ModuleMemorySize / 1024} КБ");
                sb.AppendLine($"Базова адреса: {module.BaseAddress}");
                sb.AppendLine(new string('-', 60));
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("Не вдалося отримати модулі: " + ex.Message);
            sb.AppendLine("Спробуйте запустити програму від імені адміністратора.");
        }

        modulesBox.Text = sb.ToString();
    }
}
