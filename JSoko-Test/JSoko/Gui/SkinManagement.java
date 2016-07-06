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
package de.sokoban_online.jsoko.gui;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.text.Collator;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.Properties;
import java.util.TreeSet;

import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * Class for loading and managing skins.
 * <p>
 * There are different formats for skins.
 * This class provides methods for loading the different skins
 * and converting the skins into an internal needed graphic format.
 */
public class SkinManagement {

	/**
	 * Creates object for loading skins.
	 * These objects are used for displaying the board graphically.
	 * <p>
	 * Returning only the settings, but not the skins itself,
	 * is just done for better performance.
	 */
	public SkinManagement() {}

	/**
	 * Returns the Settings of all available skins.
	 *
	 * @return an <code>ArrayList</code> of the properties of all available skins.
	 */
	public static TreeSet<Properties> getAvailableSkins() {

		TreeSet<Properties> skinSettingsList = new TreeSet<Properties>(new SkinNameComparator());

		// Get all skin settings files stored in the skin folder.
		ArrayList<File> skinFiles = Utilities.getFileList(Settings.get("skinFolder"), ".*\\.ini$|.*\\.skn$");

		// Load the properties of all available skins.
        for(File skinSettingsFile : skinFiles){

        	Properties skinSettings = new Properties();
    		try {
    			skinSettings.load(new FileInputStream(skinSettingsFile));

    			// Here the property values may be verified ...

    		} catch (IOException e) {
    			continue;
    		}

    		// Add the filename to the properties.
    		skinSettings.put("settingsFilePath", skinSettingsFile.getPath());

    		// Add the property file to the list.
    		skinSettingsList.add(skinSettings);
        }

        // Return the list of all available skins.
        return skinSettingsList;
	}


	/**
	 * Comparator for sorting the skins by name.
	 */
	static class SkinNameComparator implements Comparator<Properties> {
		@Override
		public int compare(Properties o1, Properties o2) {
			Collator myCollator = Collator.getInstance();

			// Get the name (or "null" if none is set).
			String name1 = o1.getProperty("name");
			if(name1 == null) {
				name1 = o1.getProperty("Title");
			}
			String name2 = o2.getProperty("name");
			if(name2 == null) {
				name2 = o2.getProperty("Title");
			}

			// Compare the skin names.
			int compareResult = myCollator.compare(""+name1, ""+name2);

			return (compareResult == 0) ? 1 : compareResult;
		}
	}
}