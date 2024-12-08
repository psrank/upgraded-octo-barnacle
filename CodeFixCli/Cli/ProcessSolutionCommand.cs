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
            List<string> solutions;

            if (string.IsNullOrWhiteSpace(settings.Folder))
            {
                AnsiConsole.MarkupLine("[yellow]No folder path provided.[/]");
                string solutionPath = AnsiConsole.Prompt(
                    new TextPrompt<string>("Please enter the [green]path to the solution file or folder[/]:")
                        .Validate(path =>
                        {
                            if (File.Exists(path) && path.EndsWith(".sln")) return ValidationResult.Success();
                            if (Directory.Exists(path)) return ValidationResult.Success();
                            return ValidationResult.Error("[red]Invalid path. Please try again.[/]");
                        }));

                // If user provided a direct path to a .sln file
                if (File.Exists(solutionPath) && solutionPath.EndsWith(".sln"))
                {
                    return ProcessSolutionWithMenu(solutionPath).GetAwaiter().GetResult();
                }
                else
                {
                    // Treat the given path as a folder and find solutions in it
                    solutions = _solutionLoader.GetSolutionPaths(solutionPath);
                }
            }
            else
            {
                solutions = _solutionLoader.GetSolutionPaths(settings.Folder);
            }

            if (solutions.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No solution files found in the specified folder.");
                return -1;
            }

            string finalSolutionPath;
            if (solutions.Count == 1)
            {
                // If there's only one solution, just use it
                finalSolutionPath = solutions[0];
            }
            else
            {
                // Multiple solutions: prompt user to pick one
                finalSolutionPath = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Multiple .sln files found. Please select one:")
                        .AddChoices(solutions));
            }

            AnsiConsole.MarkupLine($"[green]Processing solution:[/] {finalSolutionPath}");
            ProcessSolutionWithMenu(finalSolutionPath).GetAwaiter().GetResult();

            return 0;
        }


        private async Task<int> ProcessSolutionWithMenu(string solutionPath)
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
            
            return 0; 
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