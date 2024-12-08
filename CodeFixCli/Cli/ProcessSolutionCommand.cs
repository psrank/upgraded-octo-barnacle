using Spectre.Console.Cli;
using System.ComponentModel;
using CodeFixCli.Services;
using Spectre.Console;
using Microsoft.CodeAnalysis;

namespace CodeFixCli.Cli
{
    public class ProcessSolutionCommand : Command<ProcessSolutionCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("--folder <FOLDER>")]
            [Description("The folder containing the solution file.")]
            public string Folder { get; set; } = string.Empty;
        }

        private readonly SolutionLoader _solutionLoader;
        private readonly CodeModifier _codeModifier;

        public ProcessSolutionCommand()
        {
            _solutionLoader = new SolutionLoader();
            _codeModifier = new CodeModifier();
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            string solutionPath;

            if (string.IsNullOrWhiteSpace(settings.Folder))
            {
                // Prompt the user for the folder path if not provided
                AnsiConsole.MarkupLine("[yellow]No folder path provided.[/]");
                solutionPath = AnsiConsole.Prompt(
                    new TextPrompt<string>("Please enter the [green]path to the solution file[/]:")
                        .Validate(path =>
                        {
                            if (File.Exists(path) && path.EndsWith(".sln"))
                                return ValidationResult.Success();
                            return ValidationResult.Error("[red]Invalid solution file path. Please try again.[/]");
                        }));
            }
            else
            {
                solutionPath = _solutionLoader.GetSolutionPath(settings.Folder);
                if (solutionPath == null)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No solution file found in the specified folder.");
                    return -1;
                }
            }


            AnsiConsole.MarkupLine($"[green]Processing solution:[/] {solutionPath}");
            ProcessSolutionWithMenu(solutionPath).GetAwaiter().GetResult();

            return 0;
        }

        private async Task ProcessSolutionWithMenu(string solutionPath)
        {
            var solution = await _solutionLoader.LoadSolutionAsync(solutionPath);

            bool exit = false;
            while (!exit)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose an action:")
                        .AddChoices("List Projects", "Modify Code", "Exit"));

                switch (choice)
                {
                    case "List Projects":
                        ListProjects(solution);
                        break;

                    case "Modify Code":
                        solution = await _codeModifier.ApplyCodeChanges(solution);
                        AnsiConsole.MarkupLine("[green]Code changes applied successfully![/]");
                        break;

                    case "Exit":
                        exit = true;
                        AnsiConsole.MarkupLine("[yellow]Exiting the menu...[/]");
                        break;
                }
            }

            AnsiConsole.MarkupLine("[green]Saving changes...[/]");
            await _solutionLoader.SaveSolutionAsync(solution);
            AnsiConsole.MarkupLine("[green]All changes saved successfully![/]");
        }

        private void ListProjects(Solution solution)
        {
            var table = new Table()
                .AddColumn("Project Name")
                .AddColumn("File Count");

            foreach (var project in solution.Projects)
            {
                table.AddRow(project.Name, project.Documents.Count().ToString());
            }

            AnsiConsole.Write(table);
        }
    }
}


