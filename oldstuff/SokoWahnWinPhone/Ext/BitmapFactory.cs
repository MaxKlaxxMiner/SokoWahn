
#region # using *.*

// ReSharper disable MemberCanBeInternal
// ReSharper disable CheckNamespace

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  /// <summary>
  /// Cross-platform factory for WriteableBitmaps
  /// </summary>
  public static class BitmapFactory
  {
    /// <summary>
    /// Creates a new WriteableBitmap of the specified width and height
    /// </summary>
    /// <remarks>For WPF the default DPI is 96x96 and PixelFormat is Pbgra32</remarks>
    /// <param name="pixelWidth"></param>
    /// <param name="pixelHeight"></param>
    /// <returns></returns>
    public static WriteableBitmap New(int pixelWidth, int pixelHeight)
    {
      return new WriteableBitmap(pixelWidth, pixelHeight);
    }
  }
}
