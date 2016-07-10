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
using System.IO;
using JSoko.Gui_;
using JSoko.java.util;
using JSoko.OsSpecific_;
using JSoko.Utilities_;
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Global
#pragma warning disable 414

#endregion

namespace JSoko.ResourceHandling
{
  /// <summary>
  /// This class handles all settings operations. That is:
  /// 
  /// - loading settings from the hard disk
  /// - holding the settings while the application is running
  /// - saving the settings to the hard disk when the application is closing
  /// 
  /// All methods are static. Hence this class can be used without having an instance of it in every other object. All settings are "public static" variables and therefore can be directly accessed from every object.
  /// </summary>
  public static class Settings
  {
    /// <summary>
    /// File name of the program settings (skeleton). Set up in {@link #loadSettings(JSoko)}.
    /// </summary>
    private static string defaultSettingsFilename;

    /// <summary>
    /// Stores the currently effective property values (settings). Some of them are cached into program variables, in part by using the local annotation
    /// 
    /// Properties that have a program variable may not be up to date, since when the program variables are changed, the properties are left alone (unchanged).
    /// They are changed (synchronized from the program variables) immediately before saving them to disk, again.
    /// </summary>
    private static Properties_ settings;

    /// <summary>
    /// Stores the properties as loaded from the skeleton settings file, as distributed with the program. We save this data to check the keys when we think about property name changes.
    /// </summary>
    private static Properties_ defaultSettings;

    /// <summary>
    /// Direction of the solver search.
    /// </summary>
    public enum SearchDirection
    {
      /// <summary>
      /// Forward search
      /// </summary>
      Forward,

      /// <summary>
      /// Backward search
      /// </summary>
      Backward,

      /// <summary>
      /// Backward goal room search
      /// </summary>
      BackwardGoalRoom,

      /// <summary>
      /// Unknown search direction
      /// </summary>
      Unknown
    }

    /// <summary>
    /// Version of this program automatically set by the build file. Do not annotate it, it is handled specially.
    /// </summary>
    public static string programVersion;

    /// <summary>
    /// Constant for the line separator.
    /// </summary>
    public static readonly string LineSeparator = Environment.NewLine;

    /// <summary>
    /// x offset of the board elements shown in the editor at the left side
    /// </summary>
    public const int ObjectXoffset = 10;

    /// <summary>
    /// y offset of the first board element shown in the editor at the left side
    /// </summary>
    public const int FirstObjectYoffset = 60;

    /// <summary>
    /// y distance between the board elements shown in the editor at the left side
    /// </summary>
    public const int ObjectsYdistance = 10;

    /// <summary>
    /// Number of pixel the board is shifted right to make way for the editor elements.
    /// </summary>
    public const int XOffsetEditorelements = 50;

    /// <summary>
    /// Number of pixels the board is displayed away from the left border of the panel.
    /// </summary>
    public const int XBorderOffset = 20;

    /// <summary>
    /// Maximum size of a level (maximum rows / columns) (gross)
    /// </summary>
    public const int MaximumBoardsize = 70;

    /*
     * Variables that are saved in the settings file of the user. These variables should always correspond to those in the methods
     * "setSettingsFromProgramVariables" and "setProgramVariablesFromSettings"! Except those annotated with @SettingsVar
     *
     * ----------------------------------------------------------------------- Name changing thoughts ...
     *
     * Fixing the property name and changing the name of the program variable is not really a problem. But what about changing the property name?
     *
     * That would result in - a skeleton file with the new name, and - a user file with the old name. - after loading properties we have them both (in-core)
     *
     * Now we would like to detect that condition, and - transfer the user value from the old name - to the property with the new name - and delete the
     * (transfered) property with the old name
     *
     * Then, when saving our properties, and scanning along the skeleton with the new name, we save it normally for that (new) name, and since we deleted the
     * old name from the in-core properties, we do NOT try to save the old named property as if it were new.
     *
     * Well, when and how can we detect that? - We may use our annotation to state a renaming. That makes candidates for this operation. - Either during loading
     * or during restore we would like to check candidates. Since we expect the program itself to rather use the new name, and the old name to occur just in the
     * old user settings file, we should detect that early, i.e. during the initial loading from file.
     */

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)] // , AllowMultiple = true
    sealed class SettingsVar : Attribute { }

    /// <summary>
    /// Time delay in milliseconds between the movements on the board.
    /// </summary>
    [SettingsVar]
    public static short delayValue = 55;

    /// <summary>
    /// Time delay in milliseconds between the undo/redo movements on the board.
    /// </summary>
    [SettingsVar]
    public static short delayValueUndoRedo = 35;

    /// <summary>
    /// Flag specifying whether the single step undo/redo is activated.
    /// </summary>
    [SettingsVar]
    public static bool singleStepUndoRedo = false;

    /// <summary>
    /// Flag specifying whether the reachable squares of a box are to be highlighted.
    /// </summary>
    [SettingsVar]
    public static bool showReachableBoxPositions = true;

    /// <summary>
    /// Flag specifying whether sound effects are to be played.
    /// </summary>
    [SettingsVar]
    public static bool soundEffectsEnabled = true;

    /// <summary>
    /// Path to the settings file of the currently set skin.
    /// </summary>
    public static string currentSkin;

    /// <summary>
    /// Current Look &amp; Feel
    /// </summary>
    public static string currentLookAndFeel = "";

    /// <summary>
    /// Flag specifying whether the simple deadlock squares are to be highlighted.
    /// </summary>
    [SettingsVar]
    public static bool showDeadlockFields = false;

    /// <summary>
    /// Flag specifying whether the minimum solution length is to be displayed.
    /// </summary>
    [SettingsVar]
    public static bool showMinimumSolutionLength = false;

    /// <summary>
    /// Flag specifying whether reversely played moves should be treated as undo.
    /// </summary>
    [SettingsVar]
    public static bool treatReverseMovesAsUndo = true;

    /// <summary>
    /// Flag specifying whether moves between pushes should be optimized automatically.
    /// </summary>
    [SettingsVar]
    public static bool optimizeMovesBetweenPushes = true;

    /// <summary>
    /// Flag specifying whether the simple deadlock detection is activated.
    /// </summary>
    [SettingsVar]
    public static bool detectSimpleDeadlocks = true;

    /// <summary>
    /// Flag specifying whether the freeze deadlock detection is activated.
    /// </summary>
    [SettingsVar]
    public static bool detectFreezeDeadlocks = true;

    /// <summary>
    /// Flag specifying whether the corral deadlock detection is activated.
    /// </summary>
    [SettingsVar]
    public static bool detectCorralDeadlocks = true;

    /// <summary>
    /// Flag specifying whether the bipartite deadlock detection is activated.
    /// </summary>
    [SettingsVar]
    public static bool detectBipartiteDeadlocks = true;

    /// <summary>
    /// Flag specifying whether the bipartite deadlock detection is activated.
    /// </summary>
    [SettingsVar]
    public static bool detectClosedDiagonalDeadlocks = true;

    /**
     * This factor defines, how many moves (of the player) outweigh a push (box move). This factor makes a difference when a target square (for a box) is
     * reachable on multiple different paths. Example: <br>
     * Solution 1: 12 player moves and 2 box pushes <br>
     * Solution 2: 8 player moves and 4 box pushes <br>
     * With a value <code>1</code> for <code>movesVSpushes</code> we would prefer solution 2, since <code>12 + 2 * movesVSPushes = 14</code>, but
     * <code>8 + 4 * movesVSPushes = 12</code> (and the smaller result wins).
     * <p>
     * The initial value <code>30000</code> is a kind of small infinity, and gives much more weight to the pushes, so we optimize for pushes, initially.
     */
    [SettingsVar]
    public static float movesVsPushes = 30000f;

    //  /**
    //   * Whether solution comparison (ordering) includes the minor metrics.
    //   */
    //  @Settings.SettingsVar(propertyName = "checkAll5Metrics")
    //  public static boolean checkAllMinorMetrics = true;

    //  /**
    //   * Last file path. This path is used to set useful default values for the next file dialog.
    //   */
    //  public static String lastFilePath;

    //  /** Last played level number . This level number is set as start level at the start of the program. */
    //  @Settings.SettingsVar
    //  public static int lastPlayedLevelNumber;

    //  /** Last played collections. */
    //  public static ArrayList<SelectableLevelCollection> lastPlayedCollections = new ArrayList<SelectableLevelCollection>();

    //  /** Optimizer settings */
    //  @Settings.SettingsVar
    //  public static int vicinitySquaresBox1 = 50;
    //  @Settings.SettingsVar
    //  public static int vicinitySquaresBox2 = 10;
    //  @Settings.SettingsVar
    //  public static int vicinitySquaresBox3 = 10;
    //  @Settings.SettingsVar
    //  public static int vicinitySquaresBox4 = 10;
    //  @Settings.SettingsVar
    //  public static boolean vicinitySquaresBox1Enabled = true;
    //  @Settings.SettingsVar
    //  public static boolean vicinitySquaresBox2Enabled = false;
    //  @Settings.SettingsVar
    //  public static boolean vicinitySquaresBox3Enabled = false;
    //  @Settings.SettingsVar
    //  public static boolean vicinitySquaresBox4Enabled = false;
    //  @Settings.SettingsVar
    //  public static boolean isIteratingEnabled = true;
    //  @Settings.SettingsVar
    //  public static boolean isOnlyLastSolutionToBeSaved = false;
    //  @Settings.SettingsVar
    //  public static int CPUCoresToUse = Runtime.getRuntime().availableProcessors();
    //  @Settings.SettingsVar
    //  public static int optimizerXCoordinate = -1;
    //  @Settings.SettingsVar
    //  public static int optimizerYCoordinate = -1;
    //  @Settings.SettingsVar
    //  public static int optimizerWidth		= 1024;
    //  @Settings.SettingsVar
    //  public static int optimizerHeight		= 800;
    //  @Settings.SettingsVar
    //  public static int optimizationMethod 	= OptimizationMethod.MOVES_PUSHES.ordinal();


    //  /** Solver settings */

    //  /**
    //   * Whether the solver shall obey a time limit.
    //   *
    //   * @see #solverTimeLimitInSeconds
    //   */
    //  @Settings.SettingsVar
    //  public static boolean isSolverTimeLimited = true;

    //  /**
    //   * The numerical value of the solvers time limit.
    //   *
    //   * @see #isSolverTimeLimited
    //   */
    //  @Settings.SettingsVar
    //  public static int solverTimeLimitInSeconds = 600;

    //  /**
    //   * Whether the solver shall display solutions.
    //   */
    //  @Settings.SettingsVar
    //  public static boolean isDisplaySolutionsEnabled = false;

    //  /** Coordinates and size of the application window. */
    //  public static Rectangle applicationBounds = new Rectangle(0, 0, 1024, 800);

    //  // @SettingsVar
    //  // public static boolean testVarBool = true;
    //  // @SettingsVar( oldNames={"testVarOldInt", "testVarVeryOldInt"} )
    //  // public static int testVarInt = 88;
    //  // @SettingsVar
    //  // public static String testVarStr = "heiner";

    //  // =======================================================================


    //  private Settings() {

    //  }

    //  /**
    //   * Avoid cloning: unconditionally throws {@code CloneNotSupportedException}.
    //   *
    //   * @return never anything
    //   */
    //  @Override
    //  public Object clone() throws CloneNotSupportedException {
    //    throw new CloneNotSupportedException();
    //  }

    /// <summary>
    /// Load the settings from the hard disk.
    /// </summary>
    /// <param name="application">reference to the main object</param>
    public static void LoadSettings(JSoko application)
    {
      // First set os specific settings like the directories to load/save data from.
      OsSpecific.SetOsSpecificSettings(application);

      // The default settings of JSoko.
      defaultSettingsFilename = "settings.ini";

      // Settings file for the user specific settings.
      string userSettingsFilename = OsSpecific.GetPreferencesDirectory() + "settings.ini";
      if (!File.Exists(userSettingsFilename))
      {
        userSettingsFilename = Utilities.GetBaseFolder() + "user_settings.ini"; // versions < 1.74 stored the file in the base folder
      }

      // The properties of this program (from skeleton).
      defaultSettings = new Properties_();

      /**
       * Load the default settings file.
       */
      var propertyInputStream = Utilities.GetBufferedReader(defaultSettingsFilename);
      if (propertyInputStream == null)
      {
        // Load language texts.
        Texts.LoadAndSetTexts();

        MessageDialogs.ShowErrorString(application, Texts.GetText("message.fileMissing", "settings.ini"));
        Environment.Exit(-1);
      }

      try
      {
        defaultSettings.Load(propertyInputStream);
      }
      catch (IOException)
      {
        // Load language texts.
        Texts.LoadAndSetTexts();

        MessageDialogs.ShowErrorString(application, Texts.GetText("message.fileMissing", "settings.ini"));
        Environment.Exit(-1);
      }

      try
      {
        propertyInputStream.Close();
      }
      catch (IOException) { }

      /**
       * Load the user specific settings file.
       */
      settings = new Properties_();

      // The default settings are taken as initial content.
      settings.PutAll(defaultSettings);

      // The program version is always read from the default settings file.
      programVersion = GetString("version", "");

      //    // Load the user settings.
      //    BufferedReader in = null;
      //    try {
      //      in = Utilities.getBufferedReader(userSettingsFilename);
      //      settings.load(in);
      //      in.close();
      //      in = null; // we are completely done with it
      //    } catch (Exception e) {
      //      /* Program starts with default settings. */
      //    } finally {
      //      if (in != null) {
      //        try {
      //          in.close();
      //        } catch (IOException e) {
      //        }
      //      }
      //    }

      //    // Some Settings must always be set to the default value.
      //    /* empty */

      //    // Set the program variables corresponding to the loaded settings.
      //    // Name changes for properties are also handled there.
      //    setProgramVariablesFromSettings();

      // todo
    }

    public static string Get(string name)
    {
      // todo
      if (name == "iconFolder") return "resources/graphics/icons/";

      return null;
    }
    //  /**
    //   * Returns the value of the setting parameter corresponding to the passed parameter key.
    //   * <p>
    //   * NB: Since we recur to {@link Hashtable#get(Object)}, which is even synchronized, this may become expensive. Really often needed properties should get
    //   * their own member variable.
    //   *
    //   * @param key
    //   *            key which identifies the parameter whose value is to be returned
    //   * @return value of the settings parameter, or {@code null}
    //   * @see Settings.SettingsVar
    //   */
    //  final public static String get(String key) {
    //    return settings.getProperty(key);
    //  }

    //  /**
    //   * Sets the passed value for the property with the passed key.
    //   * <p>
    //   * This method is used for properties that aren't performance critical and therefore need not be stored in an own variable in this class.
    //   *
    //   * @param key
    //   *            key of the property to set a new value for
    //   * @param value
    //   *            value to be set
    //   */
    //  public static void set(String key, String value) {
    //    settings.setProperty(key, value);
    //  }

    //  /**
    //   * Stores the passed collections as "last played" collections in the settings.
    //   *
    //   * @param lastPlayedCollections
    //   *            the last played level collections
    //   */
    //  public static void setLastPlayedCollections(List<SelectableLevelCollection> lastPlayedCollections) {

    //    // Add the collections in the format: databaseID;collection file
    //    int counter = 1;
    //    for (SelectableLevelCollection collection : lastPlayedCollections) {
    //      settings.setProperty("lastPlayedLevelCollection" + counter, collection.databaseID + "\n" + collection.title + "\n" + collection.file);
    //      counter++;
    //    }
    //  }

    //  /**
    //   * Returns the last played collection data stored in the settings file.
    //   *
    //   * @return the last played level collections stored in the ini settings file
    //   */
    //  public static List<SelectableLevelCollection> getLastPlayedCollections() {

    //    ArrayList<SelectableLevelCollection> levelCollections = new ArrayList<SelectableLevelCollection>();

    //    for (int counter = 1; counter < 1000; counter++) {

    //      String collectionData = settings.getProperty("lastPlayedLevelCollection" + counter);
    //      if (collectionData == null) {
    //        break;
    //      }

    //      String[] data = collectionData.split("\n");

    //      if (data.length == 0) {
    //        continue;
    //      }

    //      int databaseID = Database.NO_ID;
    //      try {
    //        databaseID = Integer.parseInt(data[0]);
    //      } catch (NumberFormatException e) {
    //      }

    //      String title = data.length > 1 ? data[1] : Texts.getText("unknown");
    //      String collectionFile = data.length > 2 ? data[2] : "";
    //      levelCollections.add(new SelectableLevelCollection(title, collectionFile, databaseID, true));
    //    }

    //    return levelCollections;
    //  }

    //  /**
    //   * Saves the settings on the hard disk.
    //   * <p>
    //   * Comments are saved as they were loaded. Deleted properties are commented using "!" as comment character. New properties are appended to the
    //   * "user_settings.ini" file.
    //   *
    //   * @throws IOException
    //   *             error while accessing the settings files
    //   */
    //  final static public void saveSettings() throws IOException {

    //    // Get the current program settings in our "Properties" object.
    //    setSettingsFromProgramVariables();

    //    // Clone the settings to have a copy that can be modified.
    //    Properties currentSettings = (Properties) settings.clone();

    //    // Create BufferedReader to the old settings file.
    //    // We do NOT use the actual file from the last call to this method,
    //    // but we rather use the default settings file as a skeleton!
    //    BufferedReader oldSettingsFile = Utilities.getBufferedReader(defaultSettingsFilename);
    //    // Can be null!

    //    final String userSettingsFilename = OSSpecific.getPreferencesDirectory() + "settings.ini";

    //    // Create PrintWriter for creating a new settings file containing the current settings.
    //    final String tmpFilename =  userSettingsFilename + ".tmp";
    //    PrintWriter newSettingsFile = new PrintWriter(tmpFilename, "UTF-8");

    //    // Write the current date to the settings file.
    //    newSettingsFile.println("# Creation date: " + new Date().toString());

    //    try {
    //      // Read line by line from the original settings file and compare it
    //      // with the current settings in the program.
    //      String line;

    //      while ((line = oldSettingsFile.readLine()) != null) {

    //        // Property key and property value
    //        String key;
    //        String value;

    //        // Trimmed line of the settings file.
    //        String trimmedLine = line.trim();

    //        // Just copy empty lines.
    //        if (trimmedLine.length() == 0) {
    //          newSettingsFile.println();
    //          continue;
    //        }

    //        // Copy all comment lines, starting with "#".
    //        if (trimmedLine.charAt(0) == '#') {
    //          newSettingsFile.println(line);
    //          continue;
    //        }

    //        // Get the first index of an assignment character.
    //        int index = trimmedLine.indexOf("=");
    //        if (index == -1) {
    //          index = trimmedLine.indexOf(":");
    //        }

    //        // Just copy all lines without any assignment character.
    //        if (index == -1) {
    //          newSettingsFile.println(line);
    //          continue;
    //        }

    //        // All lines starting with "!" are logically deleted properties.
    //        if (trimmedLine.charAt(0) == '!') {

    //          // Extract property key and value.
    //          key = trimmedLine.substring(1, index).trim();

    //          // Get the value of the property corresponding to the key,
    //          // and reduce the local settings collection.
    //          value = (String) currentSettings.remove(key);

    //          // If the property doesn't exist in the current settings
    //          // just copy the old line.
    //          if (value == null) {
    //            newSettingsFile.println(line);
    //            continue;
    //          }
    //        } else {
    //          // Get the key from the current settings file.
    //          key = trimmedLine.substring(0, index).trim();

    //          // Get the value of the key from the current settings,
    //          // and reduce the local settings collection.
    //          value = (String) currentSettings.remove(key);

    //          // If the property doesn't exist in the current settings
    //          // add it as comment to the new settings file.
    //          if (value == null) {
    //            newSettingsFile.println("! " + line);
    //            continue;
    //          }
    //        }

    //        // Add the property and its current value to the new settings file.
    //        switch (trimmedLine.charAt(index)) {
    //        case '=':
    //          newSettingsFile.println(key + " = " + mask(value));
    //          break;
    //        case ':':
    //          newSettingsFile.println(key + ": " + mask(value));
    //          break;
    //        }
    //      }

    //      // If there are some properties left, they must be new ones,
    //      // i.e. not yet contained in our skeleton file.
    //      // They are appended to the new settings file.
    //      if (currentSettings.size() > 0) {

    //        newSettingsFile.println();

    //        for (Object key : currentSettings.keySet()) {

    //          String strkey = key.toString();

    //          // Not used setting keys are deleted from old settings files.
    //          if (strkey.startsWith("language") || strkey.equals("lastPlayedCollectionPath") || strkey.equals("startLevel")) {
    //            continue;
    //          }

    //          String value = currentSettings.getProperty(strkey);
    //          newSettingsFile.println(key + " = " + mask(value));

    //          if (Debug.isSettingsDebugModeActivated) {
    //            System.out.println("Warning: new (unknown) settings saved! Key: " + key);
    //          }
    //        }
    //      }
    //    } finally {
    //      // We are going to drop out of this normally (continuing below),
    //      // or we are going to jump out of this (with some exception).
    //      // We still do not want to leave opened any files, so ...
    //      if (oldSettingsFile != null) {
    //        oldSettingsFile.close();
    //      }
    //      newSettingsFile.close();
    //    }

    //    // Delete the original user settings file.
    //    new File(userSettingsFilename).delete();

    //    // Rename the new user settings file.
    //    new File(tmpFilename).renameTo(new File(userSettingsFilename));
    //  }

    //  /**
    //   * Handle the set of class fields with our annotation {@code SettingsVar}: either transfer their values to the properties, or set them from the properties.
    //   * When we use the local methods (like {@link #getInt(String, int)}, we give the old value of the field as a default.
    //   *
    //   * @param prop2var
    //   *            whether to copy property values to the annotated variables (or vice versa)
    //   */
    //  private static final void syncAnnotatedVars(boolean prop2var) {

    //    for (Field fld : Settings.class.getDeclaredFields()) {
    //      // System.out.println("Setting: check field: " + fld.getName());

    //      // We handle class fields, only (no instance fields)
    //      if (!Modifier.isStatic(fld.getModifiers())) {
    //        continue;
    //      }
    //      // System.out.println(" ... is static");

    //      final Settings.SettingsVar anno = fld.getAnnotation(Settings.SettingsVar.class);
    //      if (anno == null) {
    //        continue;
    //      }
    //      if (Debug.isSettingsDebugModeActivated) {
    //        System.out.println("  Setting: is annotated: " + fld.getName());
    //      }
    //      String pkey = anno.propertyName();
    //      if (pkey.length() == 0) {
    //        pkey = fld.getName();
    //      }
    //      if (Debug.isSettingsDebugModeActivated) {
    //        System.out.println("  + propertyName() -> " + pkey);
    //      }
    //      // The check whether the requested operation is to be done
    //      // for this annotation can be extracted from the type specific
    //      // code below, and done early ...
    //      if (!(prop2var ? anno.loadMe() : anno.saveMe())) {
    //        continue;
    //      }

    //      if (prop2var) {
    //        // Just loaded the properties from the file.
    //        // Here and now is a good point to handle property name changes.
    //        String[] oldNames = anno.oldNames();

    //        if ((oldNames != null) && (oldNames.length > 0)) {

    //          // Search for an old name present in-core,
    //          // but NOT in the skeleton data "defaultSettings"
    //          for (String oldname : oldNames) {
    //            String oldnamevalue = settings.getProperty(oldname);

    //            if ((oldnamevalue != null) && (defaultSettings.getProperty(oldname) == null)) {
    //              // Detected a property name change to happen!
    //              if (Debug.isSettingsDebugModeActivated) {
    //                System.out.println("Settings: transfer old key " + oldname + " to new key " + pkey);
    //              }
    //              settings.remove(oldname);
    //              settings.setProperty(pkey, oldnamevalue);

    //              // We do NOT search for even more old names!
    //              break;
    //            }
    //          }
    //        }
    //      }

    //      Class<?> fldtyp = fld.getType();
    //      if (Debug.isSettingsDebugModeActivated) {
    //        System.out.println("  field has type: " + fldtyp.getName());
    //      }

    //      // The failures that may occur in the following reflection code
    //      // are considered to be internal errors (and not shown to the user)
    //      try {
    //        // Now we must have special code for each primitive type
    //        // which we expect for fields with our SettingsVar annotation

    //        if (boolean.class.equals(fldtyp)) {
    //          final boolean fldval = fld.getBoolean(null);
    //          if (prop2var) { // Set var from property
    //            boolean propval = getBool(pkey, fldval);
    //            if (Debug.isSettingsDebugModeActivated) {
    //              System.out.println("  var=" + fldval + " prop=" + propval);
    //            }
    //            if (propval != fldval) {
    //              fld.setBoolean(null, propval);
    //            }
    //          } else { // Set property from var
    //            settings.setProperty(pkey, String.valueOf(fldval));
    //          }
    //          continue;
    //        }

    //        if (int.class.equals(fldtyp) || short.class.equals(fldtyp)) {
    //          final int fldval = fld.getInt(null);
    //          if (prop2var) { // Set var from property
    //            int propval = getInt(pkey, fldval);
    //            if (Debug.isSettingsDebugModeActivated) {
    //              System.out.println("  var=" + fldval + " prop=" + propval);
    //            }
    //            if (propval != fldval) {
    //              if (int.class.equals(fldtyp)) {
    //                fld.setInt(null, propval);
    //              } else {
    //                fld.setShort(null, (short) propval);
    //              }
    //            }
    //          } else { // Set property from var
    //            settings.setProperty(pkey, String.valueOf(fldval));
    //          }
    //          continue;
    //        }

    //        if (float.class.equals(fldtyp)) {
    //          final float fldval = fld.getFloat(null);
    //          if (prop2var) { // Set var from property
    //            float propval = getFloat(pkey, fldval);
    //            if (Debug.isSettingsDebugModeActivated) {
    //              System.out.println("  var=" + fldval + " prop=" + propval);
    //            }
    //            if (propval != fldval) {
    //              fld.setFloat(null, propval);
    //            }
    //          } else { // Set property from var
    //            settings.setProperty(pkey, String.valueOf(fldval));
    //          }
    //          continue;
    //        }

    //        if (String.class.equals(fldtyp)) {
    //          final Object fldobj = fld.get(null);
    //          if (fldobj instanceof String) {
    //            final String fldval = (String) fldobj;
    //            if (prop2var) { // Set var from property
    //              String propval = getString(pkey, fldval);
    //              if (Debug.isSettingsDebugModeActivated) {
    //                System.out.println("  var=" + fldval + " prop=" + propval);
    //              }
    //              if (!propval.equals(fldval)) {
    //                fld.set(null, propval);
    //              }
    //            } else { // Set property from var
    //              settings.setProperty(pkey, fldval);
    //            }
    //          } else {
    //            System.out.println("Settings: String!=String for " + fld.getName());
    //          }
    //          continue;
    //        }
    //      } catch (IllegalArgumentException e) {
    //        System.out.println("Settings: cannot handle SettingsVar " + fld.getName());
    //        e.printStackTrace();
    //        continue;
    //      } catch (IllegalAccessException e) {
    //        System.out.println("Settings: cannot handle SettingsVar " + fld.getName());
    //        e.printStackTrace();
    //        continue;
    //      }
    //      System.out.println("Settings: unhandled type of SettingsVar " + fld.getName());
    //    }
    //  }

    //  /**
    //   * Copies values from the just loaded properties into those variables (static fields) which are annotated as {@link Settings.SettingsVar}. Also handles property name
    //   * changes.
    //   */
    //  private static final void copyPropertiesToAnnotated() {
    //    syncAnnotatedVars(true);
    //  }

    //  /**
    //   * Stores the values of variables annotated {@link Settings.SettingsVar} into our properties for the following saving to a file.
    //   */
    //  private static final void copyAnnotatedToProperties() {
    //    syncAnnotatedVars(false);
    //  }

    //  /**
    //   * Puts the current program settings to the Property object.
    //   * <p>
    //   * Before the settings are saved the current settings must be put to the Property object. Therefore this method must be called before "save()" is called.
    //   */
    //  final private static void setSettingsFromProgramVariables() {

    //    /*
    //     * Put all variables to the property object. Annotated variables are handled in another method.
    //     */
    //    settings.setProperty("currentSkin", currentSkin);

    //    settings.setProperty("lastFilePath", lastFilePath != null ? lastFilePath : "");
    //    settings.setProperty("currentLookAndFeel", currentLookAndFeel);

    //    // Application bounds.
    //    // These can not easily be annotated, and are handled "manually"
    //    settings.setProperty("applicationXCoordinate", String.valueOf(applicationBounds.x));
    //    settings.setProperty("applicationYCoordinate", String.valueOf(applicationBounds.y));
    //    settings.setProperty("applicationWidth", String.valueOf(applicationBounds.width));
    //    settings.setProperty("applicationHeight", String.valueOf(applicationBounds.height));

    //    copyAnnotatedToProperties();
    //  }

    //  /**
    //   * Sets the program variables to the values from the loaded settings file. Also handles property name changes.
    //   */
    //  final private static void setProgramVariablesFromSettings() {

    //    // Set the language of the user if possible.
    //    // First the language of the settings file is used.
    //    // If there isn't one set, this is the first start of the program.
    //    // Then the language of the system properties is used.
    //    String userLanguageCode = Settings.getString("currentLanguage", "");
    //    if (userLanguageCode.length() == 0) {
    //      userLanguageCode = System.getProperties().getProperty("user.language");
    //    }

    //    // The language code the program has found for the user. Default is English.
    //    String validatedUserLanguageCode = "EN";

    //    // Check if the language of the user is supported by this program.
    //    // If yes, then set that language code instead of the default "EN".
    //    for (String languageCode : Translator.getAvailableLanguageCodes()) {
    //      if (userLanguageCode.equals(languageCode)) {
    //        validatedUserLanguageCode = userLanguageCode;
    //        break;
    //      }
    //    }

    //    // Set the determined language code as new language for JSoko.
    //    set("currentLanguage", validatedUserLanguageCode);

    //    // FFS/hm: up to here the above code does belong elsewhere.

    //    // Skin
    //    currentSkin = getString("currentSkin", "skin1");

    //    // Set the folder of the last loaded file to the folder loaded from settings file.
    //    lastFilePath = getString("lastFilePath", Settings.get("levelFolder"));

    //    // Information about the last played level number.
    //    lastPlayedLevelNumber = getInt("lastPlayedLevelNumber", 1);

    //    // Get the Look&Feel to set.
    //    // FFS/hm@mm: default value "nimRODLookAndFeel" also for the field declaration?
    //    currentLookAndFeel = getString("currentLookAndFeel", "nimRODLookAndFeel");

    //    // Application bounds: these values needn't to be stored in class
    //    // variables because they aren't important for the performance.

    //    copyPropertiesToAnnotated();

    //    // if (isSettingsDebugModeActivated) {
    //    // ++testVarInt;
    //    // testVarStr = "Y " + testVarStr;
    //    // }
    //  }

    private static string GetString(string name, string defaultValue)
    {
      // todo
      return "";
    }
    //  /**
    //   * Returns the string corresponding to the passed property name.
    //   *
    //   * @param name
    //   *            name of property
    //   * @param defaultValue
    //   *            value to be set if the property value can't be set
    //   * @return value of the property as string or {@code null}, if no property is found
    //   */
    //  final public static String getString(String name, String defaultValue) {

    //    // Get the value of the property.
    //    String propertyValue = trimValue(settings.getProperty(name));

    //    // If the the property couldn't be found set the default value.
    //    if (propertyValue == null) {
    //      settings.setProperty(name, defaultValue);
    //      return defaultValue;
    //    }

    //    return propertyValue;
    //  }

    public static int GetInt(string name, int alternate)
    {
      // todo
      return alternate;
    }
    //  /**
    //   * Returns the value of the property corresponding to the passed name as an "int".
    //   *
    //   * @param name
    //   *            name of property
    //   * @param defaultValue
    //   *            value to be set if the property value can't be set
    //   *
    //   * @return int value of the property
    //   */
    //  final public static int getInt(String name, int defaultValue) {

    //    // Get the value of the property.
    //    String propertyValue = trimValue(settings.getProperty(name));

    //    // If the the property couldn't be found set the default value.
    //    if (propertyValue == null) {
    //      settings.setProperty(name, String.valueOf(defaultValue));

    //      return defaultValue;
    //    }

    //    int val = 0;
    //    try {
    //      val = Integer.parseInt(propertyValue);
    //    } catch (NumberFormatException err) {
    //      // Set default value.
    //      settings.setProperty(name, String.valueOf(defaultValue));
    //      return defaultValue;
    //    }

    //    return val;
    //  }

    //  /**
    //   * Returns the value of the property corresponding to the passed name as a "float".
    //   *
    //   * @param name
    //   *            name of property
    //   * @param defaultValue
    //   *            value to be set if the property value can't be set
    //   *
    //   * @return float value of the property
    //   */
    //  final public static float getFloat(String name, float defaultValue) {

    //    // Get the value of the property.
    //    String propertyValue = trimValue(settings.getProperty(name));

    //    // If the the property couldn't be found set the default value.
    //    if (propertyValue == null) {
    //      settings.setProperty(name, String.valueOf(defaultValue));
    //      return defaultValue;
    //    }

    //    float val = 0;
    //    try {
    //      val = Float.parseFloat(propertyValue);
    //    } catch (NumberFormatException err) {
    //      // Set default value.
    //      settings.setProperty(name, String.valueOf(defaultValue));
    //      return defaultValue;
    //    }

    //    return val;
    //  }

    //  /**
    //   * Returns the value of the property corresponding to the passed name as an boolean. If the named property is not yet known, and a default value is given,
    //   * it is entered into the properties.
    //   *
    //   * @param name
    //   *            name of property
    //   * @param defaultValue
    //   *            value to be set if the property value can't be set
    //   *
    //   * @return <code>true</code> if property value contains string "true",<br>
    //   *         <code>false</code> otherwise
    //   */
    //  final public static boolean getBool(String name, boolean... defaultValue) {

    //    // Get the value of the property.
    //    String propertyValue = trimValue(settings.getProperty(name));

    //    // If the the property couldn't be found set the default value.
    //    if (propertyValue == null) {
    //      if (defaultValue.length > 0) {
    //        settings.setProperty(name, String.valueOf(defaultValue[0]));
    //        return defaultValue[0];
    //      }
    //      return false;
    //    }

    //    return "true".equals(propertyValue);
    //  }

    //  /**
    //   * Returns the value of the property corresponding to the passed name as a <code>Color</code>. If the named property is not yet known, and a default value
    //   * is given, the default value is returned.
    //   *
    //   * @param name
    //   *            name of property
    //   * @param defaultColor
    //   *            Color to be set if the property value can't be set
    //   *
    //   * @return <code>Color</code> if property value contains a valid color,<br>
    //   *         <code>defaultColor or Color(0, 0, 0)</code> otherwise
    //   */
    //  final public static Color getColor(String name, Color... defaultColor) {

    //    // Get the value of the property.
    //    String propertyValue = trimValue(settings.getProperty(name));

    //    // If the the property couldn't be found set the default value.
    //    if (propertyValue == null) {
    //      return defaultColor.length > 0 ? defaultColor[0] : new Color(0, 0, 0);
    //    }

    //    String[] colorString = propertyValue.split(",");

    //    if (colorString.length == 4) {
    //      try {
    //        return new Color(Integer.decode(colorString[0].trim()).intValue(), Integer.decode(colorString[1].trim()).intValue(), Integer.decode(
    //            colorString[2].trim()).intValue(), Integer.decode(colorString[3].trim()).intValue());
    //      } catch (NumberFormatException e) {
    //      }
    //    }
    //    if (colorString.length == 3) {
    //      try {
    //        return new Color(Integer.decode(colorString[0].trim()).intValue(), Integer.decode(colorString[1].trim()).intValue(), Integer.decode(
    //            colorString[2].trim()).intValue());
    //      } catch (NumberFormatException e) {
    //      }
    //    }

    //    // Dummy
    //    return new Color(0, 0, 0);
    //  }

    //  /**
    //   * Erase any comments of the passed property string.
    //   *
    //   * @param propertyValue
    //   *            value of a property as a String
    //   * @return trimmed value
    //   */
    //  final private static String trimValue(String propertyValue) {

    //    if (propertyValue == null) {
    //      return null;
    //    }

    //    if (propertyValue.length() == 0) {
    //      return propertyValue;
    //    }

    //    /*
    //     * Now we trim off all trailing blanks and tabs, either from the original value, or after deleting a comment trailer, which starts with a hashmark (#).
    //     */
    //    int lastpos = propertyValue.indexOf('#');
    //    if (lastpos == -1) {
    //      lastpos = propertyValue.length() - 1;
    //    } else {
    //      lastpos--;
    //    }
    //    // Now, "lastpos" points at the char to check for blank/tab.
    //    // Currently we want to retain it, but let us have a look...

    //    while ((lastpos >= 0) && ((propertyValue.charAt(lastpos) == ' ') || (propertyValue.charAt(lastpos) == '\t'))) {
    //      lastpos--;
    //    }
    //    // Now, "lastpos" points to the last char which we want to retain.
    //    // It can be as low as -1, in case we have left nothing.

    //    return propertyValue.substring(0, lastpos + 1);
    //  }

    //  /**
    //   * Converts the characters for saving in a property file.
    //   *
    //   * @param theString
    //   *            <code>String</code> to be converted
    //   * @return converted <code>String</code>
    //   */
    //  public static String mask(String theString) {

    //    // Length of the passed string.
    //    int stringLength = theString.length();

    //    // Create a StringBuilder for concatenating the new string.
    //    // We suspect it will not need much more space than the original.
    //    StringBuilder newString = new StringBuilder(stringLength + 10);

    //    // Convert every character.
    //    for (int srcCharPos = 0; srcCharPos < stringLength; srcCharPos++) {

    //      char character = theString.charAt(srcCharPos);

    //      // Handle the normal characters first.
    //      if (character > 61 && character < 127) {
    //        if (character == '\\') {
    //          newString.append('\\');
    //          newString.append('\\');
    //          continue;
    //        }
    //        newString.append(character);
    //        continue;
    //      }

    //      switch (character) {

    //      case ' ':
    //        if (srcCharPos == 0) {
    //          newString.append('\\');
    //        }
    //        newString.append(' ');
    //        break;

    //      case '\b': // Backspace (ASCII code 8, '\b').
    //        newString.append('\\');
    //        newString.append('b');
    //        break;

    //      case '\t': // Tabulator (ASCII code 9, '\t').
    //        newString.append('\\');
    //        newString.append('t');
    //        break;

    //      case '\n': // Line Feed (ASCII code 10, '\n')
    //        newString.append('\\');
    //        newString.append('n');
    //        break;

    //      case '\f': // Form Feed (ASCII code 12, '\f')
    //        newString.append('\\');
    //        newString.append('f');
    //        break;

    //      case '\r': // Carriage Return (ASCII code 13, '\r')
    //        newString.append('\\');
    //        newString.append('r');
    //        break;

    //      case '=': // (ASCII code 61)
    //      case ':': // (ASCII code 58)
    //      case '#': // (ASCII code 35)
    //      case '!': // (ASCII code 33)
    //        newString.append('\\');
    //        newString.append(character);
    //        break;

    //      default:
    //        if (character < 0x0020 || character > 0x007e) {
    //          // outside standard ASCII range
    //          newString.append('\\');
    //          newString.append('u');
    //          newString.append(Utilities.toHex((character >> 12) & 0xF));
    //          newString.append(Utilities.toHex((character >> 8) & 0xF));
    //          newString.append(Utilities.toHex((character >> 4) & 0xF));
    //          newString.append(Utilities.toHex(character & 0xF));
    //        } else {
    //          // inside standard ASCII range (including blank)
    //          newString.append(character);
    //        }
    //      }
    //    }

    //    // if (isSettingsDebugModeActivated) {
    //    // System.out.println("Settings:mask: " + theString.length() + " -> " + newString.length());
    //    // }

    //    return newString.toString();
    //  }

    //  /**
    //   * This annotation {@code SettingsVar} shall be used for static fields of the class {@link Settings}, which represent a configuration value to be saved into
    //   * and restored from the user settings file "user_settings.ini".
    //   * <p>
    //   * Using {@code reflection} we can -at runtime- determine the list of thusly annotated variables, and save/restore them in a systematic way.
    //   * <p>
    //   * While this obstructs some uses of these variables, the advantage is, that further variables need less maintenance.
    //   * <p>
    //   * WARNING: Renaming a field annotated with {@code @SettingsVar} also changes the property name (normally you do not want that), unless the parameter
    //   * {@link #propertyName()} is given explicitly.
    //   *
    //   * @author Heiner Marxen
    //   */
    //  @Documented
    //  @Target(ElementType.FIELD)
    //  @Retention(RetentionPolicy.RUNTIME)
    //  public @interface SettingsVar {
    //    /**
    //     * The name (key) in the property (settings) file. If empty, the name of the annotated field is used.
    //     *
    //     * @return the property name of the settings variable
    //     */
    //    String propertyName() default "";

    //    /**
    //     * @return whether this variable is to be saved to the settings file
    //     */
    //    boolean saveMe() default true;

    //    /**
    //     * @return whether this variable is to be restored from the settings file
    //     */
    //    boolean loadMe() default true;

    //    /**
    //     * To support the renaming of property keys, we list old names. They shall be accepted from old files, and "translated" to the new name. If there is
    //     * more than one old name, the oldest should be last.
    //     *
    //     * @return an array of former property names
    //     */
    //    String[] oldNames() default {};
    //  }
  }
}
