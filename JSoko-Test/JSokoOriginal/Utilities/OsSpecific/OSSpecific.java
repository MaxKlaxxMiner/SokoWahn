/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2013 by Matthias Meger, Germany
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
package de.sokoban_online.jsoko.utilities.OSSpecific;

import java.awt.Image;
import java.awt.event.ActionEvent;
import java.io.File;
import java.lang.reflect.InvocationHandler;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.util.Map;
import java.util.Properties;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class is used for all settings that are specific to a operation system.
 */
public class OSSpecific {

	private static String userHomeDirectory;
	private static String preferencesDirectory;
	private static String appDataDirectory;
	private static String cacheDirectory;


	/** Enum which returns the operating system JSoko is currently running on. */
	public enum OSType {
	    MacOS,
	    Linux,
	    Windows,
	    Other;

	    private static OSType osType;

	    public static OSType getOSType() {
	        if (osType == null) {
	        	String osName = System.getProperty("os.name").toLowerCase();
	        	osType = osName.indexOf("win") != -1 ? Windows :
	        			 osName.indexOf("mac") != -1 || osName.indexOf("os x") != -1 ? MacOS :
	        			 osName.indexOf("nix") != -1 || osName.indexOf("nux") != -1 || osName.indexOf("aix") != -1 ? Linux : Other;
			}
	        return osType;
	    }

	    public static boolean isMac 	= getOSType() == MacOS;
	    public static boolean isWindows = getOSType() == Windows;
	    public static boolean isLinux 	= getOSType() == Linux;
	    public static boolean isOther 	= getOSType() == Other;
	}


	/**
	 * Sets OS specific settings.
	 *
	 * @param application the reference to the main object holding all references
	 */
	public static void setOSSpecificSettings(final JSoko application) {

		// Set the directories for the JSoko data.
		setDirectories();

		// Set Mac specific settings.
		if (OSType.isMac) {
			setMacSettings(application);
		}
	}

	/**
	 * Returns the user home directory.
	 *
	 * @return the userHomeDirectory
	 */
	public static String getUserHomeDirectory() {
		return userHomeDirectory;
	}

	/**
	 * Returns the directory to store JSoko user preferences in.
	 *
	 * @return the preferencesDirectory
	 */
	public static String getPreferencesDirectory() {
		return preferencesDirectory;
	}

	/**
	 * Returns the directory to store JSoko user data in.
	 *
	 * @return the appDataDirectory
	 */
	public static String getAppDataDirectory() {
		return appDataDirectory;
	}

	/**
	 * Returns the directory to store cache data in.
	 *
	 * @return the cacheDirectory
	 */
	public static String getCacheDirectory() {
		return cacheDirectory;
	}

	private static void setMacSettings(final JSoko application) {

		// When using the Aqua look and feel, this property puts Swing menus in the OS X menu bar.
		// Note that JMenuBars in JDialogs are not moved to the OS X menu bar.
		System.setProperty("apple.laf.useScreenMenuBar", "true");

		// Set application name.
		System.setProperty("com.apple.mrj.application.apple.menu.about.name", "JSoko");

		// By using this listener, the typical apple-menu bar which provides own about, preferences and quit-menu-items can be used.
		try {
			// Get Mac OS X application class.
			final Class<?> macApplicationClass = Class.forName("com.apple.eawt.Application");

			// Get the Mac OS X application instance.
			final Object macApplication = macApplicationClass.newInstance();

			// Get the application listener.
			Class<?> applicationListenerClass = Class.forName("com.apple.eawt.ApplicationListener");

			Object listener = Proxy.newProxyInstance(applicationListenerClass.getClassLoader(), new Class[] { applicationListenerClass }, new InvocationHandler() {
				@Override
				public Object invoke(Object proxy, Method method, Object[] args) throws Throwable {

					String methodName = method.getName();

					if (methodName.equals("handleQuit")) {
						application.actionPerformed(new ActionEvent(macApplicationClass, 0, "programClosing"));
					}
					if (methodName.equals("handlePreferences")) {
						// nothing is done
					}
					if (methodName.equals("handleAbout")) {
						application.actionPerformed(new ActionEvent(macApplicationClass, 0, "aboutJSoko"));

						// We handled the about event. Avoid showing a second system-own about dialog.
						setApplicationEventHandled(args[0], true);
					}
					return null;
				}
			});

			// Add a listener for listening for actions of the apple menu items.
			Method addApplicationListenerMethod = macApplicationClass.getMethod("addApplicationListener", applicationListenerClass);
			addApplicationListenerMethod.invoke(macApplication, listener);

			// Don't enable the preferences menu.
			Method enablePreferenceMethod = macApplicationClass.getMethod("setEnabledPreferencesMenu", new Class[] {boolean.class});
			enablePreferenceMethod.invoke(macApplication, new Object[] {Boolean.FALSE});

			// Set the dock icon for JSoko.
			Method setDockIconImageMethod = macApplicationClass.getMethod("setDockIconImage", new Class[] {Image.class});
			setDockIconImageMethod.invoke(macApplication, Utilities.loadBufferedImage(Settings.get("iconFolder")+"jsoko dock icon.png"));

		}
		catch (Exception e) {
			e.printStackTrace();
		}
	}

    /**
     * Sets the handled status for an {@code ApplicationEvent}.
     *
     * @param event  the {@code ApplicationEvent} to set the status for
     * @param handled the status (true or false)
     */
    private static void setApplicationEventHandled(Object event, boolean handled) {
        if (event != null) {
            try {
                Method setHandledMethod = event.getClass().getDeclaredMethod("setHandled", new Class[] { boolean.class });
                setHandledMethod.invoke(event, new Object[] { Boolean.valueOf(handled) });
            } catch (Exception ex) {
            	if(Debug.isDebugModeActivated) {
					ex.printStackTrace();
				}
            }
        }
    }

    /**
     * Sets the directories that depend on the operating system.
     */
    private static void setDirectories() {

    	userHomeDirectory = System.getProperty("user.home");

    	/**
    	 * Mac OS
    	 * Directories according to Mac developer library: "Important Java Directories on Mac OS X".
    	 */
    	if(OSType.isMac) {
    		 preferencesDirectory = userHomeDirectory + "/Library/Preferences/de.sokoban_online.jsoko/";
    		 appDataDirectory 	  = userHomeDirectory + "/Library/jsoko/";
    		 cacheDirectory 	  = System.getProperty("java.io.tmpdir")+"/jsoko/";
    	}

    	/**
    	 * Unix/Linux
    	 * Directories according to "XDG Base Directory Specification".
    	 */
    	if(OSType.isLinux) {
       		preferencesDirectory = System.getenv("XDG_CONFIG_HOME");
    		if (preferencesDirectory != null && !preferencesDirectory.trim().isEmpty()) {
    			preferencesDirectory = preferencesDirectory + "/jsoko/";
    		} else {
    			preferencesDirectory = userHomeDirectory +"/.config/jsoko/";
			}

    		appDataDirectory = System.getenv("XDG_DATA_HOME");
    		if (appDataDirectory != null && !appDataDirectory.trim().isEmpty()) {
    			appDataDirectory = appDataDirectory + "/jsoko/";
    		} else {
    			appDataDirectory = userHomeDirectory +"/.local/share/jsoko/";
			}

    		cacheDirectory = System.getenv("XDG_CACHE_HOME");
    	    if (cacheDirectory != null && !cacheDirectory.trim().isEmpty()) {
    	    	cacheDirectory = cacheDirectory + "/jsoko/";
    	    } else {
    	    	cacheDirectory = userHomeDirectory + "/.cache/jsoko/";
    	    }
    	}

    	/**
    	 * Windows
    	 */
    	if(OSType.isWindows) {
    		preferencesDirectory = System.getenv("APPDATA")+"/jsoko/";
    		appDataDirectory 	 = System.getenv("APPDATA")+"/jsoko/";
    		cacheDirectory 		 = System.getProperty("java.io.tmpdir")+"/jsoko/";
    	}

    	if(OSType.isOther) {
    		preferencesDirectory = appDataDirectory = cacheDirectory = userHomeDirectory + "/jsoko/";
    	}

    	if(Debug.isDebugModeActivated) {
    		preferencesDirectory = appDataDirectory = cacheDirectory = Utilities.getBaseFolder()+"/userData/"; // use bin folder for data
    	}

    	/**
    	 * Ensure all directories are accessible.
    	 */
    	if(Utilities.createDiretory(preferencesDirectory) == null) {
    		File file = Utilities.createDiretory(userHomeDirectory + "/jsoko/");
    		if(file != null) {
				preferencesDirectory = file.getPath();
			} else {
				file = new File(Utilities.getBaseFolder());
				if(file != null && file.canWrite()) {
					preferencesDirectory = file.getPath();
				} else {
					// Show message: since the texts haven't been loaded yet an English message is displayed.
					MessageDialogs.showErrorString(null, "Can't access directory: "+userHomeDirectory + "/jsoko/");
					System.exit(-1);
				}
			}
    	}
    	if(Utilities.createDiretory(appDataDirectory) == null) {
    		appDataDirectory = preferencesDirectory;
    	}
    	if(Utilities.createDiretory(cacheDirectory) == null) {
    		cacheDirectory = preferencesDirectory;
    	}

    	// When the user edits texts they are saved in the folder "texts" which has to be created.
    	Utilities.createDiretory(preferencesDirectory+"texts/");

    }


    /**
     * DEBUG: prints the environment variables and system properties.
     */
    public static void debugPrintProperties() {

    	System.out.println("Environment variables:");
		Map<String, String> m = System.getenv();
		for (String key : m.keySet()) {
			System.out.println("key: "+key+ " value: "+m.get(key));
		}

		System.out.println("\nSystem properties:");
		Properties p = System.getProperties();
		for (Object key : p.keySet()) {
			System.out.println("key: "+key+ " value: "+p.get(key));
		}
    }
}
