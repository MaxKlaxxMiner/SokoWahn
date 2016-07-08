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
package de.sokoban_online.jsoko.leveldata.levelmanagement;

import javax.swing.JDialog;
import javax.swing.JTabbedPane;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class offers methods to view and modify the data stored in the database.
 * <p>
 * There are four different views on the data:
 * <ol>
 * <li> level view: all level data are shown and can be modified.
 *      The levels can also be added to collections.
 * <li> author view: all author data are shown and can be modified.
 * <li> collection view: all collection data are shown and can be modified.
 * <li> level assignment view: the assignment of the levels to the collections are shown
 *      and can be modified.
 *      This view is intended to be used to change the order of the levels in a collection.
 * </ol>
 */
public final class DatabaseGUI implements ChangeListener {

	/** Reference to the main object which holds all references. */
    protected final JSoko application;

    /** Direct reference to the database for easier access. */
    protected final Database database;


    // The different views on the database.
    private LevelsView levelView;
    private AuthorsView authorView;
    private CollectionsView collectionView;
    private LevelAssignmentView levelAssignmentView;

	/** The main JDialog where to which all objects are added. */
	JDialog databaseViews;

	/** A tabbedPane for switching between the views. */
	private JTabbedPane tabbedPane;


	/**
	 * The class for managing the level data.
	 *
	 * @param application Reference to the main object which holds all references
	 */
	public DatabaseGUI(JSoko application) {

		// Save local references.
		this.application = application;
		database = application.levelIO.database;

		// Create the modal JDialog for browsing the database and set the location
		// and size of the application.
		// The dialog has to be modal because the thread of the caller of this method
		// must be stopped until the dialog is closed.
		databaseViews = new JDialog(application, Texts.getText("theSokobanGame")+": "+Texts.getText("databaseBrowser"), true);
		databaseViews.setBounds(application.getBounds());
		Utilities.setEscapable(databaseViews);

		// All views have to be refreshed because they are displayed for the first time.
		DatabaseChangesInViews.setUpdateFlags(DatabaseChangesInViews.levelsView);
		DatabaseChangesInViews.setUpdateFlags(DatabaseChangesInViews.authorsView);
		DatabaseChangesInViews.setUpdateFlags(DatabaseChangesInViews.collectionsView);
		DatabaseChangesInViews.setUpdateFlags(DatabaseChangesInViews.levelAssignementView);

		// Create a tabbedPane for switching between the views and add it to the dialog.
		tabbedPane = new JTabbedPane();
		databaseViews.getContentPane().add(tabbedPane);

			// Create the JPanel for the level view and add it to the tabbedPane.
			levelView = new LevelsView(database, this, application);
			tabbedPane.addTab(Texts.getText("levelView"), null, levelView, Texts.getText("levelViewTooltip"));

			// Create the JPanel for the author view and add it to the tabbedPane.
			authorView = new AuthorsView(database, this, application);
			tabbedPane.addTab(Texts.getText("authorView"), null, authorView, Texts.getText("authorViewTooltip"));

			// Create the JPanel for the collection view and add it to the tabbedPane.
			collectionView = new CollectionsView(database, this, application);
			tabbedPane.addTab(Texts.getText("collectionView"), null, collectionView, Texts.getText("collectionViewTooltip"));

			// Create the JPanel for the level assignment view and add it to the tabbedPane.
			levelAssignmentView = new LevelAssignmentView(database, this, application);
			tabbedPane.addTab(Texts.getText("levelAssignmentView"), null, levelAssignmentView, Texts.getText("levelAssignmentViewTooltip"));

		tabbedPane.addChangeListener(this);
	}

    /**
     * Shows the database GUI in a modal {@code Dialog}.
     */
    public void showAsModalDialog() {

    	// As default select the currently played collection.
    	levelView.selectCurrentLevelCollection(application.currentLevelCollection);

    	// Refresh the level view, because it is displayed as default view.
		levelView.refreshView();

    	databaseViews.setVisible(true);
    }

    /**
	 * This method is called when the user selects another view.
	 */
	@Override
	public void stateChanged(final ChangeEvent e) {

		// Get the tabbed pane all views are stored in.
		JTabbedPane tabbedPane = (JTabbedPane) e.getSource();

		/*
		 * View on the levels
		 */
		if(tabbedPane.getSelectedComponent() == levelView) {

			// Update the author and collection combo boxes if a relevant change has occurred in the other views.
			if(DatabaseChangesInViews.changeInAuthorView[DatabaseChangesInViews.levelsView] == true) {
				levelView.updateComboBoxAuthors();
			}
			if(DatabaseChangesInViews.changeInCollectionView[DatabaseChangesInViews.levelsView] == true) {
				levelView.updateComboBoxCollections();
			}

			// Refresh the whole view if any relevant change occurred in one of the other views.
	    	if(DatabaseChangesInViews.changeInAuthorView[DatabaseChangesInViews.levelsView] == true ||
	    	   DatabaseChangesInViews.changeInCollectionView[DatabaseChangesInViews.levelsView] == true ||
	    	   DatabaseChangesInViews.changeInAssignmentView[DatabaseChangesInViews.levelsView] == true) {
	    		levelView.refreshView();
	    	}

	    	// Reset the update flag.
	    	DatabaseChangesInViews.resetUpdateFlags(DatabaseChangesInViews.levelsView);
		}


		/*
		 * View on the authors
		 */
		if(tabbedPane.getSelectedComponent() == authorView) {

			// Refresh the whole view if any relevant change occurred in one of the other views.
	    	if(DatabaseChangesInViews.changeInLevelView[DatabaseChangesInViews.levelsView] == true ||
	    	   DatabaseChangesInViews.changeInCollectionView[DatabaseChangesInViews.levelsView] == true ||
	    	   DatabaseChangesInViews.changeInAssignmentView[DatabaseChangesInViews.levelsView] == true) {
	    		authorView.refreshView();
	    	}
			authorView.refreshView();

	    	// Reset the update flag.
	    	DatabaseChangesInViews.resetUpdateFlags(DatabaseChangesInViews.authorsView);
		}


		/*
		 * View on the collection data.
		 */
		if(tabbedPane.getSelectedComponent() == collectionView) {

			// Update the author data if a relevant change has occurred in the other views.
			if(DatabaseChangesInViews.changeInAuthorView[DatabaseChangesInViews.collectionsView] == true) {
				collectionView.updateComboBoxAuthors();
			}

			// Refresh the whole view if any relevant change occurred in one of the other views.
			if(DatabaseChangesInViews.changeInAuthorView[DatabaseChangesInViews.collectionsView] == true) {
				collectionView.refreshView();
			}

	    	// Reset the update flag.
	    	DatabaseChangesInViews.resetUpdateFlags(DatabaseChangesInViews.collectionsView);
		}


		/*
		 * View on the assignments of the levels to one collection
		 */
		if(tabbedPane.getSelectedComponent() == levelAssignmentView ) {

			// Update the collection combox box if a relevant change has occurred in the other views.
			if(DatabaseChangesInViews.changeInCollectionView[DatabaseChangesInViews.levelAssignementView] == true) {
				levelAssignmentView.updateComboBoxCollections();
			}

			// Refresh the whole view if any relevant change occurred in one of the other views.
			if(DatabaseChangesInViews.changeInCollectionView[DatabaseChangesInViews.levelAssignementView] == true ||
			   DatabaseChangesInViews.changeInLevelView[DatabaseChangesInViews.levelAssignementView] == true) {
				levelAssignmentView.refreshView();
			}

	    	// Reset the update flag.
	    	DatabaseChangesInViews.resetUpdateFlags(DatabaseChangesInViews.levelAssignementView);
		}
	}



	/**
     * Close this dialog and return to the caller of this dialog.
     */
    @Override
	protected void finalize() {
    	databaseViews.dispose();
    	databaseViews = null;
    }

    /**
     * The views have always to stay up-to-date. If the user deletes a collection
     * in the collection view for instance, it mustn't be possible to assign a level
     * to this collection in the levels view. Therefore the views have to be updated
     * when relevant changes have occurred in the other views.<br>
     * This class is used to set flags when a relevant change has occurred.
     */
    public static class DatabaseChangesInViews {

    	// Constants for the different views.
    	static private final int levelsView      	  = 0;
    	static private final int authorsView	 	  = 1;
    	static private final int collectionsView 	  = 2;
    	static private final int levelAssignementView = 3;

    	// Flags, indicating whether a change has occurred in one of the views.
    	static boolean[] changeInAuthorView         = new boolean[] {true, true, true, true}; // New author, author name changed or deleted author
    	static boolean[] changeInCollectionView     = new boolean[] {true, true, true, true}; // New collection, collection name changed or deleted collection
    	static boolean[] changeInAssignmentView     = new boolean[] {true, true, true, true}; // Deleted assignment or new order of levels
    	static boolean[] changeInLevelView			= new boolean[] {true, true, true, true}; // Level title, level assignment, level deleted


    	/**
    	 * Called from the author view when the user has changed data
    	 * for the authors that is relevant for the other views.
    	 */
    	static public void authorsNamesChanged() {
    		for(int i=0; i<changeInAuthorView.length; i++) {
    			changeInAuthorView[i] = true;
    		}
    	}

    	/**
    	 * Called from collection view when the user has changed something
    	 * that is relevant for the other views.
    	 */
    	static public void collectionNamesChanged() {
    		for(int i=0; i<changeInCollectionView.length; i++) {
    			changeInCollectionView[i] = true;
    		}
    	}


    	/**
    	 * Called from level assignment view when the user has changed
    	 * something that is relevant for the other views.
    	 */
    	static public void changeInAssignmentView() {
    		for(int i=0; i<changeInAssignmentView.length; i++) {
    			changeInAssignmentView[i] = true;
    		}
    	}

    	/**
    	 * Called from level view when the user has changed something that is
    	 * relevant for the other views.
    	 */
    	static public void changeInLevelView() {
    		for(int i=0; i<changeInLevelView.length; i++) {
    			changeInLevelView[i] = true;
    		}
    	}

    	/**
    	 * After a view has been refreshed the data is up-to-date again.
    	 * Hence all update flags for this view can be reset.
    	 *
    	 * @param view index of the view to mark as "up-to-date"
    	 */
    	static public void resetUpdateFlags(int view) {
    		changeInAuthorView[view]     = false;
        	changeInCollectionView[view] = false;
        	changeInAssignmentView[view] = false;
        	changeInLevelView[view]      = false;
    	}

    	/**
    	 * Marks the passed view for being refreshed because data have changed.
    	 *
    	 * @param view index of the view to mark as "needs to be refreshed"
    	 */
    	static public void setUpdateFlags(int view) {
    		changeInAuthorView[view]     = true;
        	changeInCollectionView[view] = true;
        	changeInAssignmentView[view] = true;
        	changeInLevelView[view]      = true;
    	}
    }
}