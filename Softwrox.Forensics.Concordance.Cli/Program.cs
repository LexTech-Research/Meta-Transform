using Softwrox.Forensics.Concordance.Core;
using Terminal.Gui;
using System.Data;
using NStack;
using System.CommandLine.NamingConventionBinder;

// Meeting Location pattern: Location\s+:\s(.+?)®

/// TODO: move the Sequence of the realtionship to within the code


/// <summary>
/// TODO: Add Documentation
/// </summary>
public abstract class Process
{
    private DataTable? preview;

    public DataTable Preview
    {
        get => preview ??= new DataTable();
        set => preview = value;
    }

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
    public ImportProcess(string file = "")
    {
        this.File = file;
    }

    public string File { get; set; }

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
    SelectStep,
    UpdatePreview,
    UpdateProcessTitle,
    UpdateImportProcessFile,
    UpdateAddColumnsNewColumn,
    UpdateExtractionSource,
    UpdateExtractionPattern,
    UpdateExtractionDestination,
    UpdateDeleteColumnColumn
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

        stepsFrame = new FrameView("Process Steps") { X = 0, Y = 0, Width = Dim.Percent(30), Height = Dim.Fill() };
        detailsFrame = new FrameView("Step Details") { X = Pos.Right(stepsFrame), Y = 0, Width = Dim.Fill(), Height = Dim.Percent(50) };
        previewFrame = new FrameView("Step Preview") { X = Pos.Right(stepsFrame), Y = Pos.Bottom(detailsFrame), Width = Dim.Fill(), Height = Dim.Fill() };

        Add(stepsFrame, detailsFrame, previewFrame);
        Render();
    }

    public static AppModel Update(AppModel model, ProcessMessage msg)
    {
        switch (msg.Type)
        {
            case Msg.AddImportProcess:
                model.Steps = [.. model.Steps, new ImportProcess() { Title = $"Import" }];
                break;
            case Msg.AddExtractionProcess:
                model.Steps = [.. model.Steps, new ExtractionProcess() { Title = $"Extraction" }];
                break;
            case Msg.AddDeletionProcess:
                model.Steps = [.. model.Steps, new DeleteColumnProcess() { Title = $"Delete Column" }];
                break;
            case Msg.AddColumnProcess:
                model.Steps = [.. model.Steps, new AddColumnProcess() { Title = $"Add Column" }];
                break;
            case Msg.SelectStep:
                if (msg.Index.HasValue)
                    model.ActiveStepIndex = msg.Index.Value;
                break;
            case Msg.UpdatePreview:
                if (model.ActiveStepIndex >= 0)
                    model.Steps[model.ActiveStepIndex].Execute(model.ActiveStepIndex > 0 ? model.Steps[model.ActiveStepIndex - 1].Preview : null);
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
            //UpdatePreviewFrame();
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
        var transformations = new string[] { "Extraction", "Deletion", "Add" };

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
                    model.Steps = [.. model.Steps, new ImportProcess() { Title = categories[category] }];
                    break;
                case 2:
                case 3:
                    break;
                case 1:
                    var transformation = ShowMessageBox("Select Transformation Step Type", "Select the type of transformation you would like to add", transformations);
                    switch (transformation)
                    {
                        case 0:
                            model.Steps = [.. model.Steps, new ExtractionProcess() { Title = $"{categories[category]} - {transformations[transformation]}" }];
                            break;
                        case 1:
                            model.Steps = [.. model.Steps, new DeleteColumnProcess() { Title = $"{categories[category]} - {transformations[transformation]}" }];
                            break;
                        case 2:
                            model.Steps = [.. model.Steps, new AddColumnProcess() { Title = $"{categories[category]} - {transformations[transformation]}" }];
                            break;
                        default:
                            break;
                    }
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
        detailsFrame.Height = 7;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledTextField("File", process.File, value => Dispatch(new ProcessMessage(Msg.UpdateImportProcessFile, NewValue: value)), 3);
    }

    private void AddExtractionDetails(ExtractionProcess process)
    {
        detailsFrame.Height = 11;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledTextField("Source", process.SourceField, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionSource, NewValue: value)), 3);
        AddLabeledTextField("Pattern", process.ExtractionPattern, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionPattern, NewValue: value)), 5);
        AddLabeledTextField("Destination", process.DestinationField, value => Dispatch(new ProcessMessage(Msg.UpdateExtractionDestination, NewValue: value)), 7);
    }

    private void AddDeletionDetails(DeleteColumnProcess process)
    {
        detailsFrame.Height = 7;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledTextField("Column", process.DeletionField, value => Dispatch(new ProcessMessage(Msg.UpdateDeleteColumnColumn, NewValue: value)), 3);
    }

    private void AddColumnDetails(AddColumnProcess process)
    {
        detailsFrame.Height = 7;

        AddLabeledTextField("Title", process.Title, value => Dispatch(new ProcessMessage(Msg.UpdateProcessTitle, NewValue: value)), 1);
        AddLabeledTextField("Column", process.NewColumn, value => Dispatch(new ProcessMessage(Msg.UpdateAddColumnsNewColumn, NewValue: value)), 3);
    }

    private TextField AddLabeledTextField(string label, string initialValue, Action<string> onChanged, int yOffset)
    {
        var lbl = new Label(label) { X = 1, Y = yOffset, Width = 15 };
        detailsFrame.Add(lbl);

        var txtField = new TextField(initialValue) { X = Pos.Right(lbl) + 1, Y = yOffset, Width = Dim.Fill() - 1, TextAlignment = TextAlignment.Right };
        txtField.TextChanged += (text) => { onChanged(txtField.Text.ToString()); };
        detailsFrame.Add(txtField);

        return txtField;
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

    /// <break>

    // static void Main(string[] args)
    // {
    //     var rootCommand = CreateRootCommand();
    //     rootCommand.InvokeAsync(args);
    // }

    // private static RootCommand CreateRootCommand()
    // {
    //     var rootCommand = new RootCommand
    //     {
    //         new Option<FileInfo>("--file","Select a file"){ IsRequired = true }
    //     };

    //     rootCommand.Handler = CommandHandler.Create<FileInfo>((file) => ProcessFile(file));
    //     return rootCommand;
    // }

    // private static void ProcessFile(FileInfo file)
    // {
    //     var data = ReadConcordanceFile(file);

    //     ExtractDataIntoNewColumn(data);

    //     StartApplication(data, file);
    // }

    // private static DataTable ReadConcordanceFile(FileInfo file)
    // {
    //     return ConcordanceFileReader.Read(file.FullName);
    // }

    // private static void ExtractDataIntoNewColumn(DataTable data)
    // {
    //     data.ExtractIntoNewColumn(
    //         fromColumn: "Extracted Text",
    //         pattern: @"Location\s+:\s(.+?)®",
    //         toColumn: "Meeting Location",
    //         replaceOriginal: true
    //     );
    // }

    // private static void StartApplication(DataTable data, FileInfo file)
    // {
    //     Application.Init();

    //     var menu = CreateMenu(data, file);
    //     var table = CreateTable(data);

    //     Application.Top.Add(menu, table);

    //     Application.Run();
    //     Application.Shutdown();
    // }

    // private static MenuBar CreateMenu(DataTable data, FileInfo file)
    // {
    //     return new MenuBar(new MenuBarItem[]
    //     {
    //         new MenuBarItem("_File", new MenuItem[]
    //         {
    //             new MenuItem("_Save", "", () => SaveFile(data, file)),
    //             new MenuItem("_Quit", "", () => Application.RequestStop()),
    //             new MenuItem("_Open", "", () => OpenFile())
    //         }),
    //     });
    // }

    // private static TableView CreateTable(DataTable data)
    // {
    //     return new TableView
    //     {
    //         X = Pos.Left(Application.Top),
    //         Y = Pos.Top(Application.Top) + 1,
    //         Width = Dim.Fill(),
    //         Height = Dim.Fill(),
    //         Table = data
    //     };
    // }

    // private static void SaveFile(DataTable data, FileInfo file)
    // {
    //     var outputPath = Path.Combine(file.Directory.FullName, "result.dat");
    //     ConcordanceFileWriter.Write(data, outputPath);
    // }

    // private static void OpenFile() {

    // }
}
