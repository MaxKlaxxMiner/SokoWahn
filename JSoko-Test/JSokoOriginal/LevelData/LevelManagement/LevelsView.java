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

import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.InputEvent;
import java.awt.event.MouseEvent;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import javax.swing.DefaultCellEditor;
import javax.swing.JButton;
import javax.swing.JComboBox;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextArea;
import javax.swing.JTextField;
import javax.swing.ListSelectionModel;
import javax.swing.SwingWorker;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.table.AbstractTableModel;
import javax.swing.table.JTableHeader;
import javax.swing.table.TableColumn;
import javax.swing.table.TableRowSorter;

import com.jidesoft.swing.AutoCompletionComboBox;
import com.jidesoft.swing.DefaultOverlayable;
import com.jidesoft.swing.InfiniteProgressPanel;
import com.jidesoft.swing.SearchableUtils;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.BoardDisplay;
import de.sokoban_online.jsoko.gui.ColumnVisibility;
import de.sokoban_online.jsoko.gui.DateTimeRenderer;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseGUI.DatabaseChangesInViews;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class displays the "LevelsView" in the level management dialog and handles
 * all actions in this view.
 * This Panel contains all elements of the "LevelsView" and is displayed in a
 * tabbed pane in the level management.
 */
@SuppressWarnings("serial")
public class LevelsView extends LevelManagementView implements ListSelectionListener {

	// There are several data that can be filtered. For every data element there is an own selection field.
	protected JTextField selectionTitle;
	protected AutoCompletionComboBox  selectionAuthor;
	protected AutoCompletionComboBox  selectionCollection;
	protected JTextField selectionComment;
	protected JTextField selectionDifficulty;

	// Additional information about a level.
	private JTextArea  commentText;
	private JTextField levelWidth;
	private JTextField levelHeight;
	private JTextField levelNumberOfBoxes;
	private JTextField levelViewString;
	private JTextField levelLastChanged;
	private JTextField collectionAssignments;
	private JTextField numberOfSolutions;
	private BoardDisplay boardDisplay;

	/** ComboBox containing all collections a level can be added to. */
	protected JComboBox collectionsForAddingLevelsTo;

	/** ComboxBox holding the authors that can be set to the levels. */
	protected JComboBox authorsToBeSet;

	/** The ComboBox holding all author names.
	 *  It is used for selecting an author in a table cell.
	 */
	private JComboBox comboBoxAuthors = new JComboBox();

	/** The ComboBox holding all collection names.
	 *  It is used for selecting a collection in a table cell.
	 */
	private JComboBox comboBoxCollections = new JComboBox();

	// The table model and the table for showing the level data.
	protected JTable tableLevelData;
	private TableModelLevelData tableModelLevelData;

	/**
	 * The table cell renderer for model data of type Date/Timestamp.
	 */
	protected DateTimeRenderer dateRenderer;


	/** Overlayable for the table.
	 * The overlayable shows some graphics while the table is being filled by a SwingWorker.
	 */
	DefaultOverlayable overlayTable = null;

	/** The button for saving comments in the level. */
	private JButton saveCommentButton;

	/** SwingWorker for updating the tree with the levels of the database. */
	SwingWorker<?,?> fillTreeWithLevelDataWorker = null;

	/**
	 * Executor for executing the SwingWorker in this class.
	 * The tree may only be filled by one worker at a time.
	 * Hence, the pool is created with only one thread.
	 */
	public static ExecutorService executor = Executors.newFixedThreadPool(1);

	/** Label showing the number of levels displayed in the tree. */
	private JLabel numberOfLevelLabel = null;

	// Indicates whether the title of a level has been changed. This information is important because it might be
	// necessary to refresh other views in order to ensure they display the correct level titles.
	//	private boolean hasAnyLevelTitleBeenChanged = false;


	/**
	 * Creates the <code>JPanel</code> for displaying the data of the levels
	 * in the <code>LevelManagement</code>.
	 *
	 * @param database the database of the program
	 * @param levelManagementDialog the <code>JDialog</code> all views are displayed in
	 * @param application Reference to the main object which holds all references
	 */
	public LevelsView(Database database, DatabaseGUI levelManagementDialog, JSoko application) {

		this.database = database;
		this.levelManagementDialog = levelManagementDialog;
		this.application = application;

		// Create all things this panel needs.
		createPanel();

		// Update the ComboBoxes for the authors.
		updateComboBoxAuthors();

		// Update ComboBox holding all collections.
		updateComboBoxCollections();

		// Set the help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "database.levels-tab", null);

	}

	/**
	 * Selects the data of the passed level collection.
	 *
	 * @param levelCollection  the level collection to select
	 */
	public void selectCurrentLevelCollection(LevelCollection levelCollection) {

		// If the collection is stored in the database it is selected.
		if(levelCollection.isConnectedWithDatabase()) {
			selectionCollection.setSelectedItem(new ComboBoxEntry(levelCollection.getTitle(), levelCollection.getDatabaseID()));
			return;
		}

		// The collection isn't stored in the database => show the delegate levels collection,
		// since the currently played level is stored there.
		int collectionID = database.getDelegateLevelsCollectionID();
		for(int index = 0; index<selectionCollection.getItemCount(); index++) {
			ComboBoxEntry entry = (ComboBoxEntry) selectionCollection.getItemAt(index);
			if(entry.ID == collectionID) {
				selectionCollection.setSelectedIndex(index);
			}
		}

//		// Select a collection having the same name.
//		for(int i=0; i<selectionCollection.getModel().getSize(); i++) {
//			ComboBoxEntry entry = (ComboBoxEntry) selectionCollection.getModel().getElementAt(i);
//			if(entry.string.equals(levelCollection.getTitle())) {
//				selectionCollection.setSelectedItem(entry);
//			}
//		}
	}

	/**
	 * Creates all things this panel needs.
	 */
	private void createPanel() {

		// Set a border layout for arranging the Swing elements.
		setLayout(new BorderLayout());

		// Create a panel where the fields for the selections are located.
		JPanel selectionPanel = new JPanel(new GridBagLayout());
		Texts.helpBroker.enableHelpKey(selectionPanel, "database.levels-tab.SelectionFields", null); // Enable help

		// Add the search fields to the panel.
		GridBagConstraints constraints = new GridBagConstraints();
		constraints.insets = new Insets(2, 2, 2, 2);
		constraints.anchor = GridBagConstraints.LINE_START;

		// For better optic a filler above.
		constraints.gridy++;
		selectionPanel.add(new JLabel(" "), constraints);


		// Level title
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("levelTitle")+":"), constraints);
		constraints.gridx++;
		selectionTitle = new JTextField("*", widthOfSelectionFields);
		selectionTitle.addActionListener(this);
		selectionTitle.setActionCommand("refreshView");
		selectionPanel.add(selectionTitle, constraints);

		// Author
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("author")+":"), constraints);
		constraints.gridx++;
		selectionAuthor = new AutoCompletionComboBox();
		selectionAuthor.setStrict(false);
		selectionAuthor.setEditable(true);
		selectionAuthor.setPreferredSize(selectionTitle.getPreferredSize());
		selectionAuthor.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				// If the user selected a new item using the mouse or pressed "enter" refresh the view.
				String action = e.getActionCommand();
				if((action.equals("comboBoxChanged") && e.getModifiers() == InputEvent.BUTTON1_MASK) || // selected item with mouse
						action.equals("comboBoxEdited")) {
					LevelsView.this.actionPerformed(new ActionEvent(selectionAuthor,0, "refreshView"));
				}
			}
		});
		selectionPanel.add(selectionAuthor, constraints);


		// Collection
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("collection")+":"), constraints);
		constraints.gridx++;
		selectionCollection = new AutoCompletionComboBox();
		selectionCollection.setEditable(true);
		selectionCollection.setStrict(false);
		selectionCollection.setPreferredSize(selectionTitle.getPreferredSize());
		selectionCollection.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				// If the user selected a new item using the mouse or pressed "enter" refresh the view.
				String action = e.getActionCommand();
				if((action.equals("comboBoxChanged") && e.getModifiers() == InputEvent.BUTTON1_MASK) || // selected item with mouse
						action.equals("comboBoxEdited")) {
					LevelsView.this.actionPerformed(new ActionEvent(selectionAuthor,0, "refreshView"));
				}
			}
		});
		selectionPanel.add(selectionCollection, constraints);


		// Comment
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("comment")+":"), constraints);
		constraints.gridx++;
		selectionComment = new JTextField("*", widthOfSelectionFields);
		selectionComment.addActionListener(this);
		selectionComment.setActionCommand("refreshView");
		selectionPanel.add(selectionComment, constraints);

		// Difficulty
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("difficulty")+":"), constraints);
		constraints.gridx++;
		selectionDifficulty = new JTextField("*", widthOfSelectionFields);
		selectionDifficulty.addActionListener(this);
		selectionDifficulty.setActionCommand("refreshView");
		selectionPanel.add(selectionDifficulty, constraints);


		// Add a Panel for showing the board of the selected level
		// -------------------------------------------------------
		boardDisplay = new BoardDisplay();
		constraints.gridx++;
		constraints.gridy=0;
		constraints.weightx = 1;
		constraints.anchor = GridBagConstraints.CENTER;
		constraints.fill   = GridBagConstraints.BOTH;
		constraints.gridheight = 99;  // 99 = a lot = as many as there are available depending on the other objects in the panel
		selectionPanel.add(boardDisplay, constraints);


		// Add the buttons for the actions
		// -------------------------------

		// Button for playing a level.
		JButton playButton = new JButton(Texts.getText("playLevel"));
		playButton.setActionCommand("playLevel");
		playButton.addActionListener(this);
		constraints.gridx++;
		constraints.gridy   = 1;
		constraints.weightx = 0.1;
		constraints.gridheight = 1;
		constraints.fill = GridBagConstraints.HORIZONTAL;
		selectionPanel.add(playButton, constraints);

		// Button for deleting a level.
		JButton deleteButton = new JButton(Texts.getText("deleteLevel"));
		deleteButton.setToolTipText(Texts.getText("deleteLevelTooltip"));
		deleteButton.setActionCommand("deleteLevel");
		deleteButton.addActionListener(this);
		constraints.gridy++;
		selectionPanel.add(deleteButton, constraints);

		// Create a Panel holding the Button and ComboBox for adding a level to a collection.
		JPanel levelToCollectionArea = new JPanel(new BorderLayout());
		constraints.gridy++;
		constraints.gridheight = 2;
		selectionPanel.add(levelToCollectionArea, constraints);

		// Button for adding a level to a collection.
		JButton addLevelButton = new JButton(Texts.getText("addLevelToCollection"));
		addLevelButton.setToolTipText(Texts.getText("addLevelToCollectionTooltip"));
		addLevelButton.setActionCommand("addLevelToCollection");
		addLevelButton.addActionListener(this);
		Dimension size = deleteButton.getPreferredSize();
		size.setSize(size.getWidth()-constraints.insets.top, size.getHeight()-constraints.insets.bottom);
		addLevelButton.setPreferredSize(size);  // same size as the delete button
		levelToCollectionArea.add(addLevelButton, BorderLayout.NORTH);

		// Add the ComboBox specifying the collection levels are to be added.
		collectionsForAddingLevelsTo = new JComboBox();
		SearchableUtils.installSearchable(collectionsForAddingLevelsTo); // Typing letters automatically selects entries
		levelToCollectionArea.add(collectionsForAddingLevelsTo, BorderLayout.CENTER);

		// Create a Panel holding the Button and ComboBox for adding a level to a collection.
		JPanel setAuthorPanel = new JPanel(new BorderLayout());
		constraints.gridy+=2;
		constraints.gridheight = 2;
		selectionPanel.add(setAuthorPanel, constraints);

		// Button for setting the author of levels.
		JButton setAuthorButton = new JButton(Texts.getText("setAuthor"));
		setAuthorButton.setToolTipText(Texts.getText("setAuthorTooltip"));
		setAuthorButton.setActionCommand("setAuthor");
		setAuthorButton.addActionListener(this);
		setAuthorButton.setPreferredSize(size);  // same size as the delete button
		setAuthorPanel.add(setAuthorButton, BorderLayout.NORTH);

		// Add the ComboBox specifying the author to be set.
		authorsToBeSet = new JComboBox();
		SearchableUtils.installSearchable(authorsToBeSet); // Typing letters automatically selects entries
		setAuthorPanel.add(authorsToBeSet, BorderLayout.CENTER);


		// Add the selection panel to the levels view panel.
		add(selectionPanel, BorderLayout.NORTH);


		// Create a sortable table for showing the level data.
		tableModelLevelData = new TableModelLevelData();
		tableLevelData = new JTable(tableModelLevelData);
		Texts.helpBroker.enableHelpKey(tableLevelData, "database.levels-tab.SelectionResultTable", null); // Enable help

		// Set a sorter for the table.
		tableLevelData.setRowSorter(new TableRowSorter<TableModelLevelData>(tableModelLevelData));

		// Prepare correct rendering of Date/Timestamp model values
		dateRenderer = new DateTimeRenderer();
		dateRenderer.enterMeForTypeDate(tableLevelData);

		// Edits should automatically be finished when cells loose the focus.
		tableLevelData.putClientProperty("terminateEditOnFocusLost", Boolean.TRUE);

		// Add a mouseListener to the headers.
		tableLevelData.getTableHeader().addMouseListener(this);
		tableLevelData.addMouseListener(this);
		tableLevelData.getSelectionModel().addListSelectionListener(this);

		// Hide all columns that are not to be displayed
		Utilities.tableHideInvisibleColumns(tableLevelData, tableModelLevelData);

		// Create a ComboBox editor for the author column.
		TableColumn authorColumn = tableLevelData.getColumnModel().getColumn(tableLevelData.getColumnModel().getColumnIndex(Texts.getText("author")));
		authorColumn.setCellEditor(new DefaultCellEditor(comboBoxAuthors));

		// Add the scroll pane containing the JTable showing the level data to the main panel.
		overlayTable = new DefaultOverlayable(new JScrollPane(tableLevelData));
		add(overlayTable, BorderLayout.CENTER);


		// In the south of the level view there is this panel for showing
		// additional information about the level.
		JPanel additionalLevelData = new JPanel(new GridBagLayout());
		Texts.helpBroker.enableHelpKey(additionalLevelData, "database.levels-tab.AdditionalLevelData", null); // Enable help
		add(additionalLevelData, BorderLayout.SOUTH);

		constraints.gridy	   = 0;
		constraints.gridx	   = 4;
		constraints.weightx    = 0;
		constraints.weighty    = 1;
		constraints.gridheight = 1;
		constraints.fill       = GridBagConstraints.NONE;
		constraints.anchor     = GridBagConstraints.LINE_END;
		numberOfLevelLabel = new JLabel(Texts.getText("database.xLevelsSelected", 0));
		additionalLevelData.add(numberOfLevelLabel, constraints);

		// A textfield for displaying the comment of the level.
		constraints.gridx=0;
		constraints.gridy++;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		constraints.anchor  = GridBagConstraints.LINE_START;
		additionalLevelData.add(new JLabel(Texts.getText("comment")+":"), constraints);
		constraints.gridx++;
		constraints.gridheight = 5;
		constraints.weightx = 1;
		constraints.fill = GridBagConstraints.BOTH;
		commentText = new JTextArea(1, 1);
		commentText.setLineWrap(true);
		commentText.setEnabled(false);
		additionalLevelData.add(new JScrollPane(commentText), constraints);

		constraints.gridx=0;
		constraints.gridy++;
		constraints.gridheight = 1;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		saveCommentButton = new JButton(Texts.getText("saveComment"));
		saveCommentButton.setActionCommand("saveComment");
		saveCommentButton.setToolTipText(Texts.getText("saveCommentTooltip"));
		saveCommentButton.addActionListener(this);
		saveCommentButton.setFont(new Font(null, Font.PLAIN, 10));
		saveCommentButton.setEnabled(false);
		additionalLevelData.add(saveCommentButton, constraints);

		// Objects for displaying the view on a level
		constraints.gridx = 0;
		constraints.gridy+=4;
		additionalLevelData.add(new JLabel(Texts.getText("view")), constraints);
		constraints.fill = GridBagConstraints.HORIZONTAL;
		constraints.gridx++;
		constraints.weightx = 1;
		levelViewString = new JTextField();
		levelViewString.setEditable(false);
		additionalLevelData.add(levelViewString, constraints);

		// Objects for displaying the level width
		constraints.gridx = 3;
		constraints.gridy = 1;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		additionalLevelData.add(new JLabel(Texts.getText("width")), constraints);
		constraints.gridx++;
		levelWidth = new JTextField(3);
		levelWidth.setEditable(false);
		additionalLevelData.add(levelWidth, constraints);

		// Objects for displaying the level height
		constraints.gridx = 3;
		constraints.gridy++;
		additionalLevelData.add(new JLabel(Texts.getText("height")), constraints);
		constraints.gridx++;
		levelHeight = new JTextField(3);
		levelHeight.setEditable(false);
		additionalLevelData.add(levelHeight, constraints);

		// Objects for displaying the number of boxes of a level
		constraints.gridx = 3;
		constraints.gridy++;
		additionalLevelData.add(new JLabel(Texts.getText("numberOfBoxes")), constraints);
		constraints.gridx++;
		levelNumberOfBoxes = new JTextField(3);
		levelNumberOfBoxes.setEditable(false);
		additionalLevelData.add(levelNumberOfBoxes, constraints);

		// Objects for displaying the view on a level
		constraints.gridx = 3;
		constraints.gridy++;
		additionalLevelData.add(new JLabel(Texts.getText("lastChanged")), constraints);
		constraints.gridx++;
		levelLastChanged = new JTextField(19);
		levelLastChanged.setEditable(false);
		additionalLevelData.add(levelLastChanged, constraints);

		// Objects for displaying the collections a level is assigned to
		constraints.gridx = 3;
		constraints.gridy++;
		additionalLevelData.add(new JLabel(Texts.getText("collectionAssignments")), constraints);
		constraints.gridx++;
		constraints.weightx = 0.2;
		constraints.fill = GridBagConstraints.HORIZONTAL;
		collectionAssignments = new JTextField(11);
		collectionAssignments.setEditable(false);
		additionalLevelData.add(collectionAssignments, constraints);


		// Objects for displaying the number of solutions of the level
		constraints.gridx = 3;
		constraints.gridy++;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		additionalLevelData.add(new JLabel(Texts.getText("numberOfSolutions")), constraints);

		// Create a panel so the TextField and the Button are close to each other.
		JPanel showSolutionsPanel = new JPanel();
		constraints.gridx++;
		constraints.insets = new Insets(0, -3, 0, 0);
		additionalLevelData.add(showSolutionsPanel, constraints);

		// TextField for showing the number of solutions.
		numberOfSolutions = new JTextField(2);
		numberOfSolutions.setEditable(false);
		showSolutionsPanel.add(numberOfSolutions);

		// The button for showing the solutions.
		JButton showSolutions = new JButton(Utilities.getIcon("system-search.png", null));
		showSolutions.setToolTipText(Texts.getText("showSolutions"));
		showSolutions.setActionCommand("showSolutions");
		showSolutions.addActionListener(this);
		showSolutions.setPreferredSize(new Dimension(18, 18));
		showSolutionsPanel.add(showSolutions);


		// Add a popup menu
		// -------------------------------
		//		final JPopupMenu popupMenu = new JPopupMenu() {  //TODO: add a popup menu
		//
		//			@Override
		//			public void show(Component invoker, int x, int y) {
		//
		//				super.show(invoker, x, y);
		//
		//				// Get the index of the item under the mouse click position.
		//				int clickedRow = tableLevelData.rowAtPoint(new Point(x, y));
		//
		//				// Convert the external view row to the internal number (the user may have sorted the view).
		//				int selectedModelRow = tableLevelData.convertRowIndexToModel(clickedRow);
		//
		//				// Select the level the popup is shown for if it isn't already selected.
		//				if(!tableLevelData.getSelectionModel().isSelectedIndex(clickedRow))
		//					tableLevelData.getSelectionModel().setSelectionInterval(clickedRow, clickedRow);
		//
		//				System.out.printf(""+tableModelLevelData.getValueAt(selectedModelRow, tableModelLevelData.LEVEL_TITLE_INDEX).toString());
		//			}
		//		};
		//
		//		// Ensure the popup changes its look&feel when the user changes the look&feel.
		//		Utilities.addComponentToUpdateUI(popupMenu);
		//
		//		// Menu item for easy cancel of popup.
		//		JMenuItem cancelMenuItem = new JMenuItem(Texts.getText("cancel"));
		//		cancelMenuItem.setEnabled(false);
		//		popupMenu.add(cancelMenuItem);
		//
		//		popupMenu.addSeparator();
		//
		//		// Set the popup for the table.
		//		tableLevelData.setComponentPopupMenu(popupMenu);
	}

	/**
	 * Determines the currently selected rows, and converts the numbers
	 * into the those for the model (not the view).
	 *
	 * @return an array with the selected model row numbers
	 */
	private int[] getSelectedModelRows() {

		// Get the selected row numbers from the table view
		int[] selectedRowNumbers = tableLevelData.getSelectedRows();

		// Convert the view row numbering to the model's row numbers
		for (int i = 0; i < selectedRowNumbers.length; i++) {
			selectedRowNumbers[i] = tableLevelData.convertRowIndexToModel(selectedRowNumbers[i]);
		}

		return selectedRowNumbers;
	}

	@Override
	public void actionPerformed(ActionEvent e) {

		// Don't handle this action if the flag is set.
		if(doNotFireActions) {
			return;
		}

		// Get the action string.
		String actionCommand = e.getActionCommand();

		// Reload the data from the database.
		if(actionCommand.startsWith("refreshView")) {
			tableModelLevelData.refreshData();

			// Select the first row if no row has been selected, yet.
			if(tableLevelData.getSelectedRow() == -1 && tableLevelData.getRowCount() > 0) {
				tableLevelData.getSelectionModel().setSelectionInterval(0, 0);
			}

			return;
		}

		// Play the selected levels.
		if(actionCommand.startsWith("playLevel")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = getSelectedModelRows();

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Add every selected level for playing.
			ArrayList<Level> levels = new ArrayList<Level>();
			for (int rowNumber : selectedRowsNumbers) {

				// Get the levelID.
				int levelID = ((Integer) tableModelLevelData.getValueAt(rowNumber, TableModelLevelData.LEVELID_INDEX)).intValue();

				// Add the level to the current loaded collection.
				levels.add(database.getLevel(levelID));
			}

			// Create a new (in-core) collection for the selected levels.
			LevelCollection levelCollection = new LevelCollection.Builder().setLevels(levels).build();

			// Set the new collection as current collection.
			application.setCollectionForPlaying(levelCollection);

			// Prepare everything for playing the first of the new levels.
			application.setLevelForPlaying(1);

			// Close the level management dialog, so the user can play the level.
			levelManagementDialog.finalize();

			return;
		}


		// Delete the selected row from the table and delete the level from the database.
		if(actionCommand.startsWith("deleteLevel")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = getSelectedModelRows();

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(this, Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			// List of all deleted level IDs.
			ArrayList<Integer> deletedLevelIDs = new ArrayList<Integer>();

			// Contains the IDs of changed collections due to deleting levels.
			HashSet<Integer> changedCollections = new HashSet<Integer>();

			// Delete all selected rows.
			for(int rowCount=selectedRowsNumbers.length; --rowCount>=0; ) {

				// Get the levelID.
				String levelIDString = tableModelLevelData.getValueAt(selectedRowsNumbers[rowCount], TableModelLevelData.LEVELID_INDEX).toString();
				int levelID = Integer.parseInt(levelIDString);

				// Get title and ID of all collections the level is part of.
				HashMap<Integer, String> assignedCollections = database.getInfoAboutAssignedCollections(levelID);

				// Display a warning when the level is assigned to more than one collection.
				if(assignedCollections.size() > 1) {
					String levelTitle = tableModelLevelData.getValueAt(selectedRowsNumbers[rowCount], TableModelLevelData.LEVEL_TITLE_INDEX).toString();
					int answer = JOptionPane.showConfirmDialog(this, "Level \""+levelTitle+"\" "+Texts.getText("hasSeveralAssignments"), Texts.getText("warning"), JOptionPane.WARNING_MESSAGE);
					if(answer == JOptionPane.CANCEL_OPTION) {
						continue;
					}
				}

				// Delete the level from the database.
				database.deleteLevel(levelID);

				// Delete the level from the tableModel
				tableModelLevelData.tableData.remove(selectedRowsNumbers[rowCount]);

				deletedLevelIDs.add(levelID);

				// The IDs are stored as keys.
				changedCollections.addAll(assignedCollections.keySet());
			}

			if(!deletedLevelIDs.isEmpty()) {

				// The level numbers of all changed collections must be renumbered.
				for(int collectionID : changedCollections) {
					database.updateLevelNumbers(collectionID);
				}

				// Adjust the number of levels shown in the tree.
				numberOfLevelLabel.setText(Texts.getText("database.xLevelsSelected", tableModelLevelData.getRowCount()));

				// Fire event to refresh the display.
				tableModelLevelData.fireTableDataChanged();

				// Select the first displayed level.
				if(tableModelLevelData.getRowCount() > 0) {
					tableLevelData.getSelectionModel().setSelectionInterval(0, 0);
				}

				// Set the flag that a relevant change for the other views has been done.
				DatabaseChangesInViews.changeInLevelView();
			}

			return;
		}

		// Save the comment
		if(actionCommand.startsWith("saveComment")) {

			// Immediately return if no row is selected.
			if(tableLevelData.getSelectedRow() == -1) {
				return;
			}

			// Set the new comment.
			tableModelLevelData.setValueAt(commentText.getText().intern(), tableLevelData.convertRowIndexToModel(tableLevelData.getSelectedRow()), TableModelLevelData.COMMENT_INDEX);
			return;
		}

		// Add the level to a collection.
		if(actionCommand.startsWith("addLevelToCollection")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = getSelectedModelRows();

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Get the collectionID of the collection where the levels are to be assigned.
			int collectionID = ((ComboBoxEntry) collectionsForAddingLevelsTo.getSelectedItem()).getID();

			int addedLevelsCount = 0;

			// Add all selected levels to to the collection.
			for (int rowNumber : selectedRowsNumbers) {

				// Get the levelID.
				int levelID = (Integer) tableModelLevelData.getValueAt(rowNumber, TableModelLevelData.LEVELID_INDEX);

				// Assign the level to the collection (if it isn't already assigned).
				boolean isAdded = database.addLevelToCollection(levelID, collectionID);

				if(isAdded) {
					addedLevelsCount++;
				}

				// TODO: Set status text -> number of added / number of already assigned.
			}

			if(addedLevelsCount > 0) {

				// Update the additional information of the first selected level for showing the new assigned collection name.
				updateAdditionalInformation();

				// If levels were assigned that hadn't been assigned to any collection yet, then
				// they aren't to be displayed anymore for the current selection criteria.
				if(selectionCollection.getEditor().getItem().toString().equals(Texts.getText("withoutCollection"))) {
					tableModelLevelData.refreshData();
				}

				// Set the flag that a relevant change for the other views has to been done.
				DatabaseChangesInViews.changeInLevelView();
			}

			return;
		}

		// Set the author of the selected levels.
		if(actionCommand.startsWith("setAuthor")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = getSelectedModelRows();

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Set the author of all selected levels.
			for (int rowNumber : selectedRowsNumbers) {
				tableModelLevelData.setValueAt(authorsToBeSet.getSelectedItem(), rowNumber, TableModelLevelData.AUTHOR_INDEX);
			}

			return;
		}

		// Show the solutions of the first selected level.
		if(actionCommand.startsWith("showSolutions")) {

			// Return if no row is selected.
			if(tableLevelData.getSelectedRow() == -1) {
				return;
			}

			// Get the number of the first selected row and convert it to the model number.
			int selectedRowNumber = tableLevelData.convertRowIndexToModel(tableLevelData.getSelectedRow());

			// Get the levelID.
			int levelID = new Integer(tableModelLevelData.getValueAt(selectedRowNumber, TableModelLevelData.LEVELID_INDEX).toString()).intValue();

			// Check if this level is in the collection which is just played. If yes, then
			// use that level data object so the changes directly change that object, too.
			Level level = application.currentLevelCollection.getLevelByID(levelID);

			// If the level isn't part of the currently played collection create a new level for showing the solutions.
			if(level == null) {
				level = database.getLevel(levelID);
			}

			// Display the solutions in a modal JDialog.
			// "false" = don't display the "taken solution as history" button
			// because the solutions may belong to another level than the
			// currently loaded level for playing.
			level.getSolutionsManager().displaySolutionsInDialog(levelManagementDialog.databaseViews);

			// The user may have added new solutions or deleted some solutions.
			// Therefore update all information.
			updateAdditionalInformation();

			return;
		}
	}


	/* (non-Javadoc)
	 * @see java.awt.event.MouseListener#mouseClicked(java.awt.event.MouseEvent)
	 */
	@Override
	public void mouseClicked(MouseEvent e) {

		// Handle clicks on the headers of the table.
		if(e.getSource() instanceof JTableHeader) {

			// Get the point the user has clicked.
			Point clickPoint = e.getPoint();

			// Get the header of the clicked table.
			JTableHeader header = (JTableHeader) e.getSource();

			// Get the number of the clicked column.
			int columnNumber = header.columnAtPoint(clickPoint);

			// Get the area filled by the header and reduce its size by 3 pixels.
			Rectangle headerArea = header.getHeaderRect(columnNumber);
			headerArea.grow(-3, 0);

			// If the user hasn't clicked in the area this means he has clicked in the 3 pixel
			// next to the area -> he has clicked at one of the separators.
			if (headerArea.contains(clickPoint) == false) {

				// Determine on which side of the separator the user has clicked and determine
				// the corresponding column index.
				int midPoint = headerArea.x + headerArea.width / 2;
				int columnIndex;
				if (header.getComponentOrientation().isLeftToRight()) {
					columnIndex = (clickPoint.x < midPoint) ? columnNumber - 1 : columnNumber;
				} else {
					columnIndex = (clickPoint.x < midPoint) ? columnNumber : columnNumber - 1;
				}

				// If the user hasn't clicked at the left side of the most left column and
				// he has double clicked then adjust the width of the column.
				if(columnIndex != -1 && e.getClickCount() == 2) {
					optimizeColumnWidths(header.getTable(),columnIndex);
				}
			}
		}
	}

	/**
	 * This methods sets the information about a level in some extra fields
	 * every time they change.
	 *
	 * @param e the event indicating that a value has changed
	 */
	@Override
	public void valueChanged(ListSelectionEvent e) {

		//Ignore adjusting events.
		if (e.getValueIsAdjusting()) {
			return;
		}

		ListSelectionModel lsm = (ListSelectionModel) e.getSource();

		// Get the number of the first selected row of the view.
		int selectedRow = lsm.getMinSelectionIndex();

		// If no row is selected delete all additional information displays.
		if (lsm.isSelectionEmpty()) {

			// Disable the comment area and button.
			commentText.setEnabled(false);
			saveCommentButton.setEnabled(false);

			commentText.setText("");
			levelWidth.setText("");
			levelHeight.setText("");
			levelViewString.setText("");
			levelNumberOfBoxes.setText("");
			levelLastChanged.setText("");
			collectionAssignments.setText("");
			numberOfSolutions.setText("");
			boardDisplay.setBoardToDisplay("");

			return;
		}

		// Convert the external view row to the internal number (the user may have sorted the view).
		selectedRow = tableLevelData.convertRowIndexToModel(selectedRow);

		// Enable the comment area and button.
		commentText.setEnabled(true);
		saveCommentButton.setEnabled(true);

		// Set the additional level information.
		commentText.setText(tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.COMMENT_INDEX).toString());
		commentText.setCaretPosition(0);
		levelWidth.setText(tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.WIDTH_INDEX).toString());
		levelHeight.setText(tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.HEIGHT_INDEX).toString());
		levelViewString.setText(tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.VIEW_INDEX).toString());
		levelViewString.setCaretPosition(0);
		levelNumberOfBoxes.setText(tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.NUMBER_OF_BOXES_INDEX).toString());

		Object lastChangeVal = tableModelLevelData.getValueAt(selectedRow, TableModelLevelData.LAST_CHANGED_INDEX);
		String lastChangeStr;
		if (dateRenderer != null) {
			lastChangeStr = dateRenderer.valueToString(lastChangeVal);
		} else {
			lastChangeStr = lastChangeVal.toString();
		}
		levelLastChanged.setText(lastChangeStr);

		// Get the levelID of the first selected level.
		int levelID = (Integer) tableModelLevelData.tableData.get(selectedRow)[TableModelLevelData.LEVELID_INDEX];

		// Get the board data of the level for showing it.
		Level level = database.getLevel(levelID);
		boardDisplay.setBoardToDisplay(level.getBoardDataAsString());

		// StringBuilder for concatenating all collection assignments.
		StringBuilder assignedCollections = new StringBuilder();

		HashMap<Integer, String> assignedCollectionsMap = database.getInfoAboutAssignedCollections(levelID);
		for(String collectionTitle : assignedCollectionsMap.values()) {
			assignedCollections.append(assignedCollections.length() == 0 ? collectionTitle : ", "+collectionTitle);
		}

		// Set the names of the collections the level is assigned to and scroll to the left.
		collectionAssignments.setText(assignedCollections.toString());
		collectionAssignments.setCaretPosition(0);
		collectionAssignments.setToolTipText(assignedCollections.toString());

		numberOfSolutions.setText(""+level.getSolutionsManager().getSolutionCount());
	}


	/**
	 * This method is called whenever the additional information of a selected
	 * level has changed.
	 * (For example, when a solution has been deleted)
	 */
	protected void updateAdditionalInformation() {
		int firstSelectedRow = tableLevelData.convertRowIndexToModel(tableLevelData.getSelectionModel().getMinSelectionIndex());
		valueChanged(new ListSelectionEvent(tableLevelData.getSelectionModel(), firstSelectedRow, firstSelectedRow, false));
	}

	/**
	 * Adds all author names to the author <code>ComboxBox</code>.
	 */
	void updateComboBoxAuthors() {

		// Update all needed ComoboBoxes of this view.
		super.updateComboBoxAuthors(comboBoxAuthors, selectionAuthor, authorsToBeSet);

		// When adding new items the combo boxes should not fire actions.
		// (If this isn't set the combo boxes refresh the views every time their content changes)
		doNotFireActions = true;

		// The selection ComboBox's first item is always the wildcard "*".
		selectionAuthor.insertItemAt(new ComboBoxEntry("*", 0), 0);

		// Set the wildcard as selected.
		selectionAuthor.setSelectedIndex(0);

		// Actions may be fired again.
		doNotFireActions = false;
	}

	/**
	 * Adds all collections names to the collections <code>ComboxBox</code>.
	 */
	protected void updateComboBoxCollections() {

		// Save the currently selected collection.
		String selectedItemString = "";
		Object object = selectionCollection.getSelectedItem();
		if(object != null) {
			selectedItemString = object.toString();
		}

		// Update the ComboBoxes using the new collection titles from the database.
		super.updateComboBoxCollections(comboBoxCollections, selectionCollection, collectionsForAddingLevelsTo);

		// When adding new items the ComboBoxes should not fire actions.
		doNotFireActions = true;

		// In the level menu all levels without a collection assignment can be selected, too.
		selectionCollection.addItem(new ComboBoxEntry(Texts.getText("withoutCollection"), -1));

		// Set back the selection if possible.
		for(int itemNo=selectionCollection.getItemCount(); --itemNo>=0; ) {
			if(selectionCollection.getItemAt(itemNo).toString().equals(selectedItemString)) {
				selectionCollection.setSelectedIndex(itemNo);
				selectedItemString = null;
				break;
			}
		}

		// If the collection isn't available anymore (deleted, renamed or
		// manually typed in) but there has been some collection selected
		// before, then set the collection name back (it may contain manually
		// typed in data which also may contain wildcards).
		if(selectedItemString != null && selectedItemString.equals("") == false) {
			selectionCollection.getEditor().setItem(selectedItemString);
			selectionCollection.setSelectedItem(selectedItemString);
		}

		// Actions may be fired again.
		doNotFireActions = false;
	}

	/**
	 * Returns the selected collection.
	 *
	 * @return the currently selected collection item
	 */
	protected Object getSelectedCollection() {
		return selectionCollection.getSelectedItem();
	}

	/**
	 * Table model for the table showing the level data.
	 */
	private class TableModelLevelData extends AbstractTableModel
	implements ColumnVisibility
	{

		// The indices of the columns.
		final protected static int LEVELID_INDEX 	       =  0;
		final protected static int AUTHORID_INDEX 		   =  1;
		final protected static int LEVEL_TITLE_INDEX 	   =  2;
		final protected static int AUTHOR_INDEX 		   =  3;
		final protected static int WIDTH_INDEX 	 	   	   =  4;
		final protected static int HEIGHT_INDEX	      	   =  5;
		final protected static int COLLECTION_TITLE_INDEX  =  6;
		final protected static int NUMBER_OF_BOXES_INDEX   =  7;
		final protected static int VIEW_INDEX 			   =  8;
		final protected static int DIFFICULTY_INDEX  	   =  9;
		final protected static int COMMENT_INDEX 		   = 10;
		final protected static int LAST_CHANGED_INDEX      = 11;

		// The names of the columns.
		private String[] columnNames = {
				"LEVELID",	 		 		 "AUTHORID",     	Texts.getText("levelTitle"),
				Texts.getText("author"), 	 "WIDTH", 			"HEIGHT",
				Texts.getText("collection"), "NUMBER_OF_BOXES", "VIEW",
				Texts.getText("difficulty"), "COMMENT", 		"LAST_CHANGED"};

		// Determination of the editable fields.
		private boolean[] isEditable = {
				false, false, true ,
				true , false, false,
				false, false, false,
				true , true , false };

		// Determination of the visible columns.
		private boolean[] isVisible = {
				false, false, true ,
				true , false, false,
				true , false, false,
				true , false, false };

		// The data of the table.
		protected ArrayList<Object[]> tableData = new ArrayList<Object[]>();


		/**
		 * Creates a table model for the level data table.
		 */
		public TableModelLevelData() {}


		/**
		 * Loads the data of the levels specified by the passed search pattern into
		 * this table model.
		 */
		public final void refreshData() {

			// If a SwingWorker is currently updating the tree stop it
			// because a new one is started now. The new one will be
			// executed when the currently running one has finished.
			if (fillTreeWithLevelDataWorker != null) {
				fillTreeWithLevelDataWorker.cancel(true);
			}

			// Delete the old data.
			tableData.clear();
			fireTableDataChanged();

			// Show a animation as long as the table is filled.
			final InfiniteProgressPanel progressPanel = new InfiniteProgressPanel() {
				@Override
				public Dimension getPreferredSize() {
					return new Dimension(20, 20);
				}
			};
			overlayTable.addOverlayComponent(progressPanel);
			progressPanel.start();

			// Create a new SwingWorker for loading the data from the database.
			fillTreeWithLevelDataWorker = new SwingWorker<Void, Object[]>() {

				final int WITHOUT_COLLECTION_ASSIGNMENT = -1;
				final int SELECT_COLLECTION_BY_NAME  	= -2;
				private long debugStartTimestamp = System.currentTimeMillis();

				@Override
				protected Void doInBackground() throws Exception {
					try {
						// Get the strings for the author selection.
						String author = selectionAuthor.getEditor().getItem().toString().replace('*', '%');

						// By default all collections are selected.
						String collectionNameForSelection = "%";
						int collectionIDForSelection = SELECT_COLLECTION_BY_NAME;

						// If the user entered a title manually then search for the title, otherwise search for the concrete ID.
						Object selectedItem = selectionCollection.getSelectedItem();
						if(selectedItem != null) {

							// If the user has manually entered a collection name we've gotten a String.
							// Otherwise we've gotten a ComboBoxEntry (which ID is WITHOUT_COLLECTION_ASSIGNMENT
							// when only the levels having no collection assignment are to be displayed)
							if(selectedItem instanceof ComboBoxEntry) {
								collectionIDForSelection = ((ComboBoxEntry) selectedItem).getID();
							} else {
								collectionNameForSelection = selectedItem.toString().replace('*', '%');
							}
						}

						// "Nothing" is treated as "display all".
						if(author.length() == 0) {
							author = "%";
						}
						if(collectionNameForSelection.length() == 0) {
							collectionNameForSelection = "%";
						}

						// Select statement to be sent to the database.
						final StringBuilder statement = new StringBuilder();

						/*
						 * Depending on what field the user has restricted to specific values these tables are selected first to reduce the size of the join.
						 */
						// The user has selected all levels not belonging to any collection.
						if(collectionIDForSelection == WITHOUT_COLLECTION_ASSIGNMENT) {
							statement.append(
									"SELECT levelID, authorID, levelTitle, name, width, height, numberOfBoxes, view, difficulty, levelComment, lastChanged " +
											"FROM levelData l LEFT JOIN collectionLevel cl on l.levelID = cl.levelID " +
											"     INNER JOIN authorData  a on l.authorID = a.authorID    "+
											"WHERE cl.levelID IS NULL AND " +
											"      LCASE(levelTitle) 	    like ? AND " +
											"      LCASE(levelComment)      like ? AND " +
											"      LCASE(difficulty) 	    like ? AND " +
									"      LCASE(a.name)		    like ?     ");
						}
						else {
							// The user has chosen a collection by using the combobox => the ID of the collection is known and this is selected first.
							if(collectionIDForSelection >= 0) {
								statement.append(
										"SELECT levelID, collectionTitle, authorID, levelTitle, name, width, height, numberOfBoxes, view, difficulty, levelComment, lastChanged, levelNumber " +
												"FROM collectionLevel cl  INNER JOIN  levelData l        on cl.levelID      = l.levelID  AND cl.collectionID = " + collectionIDForSelection +
												"                         INNER JOIN  authorData a       on l.authorID 	    = a.authorID 	                   " +
										"						  INNER JOIN  collectionData cd  on cl.collectionID = cd.collectionID				   ");
							}
							// The user has entered a specific collection name => the collections are selected first.
							else if(collectionNameForSelection.equals("%") == false){
								statement.append(
										"SELECT levelID, collectionTitle, authorID, levelTitle, name, width, height, numberOfBoxes, view, difficulty, levelComment, lastChanged, levelNumber " +
										"FROM collectionData cd   INNER JOIN collectionLevel cl on cd.collectionID = cl.collectionID ");
								if(author.equals("%")) {
									statement.append("        INNER JOIN levelData l        on cl.levelID      = l.levelID       " +
											"        INNER JOIN authorData a       on l.authorID 	   = a.authorID 	 ");
								}
								else {
									statement.append("        INNER JOIN authorData a       on l.authorID 	   = a.authorID 	 " +
											"        INNER JOIN levelData l        on cl.levelID      = l.levelID       ");

								}
							}
							// The user has set ALL collections to be selected. This means also levels without a collection assignment must be selected.
							else {
								statement.append(
										"SELECT levelID, collectionTitle, authorID, levelTitle, name, width, height, numberOfBoxes, view, difficulty, levelComment, lastChanged, levelNumber " +
												"FROM levelData l INNER JOIN authorData            a on l.authorID 	    = a.authorID 	  " +
												"				  LEFT OUTER JOIN collectionLevel cl on l.levelID 	    = cl.levelID 	  " +
										" 			      LEFT OUTER JOIN collectionData  cd on cl.collectionID = cd.collectionID ");

							}

							// Append the where clause including all data fields the user may have entered and an order clause.
							statement.append("WHERE LCASE(levelTitle) 	    like ? and " +
									"      LCASE(levelComment)     like ? and " +
									"      LCASE(difficulty) 	    like ? and " +
									"      LCASE(a.name)		    like ? and " +
									"     (LCASE(collectionTitle)  like ? OR collectionTitle IS NULL) and " +
									"      levelID  				>    ? ");

							// If levels of a specific collection are to be selected the levels are selected
							// ascending according to their level number (this avoids the usage of the "limit 1000").
							if(collectionIDForSelection >= 0) {
								statement.append(" order by  levelNumber");
							}
							else {
								// The select may result in several thousands of rows -> select in slices each containing 1000 rows.
								statement.append(" limit 1000");
							}
						}

						// Prepare the statement for the database.
						final PreparedStatement preparedLevelQuery = database.prepareStatement(statement.toString().trim());
						try {
							int parameterIndex = 1;
							preparedLevelQuery.clearParameters();
							preparedLevelQuery.setString(parameterIndex++, selectionTitle.getText().replace('*', '%').toLowerCase().trim());
							preparedLevelQuery.setString(parameterIndex++, selectionComment.getText().replace('*', '%').toLowerCase());
							preparedLevelQuery.setString(parameterIndex++, selectionDifficulty.getText().replace('*', '%').toLowerCase());
							preparedLevelQuery.setString(parameterIndex++, author.toLowerCase());
							if(collectionIDForSelection != WITHOUT_COLLECTION_ASSIGNMENT) {
								preparedLevelQuery.setString(parameterIndex++, collectionNameForSelection.toLowerCase());
							}
						} catch (SQLException e1) {
							e1.printStackTrace();
							return null;
						}

						int lastLevelID = -1;
						int highestReadLevelID = -1;
						int foundLevelsCount = 0;
						do {
							// If all level without a collection assignment are to be selected then this is done in one step.
							// Otherwise select all levels with an id than the highest selected before.
							if(collectionIDForSelection != WITHOUT_COLLECTION_ASSIGNMENT) {
								preparedLevelQuery.setInt(6, highestReadLevelID);
							}
							ResultSet resultLevelQuery = preparedLevelQuery.executeQuery();

							// The levelID of the last processed level.
							lastLevelID = -1;

							// The titles of the collections the level is assigned to.
							StringBuilder collectionTitles = new StringBuilder();

							// Number of selected levels.
							foundLevelsCount = 0;

							// Add the level data to the data of this model.
							while(resultLevelQuery.next() && !isCancelled()) {

								int levelID = resultLevelQuery.getInt("levelID");

								foundLevelsCount++;

								// Check if the levelID has changed. One level can be assigned to x collections,
								// so the ResultSet may contain several rows for the same level.
								if(levelID == lastLevelID) {
									collectionTitles.append(","+resultLevelQuery.getString("collectionTitle"));
									continue;
								}

								// The data of a new level have been reached. Save the current levelID.
								lastLevelID = levelID;

								highestReadLevelID = Math.max(levelID, highestReadLevelID);

								// Get the collection title.
								try {
									String title = resultLevelQuery.getString("collectionTitle");
									collectionTitles = title == null ? new StringBuilder() : new StringBuilder(title);
								}catch (Exception e) {
									/* levels without a collection assignment don't have a collection title. */
								}

								// Create a data row for the table.
								Object[] dataRow = {
										levelID,
										resultLevelQuery.getInt("authorID"),
										resultLevelQuery.getString("levelTitle"),
										new ComboBoxEntry(resultLevelQuery.getString("name"),resultLevelQuery.getInt("authorID")) ,
										resultLevelQuery.getInt("width"),
										resultLevelQuery.getInt("height"),
										collectionTitles,
										resultLevelQuery.getInt("numberOfBoxes"),
										resultLevelQuery.getString("view"),
										resultLevelQuery.getString("difficulty"),
										resultLevelQuery.getString("levelComment"),
										resultLevelQuery.getTimestamp("lastChanged"),
								};

								// Add the level data to the table.
								publish(dataRow);
							}

							// Show the current results in the table.
							process(null);

							// The select reads 1000 levels at a time. If fewer levels have been read the search is over.
							// If all level without a collection assignment are to be selected then this is done in one step.
						}while(foundLevelsCount == 1000 && collectionIDForSelection != WITHOUT_COLLECTION_ASSIGNMENT);

					}catch(SQLException e) {
						e.printStackTrace();
					}

					return null;
				}

				@Override
				protected void process(List<Object[]> data) {

					if(data == null) {

						// Hide the overlayable.
						if(progressPanel.isVisible()) {
							overlayTable.removeOverlayComponent(progressPanel);
							progressPanel.stop();
							progressPanel.setVisible(false);
						}

						// Display the number of loaded levels.
						numberOfLevelLabel.setText(Texts.getText("database.xLevelsSelected", tableModelLevelData.getRowCount()));

						// Display the current table so the user can see the first result of the database query.
						fireTableDataChanged();
					} else {
						// Add the level data to the table.
						for(Object[] dataRow : data) {
							tableData.add(dataRow);
						}
					}
				}

				@Override
				protected void done() {

					// Display the number of loaded levels.
					numberOfLevelLabel.setText(Texts.getText("database.xLevelsSelected", tableModelLevelData.getRowCount()));

					// Hide the overlayable.
					overlayTable.removeOverlayComponent(progressPanel);
					progressPanel.stop();
					overlayTable.repaint();

					// Notify the listeners that the data have been changed.
					fireTableDataChanged();

					if(tableData.size() > 0) {
						tableLevelData.setRowSelectionInterval(0, 0);
					}

					if(Debug.isDebugModeActivated) {
						System.out.printf("\nTime for select: %dms\n", (System.currentTimeMillis()-debugStartTimestamp));
					}
				}
			};
			executor.submit(fillTreeWithLevelDataWorker);
		}


		@Override
		public final int getColumnCount() {
			return columnNames.length;
		}

		@Override
		public final int getRowCount() {
			return tableData.size();
		}

		@Override
		public final String getColumnName(int col) {
			return columnNames[col];
		}

		@Override
		public final Object getValueAt(int row, int col) {
			try {
				return tableData.get(row)[col];
			}
			catch(IndexOutOfBoundsException e) {
				return null;
			}
		}

		@Override
		public final Class<?> getColumnClass(int c) {
			Object o = getValueAt(0, c);
			if(o == null) {
				return String.class;
			}

			return o.getClass();
		}


		/**
		 * Returns whether a cell is editable.
		 *
		 * @param row the row of the cell
		 * @param col the column of the cell
		 *
		 * @return <code>true</code> if the cell is editable, and<br>
		 * 		  <code>false</code> if the cell is not editable
		 */
		@Override
		public final boolean isCellEditable(int row, int col) {
			return isEditable[col];
		}


		@Override
		public final boolean isColumnVisible(int col) {
			return isVisible[col];
		}


		/* (non-Javadoc)
		 * @see javax.swing.table.AbstractTableModel#setValueAt(java.lang.Object, int, int)
		 */
		@Override
		public void setValueAt(Object value, int row, int col) {

			// A value "null" must never be set. (this might be possible when a ComboBox is open
			// and then the content of this ComboBox changes. The ComboBox then returns "null").
			if (value == null) {
				return;
			}

			// Get the data of the changed row.
			Object[] rowData = tableData.get(row);

			// If the data hasn't changed return immediately.
			if(rowData[col].equals(value)) {
				return;
			}

			// Determine the new timestamp.
			Timestamp newTimestamp = new Timestamp(System.currentTimeMillis());

			// Set the new value and a new timestamp.
			rowData[col] = value;
			rowData[LAST_CHANGED_INDEX]  = newTimestamp;
			fireTableRowsUpdated(row, row);

			// If the level title has changed the level assignment view must be refreshed
			// the next time it is displayed.
			if(col == LEVEL_TITLE_INDEX) {
				// Set the flag that a relevant change for the other views has been done.
				DatabaseChangesInViews.changeInLevelView();
			}

			// If a new author has been set update the authorID
			if(value instanceof ComboBoxEntry) {
				rowData[AUTHORID_INDEX] = ((ComboBoxEntry) value).getID();
			}

			// Extract the data from the table row.
			int databaseID 		= (Integer) rowData[LEVELID_INDEX];
			String title 	 	= (String)  rowData[LEVEL_TITLE_INDEX];
			Integer authorID 	= (Integer) rowData[AUTHORID_INDEX];
			String comment 		= (String)  rowData[COMMENT_INDEX];
			String difficulty 	= (String)  rowData[DIFFICULTY_INDEX];

			database.updateLevel(databaseID, title, authorID, comment, difficulty, newTimestamp);

			// Update the time stamp information if the selected row has been modified.
			if(row == tableLevelData.convertRowIndexToModel(tableLevelData.getSelectionModel().getMinSelectionIndex())) {
				updateAdditionalInformation();
			}
		}
	}
}