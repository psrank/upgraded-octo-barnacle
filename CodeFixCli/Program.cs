using CodeFixCli.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp<ProcessSolutionCommand>();
app.Configure(config =>
{
    config.SetApplicationName("RoslynCLI");
    config.AddExample(new[] { "process", "--folder", "/path/to/solution-folder" });
});

return app.Run(args);