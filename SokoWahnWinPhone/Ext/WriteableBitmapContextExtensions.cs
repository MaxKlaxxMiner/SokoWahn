
#region # using *.*

// ReSharper disable MemberCanBeInternal
// ReSharper disable CheckNamespace

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  /// <summary>
  /// Provides the WriteableBitmap context pixel data
  /// </summary>
  public static class WriteableBitmapContextExtensions
  {
    /// <summary>
    /// Gets a BitmapContext within which to perform nested IO operations on the bitmap
    /// </summary>
    /// <remarks>For WPF the BitmapContext will lock the bitmap. Call Dispose on the context to unlock</remarks>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static BitmapContext GetBitmapContext(this WriteableBitmap bmp)
    {
      return new BitmapContext(bmp);
    }
  }
}
