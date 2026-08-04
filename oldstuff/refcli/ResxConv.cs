// Mini-Werkzeug: wandelt eine .resx-Datei in eine binaere .resources-Datei um
// (Ersatz fuer resgen.exe, das ohne Windows-SDK nicht vorhanden ist)

using System.Resources;

internal static class ResxConv
{
  private static void Main(string[] args)
  {
    using (var reader = new ResXResourceReader(args[0]))
    using (var writer = new ResourceWriter(args[1]))
    {
      foreach (System.Collections.DictionaryEntry entry in reader)
      {
        writer.AddResource((string)entry.Key, entry.Value);
      }
    }
  }
}
