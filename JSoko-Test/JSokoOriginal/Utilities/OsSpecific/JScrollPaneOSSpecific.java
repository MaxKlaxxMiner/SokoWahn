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

import java.awt.Component;

import javax.swing.JScrollPane;

import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific.OSType;

/**
 * Factory for {@code JScrollPane}s having OS specific settings.
 */
public class JScrollPaneOSSpecific {

	 /**
     * Creates a <code>JScrollPane</code> that displays the view
     * component in a viewport whose view position can be controlled with a pair of scrollbars.
     *
     * @param view the component to display in the scrollpanes viewport
     * @param vsbPolicy an integer that specifies the vertical scrollbar policy
     * @param hsbPolicy an integer that specifies the horizontal scrollbar policy
	 * @return {@code JScrollPane} having OS specific settings
     */
    public static JScrollPane getJScrollPane(Component view, int vsbPolicy, int hsbPolicy) {
    	JScrollPane scrollPane = new JScrollPane(view, vsbPolicy, hsbPolicy);
    	setOSSpecificSettingsScrollPane(scrollPane);
    	return scrollPane;
    }


    /**
     * Creates a <code>JScrollPane</code> that displays the
     * contents of the specified component, where both horizontal and vertical scrollbars appear
     * whenever the component's contents are larger than the view.
     *
     * @param view the component to display in the scrollpane's viewport
     * @return {@code JScrollPane} having OS specific settings
     */
    public static JScrollPane getJScrollPane(Component view) {
        return getJScrollPane(view, JScrollPane.VERTICAL_SCROLLBAR_AS_NEEDED, JScrollPane.HORIZONTAL_SCROLLBAR_AS_NEEDED);
    }


    /**
     * Creates an empty (no viewport view) <code>JScrollPane</code>
     * with specified scrollbar policies.
     *
     * @param vsbPolicy an integer that specifies the vertical scrollbar policy
     * @param hsbPolicy an integer that specifies the horizontal scrollbar policy
     * @return {@code JScrollPane} having OS specific settings
     */
    public JScrollPane getJScrollPane(int vsbPolicy, int hsbPolicy) {
    	return getJScrollPane(null, vsbPolicy, hsbPolicy);
    }


    /**
     * Creates an empty (no viewport view) <code>JScrollPane</code>
     * where both horizontal and vertical scrollbars appear when needed.
     *
     * @return {@code JScrollPane} having OS specific settings
     */
    public JScrollPane getJScrollPane() {
    	return getJScrollPane(null, JScrollPane.VERTICAL_SCROLLBAR_AS_NEEDED, JScrollPane.HORIZONTAL_SCROLLBAR_AS_NEEDED);
    }

    /**
     * Sets OS specific settings for {@code JScrollPane}s.
     *
     * @param scrollPane {@code JScrollPane} to set OS specific settings for
     */
    private static void setOSSpecificSettingsScrollPane(JScrollPane scrollPane) {

    	if(OSType.isMac) {
    		// On Mac OS the scroll bars are always visible.
    	    scrollPane.setVerticalScrollBarPolicy(JScrollPane.VERTICAL_SCROLLBAR_ALWAYS);
    	    scrollPane.setHorizontalScrollBarPolicy(JScrollPane.HORIZONTAL_SCROLLBAR_ALWAYS);
    	}
    }
}