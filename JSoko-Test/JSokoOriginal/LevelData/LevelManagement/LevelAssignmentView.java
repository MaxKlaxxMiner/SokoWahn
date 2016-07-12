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
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.InputEvent;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.util.ArrayList;

import javax.swing.JButton;
import javax.swing.JComboBox;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JTable;
import javax.swing.ListSelectionModel;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.table.AbstractTableModel;
import javax.swing.table.TableRowSorter;

import com.jidesoft.swing.AutoCompletionComboBox;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.BoardDisplay;
import de.sokoban_online.jsoko.gui.ColumnVisibility;
import de.sokoban_online.jsoko.gui.DateTimeRenderer;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseGUI.DatabaseChangesInViews;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.JScrollPaneOSSpecific;


/**
 * This class is used in the database GUI and displayed in the tabbed pane as
 * "collection levels".
 * It displays the levels of a collection which can be reordered by drag&drop
 * and be deleted.
 */
@SuppressWarnings("serial")
public class LevelAssignmentView extends LevelManagementView implements ListSelectionListener {

	/** ComboBox for selecting a specific collection to be displayed. */
	protected JComboBox  selectionCollection;

	/** Reference to the object for the graphical output of a board. */
	private BoardDisplay boardDisplay;

	// The table models for showing the data and the corresponding tables.
	private JTable tableLevelAssignment;
	private TableModelLevelAssignmentData tableModelLevelAssignmentData;


	/**
	 * Creates the <code>JPanel</code> for managing the assignments of levels to a collection.
	 *
	 * @param database the database of the program
	 * @param levelManagementDialog the <code>JDialog</code> all views are displayed in
	 * @param application Reference to the main object which holds all references
	 */
	public LevelAssignmentView(Database database, DatabaseGUI levelManagementDialog, JSoko application) {

		this.database = database;
		this.levelManagementDialog = levelManagementDialog;
		this.application = application;

		// Create all things this panel needs.
		createPanel();

		// Update ComboBox holding all collections.
		updateComboBoxCollections(selectionCollection);

		// Set the help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "database.collection-levels-tab", null); // Enable help
	}

	/**
	 * Creates all things this panel needs.
	 */
	private final void createPanel() {

		// Set a border layout for arranging the Swing elements.
		setLayout(new BorderLayout());

		// Create a panel where the fields for the selections are located.
		JPanel selectionPanel = new JPanel(new GridBagLayout());

		// Add the search fields to the panel.
		GridBagConstraints constraints = new GridBagConstraints();
		constraints.insets = new Insets(2, 2, 2, 2);
		constraints.anchor = GridBagConstraints.LINE_START;

		// For better optic a filler above.
		constraints.gridy++;
		selectionPanel.add(new JLabel(" "), constraints);

		// Collection
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("collection")+":"), constraints);
		constraints.gridx++;
		selectionCollection = new AutoCompletionComboBox();
		selectionCollection.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				// If the user selected a new item using the mouse or pressed "enter" refresh the view.
				String action = e.getActionCommand();
				if((action.equals("comboBoxChanged") && e.getModifiers() == InputEvent.BUTTON1_MASK) || // selected item with mouse
				   action.equals("comboBoxEdited")) {
					LevelAssignmentView.this.actionPerformed(new ActionEvent(selectionCollection,0, "refreshView"));
				}
			}
		});
		selectionPanel.add(selectionCollection, constraints);


		// Add a Panel for showing the board of the selected level
		// -------------------------------------------------------
		boardDisplay = new BoardDisplay();
		constraints.gridx++;
		constraints.gridy=0;
		constraints.weightx = 1;
		constraints.anchor = GridBagConstraints.CENTER;
		constraints.fill   = GridBagConstraints.BOTH;
		constraints.gridheight = 99;  // 99 = a lot = as many as there are available depending on the other objects in the panel
		constraints.gridwidth  = 2;
		selectionPanel.add(boardDisplay, constraints);


		// Add the buttons for the actions
		// -------------------------------

		// Button for deleting an assignment.
		constraints.gridx   = 4;
		constraints.gridy   = 1;
		constraints.weightx = 0;
		constraints.anchor  = GridBagConstraints.LINE_START;
		constraints.fill    = GridBagConstraints.HORIZONTAL;
		constraints.gridheight = 1;
		constraints.gridwidth  = 1;
		JButton deleteButton = new JButton(Texts.getText("deleteAssignment"));
		deleteButton.setToolTipText(Texts.getText("deleteAssignmentTooltip"));
		deleteButton.setActionCommand("deleteLevelAssignment");
		deleteButton.addActionListener(this);
		selectionPanel.add(deleteButton, constraints);

		// Button for playing the selected collection.
		constraints.gridy++;
		JButton playCollectionButton = new JButton(Texts.getText("playCollection"));
		playCollectionButton.setActionCommand("playCollection");
		playCollectionButton.addActionListener(this);
		selectionPanel.add(playCollectionButton, constraints);

		// Button for playing the selected levels.
		constraints.gridy++;
		JButton playLevelsButton = new JButton(Texts.getText("playLevel"));
		playLevelsButton.setActionCommand("playLevel");
		playLevelsButton.addActionListener(this);
		selectionPanel.add(playLevelsButton, constraints);

		// For better optic and the correct alignment add a filler below.
		constraints.gridx=3;
		constraints.gridy++;
		constraints.weightx = 1;
		selectionPanel.add(new JLabel(" "), constraints);

		// Add the selection panel to the view panel.
		add(selectionPanel, BorderLayout.NORTH);


		// Create the table for showing the data.
		tableModelLevelAssignmentData = new TableModelLevelAssignmentData();
		tableLevelAssignment = new JTable(tableModelLevelAssignmentData);

		// Set a sorter for the table.
		tableLevelAssignment.setRowSorter(new TableRowSorter<TableModelLevelAssignmentData>(tableModelLevelAssignmentData));

		// Prepare for correct conversion of Date/Timestamp values
		new DateTimeRenderer().enterMeForTypeDate(tableLevelAssignment);

		// Edits should automatically be finished when cells loose the focus.
		tableLevelAssignment.putClientProperty("terminateEditOnFocusLost", Boolean.TRUE);

		// Add a mouseListener to the headers. Note: It's important that this happens
		// before the sorter adds his listeners, because the sorter methods depends
		// on flags set in the method "mouseClicked" !
		tableLevelAssignment.getTableHeader().addMouseListener(this);
		tableLevelAssignment.addMouseListener(this);

		// Add a list selection listener to handle selections of a level in the list.
		tableLevelAssignment.getSelectionModel().addListSelectionListener(this);

		// Hide all columns that are not to be displayed
		Utilities.tableHideInvisibleColumns(tableLevelAssignment, tableModelLevelAssignmentData);

		// Adjust the size of the level number to a proper value.
		tableLevelAssignment.getColumn(Texts.getText("levelTitle")).setPreferredWidth(1000);
		optimizeColumnWidths(tableLevelAssignment, 1);

		// Add the scrollpane containing the JTable to the main panel.
		add(JScrollPaneOSSpecific.getJScrollPane(tableLevelAssignment), BorderLayout.CENTER);
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
		if(actionCommand.startsWith("refresh")) {
			tableModelLevelAssignmentData.refreshData();

			// Select the first row if no row has been selected, yet.
			if(tableLevelAssignment.getSelectedRow() == -1 && tableLevelAssignment.getRowCount() > 0) {
				tableLevelAssignment.getSelectionModel().setSelectionInterval(0, 0);
			}

			return;
		}

		// Delete the selected levels from the collection.
		if(actionCommand.startsWith("delete")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = tableLevelAssignment.getSelectedRows();
			for (int i = 0; i < selectedRowsNumbers.length; i++) {
				selectedRowsNumbers[i] = tableLevelAssignment.convertRowIndexToModel(selectedRowsNumbers[i]);
			}

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(this, Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			// Get the collectionID
			int collectionID = ((ComboBoxEntry) selectionCollection.getSelectedItem()).getID();

			// Delete all selected levels from the table model.
			for(int rowCount=selectedRowsNumbers.length; --rowCount>=0;) {
				tableModelLevelAssignmentData.tableData.remove(selectedRowsNumbers[rowCount]);
			}

			ArrayList<Integer> levelIDsInNewOrder = new ArrayList<Integer>();

			// Renumber all level numbers according to their position in the list and
			// insert the new collection structure to the database.
			for(int rowIndex=0; rowIndex < tableModelLevelAssignmentData.getRowCount(); rowIndex++) {

				Object[] rowData = tableModelLevelAssignmentData.tableData.get(rowIndex);

				// Set the new level number.
				rowData[tableModelLevelAssignmentData.LEVEL_NUMBER_INDEX] = rowIndex+1;

				levelIDsInNewOrder.add((Integer) rowData[tableModelLevelAssignmentData.LEVELID_INDEX]);

			}

			// Set the new level order in the database.
			database.setCollectionLevels(collectionID, levelIDsInNewOrder);

			// Notify the model that the data have been changed.
			tableModelLevelAssignmentData.fireTableDataChanged();

			// Set the flag that a relevant change for the other views has been done.
			DatabaseChangesInViews.changeInAssignmentView();

			return;
		}

		// Play the selected levels.
		if(actionCommand.startsWith("playLevel")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = tableLevelAssignment.getSelectedRows();
			for (int i = 0; i < selectedRowsNumbers.length; i++) {
				selectedRowsNumbers[i] = tableLevelAssignment.convertRowIndexToModel(selectedRowsNumbers[i]);
			}

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Create a new collection for the selected levels.
			LevelCollection.Builder levelCollectionBuilder = new LevelCollection.Builder();

			ArrayList<Level> collectionLevels = new ArrayList<Level>();
			for (int rowNumber : selectedRowsNumbers) {

				int levelID = ((Integer) tableModelLevelAssignmentData.getValueAt(rowNumber, tableModelLevelAssignmentData.LEVELID_INDEX)).intValue();

				collectionLevels.add(database.getLevel(levelID));
			}

			levelCollectionBuilder.setLevels(collectionLevels)
								  .setTitle(Texts.getText("withoutCollection"));


			// Set the new collection as current collection.
			application.setCollectionForPlaying(levelCollectionBuilder.build());		//TODO: fire event instead

			// Prepare everything for playing the first of the new levels.
			application.setLevelForPlaying(1);

			// Close the database browser, so the user can play the level.
			levelManagementDialog.finalize();

			return;
		}

		// Play the selected collection.
		if(actionCommand.startsWith("playCollection")) {

			// If there are no levels in the collection the collection isn't loaded.
			if(tableModelLevelAssignmentData.getRowCount() == 0) {
				return;
			}

			ComboBoxEntry entry = (ComboBoxEntry) selectionCollection.getSelectedItem();
			if(entry == null) {
				return;
			}

			LevelCollection levelCollection = database.getLevelCollection(entry.ID);

			// Set the new collection as current collection.
			application.setCollectionForPlaying(levelCollection);  // TODO: fire events instead of direct call of the controller class

			// Prepare everything for playing the first of the new levels.
			application.setLevelForPlaying(1);

			// Close the database browser, so the user can play the level.
			levelManagementDialog.finalize();

			return;
		}
	}

	/**
	 * This methods ensures that always the board of the currently selected level is shown.
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

		// If no row is selected no board is shown.
		if (lsm.isSelectionEmpty()) {
			boardDisplay.setBoardToDisplay("");
			return;
		}

		// Convert the external view row to the internal number (the user may have sorted the view).
		selectedRow = tableLevelAssignment.convertRowIndexToModel(selectedRow);

		// Set the board of the first selected level as board to be shown.
		boardDisplay.setBoardToDisplay(tableModelLevelAssignmentData.getValueAt(selectedRow, tableModelLevelAssignmentData.BOARD_DATA).toString());
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

		// Update ComboBox holding all collections.
		super.updateComboBoxCollections(selectionCollection);

		// When adding new items the ComboBoxes should not fire actions.
		doNotFireActions = true;

		// Set back the selection if possible.
		for(int itemNo=selectionCollection.getItemCount(); --itemNo>=0; ) {
			if(selectionCollection.getItemAt(itemNo).toString().equals(selectedItemString)) {
				selectionCollection.setSelectedIndex(itemNo);
				selectedItemString = null;
				break;
			}
		}

		// Actions may be fired again.
		doNotFireActions = false;
	}


	/**
	 * Table model for the table showing the collection data.
	 */
	private class TableModelLevelAssignmentData extends AbstractTableModel
		implements ColumnVisibility
	{

		// The indices of the columns.
		final protected int COLLECTIONID_INDEX = 0;
		final protected int LEVELID_INDEX      = 1;
		final protected int LEVEL_NUMBER_INDEX = 2;
		final protected int LEVEL_TITLE_INDEX  = 3;
		final protected int LAST_CHANGED_INDEX = 4;
		final protected int BOARD_DATA		   = 5;

		// The names of the columns
		private String[] columnNames = {"COLLECTIONID", "LEVELID",
				Texts.getText("levelnumber"), Texts.getText("levelTitle"), Texts.getText("lastChanged"), "BOARD_DATA"};

		// Determination of the editable fields.
		private boolean[] editable = {false, false, true, false, false, false};

		// Determination of the visible columns.
		private boolean[] isVisible = {false, false, true, true, false, false};

		// The data of the table.
		protected ArrayList<Object[]> tableData = new ArrayList<Object[]>();


		/**
		 * Creates a table model for the collection data table.
		 */
		public TableModelLevelAssignmentData() {}


		/**
		 * Loads the data of the collections specified by the passed search pattern into
		 * this table model.
		 */
		public final void refreshData() {

			// Check whether there is any selected collection.
			if(selectionCollection.getSelectedIndex() == -1) {
				return;
			}

			// Get the ID of the selected collection.
			int collectionID = ((ComboBoxEntry) selectionCollection.getSelectedItem()).getID();

			// Get the collection data.
			String statement =
				"SELECT * " +
				"FROM collectionLevel cl INNER JOIN levelData l ON cl.levelID = l.levelID AND cl.collectionID = ? " +
				" ORDER BY levelNumber";

			PreparedStatement p = database.prepareStatement(statement);

			try {
				int index = 1;
				p.clearParameters();
				p.setInt(index++, collectionID);
				ResultSet result = p.executeQuery();

				// Immediately return if an error occurred.
				if(result == null) {
					return;
				}

				// Delete the old data.
				tableData.clear();

				// Add the collection data to the data of this model.
				while(result.next()) {
					Object[] dataRow = {
							result.getInt("collectionID"),
							result.getInt("levelID"),
							result.getInt("levelNumber"),
							result.getString("levelTitle"),
							result.getTimestamp("lastChanged"),
							result.getString("BOARDDATA")
					};

					// Add the collection data to the table data.
					tableData.add(dataRow);
				}
			}catch(SQLException e) {e.printStackTrace();}

			// Notify the model that the data have been changed.
			fireTableDataChanged();
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
		 * @return <code>true</code> if the cell is editable,<br>
		 * 		  <code>false</code> if the cell is not editable
		 */
		@Override
		public final boolean isCellEditable(int row, int col) {
			return editable[col];
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
			if(value == null) {
				return;
			}

			// Get the data of the changed row.
			Object[] rowData = tableData.get(row);

			// If the level number value hasn't changed immediately return.
			if(col == LEVEL_NUMBER_INDEX && ((Integer) rowData[col]).compareTo((Integer) value) == 0) {
				return;
			}

			// Determine the new timestamp.
			Timestamp newTimestamp = new Timestamp(System.currentTimeMillis());

			// Set the new value and a new timestamp.
			rowData[col] = value;
			rowData[LAST_CHANGED_INDEX] = newTimestamp;

			// Delete the row from the table.
			tableData.remove(row);

			// New level number as Integer.
			Integer newLevelNumber = (Integer) value;

			// Ensure the new number is in the range.
			if(newLevelNumber.intValue() < 1) {
				newLevelNumber = 1;
			}
			if(newLevelNumber.intValue() > getRowCount()) {
				newLevelNumber = getRowCount() + 1;
			}

			// Insert the row to the new position.
			tableData.add(newLevelNumber.intValue()-1, rowData);

			// Renumber all level numbers according to their position in the list and
			// insert the new collection structure to the database.
			ArrayList<Integer> levelIDsInNewOrder = new ArrayList<Integer>();
			for(int rowIndex=0; rowIndex < getRowCount(); rowIndex++) {
				rowData = tableData.get(rowIndex);
				rowData[LEVEL_NUMBER_INDEX] = rowIndex+1;

				levelIDsInNewOrder.add((Integer) rowData[LEVELID_INDEX]);
			}

			int collectionID = (Integer) rowData[COLLECTIONID_INDEX];
			database.setCollectionLevels(collectionID, levelIDsInNewOrder);

			// Fire table changed so the table redraws itself.
			fireTableDataChanged();

			// Set the flag that a relevant change for the other views has been done.
			DatabaseChangesInViews.changeInAssignmentView();
		}
	}
}