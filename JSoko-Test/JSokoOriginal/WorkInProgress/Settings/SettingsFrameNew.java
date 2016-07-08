package de.sokoban_online.jsoko.workInProgress.settings;

import javax.swing.JDialog;
import javax.swing.JTabbedPane;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.resourceHandling.Texts;


/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2011 by Matthias Meger, Germany
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *	
 *	JSoko is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *	GNU General Public License for more details.
 *	
 *	You should have received a copy of the GNU General Public License
 *	along with JSoko; if not, write to the Free Software
 *	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

/**
 * This class offers methods to view and modify the settings of JSoko.
 * <p>
 * There are four different views on the data:
 * <ol>
 * <li> ... view: all level data are shown and can be modified.    //TODO: document
 *      The levels can also be added to collections.
 * <li> author view: all author data are shown and can be modified.
 * <li> collection view: all collection data are shown and can be modified.
 * <li> level assignment view: the assignment of the levels to the collections are shown
 *      and can be modified.
 *      This view is intended to be used to change the order of the levels in a collection.
 * </ol>
 */
public final class SettingsFrameNew implements ChangeListener {

	/** Reference to the main object which holds all references. */
    private final JSoko application;
    
    
    // The different settings views.
    private GeneralSettingsView languageSettings;
//    private fontSettings FontSettings;
	
	/** The main JDialog where to which all objects are added. */
	JDialog settingsViews;
	
	/** A tabbedPane for switching between the views. */
	private JTabbedPane tabbedPane;
	
    
	/**
	 * The class for managing the level data.
	 *
	 * @param application Reference to the main object which holds all references
	 */
	public SettingsFrameNew(JSoko application) {
		
		// Save local references.
		this.application = application;
		
		
		// Create the modal JDialog for managing the settings and set the location and size of the application.
		settingsViews = new JDialog(application, Texts.getText("theSokobanGame")+": "+Texts.getText("settings"), true);
		settingsViews.setBounds(application.getBounds());
				
		// Create a tabbedPane for switching between the views and add it to the dialog.
		tabbedPane = new JTabbedPane();
		settingsViews.getContentPane().add(tabbedPane);
		
			// Create the JPanel for the level view and add it to the tabbedPane.
			languageSettings = new GeneralSettingsView(application);
			tabbedPane.addTab(Texts.getText("levelView"), null, languageSettings, Texts.getText("levelViewTooltip"));
			
//			// Create the JPanel for the author view and add it to the tabbedPane. 
//			authorView = new AuthorsView(database, this, application);
//			tabbedPane.addTab(Texts.getText("authorView"), null, authorView, Texts.getText("authorViewTooltip"));
//	
//			// Create the JPanel for the collection view and add it to the tabbedPane. 
//			collectionView = new CollectionsView(database, this, application);
//			tabbedPane.addTab(Texts.getText("collectionView"), null, collectionView, Texts.getText("collectionViewTooltip"));
//				
//			// Create the JPanel for the level assignment view and add it to the tabbedPane.
//			levelAssignmentView = new LevelAssignmentView(database, this, application);
//			tabbedPane.addTab(Texts.getText("levelAssignmentView"), null, levelAssignmentView, Texts.getText("levelAssignmentViewTooltip"));

		tabbedPane.addChangeListener(this);
				
		// Set this dialog visible.
		settingsViews.setVisible(true);
	}
	
    /**
	 * This method is called when the user selects another view.
	 */
	public void stateChanged(final ChangeEvent e) {
				
		// Get the tabbed pane all views are stored in.
		JTabbedPane tabbedPane = (JTabbedPane) e.getSource();
		
		/*
		 * View on the levels
		 */
		if(tabbedPane.getSelectedComponent() == languageSettings) {
		
		}		
		
	}
}