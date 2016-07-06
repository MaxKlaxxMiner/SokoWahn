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

import javax.swing.JDialog;
import javax.swing.JTabbedPane;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.resourceHandling.Texts;



/**
 * This class offers methods to view and modify the settings of JSoko.
 * <p>
 * There are four different views on the data:
 * <ol>
 * <li> ...
 * <li> ...
 * <li> ...  //TODO
 * <li> ...
 * </ol>
 */
public final class SettingsFrameNew implements ChangeListener {

    // The different settings views.
    private GeneralSettingsView languageSettings;
    private FontSettingsView fontSettings;

	/** The main JDialog where to which all objects are added. */
	JDialog settingsViews;

	/** A tabbedPane for switching between the views. */
	private JTabbedPane tabbedPane;


	/**
	 * The class for managing the settings tabs.
	 *
	 * @param application Reference to the main object which holds all references
	 */
	public SettingsFrameNew(JSoko application) {

		// Create the modal JDialog for managing the settings and set the location and size of the application.
		settingsViews = new JDialog(application, Texts.getText("theSokobanGame")+": "+Texts.getText("settings"), true);
		settingsViews.setBounds(application.getBounds());

		// Create a tabbedPane for switching between the views and add it to the dialog.
		tabbedPane = new JTabbedPane();
		settingsViews.getContentPane().add(tabbedPane);

			// Create the JPanel for the general settings and add it to the tabbedPane.
			languageSettings = new GeneralSettingsView(application);
			tabbedPane.addTab(Texts.getText("settings.gameplay"), null, languageSettings, "");

			// Create the JPanel for the font settings and add it to the tabbedPane.
			fontSettings = new FontSettingsView(application);
			tabbedPane.addTab(Texts.getText("settings.font"), null, fontSettings, "");

		tabbedPane.addChangeListener(this);

		// Set this dialog visible.
		settingsViews.setVisible(true);
	}

    /**
	 * This method is called when the user selects another view.
	 */
	@Override
	public void stateChanged(final ChangeEvent e) {

		// Get the tabbed pane all views are stored in.
		JTabbedPane tabbedPane = (JTabbedPane) e.getSource();

		/*
		 * Language settings
		 */
		if(tabbedPane.getSelectedComponent() == languageSettings) {

		}

	}
}