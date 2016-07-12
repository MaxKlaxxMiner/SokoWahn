/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2012 by Matthias Meger, Germany
 *
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *
 *	This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#region # using *.*

using System;
using System.Drawing;
using System.IO;
using JSoko.java.io;
using JSoko.ResourceHandling;

#endregion

namespace JSoko.Utilities_
{
  /// <summary>
  /// This class provides a mixture of helper functions and tools used throughout JSoko.
  /// </summary>
  public class Utilities
  {
    //  /** A table of hex digits (with upper case letters) */
    //  public static final char[] hexDigit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    //  /**
    //   * The standard Unicode character for a horizontal "ellipsis" (...)
    //   */
    //  public static final char ellipsisChar = '\u2026';

    //  /**
    //   * The standard Unicode character for a horizontal "ellipsis" (...)
    //   * as a <code>String</code> of length 1.
    //   */
    //  public static final String ellipsisString = "\u2026";

    //  /**
    //   * Components whose UI has to be updated when the look&feel changes.
    //   * Implemented by a ListenerSet, so that dead components vanish from it
    //   * on the fly.
    //   */
    //  private static ListenerSet<Component,Void> uiComponentSet =
    //         new ListenerSet<Component,Void>(makeUpdateUIcaller());

    //  /**
    //   * Executor for executing new threads. This executor should be used in order to ensure
    //   * JSoko isn't using more threads / CPUs than the user has allowed (yet, unlimited number of threads).
    //   */
    //  public static ExecutorService executor = Executors.newCachedThreadPool();

    //  /** The folder this program is executed from */
    //  private static String baseFolder;


    //  /**
    //   * Avoid instantiation of this class.
    //   */
    //  private Utilities() {
    //    throw new AssertionError();
    //  }

    //  /**
    //   * Returns a URL to the passed file.
    //   *
    //   * @param filename	name of the file the a URL is created for.
    //   * @return the <code>URL</code> to the passed file
    //   */
    //  public final static URL getURLFromClassPath(String filename) {

    //    URL url = null;
    //    try {
    //      url = JSoko.class.getResource(filename);
    //    } catch (Exception e) {
    //      return getURLFromClassPath(File.separator+filename);
    //    }

    //    return url;
    //  }

    //  /**
    //   * Returns a <code>File</code> to the file corresponding to the passed filename.
    //   * <p>
    //   * This method searches in the class folder path.
    //   *
    //   * @param filename
    //   * @return the <code>File</code> having the passed filename
    //   */
    //  public static File getFileFromClassPath(String filename) {
    //    URL url = getURLFromClassPath(filename);
    //    if(url == null) {
    //      return new File(filename);
    //    }

    //    File file = null;
    //    try {
    //      file = new File(url.toURI());
    //    } catch(URISyntaxException e) {
    //      file = new File(url.getPath());
    //    }

    //    return file;
    //  }

    public static string GetBaseFolder()
    {
      // todo
      return "";
    }
    //  /**
    //   * Returns the folder this program is executed in.
    //   * <p>
    //   * Example: /home/games/JSoko/
    //   *
    //   * @return <code>String</code> containing the path to the folder this program is executed in.
    //   */
    //  public static String getBaseFolder() {
    //    if(baseFolder == null) {
    //      try {
    //        // Get folder of the JSoko.jar.
    //        baseFolder = ClassLoader.getSystemResource(".").toString();

    //        String s = JSoko.class.getClassLoader().getResource("settings.ini").getPath();
    //        if(s != null) {
    //          baseFolder = s.split("settings.ini")[0];
    //        }

    //        // Transform %20 to spaces.
    //        baseFolder = new URI(baseFolder).getPath();

    //      } catch (URISyntaxException e) {
    //        e.printStackTrace();
    //      }
    //    }

    //    return baseFolder;
    //  }

    //  /**
    //   * Returns a <code>InputStream</code> to the file corresponding to the passed filename.
    //   *
    //   * @param filename  the name of the file
    //   * @return the <code>InputStream</code> to the file corresponding to the passed filename
    //   */
    //  public static InputStream getInputStream(String filename) {

    //    InputStream inputStream = null;

    //    // Try to read the file, no matter if it is in the class path or not.
    //    try {
    //      inputStream = new FileInputStream(filename);
    //    } catch (FileNotFoundException e1) {}

    //    // If the program is started as web start application the file may be
    //    // in the jar file. Hence try to read it from the jar, too.
    //    try {
    //      if(inputStream == null) {
    //        inputStream = JSoko.class.getResourceAsStream(filename);
    //      }
    //    } catch (Exception e) {}

    //    return inputStream;
    //  }

    /// <summary>
    /// Returns a <code>BufferedReader</code> to the file corresponding to the passed filename.
    /// 
    /// This method also searches for the file in the class path.
    /// </summary>
    /// <param name="filename">the name of the file</param>
    /// <returns>the <code>BufferedReader</code> to the file corresponding to the passed filename</returns>
    public static BufferedReader GetBufferedReader(string filename)
    {
      BufferedReader b = null;

      // Try to read the file, no matter if it is in the class path or not.
      try
      {
        b = new BufferedReader(File.OpenRead(filename));
      }
      catch (Exception)
      {
        // ignored
      }

      //if (b == null)
      //{
      //  // If the program is started as web start application the file
      //  // may be in the jar file. Hence try to read it from the jar, too.
      //  try
      //  {
      //    b = new BufferedReader(new InputStreamReader(JSoko.classs.getResourceAsStream(filename)));
      //  }
      //  catch (Exception e) { }
      //}
      return b;
    }


    //  /**
    //   * Returns the <code>ImageIcon</code> specified by the passed name.
    //   *
    //   * @param iconName the name of the icon to return
    //   * @param iconDescription the description of the icon
    //   * @return	the <code>ImageIcon</code>
    //   */
    //  final public static ImageIcon getIcon(String iconName, String iconDescription) {

    //    URL iconURL = getURLFromClassPath(Settings.get("iconFolder") + iconName);
    //    if (iconURL != null) {
    //      return new ImageIcon(iconURL, iconDescription);
    //    }

    //    if (Debug.isDebugModeActivated) {
    //      System.out.println("Icon not found" + Settings.get("iconFolder") + iconName);
    //    }

    //    return null;
    //  }

    //  /**
    //   * Returns the <code>BufferedImage</code> loaded from the provided file path.
    //   *
    //   * @param filepath file's name and its location in the file system
    //   * @return the loaded <code>BufferedImage</code>
    //   * @throws FileNotFoundException if the file couldn't be loaded
    //   */
    //  final public static BufferedImage loadBufferedImage(String filepath) throws FileNotFoundException {

    //    // Load the image. If this fails return a dummy image.
    //    InputStream inputStream = null;
    //    try {
    //      inputStream = getInputStream(filepath);
    //      if(inputStream != null) {
    //        return ImageIO.read(inputStream);
    //      }
    //    } catch (Exception e1) {
    //      if(Debug.isDebugModeActivated) {
    //        e1.printStackTrace();
    //      }
    //    }
    //    finally {
    //      if(inputStream != null) {
    //        try {
    //          inputStream.close();
    //        } catch (IOException e) {}
    //      }
    //    }

    //    throw new FileNotFoundException(Texts.getText("message.fileMissing", filepath));
    //  }


    //  /**
    //   * Converts a nibble to a hex character
    //   *
    //   * @param  nibble	the nibble to convert.
    //   * @return character represented by the nibble
    //   */
    //  public final static char toHex(int nibble) {
    //    return hexDigit[nibble & 0xF];
    //  }


    //  /**
    //   * Checks the length of the specified String, and if it is longer than
    //   * the specified maximal length, truncates it to that length,
    //   * and appends an ellipsis.
    //   *
    //   * @param str     the string to check
    //   * @param maxlen  maximal length to not change
    //   * @return either the original <code>str</code>, or the truncated and
    //   *         extended version of it
    //   */
    //  public final static String clipToEllipsis( String str, int maxlen ) {
    //    if (str != null && str.length() > maxlen) {
    //      /*
    //       * Here we assume, that our font has a representation of the
    //       * horizontal ellipsis, or at least implements a good surrogate.
    //       */
    //      str = str.substring(0, maxlen) + ellipsisString;
    //    }
    //    return str;
    //  }

    //  /**
    //   * Computes a standard representation for the passed date.
    //   *
    //   * @param date the date to be converted, or <code>null</code> for "now"
    //   * @return the date as a string
    //   */
    //  public final static String dateString(Date date) {
    //    if (date == null) {
    //      date = new Date();			// now
    //    }
    //    return DateFormat.getInstance().format(date);
    //  }

    //  /**
    //   * Computes a standard representation for "now".
    //   * @return the current date and time as a string
    //   */
    //  public final static String nowString() {
    //    return dateString(null);
    //  }

    //  /**
    //   * Creates and returns a string consisting from {@code repcnt} copies
    //   * of the specified string.
    //   * A {@code null} string is replaced by an empty string.
    //   * We do never return a {@code null}, not even when we get one.
    //   *
    //   * @param repcnt the number of copies we want
    //   * @param toRep  the string to be repeated
    //   * @return string built from repeated copies
    //   */
    //  public static final String makeRepStr(int repcnt, String toRep) {
    //    if (repcnt <= 0) {
    //      return "";
    //    }
    //    if (toRep == null) {
    //      toRep = "";
    //    }
    //    if (repcnt == 1) {
    //      return toRep;
    //    }
    //    if (toRep.length() <= 0) {
    //      return "";
    //    }
    //    // repcnt>=2  |toRep|>=1
    //    int len = repcnt * toRep.length();
    //    StringBuilder sb = new StringBuilder(len);
    //    for (int i = 0; i < repcnt; i++) {
    //      sb.append(toRep);
    //    }
    //    return sb.toString();
    //  }

    //  /**
    //   * The maximal length of a blank string created by {@link #blankStr(int)}.
    //   */
    //  public static final int MAXLEN_BLANK_STR = 999;

    //  /**
    //   * Here we cache some small blank strings.
    //   * While this is not strictly necessary for performance, it is not bad.
    //   * Consider it a demo cache implementation.
    //   */
    //  private static final String[] cachedBlanksStrs = new String[20];

    //  /**
    //   * Return a String containing just the specified amount of blanks.
    //   * This is thought of as kind of statistics support, and so we
    //   * deliberately limit the length to at most {@value #MAXLEN_BLANK_STR}.
    //   *
    //   * @param len how many blanks we want
    //   * @return String with that many blanks
    //   */
    //  public static final String blankStr(int len) {
    //    if (len <= 0) {
    //      return "";
    //    }
    //    if (len > MAXLEN_BLANK_STR) {
    //      len = MAXLEN_BLANK_STR;
    //    }

    //    String s = null;

    //    // Check the cache, whether the string is there, already...
    //    if (len < cachedBlanksStrs.length) {
    //      synchronized (cachedBlanksStrs) {
    //        s = cachedBlanksStrs[len];
    //        if (s != null) {
    //          return s;
    //        }
    //      }
    //    }

    //    // Really build the string
    //    s = makeRepStr(len, " ");

    //    // Check whether we want to feed the cache with it ...
    //    if (len < cachedBlanksStrs.length) {
    //      synchronized (cachedBlanksStrs) {
    //        String x = cachedBlanksStrs[len];
    //        if (x != null) {
    //          // Someone else was faster than we.
    //          // We use the early result, avoiding yet another copy
    //          s = x;
    //        } else {
    //          cachedBlanksStrs[len] = s;
    //        }
    //      }
    //    }

    //    return s;
    //  }

    //  /**
    //   * Fill a string with blanks at the left side
    //   * to reach the indicated length.
    //   * @param len minimum string length wanted
    //   * @param str string to be augmented (at left side)
    //   * @return eventually augmented string
    //   */
    //  public static final String fillL(int len, String str) {
    //    if (str == null) {
    //      str = "";
    //    }
    //    if (len > str.length()) {
    //      str = blankStr(len - str.length()) + str;
    //    }
    //    return str;
    //  }

    //  /**
    //   * Statistics support: computes a quotient {@code (part / total)}
    //   * as a {@code double}, avoiding division by zero.
    //   *
    //   * @param part
    //   * @param total
    //   * @return 0.0, or part / total
    //   * @see Utilities#percOf(long, long)
    //   */
    //  public static final double partOf(long part, long total) {
    //    if (total == 0) {
    //      return 0.0;
    //    }
    //    return (double)part / (double)total;
    //  }

    //  /**
    //   * Statistics support: computes a percentage
    //   * as a {@code double}, avoiding division by zero.
    //   * @param part
    //   * @param total
    //   * @return 0.0, or 100 * (part / total)
    //   */
    //  public static final double percOf(long part, long total) {
    //    return 100.0 * partOf(part, total);
    //  }

    //  /**
    //   * Statistics support: computes a percentage and converts it into
    //   * a standard String of fixed length, like {@code " 97.12%"}.
    //   * @param part
    //   * @param total
    //   * @return standard string for a percentage
    //   */
    //  public static final String percStr(long part, long total) {
    //    return String.format("%6.2f%%", percOf(part, total));
    //  }

    //  /**
    //   * Statistics support: computes a percentage and converts it into
    //   * a standard String of fixed length, with a standard decoration,
    //   * like {@code " ( 97.12%)"}.
    //   * @param part
    //   * @param total
    //   * @return standard string for a percentage with decoration
    //   */
    //  public static final String percStrEmb(long part, long total) {
    //    return String.format(" (%6.2f%%)", percOf(part, total));
    //  }

    //  /**
    //   * A simple (debug) method to record a code location along with
    //   * the current time (in ms).
    //   * @param location number representing the code location
    //   */
    //  public static final void loggTime(int location) {
    //    loggTime(location, "");
    //  }

    //  /**
    //   * A simple (debug) method to record a code location along with
    //   * the current time (in ms).
    //   * @param location number representing the code location
    //   * @param funcname function name within which this event happens
    //   */
    //  public static final void loggTime(int location, String funcname) {
    //    if (Debug.isTimingDebugModeActivated) {
    //      log_Time(location, funcname);
    //    }
    //  }

    //  private static final void log_Time(int location, String funcname) {
    //    final int loclen = 6;

    //    if (funcname == null) {
    //      funcname =  "";
    //    }

    //    String locstr = fillL(loclen, "" + location);

    //    System.out.println("" + System.currentTimeMillis()
    //                   + " " + locstr
    //                   + " " + funcname );
    //  }

    //  /**
    //   * Returns all files that are in the passed root directory or any sub-directory.
    //   * <p>
    //   * Example:<code>
    //   * getFileList("./test/", ".*\\.txt$");</code><br>
    //   * returns all text files in the directory "test".
    //   *
    //   * @param rootDirectory the directory to start the search in
    //   * @param extensionPattern the pattern all relevant files have to match
    //   *
    //   * @return <code>ArrayList</code> of the found <code>File</code>s
    //   */
    //  public static ArrayList<File> getFileList(String rootDirectory, final String extensionPattern ) {

    //    // ArrayList for storing all found files.
    //    ArrayList<File> files = new ArrayList<File>(64);


    //    // Stack for storing all found directories.
    //    Stack<File> directories = new Stack<File>();

    //    // Get a file for the root directory.
    //    File startdirectory = getFileFromClassPath(rootDirectory);

    //    // Push the directory to the stack.
    //    if (startdirectory != null && startdirectory.isDirectory()) {
    //      directories.push(startdirectory);
    //    }

    //    // Create the pattern for filtering the files.
    //    Pattern pattern = Pattern.compile(extensionPattern, Pattern.CASE_INSENSITIVE);

    //    // As long as there are more directories: add all files having the correct
    //    // filename extension to the list.
    //    while (directories.size() > 0) {
    //      for (File file : directories.pop().listFiles()) {
    //        if (file.isDirectory()) {
    //          directories.push(file);
    //        } else {
    //          if (pattern.matcher(file.getName()).matches()) {
    //            files.add(file);
    //          }
    //        }
    //      }
    //    }

    //    // Return the found files.
    //    return files;
    //  }

    //  /**
    //   * Returns the names of all files that are stored in the main jar: "JSoko.jar".
    //   * <p>
    //   * Example:<code>
    //   * getFileList("./test/", ".*\\.txt$");</code><br>
    //   * returns all text files in the directory "test".
    //   * This method is used if the program is started as web start application.
    //   *
    //   * @param rootDirectory the directory to start the search in
    //   * @param extensionPattern the pattern all relevant files have to match
    //   *
    //   * @return <code>ArrayList</code> of the found <code>File</code>s
    //   */
    //  public static ArrayList<String> getFileListFromJar(final String rootDirectory, final String extensionPattern ) {

    //    // ArrayList for storing all found file names.
    //    ArrayList<String> files = new ArrayList<String>(64);

    //    // Create the pattern for filtering the files.
    //    Pattern pattern = Pattern.compile(extensionPattern, Pattern.CASE_INSENSITIVE);

    //    try {
    //      // Read the main jar. Note: this is impossible if the program is started by web start, because
    //      // the operating system can store this jar file anywhere and rename it then.
    //      JarFile jarfile = new JarFile("JSoko.jar");

    //      for (Enumeration<JarEntry> em1 = jarfile.entries(); em1.hasMoreElements(); ) {
    //        JarEntry entry = em1.nextElement();

    //        // Only the relevant directory is processed.
    //        if(("/"+entry.getName()).indexOf(rootDirectory) == -1) {
    //          continue;
    //        }

    //        if(entry.isDirectory() == false && pattern.matcher(entry.getName()).matches()) {
    //          files.add("/"+entry.getName());
    //        }
    //      }

    //    } catch (IOException e) {
    //      if(Debug.isDebugModeActivated) {
    //        e.printStackTrace();
    //      }
    //    }

    //    // Return the found file names.
    //    return files;
    //  }

    /// <summary>
    /// Returns the "JSoko icon".
    /// </summary>
    /// <returns>the icon of JSoko</returns>
    public static Bitmap GetJSokoIcon()
    {
      try
      {
        return new Bitmap(Settings.Get("iconFolder") + "JSoko-Icon.png");
      }
      catch
      {
        return null;
      }
    }


    //  /**
    //   * Debug method: Displays the class path.
    //   */
    //  public static void showClassPath() {

    //    //Get the URLs
    //    URL[] urls = ((URLClassLoader)ClassLoader.getSystemClassLoader()).getURLs();

    //    for(URL url : urls) {
    //      System.out.println(url.getFile());
    //    }
    //  }


    //  /**
    //   * Computes the maximum amount of RAM (in bytes) the program may use.
    //   * <p>
    //   * This computation can be a bit confusing, so I explain it:<br>
    //   * - The <code>freeMemory</code> is that part of
    //   *   the <code>totalMemory</code>, which is just now
    //   *   not allocated to some object.  Running the GC
    //   *   may change this value.<br>
    //   * - The <code>totalMemory</code> is what the JVM currently
    //   *   has obtained from the OS for user objects.<br>
    //   * - The <code>maxMemory</code> is the (upper) limit for
    //   *   the <code>totalMemory</code>, which the JVM will never exceed.
    //   *   If there is no limit, we get <code>Long.MAX_VALUE</code>.
    //   * <p>
    //   * Obviously, we want to include <code>freeMemory</code>.
    //   * Adding <code>(maxMemory - totalMemory)</code> is the attempt
    //   * to add that amount, which the JVM later will also allocate
    //   * from the OS, in case we demand more memory.
    //   *
    //   * @return the maximum amount of RAM the program may use
    //   * @see #getMaxUsableRAMinMiB()
    //   */
    //  public static long getMaxUsableRAMInBytes() {
    //    Runtime rt = Runtime.getRuntime();
    //    long usableRAM = 0;

    //    usableRAM += rt.freeMemory();						// available just now
    //    usableRAM += rt.maxMemory() - rt.totalMemory();		// future potential
    //    return usableRAM;
    //  }

    //  /**
    //   * Computes the maximum amount of RAM (in MiB) the program may use.
    //   * @return  the maximum amount of MiB the program may use
    //   * @see #getMaxUsableRAMInBytes()
    //   */
    //  public static long getMaxUsableRAMinMiB() {
    //    return getMaxUsableRAMInBytes() / (1024 * 1024);
    //  }

    //  /**
    //   * Creates an icon containing the passed character.
    //   *
    //   * @param iconSize the size of the icon in pixels
    //   * @param letter the letter the icon should show
    //   * @param backgroundColor the background color of the icon
    //   * @return the icon
    //   */
    //  public static ImageIcon getIconWithCharacter(int iconSize, char letter, Color backgroundColor) {

    //    // Create a buffered image for creating the icon image.
    //    BufferedImage image = new BufferedImage(iconSize, iconSize, BufferedImage.TYPE_INT_ARGB);

    //    // Set the image completely transparent.
    //    for (int col = 0; col < iconSize; col++) {
    //      for (int row = 0; row < iconSize; row++) {
    //        image.setRGB(col, row, 0x0);
    //      }
    //    }
    //    Graphics2D graphics = (Graphics2D) image.getGraphics();
    //    graphics.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);
    //    graphics.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);

    //    // Set the background color.
    //    graphics.setColor(backgroundColor);
    //    graphics.fillOval(0, 0, iconSize - 1, iconSize - 1);

    //    // Create a whitish spot in the left-top corner of the icon for a better look.
    //    double id4 = iconSize / 4.0;
    //    double spotX = id4;
    //    double spotY = id4;
    //    for (int col = 0; col < iconSize; col++) {
    //      for (int row = 0; row < iconSize; row++) {

    //        // Distance to spot.
    //        double dx = col - spotX;
    //        double dy = row - spotY;
    //        double dist = Math.hypot(dx, dy);

    //        // distance of 0.0 - comes 90% to Color.white
    //        // distance of the icon size - stays the same
    //        if (dist > iconSize) {
    //          dist = iconSize;
    //        }

    //        int currColor = image.getRGB(col, row);
    //        int transp = (currColor >>> 24) & 0xFF;
    //        int oldR   = (currColor >>> 16) & 0xFF;
    //        int oldG   = (currColor >>>  8) & 0xFF;
    //        int oldB   = (currColor >>>  0) & 0xFF;

    //        double coef = 0.9 - 0.9 * dist / iconSize;
    //        int dr = 255 - oldR;
    //        int dg = 255 - oldG;
    //        int db = 255 - oldB;

    //        int newR = (int) (oldR + coef * dr);
    //        int newG = (int) (oldG + coef * dg);
    //        int newB = (int) (oldB + coef * db);

    //        int newColor = (transp << 24) | (newR << 16) | (newG << 8) | newB;
    //        image.setRGB(col, row, newColor);
    //      }
    //    }

    //    // Draw the outline of the icon.
    //    graphics.setColor(backgroundColor.darker());
    //    graphics.drawOval(0, 0, iconSize - 1, iconSize - 1);

    //    // Set the font for the letter.
    //    graphics.setFont(new Font("Arial", Font.BOLD, iconSize-5));

    //    // Calculate the position for the character.
    //    FontRenderContext frc = graphics.getFontRenderContext();
    //    TextLayout mLayout = new TextLayout("" + letter, graphics.getFont(), frc);
    //    float x = (float) ((iconSize - mLayout.getBounds().getWidth()) / 2);
    //    float y = iconSize - (float) ((iconSize - mLayout.getBounds().getHeight()) / 2);

    //    // Draw the letter into the graphic.
    //    graphics.drawString("" + letter, x, y);

    //    // Return the image as an icon.
    //    return new ImageIcon(image);
    //  }

    //  private static ListenerCaller<Component, Void> makeUpdateUIcaller() {
    //    return (new ListenerCaller<Component, Void>() {
    //      @Override
    //      public void call(Component c, Void v) {
    //        SwingUtilities.updateComponentTreeUI(c);
    //      }
    //    });
    //  }

    //  /**
    //   * Updates the UI. Must be called after the look&feel has been changed.
    //   */
    //  public static void updateUI() {

    //    // Install the JideExtension.
    //    try {
    //      LookAndFeelFactory.installJideExtension(LookAndFeelFactory.XERTO_STYLE_WITHOUT_MENU);
    //    }catch(Exception e) {}

    //    // Update all swing components.
    //    for( Frame f : Frame.getFrames() ) {
    //      SwingUtilities.updateComponentTreeUI(f);
    //      for( Window w : f.getOwnedWindows()  ) {
    //        SwingUtilities.updateComponentTreeUI(w);
    //      }
    //    }

    //    // Update the components which aren't contained in any Frame or Window
    //    // (popups, filechooser, ...).  These components must have been added
    //    // manually to the list before this method is called!
    //    uiComponentSet.informAllUnsync(null);
    //  }

    //  /**
    //   * This method is used to collect components that have to update their UI
    //   * when the look&feel changes.
    //   *
    //   * @param c the component to be added
    //   */
    //  public static void addComponentToUpdateUI(Component c) {
    //    uiComponentSet.register(c);
    //  }

    //  /**
    //   * In the specified table all nominally invisible columns shall not be
    //   * displayed, by setting their width to {@code 0}.
    //   *
    //   * @param table  the table to be handled
    //   * @param colvis the nominal column visibility (typically the model)
    //   */
    //  public static void tableHideInvisibleColumns(JTable table, ColumnVisibility colvis) {

    //    // Hide all columns that are not to be displayed
    //    for(int columnNo=table.getColumnCount(); --columnNo>=0; ) {
    //      if(colvis.isColumnVisible(columnNo) == false) {
    //        String columnName = table.getColumnName(columnNo);

    //        table.getColumn(columnName).setMinWidth(0);
    //        table.getColumn(columnName).setMaxWidth(0);
    //      }
    //    }
    //  }

    //  /**
    //   * Returns a list of file names read from the passed file.
    //   * <p>
    //   * This method is used when JSoko is started as web start application.
    //   * In this case for instance the background image names are stored
    //   * in a specific file which is read by JSoko to display the available
    //   * background images.
    //   *
    //   * @param filename the name of the file to be loaded
    //   * @return the read file names
    //   */
    //  public static ArrayList<String> getWebStartData(String filename) {

    //    // Read line from the input file.
    //    String dataRow = null;

    //    // The array of strings of filenames to be returned.
    //    ArrayList<String> fileNames = new ArrayList<String>();

    //    // Create BufferedReader for the input file.
    //    BufferedReader file = Utilities.getBufferedReader(filename);

    //    // The file hasn't been found => return an empty list.
    //    if(file == null) {
    //      return new ArrayList<String>(0);
    //    }

    //    // Read in line by line of the input data.
    //    try {
    //      while ((dataRow = file.readLine()) != null) {
    //        fileNames.add(dataRow);
    //      }
    //    } catch (IOException e1) {}
    //    finally {
    //      try {
    //        file.close();
    //      } catch (IOException e) {}
    //    }

    //    return fileNames;
    //  }

    //  /**
    //   * Returns the clipboard content as <code>String</code>.
    //   *
    //   * @return null, or the content of the clipboard
    //   */
    //  public static String getStringFromClipboard() {

    //    try {
    //      // Import data from clipboard.
    //      Transferable contents = Toolkit.getDefaultToolkit().getSystemClipboard().getContents(null);

    //      // If it is a string then return it.
    //      if(contents != null && contents.isDataFlavorSupported(DataFlavor.stringFlavor)) {
    //        return (String) contents.getTransferData(DataFlavor.stringFlavor);
    //      }

    //    }catch (Exception ex) {}

    //    // No valid data has been found. Therefore return null.
    //    return null;
    //  }

    //  /**
    //   * Puts the passed string to the system clipboard.
    //   *
    //   * @param strToPut string to be put to the clipboard
    //   * @param owner    null, or the new owner of the clipboard
    //   */
    //  public static void putStringToClipboard(String strToPut, ClipboardOwner owner) {
    //    StringSelection ss = new StringSelection(strToPut);
    //    Toolkit.getDefaultToolkit().getSystemClipboard().setContents(ss, owner);
    //  }

    //  /**
    //   * Puts the passed string to the system clipboard, without setting up an owner.
    //   *
    //   * @param strToPut string to be put to the clipboard
    //   */
    //  public static void putStringToClipboard(String strToPut) {
    //    putStringToClipboard(strToPut, null);
    //  }

    //  /**
    //   * Normalize the result of a comparison
    //   * (e.g. from {@link Comparable#compareTo(Object)}) to -1|0|+1.
    //   * @param cmpres the comparison result so far
    //   * @return the normalized comparison result: -1|0|+1.
    //   */
    //  public static final int normCompareResult(int cmpres) {
    //    return Integer.signum(cmpres);
    //  }

    //  /**
    //   * Compare two integer values in naive ascending order.
    //   * Equivalent to {@code new Int(x).compareTo(y)}.
    //   *
    //   * @param x  first  value to compare
    //   * @param y  second value to compare
    //   * @return <code>-1</code> if x is less than    y,
    //   *     <br><code> 0</code> if x is equal to     y and
    //   *     <br><code>+1</code> if x is greater than y.
    //   */
    //  public static final int intCompare1Pair(int x, int y) {
    //    if (x != y) {
    //      return (x < y) ? -1 : +1;
    //    }
    //    return 0;			// no difference detected
    //  }

    //  /**
    //   * Compare two integer value sequences of length 2, in naive dictionary order.
    //   * The element values of the vectors "x" and "y" are given in pairs,
    //   * highest comparison order elements first.
    //   *
    //   * @param x1 first value of x vector
    //   * @param y1 first value of y vector
    //   * @param x2 second value of x vector
    //   * @param y2 second value of y vector
    //   * @return <code>-1</code> if (x1,x2) is less than    (y1,y2),
    //   *     <br><code> 0</code> if (x1,x2) is equal to     (y1,y2) and
    //   *     <br><code>+1</code> if (x1,x2) is greater than (y1,y2).
    //   */
    //  public static final int intCompare2Pairs(int x1, int y1, int x2, int y2) {
    //    if (x1 != y1) {
    //      // This high pair is going to decide it...
    //      return (x1 < y1) ? -1 : +1;
    //    }
    //    if (x2 != y2) {
    //      // This second pair is going to decide it...
    //      return (x2 < y2) ? -1 : +1;
    //    }
    //    return 0;			// no difference detected
    //  }

    //  /**
    //   * Compare two integer value sequences of length 3, in naive dictionary order.
    //   * Values are given in pairs, highest order first.
    //   * The element values of the vectors "x" and "y" are given in pairs,
    //   * highest comparison order elements first.
    //   *
    //   * @param x1 first value of x vector
    //   * @param y1 first value of y vector
    //   * @param x2 second value of x vector
    //   * @param y2 second value of y vector
    //   * @param x3 third value of x vector
    //   * @param y3 third value of y vector
    //   * @return <code>-1</code> if (x1,x2,x3) is less than    (y1,y2,y3),
    //   *     <br><code> 0</code> if (x1,x2,x3) is equal to     (y1,y2,y3) and
    //   *     <br><code>+1</code> if (x1,x2,x3) is greater than (y1,y2,y3).
    //   */
    //  public static final int intCompare3Pairs(int x1, int y1, int x2, int y2, int x3, int y3) {
    //    if (x1 != y1) {
    //      // This high pair is going to decide it...
    //      return (x1 < y1) ? -1 : +1;
    //    }
    //    if (x2 != y2) {
    //      // This second pair is going to decide it...
    //      return (x2 < y2) ? -1 : +1;
    //    }
    //    if (x3 != y3) {
    //      // This third pair is going to decide it...
    //      return (x3 < y3) ? -1 : +1;
    //    }

    //    return 0;			// no difference detected
    //  }

    //  /**
    //   * Compare two integer value sequences of length 4, in naive dictionary order.
    //   * Values are given in pairs, highest order first.
    //   * The element values of the vectors "x" and "y" are given in pairs,
    //   * highest comparison order elements first.
    //   *
    //   * @param x1 first value of x vector
    //   * @param y1 first value of y vector
    //   * @param x2 second value of x vector
    //   * @param y2 second value of y vector
    //   * @param x3 third value of x vector
    //   * @param y3 third value of y vector
    //   * @param x4 fourth value of x vector
    //   * @param y4 fourth value of y vector
    //   * @return <code>-1</code> if (x1,x2,x3,x4) is less than    (y1,y2,y3,y4),
    //   *     <br><code> 0</code> if (x1,x2,x3,x4) is equal to     (y1,y2,y3,y4) and
    //   *     <br><code>+1</code> if (x1,x2,x3,x4) is greater than (y1,y2,y3,y4).
    //   */
    //  public static final int intCompare4Pairs( int x1, int y1,
    //                                        int x2, int y2,
    //                                        int x3, int y3,
    //                                        int x4, int y4 )
    //  {
    //    if (x1 != y1) {
    //      // This high pair is going to decide it...
    //      return (x1 < y1) ? -1 : +1;
    //    }
    //    if (x2 != y2) {
    //      // This second pair is going to decide it...
    //      return (x2 < y2) ? -1 : +1;
    //    }
    //    if (x3 != y3) {
    //      // This third pair is going to decide it...
    //      return (x3 < y3) ? -1 : +1;
    //    }
    //    if (x4 != y4) {
    //      // This fourth pair is going to decide it...
    //      return (x4 < y4) ? -1 : +1;
    //    }

    //    return 0;			// no difference detected
    //  }

    //  /**
    //   * Compares two integer value sequences in naive dictionary order.
    //   * Values are given in pairs, highest order first.
    //   * <p>
    //   * Example:<br>
    //   * intComparePairs(5, 5, 6, 17, 3, 2)<br>
    //   * first compares 5 with 5 and since they are equal, compares 6 with 17.
    //   * Since 6 is less than 17 "-1" is returned.
    //   * <p>
    //   * An even number of parameters must be passed to this method, otherwise
    //   * an {@code InvalidParameterException} is thrown.
    //   *
    //   * @param valuePairs  the values to be compared (a1, a2, b1, b2, c1, c2, ...)
    //   * @return <code>-1</code> if a1 < a2 or a1 == a2 and b1 < b2 or ...
    //   *     <br><code> 0</code> if a1 == a2 and b1 == b2 and ...
    //   *     <br><code>+1</code> if a1 > a2 or a1 == a2 and b1 > b2 or ...
    //   */
    //  public static final int intComparePairs(int ... valuePairs) {
    //    if((valuePairs.length&1) != 0) {
    //      throw new InvalidParameterException();
    //    }

    //    for(int i=0; i<valuePairs.length; i+=2) {
    //      if(valuePairs[i] != valuePairs[i+1]) {
    //        return valuePairs[i] < valuePairs[i+1] ? -1 : +1;
    //      }
    //    }

    //    return 0;
    //  }

    //  /**
    //   * Converts the passed collection to an integer array.
    //   *
    //   * @param coll the collection to convert
    //   * @return an integer array containing the content of the passed collection
    //   */
    //  public static int[] toIntArray(Collection<Integer> coll) {
    //    Iterator<Integer> iter = coll.iterator();
    //    int[] arr = new int[coll.size()];
    //    int i = 0;
    //    while (iter.hasNext()) {
    //      arr[i++] = iter.next().intValue();
    //    }
    //    return arr;
    //  }

    //    /**
    //     * Shuts down the passed {@code ExecutorService} and waits until all tasks have terminated.
    //     *
    //     * @param executor {@code ExecutorService} to shutdown and wait for termination
    //     * @param timeout the maximum time to wait
    //     * @param unit the time unit of the timeout argument
    //     */
    //  public static void shutdownAndAwaitTermination(ExecutorService executor, long timeout, TimeUnit unit) {

    //    // Disable new tasks from being submitted
    //    executor.shutdown();

    //    try {
    //      // Wait for existing tasks to terminate.
    //      if (!executor.awaitTermination(timeout, unit)) {
    //        executor.shutdownNow(); // Cancel currently executing tasks
    //        // Wait a while for tasks to respond to being cancelled
    //        executor.awaitTermination(3, TimeUnit.SECONDS);
    //        return;
    //      }
    //    } catch (InterruptedException ie) {
    //      // (Re-)Cancel if current thread also interrupted
    //      executor.shutdownNow();

    //      try {
    //        executor.awaitTermination(timeout, unit);
    //      } catch (InterruptedException e) { }

    //      // Preserve interrupt status
    //      Thread.currentThread().interrupt();
    //    }
    //  }


    //   /**
    //     * Returns a list of all the selected items, in increasing order based
    //     * on their indices in the list.
    //     * <p>
    //     * This method is used to stay compatible with Java 6.
    //     * @param list the list the selected items are read from
    //     *
    //     * @return the selected items, or an empty list if nothing is selected
    //     */
    //    public static <E> List<E> getSelectedValuesList(JList list) {
    //        ListSelectionModel sm = list.getSelectionModel();
    //        ListModel dm = list.getModel();

    //        int iMin = sm.getMinSelectionIndex();
    //        int iMax = sm.getMaxSelectionIndex();

    //        if ((iMin < 0) || (iMax < 0)) {
    //            return Collections.emptyList();
    //        }

    //        List<E> selectedItems = new ArrayList<E>();
    //        for(int i = iMin; i <= iMax; i++) {
    //            if (sm.isSelectedIndex(i)) {
    //                selectedItems.add((E) dm.getElementAt(i));
    //            }
    //        }
    //        return selectedItems;
    //    }

    //    /**
    //     * Returns the name of the file corresponding to the passed file path.
    //     *
    //     * @param filePath the path to the file
    //     * @return the file name
    //     */
    //    public static String getFileName(String filePath) {
    //      if(filePath != null) {
    //        int indexOfFileSeparator = filePath.lastIndexOf(File.separator);
    //        if(indexOfFileSeparator == -1) {
    //          indexOfFileSeparator = filePath.lastIndexOf("/");
    //        }
    //        String fileNameWithoutPath = (indexOfFileSeparator == -1) ? filePath : filePath.substring(++indexOfFileSeparator);
    //        filePath = fileNameWithoutPath;
    //      }

    //      return filePath;
    //    }

    //    /**
    //     * Fills a two dimensional array with the given value.
    //     *
    //     * @param array  the array to be filled
    //     * @param value  the value to be set
    //     */
    //    public static void fillArray(int[][] array, int value) {
    //        for(int[] subarray : array) {
    //            Arrays.fill(subarray, value);
    //        }
    //    }

    //    /**
    //     * Fills a three dimensional array with the given value.
    //     *
    //     * @param array  the array to be filled
    //     * @param value  the value to be set
    //     */
    //    public static void fillArray(int[][][] array, int value) {
    //        for(int[][] subarray : array) {
    //           for(int[] subarray2 : subarray) {
    //                 Arrays.fill(subarray2, value);
    //             }
    //        }
    //    }

    //    /**
    //     * Fills a two dimensional array with the given value.
    //     *
    //     * @param array  the array to be filled
    //     * @param value  the value to be set
    //     */
    //    public static void fillArray(short[][] array, short value) {
    //        for(short[] subarray : array) {
    //            Arrays.fill(subarray, value);
    //        }
    //    }

    //    /**
    //     * Fills a three dimensional array with the given value.
    //     *
    //     * @param array  the array to be filled
    //     * @param value  the value to be set
    //     */
    //    public static void fillArray(short[][][] array, short value) {
    //        for(short[][] subarray : array) {
    //           for(short[] subarray2 : subarray) {
    //                 Arrays.fill(subarray2, value);
    //             }
    //        }
    //    }

    //    /**
    //     * Removes all trailing space characters.
    //     *
    //     * @param data the data to remove trailing spaces from
    //     */
    //    public static void removeTrailingSpaces(StringBuilder data) {
    //    int lastIndex = data.length() - 1;
    //    while(lastIndex > 0 && data.charAt(lastIndex) == ' ') {
    //      lastIndex--;
    //    }
    //    data.setLength(lastIndex+1);
    //    }

    //    /**
    //     * Creates the given directory if it does not exist, otherwise expects
    //     * it to be writable.
    //     *
    //     * @param directory the <code>File</code> specifying the required directory
    //     * @return the required directory, or <code>null</code> on failure
    //     */
    //    public static File createDiretory(String directoryPath) {

    //      if(directoryPath == null) {
    //      return null;
    //    }

    //      File directory = new File(directoryPath);
    //        if (directory.exists()) {
    //            if (directory.isDirectory() && directory.canWrite()) {
    //        return directory;
    //      }
    //        } else {
    //            if (directory.mkdir()) {
    //        return directory;
    //      }
    //        }
    //        return null;
    //    }

    //    /**
    //     * The passed {@link JDialog} will be closeable by pressing the "Escape" key after
    //     * calling this method.
    //     * <p>
    //     * Note: This method must be called BEFORE the {@link JDialog} is set visible.
    //     *
    //     * @param dialog
    //     */
    //    @SuppressWarnings("serial")
    //  public static void setEscapable(final JDialog dialog) {

    //       // Close the dialog when "escape" has been pressed.
    //    dialog.getRootPane().getInputMap(JComponent.WHEN_ANCESTOR_OF_FOCUSED_COMPONENT)
    //    .put(KeyStroke.getKeyStroke(KeyEvent.VK_ESCAPE, 0), "close");

    //    dialog.getRootPane().getActionMap().put("close", new AbstractAction() {
    //      @Override
    //      public void actionPerformed(ActionEvent e) {
    //        dialog.dispose();
    //      }
    //    });
    //    }
  }
}
