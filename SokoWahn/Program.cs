#region # using *.*

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
// ReSharper disable TailRecursiveCall
// ReSharper disable ConvertToLambdaExpressionWhenPossible

#endregion

namespace SokoWahn
{
  unsafe class Program
  {
    const string PathData = "../../../Data/";
    const string PathTest = PathData + "Test/";

    const string TestLevel = "      ###      \n"
                           + "      #.#      \n"
                           + "  #####.#####  \n"
                           + " ##         ## \n"
                           + "##  # # # #  ##\n"
                           + "#  ##     ##  #\n"
                           + "# ##  # #  ## #\n"
                           + "#     $@$     #\n"
                           + "####  ###  ####\n"
                           + "   #### ####   \n";

    static void Main(string[] args)
    {
      Directory.CreateDirectory(PathData);
      Directory.CreateDirectory(PathTest);

      var solutionGuid = Guid.NewGuid();
      var projectGuid = Guid.NewGuid();
      const string ProjectName = "Sokowahn";
      const string ProjectFile = ProjectName + ".csproj";
      const string SolutionFile = ProjectName + ".sln";
      const string CsFile = ProjectName + ".cs";


      var csFile = new CsFile(true);
      csFile.Write();
      csFile.Write("using System;");
      csFile.Write();
      csFile.Write("namespace " + ProjectName, ns =>
      {
        ns.Write("unsafe class Program", cl =>
        {
          cl.Write("static void Main(string[] args)", main =>
          {
            main.Write("Console.WriteLine('Hello World!');");
            main.Write("Console.ReadLine();");
          });
        });
      });

      csFile.SaveToFile(PathTest + CsFile);

      var projectFile = CsProject.CreateCsProjectFile(projectGuid, ProjectName, new[] { "System" }, new[] { CsFile });
      projectFile.SaveToFile(PathTest + ProjectFile);

      var solutionFile = CsProject.CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", ProjectFile);
      solutionFile.SaveToFile(PathTest + SolutionFile);
    }
  }
}
