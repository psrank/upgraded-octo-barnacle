using Microsoft.Build.Locator;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Task = System.Threading.Tasks.Task;

namespace CodeFixCli.Services
{
    public class SolutionLoader
    {
        public SolutionLoader()
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch (Exception)
            {
                var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (!visualStudioInstances.Any())
                {
                    Console.WriteLine("No VisualStudioInstances instances detected.");

                    var msbuildPath = ThisAssembly.Project.DefaultMSBuildBinPath;
                    MSBuildLocator.RegisterMSBuildPath(msbuildPath);

                    Console.WriteLine($"Manually registered MSBuild at: {msbuildPath}");
                }
                else
                {
                    var instance = visualStudioInstances.First(); // Pick the first available instance
                    MSBuildLocator.RegisterInstance(instance);

                    Console.WriteLine($"Using MSBuild at: {instance.MSBuildPath}");
                }
                
                if (!MSBuildLocator.IsRegistered)
                {
                    throw;
                }
            }
        }

        public string? GetSolutionPath(string folderPath)
        {
            return Directory.GetFiles(folderPath, "*.sln").FirstOrDefault();
        }

        public async Task<Solution> LoadSolutionAsync(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();
            return await workspace.OpenSolutionAsync(solutionPath);
        }

        public async Task SaveSolutionAsync(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var updatedDocument = solution.GetDocument(document.Id);
                    if (updatedDocument == null) continue;

                    var updatedText = await updatedDocument.GetTextAsync();
                    await File.WriteAllTextAsync(document.FilePath, updatedText.ToString());
                    Console.WriteLine($"Updated: {document.FilePath}");
                }
            }
        }
    }

}
