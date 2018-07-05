#region # using *.*

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse mit Methoden zum erstellen von Visual-Studio Projekten
  /// </summary>
  internal static class CsProject
  {
    /// <summary>
    /// erstellt eine einfache Visual Studio Projekt-Datei (Konsolenzeilen Programm)
    /// </summary>
    /// <param name="projectGuid">GUID des Projektes</param>
    /// <param name="projectName">Name des Projektes (Assembly-Name)</param>
    /// <param name="references">Liste mit erforderlichen Verweisen</param>
    /// <param name="compileFiles">Liste der compilierbaren Dateien, welche zum Projekt gehören</param>
    /// <returns>fertig erstellte Projekt-Datei</returns>
    public static CsFile CreateCsProjectFile(Guid projectGuid, string projectName, IEnumerable<string> references, IEnumerable<string> compileFiles)
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

    /// <summary>
    /// erstellt eine einfach Projektmappen-Datei
    /// </summary>
    /// <param name="solutionGuid">GUID der Projektmappe</param>
    /// <param name="projectGuid">GUID des Projektes</param>
    /// <param name="projectName">Name des Projektes</param>
    /// <param name="projectFile">Dateiname des Projektes</param>
    /// <returns>fertig erstellte Projektmappen-Datei</returns>
    public static CsFile CreateSolutionFile(Guid solutionGuid, Guid projectGuid, string projectName, string projectFile)
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

    /// <summary>
    /// erstellt eine Guid anhand eines bestimmten Begriffes (gleicher Begriff = gleiche Guid)
    /// </summary>
    /// <param name="passPhrase">Begriff, welcher verwendnet werden soll</param>
    /// <returns>fertig erstellte GUID</returns>
    public static Guid NewGuid(string passPhrase)
    {
      return new Guid(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(passPhrase)));
    }
  }
}
