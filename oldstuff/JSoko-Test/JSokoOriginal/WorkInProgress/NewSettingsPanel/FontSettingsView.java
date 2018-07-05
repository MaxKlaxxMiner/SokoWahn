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
package de.sokoban_online.jsoko.workInProgress.newSettingsPanel;

import java.awt.BorderLayout;
import java.awt.Component;
import java.awt.GridLayout;

import javax.swing.BorderFactory;
import javax.swing.JPanel;

import com.nilo.plaf.nimrod.NimRODFontDialog;

import de.sokoban_online.jsoko.JSoko;


/**
 * This class displays the panel for changing the font settings.
 * This Panel contains all elements to change the fonts used in the game and is displayed in a tabbed pane in the settings management.
 */
@SuppressWarnings("serial")
public class FontSettingsView extends JPanel {

	/** Reference to the main object which holds all references. */
	private final JSoko application;

	/**
	 *	Creates the panel for setting the fonts in the game.
	 *
	 * @param application Reference to the main object which holds all references
	 */
	public FontSettingsView(JSoko application) {

		// Save local references.
		this.application = application;

		// Create all things this panel needs.
		createPanel();
	}

	/**
	 * Creates all things this panel needs.
	 */
	private void createPanel() {
		
		setLayout(new BorderLayout());

		JPanel guiPanel = new JPanel(new GridLayout(0, 1, 0, 10));
		guiPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createRaisedBevelBorder(), BorderFactory.createEmptyBorder(10, 10, 10, 10)));

		// Just a quick and dirty test coding ...
		NimRODFontDialog d = new NimRODFontDialog(null);
		Component[] c = d.getContentPane().getComponents();
		guiPanel.add(c[0]);
		
		add(guiPanel, BorderLayout.NORTH);
	}
}	