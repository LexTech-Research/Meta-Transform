using Softwrox.Forensics.Concordance.Core;
using Terminal.Gui;
using System.Data;
using NStack;
using System.CommandLine.NamingConventionBinder;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;

// Meeting Location pattern: Location\s+:\s(.+?)®

/// TODO: move the Sequence of the realtionship to within the code


/// <summary>
/// TODO: Add Documentation
/// </summary>
public abstract class Process
{
    private DataTable? preview;

    [Newtonsoft.Json.JsonIgnore]
    public DataTable Preview
    {
        get => preview ??= new DataTable();
        set => preview = value;
    }

    public int PreviousStepIndex { get; set; }

    public required string Title { get; set; }

    public override string ToString()
    {
        return this.Title;
    }

    public abstract void Execute(DataTable? input = null);
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class ImportProcess : Process
{
    private string? file;

    public string File
    {
        get => file ??= string.Empty;
        set => file = value;
    }

    public override void Execute(DataTable? input)
    {
        if (!System.IO.File.Exists(this.File))
        {
            this.Preview = new DataTable();
            return;
        }

        this.Preview = ConcordanceFileReader.Read(this.File);
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class ExportProcess : Process
{
    private string? file;

    public string File
    {
        get => file ??= string.Empty;
        set => file = value;
    }

    public override void Execute(DataTable? input = null)
    {
        if (System.IO.File.Exists(this.File) || input == null || !this.File.EndsWith(".dat"))
        {
            return;
        }

        this.Preview = input.Copy();
        ConcordanceFileWriter.Write(this.Preview, this.File);
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class AddColumnProcess : Process
{
    private string? newColumn;

    public string NewColumn
    {
        get => newColumn ??= string.Empty;
        set => newColumn = value;
    }

    public override void Execute(DataTable? input = null)
    {
        if (input == null || this.NewColumn == string.Empty)
        {
            this.Preview = new DataTable();
            return;
        }

        this.Preview = input.Copy();
        this.Preview.Columns.Add(this.NewColumn);
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class FilterNotBlankColumnProcess : Process
{
    private string? column;

    public string Column
    {
        get => column ??= string.Empty;
        set => column = value;
    }
    public override void Execute(DataTable? input = null)
    {
        if (input == null || this.Column == string.Empty || !input.Columns.Contains(Column))
        {
            this.Preview = new DataTable();
            return;
        }

        this.Preview = input.Copy();
        foreach (var row in this.Preview.Select($"[{this.Column}] <> '' AND [{this.Column}] IS NOT NULL"))
        {
            this.Preview.Rows.Remove(row);
        }
    }
}


/// <summary>
/// TODO: Add Documentation
/// </summary>
public class ExtractionProcess : Process
{
    public ExtractionProcess()
    {
        SourceField = "";
        ExtractionPattern = "";
        DestinationField = "";
    }

    public string SourceField { get; set; }
    public string ExtractionPattern { get; set; }
    public string DestinationField { get; set; }

    public override void Execute(DataTable? input)
    {
        if (input != null && input.Columns.Contains(SourceField) && input.Columns.Contains(DestinationField))
        {
            this.Preview = input.Copy();
            this.Preview.ExtractIntoExistingColumn(this.SourceField, this.ExtractionPattern, this.DestinationField);
        }
        else
            this.Preview = new DataTable();
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class DeleteColumnProcess : Process
{
    public DeleteColumnProcess()
    {
        DeletionField = "";
    }

    public string DeletionField { get; set; }

    public override void Execute(DataTable? input)
    {
        if (input != null && this.DeletionField != string.Empty)
        {
            if (input.Columns.Contains(this.DeletionField))
            {
                this.Preview = input.Copy();
                this.Preview.Columns.Remove(this.DeletionField);
            }
            else
            {
                this.Preview = new DataTable();
            }
        }
        else
        {
            this.Preview = new DataTable();
        }
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class AppModel
{
    public Process[] Steps { get; set; } = [];
    public int ActiveStepIndex { get; set; } = -1;
}

public enum Msg
{
    AddImportProcess,
    AddExtractionProcess,
    AddDeletionProcess,
    AddColumnProcess,
    AddExportProcess,
    AddFilterNotBlankColumnProcess,
    SelectStep,
    UpdatePreview,
    UpdateProcessTitle,
    UpdateImportProcessFile,
    UpdateAddColumnsNewColumn,
    UpdateExtractionSource,
    UpdateExtractionPattern,
    UpdateExtractionDestination,
    UpdateDeleteColumnColumn,
    UpdateExportProcessFile,
    UpdateFilterNotBlankColumnColumn
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public record ProcessMessage(Msg Type, int? Index = null, string? NewValue = null);

/// <summary>
/// TODO: Add Documentation
/// </summary>
public class ExampleWindow : Window
{
    private FrameView stepsFrame, detailsFrame, previewFrame;
    private AppModel model = new AppModel();

    public ExampleWindow()
    {

        Title = $"Concordance File Processing ({Application.QuitKey} to quit)";
        Border.BorderStyle = BorderStyle.None;

        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Load", "Load a saved process", () => LoadProcessUI()),
                new MenuItem("_Save", "Save the current process", () => SaveProcessUI()),
                new MenuItem("_Quit", "Exit the application", () => Application.RequestStop())
            })
        });

        stepsFrame = new FrameView("Process Steps") { X = 0, Y = 1, Width = Dim.Percent(30), Height = Dim.Fill() };
        detailsFrame = new FrameView("Step Details") { X = Pos.Right(stepsFrame), Y = 1, Width = Dim.Fill(), Height = Dim.Percent(50) };
        previewFrame = new FrameView("Step Preview") { X = Pos.Right(stepsFrame), Y = Pos.Bottom(detailsFrame), Width = Dim.Fill(), Height = Dim.Fill() };

        Add(menu, stepsFrame, detailsFrame, previewFrame);
        Render();
    }

        private void LoadProcessUI()
    {
        var dialog = new OpenDialog("Load Process", "Choose a JSON file");
        dialog.AllowedFileTypes = [".json"];

        Application.Run(dialog);

        if (!dialog.Canceled && dialog.FilePaths.Count > 0)
        {
            model = LoadProcess(dialog.FilePaths[0]);
            Render();
        }
    }

    private void SaveProcessUI()
    {
        var dialog = new SaveDialog("Save Process", "Save as JSON file");

        Application.Run(dialog);

        if (!dialog.Canceled && !string.IsNullOrWhiteSpace((string?)dialog.FilePath))
        {
            SaveProcess(model, (string)dialog.FileName);
        }
    }

    public static void SaveProcess(AppModel model, string filePath)
    {
        var settings = new JsonSerializerSettings 
        { 
            Formatting = Newtonsoft.Json.Formatting.Indented, 
            TypeNameHandling = TypeNameHandling.All 
        };
        string json = JsonConvert.SerializeObject(model, settings);
        File.WriteAllText(filePath, json);
    }

    public static AppModel LoadProcess(string filePath)
    {
        if (!File.Exists(filePath))
            return new AppModel();

        var settings = new JsonSerializerSettings 
        { 
            TypeNameHandling = TypeNameHandling.All 
        };
        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<AppModel>(json,settings) ?? new AppModel();
    }

    public static AppModel Update(AppModel model, ProcessMessage msg)
    {
        switch (msg.Type)
        {
            case Msg.AddImportProcess:
                model.Steps = [.. model.Steps, new ImportProcess() { Title = $"Import", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.AddExtractionProcess:
                model.Steps = [.. model.Steps, new ExtractionProcess() { Title = $"Extraction", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.AddDeletionProcess:
                model.Steps = [.. model.Steps, new DeleteColumnProcess() { Title = $"Delete Column", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.AddColumnProcess:
                model.Steps = [.. model.Steps, new AddColumnProcess() { Title = $"Add Column", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.AddExportProcess:
                model.Steps = [.. model.Steps, new ExportProcess() { Title = $"Export", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.AddFilterNotBlankColumnProcess:
                model.Steps = [.. model.Steps, new FilterNotBlankColumnProcess() { Title = $"Filter", PreviousStepIndex = model.Steps.GetUpperBound(0) }];
                break;
            case Msg.SelectStep:
                if (msg.Index.HasValue)
                    model.ActiveStepIndex = msg.Index.Value;
                break;
            case Msg.UpdatePreview:
                if (model.ActiveStepIndex >= 0)
                    model.Steps[model.ActiveStepIndex].Execute(model.ActiveStepIndex > 0 ?  model.Steps[model.Steps[model.ActiveStepIndex].PreviousStepIndex].Preview : null);
                break;
            case Msg.UpdateProcessTitle:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    model.Steps[model.ActiveStepIndex].Title = msg.NewValue;
                break;
            case Msg.UpdateImportProcessFile:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((ImportProcess)model.Steps[model.ActiveStepIndex]).File = msg.NewValue;
                break;
            case Msg.UpdateAddColumnsNewColumn:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((AddColumnProcess)model.Steps[model.ActiveStepIndex]).NewColumn = msg.NewValue;
                break;
            case Msg.UpdateExtractionSource:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((ExtractionProcess)model.Steps[model.ActiveStepIndex]).SourceField = msg.NewValue;
                break;
            case Msg.UpdateExtractionPattern:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((ExtractionProcess)model.Steps[model.ActiveStepIndex]).ExtractionPattern = msg.NewValue;
                break;
            case Msg.UpdateExtractionDestination:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((ExtractionProcess)model.Steps[model.ActiveStepIndex]).DestinationField = msg.NewValue;
                break;
            case Msg.UpdateDeleteColumnColumn:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((DeleteColumnProcess)model.Steps[model.ActiveStepIndex]).DeletionField = msg.NewValue;
                break;
            case Msg.UpdateExportProcessFile:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((ExportProcess)model.Steps[model.ActiveStepIndex]).File = msg.NewValue;
                break;
            case Msg.UpdateFilterNotBlankColumnColumn:
                if (model.Steps.IsInBounds(model.ActiveStepIndex) && msg.NewValue != null)
                    ((FilterNotBlankColumnProcess)model.Steps[model.ActiveStepIndex]).Column = msg.NewValue;
                break;
        }
        return model;
    }

    private void Dispatch(ProcessMessage msg)
    {
        model = Update(model, msg);
        Render();
    }

    public void Render()
    {
        if (!stepsFrame.HasFocus)
            UpdateStepsFrame();
        if (!detailsFrame.HasFocus)
            UpdateDetailsFrame();
        if (!previewFrame.HasFocus)
        {
            UpdatePreview();
        }
    }

    private int ShowMessageBox(string title, string message, params string[] options)
    {
        return MessageBox.Query(title, message, options.Select(x => ustring.Make(x)).ToArray());
    }

    private void UpdateStepsFrame()
    {
        var stepsList = new ListView(model.Steps) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1) };
        if (model.Steps.IsInBounds(model.ActiveStepIndex))
        {
            stepsList.SelectedItem = model.ActiveStepIndex;
        }
        stepsList.SelectedItemChanged += (args) => { Dispatch(new ProcessMessage(Msg.SelectStep, args.Item)); };

        var categories = new string[] { "Import", "Transformation", "Report", "Export" };
        var transformations = new string[] { "Extraction", "Deletion", "Add", "Filter" };

        var add = new Button("Add Step")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(stepsList)
        };
        add.Clicked += () =>
        {
            var category = ShowMessageBox(
                "Select Process Step Type",
                "Select the type of process you would like to add",
                categories
            );

            switch (category)
            {
                case 0:
                    Dispatch(new ProcessMessage(Msg.AddImportProcess));
                    break;
                case 1:
                    var transformation = ShowMessageBox("Select Transformation Step Type", "Select the type of transformation you would like to add", transformations);
                    switch (transformation)
                    {
                        case 0:
                            Dispatch(new ProcessMessage(Msg.AddExtractionProcess));
                            break;
                        case 1:
                            Dispatch(new ProcessMessage(Msg.AddDeletionProcess));
                            break;
                        case 2:
                            Dispatch(new ProcessMessage(Msg.AddColumnProcess));
                            break;
                        case 3:
                            Dispatch(new ProcessMessage(Msg.AddFilterNotBlankColumnProcess));
                            break;
                        default:
                            break;
                    }
                    break;
                case 2:
                case 3:
                    Dispatch(new ProcessMessage(Msg.AddExportProcess));
                    break;
                default:
                    break;
            }
            stepsList.SetSource(model.Steps.ToArray());
        };


        stepsFrame.RemoveAll();
        stepsFrame.Add(stepsList);
        stepsFrame.Add(add);
    }

    private void UpdateDetailsFrame()
    {
        detailsFrame.RemoveAll();

        var handlers = new Dictionary<Type, Action<Process>>
        {
            { typeof(ImportProcess), p => AddImportDetails((ImportProcess)p) },
            { typeof(ExtractionProcess), p => AddExtractionDetails((ExtractionProcess)p) },
            { typeof(DeleteColumnProcess), p => AddDeletionDetails((DeleteColumnProcess)p) },
            { typeof(AddColumnProcess), p => AddColumnDetails((AddColumnProcess)p) },
            { typeof(ExportProcess), p => AddExportDetails((ExportProcess)p) },
            { typeof(FilterNotBlankColumnProcess), p => FilterNotBlankColumnDetails((FilterNotBlankColumnProcess)p)}
        };

        if (model.Steps.IsInBounds(model.ActiveStepIndex))
        {
            if (model.Steps[model.ActiveStepIndex] is Process process && handlers.TryGetValue(process.GetType(), out var action))
            {
                action(process);
            }
        }
    }

    private void UpdatePreview()
    {
        if (model.Steps.IsInBounds(model.ActiveStepIndex))
        {
            var process = model.Steps[model.ActiveStepIndex];
            if (model.ActiveStepIndex > 0)
                process.Execute(model.Steps[model.ActiveStepIndex - 1].Preview);
            else
                process.Execute();
        }
        UpdatePreviewFrame();
    }

    private void UpdatePreviewFrame()
    {
        previewFrame.RemoveAll();
        if (model.Steps.IsInBounds(model.ActiveStepIndex))
        {
            var process = model.Steps[model.ActiveStepIndex];
            if (process.Preview != null)
            {
                var table = new TableView(process.Preview) { X = 1, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() };
                previewFrame.Add(table);
            }
        }
    }

    private void AddImportDetails(ImportProcess process)
    {
        detailsFrame.Height = 9;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("File", process.File, value => Dispatch(new ProcessMessage(Msg.UpdateImportProcessFile, NewValue: value)), 5);
    }

    private void AddExportDetails(ExportProcess process)
    {
        detailsFrame.Height = 9;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("File", process.File, value => Dispatch(new ProcessMessage(Msg.UpdateExportProcessFile, NewValue: value)), 5);
    }

    private void AddExtractionDetails(ExtractionProcess process)
    {
        detailsFrame.Height = 13;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("Source", process.SourceField, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionSource, NewValue: value)), 5);
        AddLabeledTextField("Pattern", process.ExtractionPattern, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionPattern, NewValue: value)), 7);
        AddLabeledTextField("Destination", process.DestinationField, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionDestination, NewValue: value)), 9);
    }

    private void AddDeletionDetails(DeleteColumnProcess process)
    {
        detailsFrame.Height = 9;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("Column", process.DeletionField, value => Dispatch(new ProcessMessage(Msg.UpdateDeleteColumnColumn, NewValue: value)), 5);
    }

    private void AddColumnDetails(AddColumnProcess process)
    {
        detailsFrame.Height = 9;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("Column", process.NewColumn, value => Dispatch(new ProcessMessage(Msg.UpdateAddColumnsNewColumn, NewValue: value)), 5);
    }

    private void FilterNotBlankColumnDetails(FilterNotBlankColumnProcess process)
    {
        detailsFrame.Height = 9;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledDataField("Previous Step", process.PreviousStepIndex.ToString(), 3);
        AddLabeledTextField("Column", process.Column, value => Dispatch(new ProcessMessage(Msg.UpdateFilterNotBlankColumnColumn, NewValue: value)), 5);
    }

    private void AddLabeledTextField(string label, string initialValue, Action<string> onChanged, int yOffset)
    {
        var lbl = new Label(label) { X = 1, Y = yOffset, Width = 15 };
        detailsFrame.Add(lbl);

        var txtField = new TextField(initialValue) { X = Pos.Right(lbl) + 1, Y = yOffset, Width = Dim.Fill() - 1, TextAlignment = TextAlignment.Right };
        txtField.TextChanged += (text) => { onChanged(txtField.Text.ToString()); };
        detailsFrame.Add(txtField);
    }

    private void AddLabeledDataField(string label, string initialValue, int yOffset)
    {
        var lbl = new Label(label) { X = 1, Y = yOffset, Width = 15 };
        detailsFrame.Add(lbl);

        var txtField = new TextField(initialValue)
        {
            X = Pos.Right(lbl) + 1,
            Y = yOffset,
            Width = Dim.Fill() - 1,
            TextAlignment = TextAlignment.Right,
            Enabled = false
        };
        detailsFrame.Add(txtField);
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
public static class ArrayExtension
{
    public static bool IsInBounds<T>(this T[] array, int index, int dimension = 0)
    {
        return index >= 0 && index < (array?.Length ?? 0);
    }
}

/// <summary>
/// TODO: Add Documentation
/// </summary>
class Program
{
    static void Main()
    {
        Application.Run<ExampleWindow>();
        Application.Shutdown();
    }


}
