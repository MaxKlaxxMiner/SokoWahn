#region # using *.*

using System.Diagnostics;
using System.IO;
using System.Linq;

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum kompilieren von CS-Projekten
  /// </summary>
  public static class CsCompiler
  {
    /// <summary>
    /// sucht den Pfad zur "msbuild.exe" order wirft eine Exception, wenn nicht gefunden
    /// </summary>
    public static string MsBuildPath
    {
      get
      {
        string[] searchPaths =
        {
          @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe",
          @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
        };

        foreach (var path in searchPaths.Where(File.Exists))
        {
          return path;
        }

        throw new FileNotFoundException("MSBuild.exe");
      }
    }

    /// <summary>
    /// kompiliert ein bestimmtes Projekt
    /// </summary>
    /// <param name="solutionFile">Projektmappe, welche kompiliert werden soll</param>
    public static void Compile(string solutionFile)
    {
      string msBuild = MsBuildPath;
      var slnFile = new FileInfo(solutionFile);
      if (!slnFile.Exists) throw new FileNotFoundException(solutionFile);
      if (slnFile.DirectoryName == null) throw new DirectoryNotFoundException();

      var processInfo = new ProcessStartInfo
      {
        WorkingDirectory = slnFile.DirectoryName,
        FileName = msBuild,
        Arguments = slnFile.Name + " /t:Rebuild /p:Configuration=Release",
        CreateNoWindow = true,
        UseShellExecute = false
      };

      Process.Start(processInfo).WaitForExit();
    }
  }
}
