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

    #region # static CsFile CreateCsProjectFile(Guid projectGuid, string projectName, IEnumerable<string> references, IEnumerable<string> compileFiles) // erstellt eine einfache Visual Studio Projekt-Datei (Konsolenzeilen Programm)
    /// <summary>
    /// erstellt eine einfache Visual Studio Projekt-Datei (Konsolenzeilen Programm)
    /// </summary>
    /// <param name="projectGuid">GUID des Projektes</param>
    /// <param name="projectName">Name des Projektes (Assembly-Name)</param>
    /// <param name="references">Liste mit erforderlichen Verweisen</param>
    /// <param name="compileFiles">Liste der compilierbaren Dateien, welche zum Projekt gehören</param>
    /// <returns>fertig erstellte Projekt-Datei</returns>
    static CsFile CreateCsProjectFile(Guid projectGuid, string projectName, IEnumerable<string> references, IEnumerable<string> compileFiles)
    {
      var prj = new CsFile(true);

      prj.Write("<?xml version='1.0' encoding='utf-8'?>");
      prj.WriteX("Project ToolsVersion='12.0' DefaultTargets='Build' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'", p =>
      {
        p.WriteX("Import Project='$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props' Condition='Exists(´$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props´)'");
        p.WriteX("PropertyGroup", x =>
        {
          x.WriteXv("Configuration Condition=' ´$(Configuration)´ == ´´ '", "Debug");
          x.WriteXv("Platform Condition=' ´$(Platform)´ == ´´ '", "AnyCPU");
          x.WriteXv("ProjectGuid", "{" + projectGuid.ToString().ToUpperInvariant() + "}");
          x.WriteXv("OutputType", "Exe");
          x.WriteXv("AppDesignerFolder", "Properties");
          x.WriteXv("RootNamespace", projectName);
          x.WriteXv("AssemblyName", projectName);
          x.WriteXv("TargetFrameworkVersion", "v4.5");
          x.WriteXv("FileAlignment", "512");
        });
        p.WriteX("PropertyGroup Condition=' ´$(Configuration)|$(Platform)´ == ´Debug|AnyCPU´ '", x =>
        {
          x.WriteXv("PlatformTarget", "AnyCPU");
          x.WriteXv("DebugSymbols", "true");
          x.WriteXv("DebugType", "full");
          x.WriteXv("Optimize", "false");
          x.WriteXv("OutputPath", "bin\\Debug\\");
          x.WriteXv("DefineConstants", "DEBUG;TRACE");
          x.WriteXv("ErrorReport", "prompt");
          x.WriteXv("WarningLevel", "4");
          x.WriteXv("Prefer32Bit", "false");
          x.WriteXv("AllowUnsafeBlocks", "true");
        });
        p.WriteX("PropertyGroup Condition=' ´$(Configuration)|$(Platform)´ == ´Release|AnyCPU´ '", x =>
        {
          x.WriteXv("PlatformTarget", "AnyCPU");
          x.WriteXv("DebugType", "pdbonly");
          x.WriteXv("Optimize", "true");
          x.WriteXv("OutputPath", "bin\\Release\\");
          x.WriteXv("DefineConstants", "TRACE");
          x.WriteXv("ErrorReport", "prompt");
          x.WriteXv("WarningLevel", "4");
          x.WriteXv("Prefer32Bit", "false");
          x.WriteXv("AllowUnsafeBlocks", "true");
        });
        p.WriteX("ItemGroup", x =>
        {
          foreach (var r in references)
          {
            x.WriteX("Reference Include='" + r + "'");
          }
        });
        p.WriteX("ItemGroup", x =>
        {
          foreach (var file in compileFiles)
          {
            x.WriteX("Compile Include='" + file + "'");
          }
        });
        p.WriteX("Import Project='$(MSBuildToolsPath)\\Microsoft.CSharp.targets'");
      });

      return prj;
    }
    #endregion

    #region # static CsFile CreateSolutionFile(Guid solutionGuid, Guid projectGuid, string projectName, string projectFile) // erstellt eine einfach Projektmappen-Datei
    /// <summary>
    /// erstellt eine einfach Projektmappen-Datei
    /// </summary>
    /// <param name="solutionGuid">GUID der Projektmappe</param>
    /// <param name="projectGuid">GUID des Projektes</param>
    /// <param name="projectName">Name des Projektes</param>
    /// <param name="projectFile">Dateiname des Projektes</param>
    /// <returns>fertig erstellte Projektmappen-Datei</returns>
    static CsFile CreateSolutionFile(Guid solutionGuid, Guid projectGuid, string projectName, string projectFile)
    {
      var sol = new CsFile(true);

      sol.Write("Microsoft Visual Studio Solution File, Format Version 12.00");
      sol.Write("# Visual Studio 2013");
      sol.Write("VisualStudioVersion = 12.0.31101.0");
      sol.Write("MinimumVisualStudioVersion = 10.0.40219.1");
      sol.Write("Project('{" + solutionGuid.ToString().ToUpperInvariant() + "}') = '" + projectName + "', '" + projectFile + "', '{" + projectGuid.ToString().ToUpperInvariant() + "}'");
      sol.Write("EndProject");
      sol.Write("Global");
      {
        sol.Write("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        {
          sol.Write("\t\tDebug|Any CPU = Debug|Any CPU");
          sol.Write("\t\tRelease|Any CPU = Release|Any CPU");
        }
        sol.Write("\tEndGlobalSection");
        sol.Write("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        {
          sol.Write("\t\t{" + projectGuid.ToString().ToUpperInvariant() + "}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
          sol.Write("\t\t{" + projectGuid.ToString().ToUpperInvariant() + "}.Debug|Any CPU.Build.0 = Debug|Any CPU");
          sol.Write("\t\t{" + projectGuid.ToString().ToUpperInvariant() + "}.Release|Any CPU.ActiveCfg = Release|Any CPU");
          sol.Write("\t\t{" + projectGuid.ToString().ToUpperInvariant() + "}.Release|Any CPU.Build.0 = Release|Any CPU");
        }
        sol.Write("\tEndGlobalSection");
        sol.Write("\tGlobalSection(SolutionProperties) = preSolution");
        {
          sol.Write("\t\tHideSolutionNode = FALSE");
        }
        sol.Write("\tEndGlobalSection");
      }
      sol.Write("EndGlobal");

      return sol;
    }
    #endregion

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

      var projectFile = CreateCsProjectFile(projectGuid, ProjectName, new[] { "System" }, new[] { CsFile });
      projectFile.SaveToFile(PathTest + ProjectFile);

      var solutionFile = CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", ProjectFile);
      solutionFile.SaveToFile(PathTest + SolutionFile);
    }
  }
}
