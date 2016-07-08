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
import java.awt.Font;
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
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.table.AbstractTableModel;
import javax.swing.table.TableColumn;
import javax.swing.table.TableRowSorter;

import com.jidesoft.swing.AutoCompletionComboBox;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.ColumnVisibility;
import de.sokoban_online.jsoko.gui.DateTimeRenderer;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseGUI.DatabaseChangesInViews;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.JScrollPaneOSSpecific;



/**
 * This class displays the "CollectionsView" in the level management dialog
 * and handles all actions in this view.
 * This Panel contains all elements of the "CollectionsView" and is displayed
 * in a tabbed pane in the level management.
 */
@SuppressWarnings("serial")
public class CollectionsView extends LevelManagementView implements ListSelectionListener {

	/**
	 * The ComboBox holding all collection names.
	 * It is used for selecting a collection in a table cell.
	 */
	protected JComboBox comboBoxCollections = new JComboBox();

	/**
	 * The ComboBox holding all author names.
	 * It is used for selecting an author in a table cell.
	 */
	private JComboBox comboBoxAuthors = new JComboBox();

	// There are several data that can be filtered. For every data element there is an own selection field.
	protected JTextField selectionCollection;
	protected AutoCompletionComboBox selectionAuthor;
	protected JTextField selectionComment;

	/** Additional information about the collection. */
	private JTextArea commentText;

	/** The buttons for saving comments in the level and the collection view. */
	private JButton saveCommentButton;

	/** The table for showing the collection data. */
	private JTable tableCollectionData;

	/** The table model for this collection data. */
	private TableModelCollectionData tableModelCollectionData;


	/**
	 * Creates the <code>JPanel</code> for displaying the data of the collections
	 * in the <code>LevelManagement</code>.
	 *
	 * @param database the database of the program
	 * @param levelManagementDialog the {@code JDialog} all views are displayed in
	 * @param application Reference to the main object which holds all references
	 */
	public CollectionsView(Database database, DatabaseGUI levelManagementDialog, JSoko application) {

		this.database = database;
		this.levelManagementDialog = levelManagementDialog;
		this.application = application;

		// Create all things this panel needs.
		createPanel();

		// Update the ComboBoxes for the authors.
		updateComboBoxAuthors();

		// Update ComboBox holding all collections.
		updateComboBoxCollections(comboBoxCollections);

		// Set the help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "database.collections-tab", null);
	}

	/**
	 * Creates all things this panel needs.
	 */
	private void createPanel() {

		// Set a border layout for arranging the Swing elements.
		setLayout(new BorderLayout());

		// Create a panel where the fields for the selections are located.
		JPanel selectionPanel = new JPanel(new GridBagLayout());

		// Add the search fields to the panel.
		GridBagConstraints constraints = new GridBagConstraints();
        constraints.insets = new Insets(2, 2, 2, 2);
        constraints.anchor = GridBagConstraints.LINE_START;

        // For better optic define a filler above.
        constraints.gridy++;
        selectionPanel.add(new JLabel(" "), constraints);

        // Collection
        constraints.gridy++;
        constraints.gridx=0;
        selectionPanel.add(new JLabel(Texts.getText("collection")+":"), constraints);
        constraints.gridx++;
        selectionCollection = new JTextField("*", widthOfSelectionFields);
        selectionCollection.addActionListener(this);
        selectionCollection.setActionCommand("refreshView");
        selectionPanel.add(selectionCollection, constraints);

		// Author
        constraints.gridy++;
        constraints.gridx=0;
        selectionPanel.add(new JLabel(Texts.getText("author")+":"), constraints);
        constraints.gridx++;
        selectionAuthor = new AutoCompletionComboBox();
        selectionAuthor.setEditable(true);
        selectionAuthor.setStrict(false);
        selectionAuthor.setPreferredSize(selectionCollection.getPreferredSize());
        selectionAuthor.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				// If the user selected a new item using the mouse or pressed "enter" refresh the view.
				String action = e.getActionCommand();
				if((action.equals("comboBoxChanged") && e.getModifiers() == InputEvent.BUTTON1_MASK) || // selected item with mouse
				   action.equals("comboBoxEdited")) {
					CollectionsView.this.actionPerformed(new ActionEvent(selectionAuthor,0, "refreshView"));
				}
			}
		});
        selectionPanel.add(selectionAuthor, constraints);

        // Comment
        constraints.gridy++;
        constraints.gridx=0;
        selectionPanel.add(new JLabel(Texts.getText("comment")+":"), constraints);
        constraints.gridx++;
        selectionComment = new JTextField("*", widthOfSelectionFields);
        selectionComment.addActionListener(this);
        selectionComment.setActionCommand("refreshView");
        selectionPanel.add(selectionComment, constraints);


        // Add the buttons for the actions
        // -------------------------------

		// Button for inserting a collection.
		JButton insertButton = new JButton(Texts.getText("insertDataset"));
		insertButton.setActionCommand("insertCollection");
		insertButton.addActionListener(this);
		constraints.gridx=4;
        constraints.gridy = 1;
        constraints.weightx = 0;
        constraints.fill = GridBagConstraints.HORIZONTAL;
		selectionPanel.add(insertButton, constraints);

		// Button for deleting a collection.
		JButton deleteButton = new JButton(Texts.getText("deleteCollection"));
		deleteButton.setToolTipText(Texts.getText("deleteCollectionTooltip"));
		deleteButton.setActionCommand("deleteCollection");
		deleteButton.addActionListener(this);
        constraints.gridy++;
		selectionPanel.add(deleteButton, constraints);

		// Button for playing a collection.
		JButton playButton = new JButton(Texts.getText("playCollection"));
		playButton.setActionCommand("playCollection");
		playButton.addActionListener(this);
        constraints.gridy++;
		selectionPanel.add(playButton, constraints);


		// For better optic and the correct alignment add a filler below.
        constraints.gridx=3;
        constraints.gridy++;
        constraints.weightx = 1;
        selectionPanel.add(new JLabel(" "), constraints);

        // Add the selection panel to the collection view panel.
        add(selectionPanel, BorderLayout.NORTH);

        // In the south of the level view there is this panel for showing
        // additional information about the level.
		JPanel southPanel = new JPanel(new GridBagLayout());
		add(southPanel, BorderLayout.SOUTH);

        constraints.gridy	   = 0;
        constraints.gridx	   = 0;
        constraints.weightx    = 0;
        constraints.gridheight = 1;
        constraints.anchor  = GridBagConstraints.LINE_START;
		southPanel.add(new JLabel(" "), constraints);

		// A textfield for displaying the comment of the level.
		constraints.gridx=0;
		constraints.gridy++;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		southPanel.add(new JLabel(Texts.getText("comment")+":"), constraints);
		constraints.gridx++;
		constraints.gridheight = 6;
		constraints.weightx = 1;
		constraints.fill = GridBagConstraints.BOTH;
		commentText = new JTextArea(4, 1);
		commentText.setLineWrap(true);
		commentText.setEnabled(false);
		southPanel.add(JScrollPaneOSSpecific.getJScrollPane(commentText), constraints);

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
		southPanel.add(saveCommentButton, constraints);

        // Create the table for showing the collection data.
        tableModelCollectionData = new TableModelCollectionData();
		tableCollectionData = new JTable(tableModelCollectionData);

		// Set a sorter for the table.
		tableCollectionData.setRowSorter(new TableRowSorter<TableModelCollectionData>(tableModelCollectionData));


		// If a cell lost its focus the edited value is to be set.
		// This avoids situations like this:
		// - 2 collections are shown in the table.
		// - The user edits the name of the 1st collection and doesn't finish
		//   the edit with "enter".
		// - Now the user limits the shown collection to the 2nd collection
		//   (so the 1st collection isn't shown anymore).
		// - If the user now selects the shown line the JTable tries to finish
		//   the edit - but the line isn't stored in the JTable anymore
		// --> exception.
		tableCollectionData.putClientProperty("terminateEditOnFocusLost", Boolean.TRUE);

		// Add a mouseListener to the headers.
		tableCollectionData.getTableHeader().addMouseListener(this);
		tableCollectionData.addMouseListener(this);
		tableCollectionData.getSelectionModel().addListSelectionListener(this);

		// Hide all columns that are not to be displayed
		Utilities.tableHideInvisibleColumns(tableCollectionData, tableModelCollectionData);

		// Create a ComboBox editor for the author column.
		TableColumn authorColumn = tableCollectionData.getColumnModel().getColumn(tableCollectionData.getColumnModel().getColumnIndex(Texts.getText("author")));
		authorColumn.setCellEditor(new DefaultCellEditor(comboBoxAuthors));

		//tableCollectionData.setDefaultRenderer(Timestamp.class, new DateTimeRenderer());
		//tableCollectionData.getColumnModel().getColumn(tableModelCollectionData.LAST_CHANGED_INDEX).setCellRenderer(new DateTimeRenderer());
		new DateTimeRenderer().enterMeForTypeDate(tableCollectionData);

		// Add the scrollpane containing the JTable showing the
		// collection data to the main panel.
		add(new JScrollPane(tableCollectionData), BorderLayout.CENTER);
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
			tableModelCollectionData.refreshData();

			return;
		}

		// Insert a new collection to the database.
		if(actionCommand.startsWith("insertCollection")) {

			try {
				LevelCollection levelCollection = new LevelCollection.Builder().build();
				levelCollection = database.insertLevelCollectionWithoutLevels(levelCollection);

				// Add the collection data to the data of this model.
				Object[] dataRow = {levelCollection.getDatabaseID(),
						levelCollection.getAuthor().getDatabaseID(),
						levelCollection.getTitle(),
						levelCollection.getAuthor().getName(),
						levelCollection.getComment(),
						Utilities.nowString()
				};

				// Add the collection data to the table data.
				tableModelCollectionData.tableData.add(0, dataRow);

				// A new collection has been inserted, hence the collection ComboBox has to be updated.
				updateComboBoxCollections(comboBoxCollections);

				// Notify the model that the data have been changed.
				tableModelCollectionData.fireTableRowsInserted(tableModelCollectionData.getRowCount()-1, tableModelCollectionData.getRowCount()-1);

				// Set the flag that a relevant change for the other views has been done.
				DatabaseChangesInViews.collectionNamesChanged();

			}catch(SQLException exception) {exception.printStackTrace();}

			return;
		}


		// Delete the selected collection from the table and delete the collection from the database.
		if(actionCommand.startsWith("deleteCollection")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = tableCollectionData.getSelectedRows();
			for (int i = 0; i < selectedRowsNumbers.length; i++) {
				selectedRowsNumbers[i] = tableCollectionData.convertRowIndexToModel(selectedRowsNumbers[i]);
			}

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(this, Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			ArrayList<Integer> deletedCollectionIDs = new ArrayList<Integer>();

			// Delete all selected rows.
			for(int rowCount=selectedRowsNumbers.length; --rowCount>=0;) {

				// Get the collectionID.
				int collectionID = (Integer) tableModelCollectionData.getValueAt(selectedRowsNumbers[rowCount], tableModelCollectionData.COLLECTIONID_INDEX);

				database.deleteLevelCollection(collectionID);

				// Delete the collection from the tableModel.
				tableModelCollectionData.tableData.remove(selectedRowsNumbers[rowCount]);

				deletedCollectionIDs.add(collectionID);
			}

			if(!deletedCollectionIDs.isEmpty()) {
				// A collection has been deleted, hence the collection ComboBox has to be updated.
				updateComboBoxCollections(comboBoxCollections);

				// Notify the model that the data have been changed.
				tableModelCollectionData.fireTableDataChanged();

				// All other views must refresh, because the collections have changed, levels may have
				// been deleted and also authors may have been deleted.
				DatabaseChangesInViews.changeInAssignmentView();
				DatabaseChangesInViews.authorsNamesChanged();
				DatabaseChangesInViews.changeInLevelView();
				DatabaseChangesInViews.collectionNamesChanged();

			}

			return;
		}

		// Sets the first selected collection for playing.
		if(actionCommand.startsWith("playCollection")) {

			// Return if no row is selected.
			if(tableCollectionData.getSelectedRow() == -1) {
				return;
			}

			// Get the number of the first selected row.
			int selectedRowNumber = tableCollectionData.convertRowIndexToModel(tableCollectionData.getSelectedRow());

			// Get the collectionID.
			int collectionID = ((Integer ) tableModelCollectionData.getValueAt(selectedRowNumber, tableModelCollectionData.COLLECTIONID_INDEX)).intValue();

			// Set the selected collection as new collection for playing.
			LevelCollection levelCollection = application.levelIO.database.getLevelCollection(collectionID);

			if(levelCollection == null || levelCollection.isEmpty()) {
				MessageDialogs.showFailureTextKey(this, "noLevelsInCollection");
				return;
			}

			// Set this collection as new collection for playing.
			application.setCollectionForPlaying(levelCollection);

			// Set the first level for playing.
			application.setLevelForPlaying(1);

			// Close the database browser, so the user can play the level.
			levelManagementDialog.finalize();

			return;
		}

		// Save the comment
		if(actionCommand.startsWith("saveComment")) {

			// Immediately return if no row is selected.
			if(tableCollectionData.getSelectedRow() == -1) {
				return;
			}

			// Set the new comment.
			tableModelCollectionData.setValueAt(commentText.getText().intern(),
					tableCollectionData.convertRowIndexToModel(tableCollectionData.getSelectedRow()),
					tableModelCollectionData.COMMENT_INDEX);
			return;
		}
	}

	/**
	 * This methods sets the information about a collection in some extra
	 * fields every time they change.
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

		// Set the new comment depending on which row has been selected.
		if(selectedRow == -1) {
			commentText.setText("");
		} else {
			final int row = tableCollectionData.convertRowIndexToModel(selectedRow);
			final int col = tableModelCollectionData.COMMENT_INDEX;
			commentText.setText(tableModelCollectionData.getValueAt(row, col).toString());
		}
		commentText.setCaretPosition(0);

		// Enable / Disable the comment area and button depending on whether or not
		// a collection has been selected.
		commentText.setEnabled(selectedRow != -1);
		saveCommentButton.setEnabled(selectedRow != -1);
	}

	/**
	 * Adds all author names to the author <code>ComboxBox</code>es.
	 */
	@SuppressWarnings("unchecked")
	protected void updateComboBoxAuthors() {

		// Update all needed ComoboBoxes of this view.
		super.updateComboBoxAuthors(comboBoxAuthors, selectionAuthor);

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
	 * Table model for the table showing the collection data.
	 */
	private class TableModelCollectionData extends AbstractTableModel
		implements ColumnVisibility
	{

		// The indices of the columns.
		final protected int COLLECTIONID_INDEX 	   = 0;
		final protected int AUTHORID_INDEX 		   = 1;
		final protected int COLLECTION_TITLE_INDEX = 2;
		@SuppressWarnings("unused")
		final protected int AUTHOR_NAME_INDEX      = 3;
		final protected int COMMENT_INDEX 		   = 4;
		final protected int LAST_CHANGED_INDEX     = 5;

		// The names of the columns
		private String[] columnNames = {"COLLECTIONID", "AUTHORID", Texts.getText("collection"),
				Texts.getText("author"), Texts.getText("comment"), Texts.getText("lastChanged")};

		// Determination of the editable fields.
		private boolean[] editable  = {false, false, true, true, true, false};

		// Determination of the visible columns.
		private boolean[] isVisible = {false, false, true, true, false, true};

		// The data of the table.
		protected ArrayList<Object[]> tableData = new ArrayList<Object[]>();


		public TableModelCollectionData() { }


		/**
		 * Loads the data of the collections specified by the passed
		 * search pattern into this table model.
		 */
		public final void refreshData() {

			// Get the string for the author selection (ComboBoxes needn't to have a selected item!)
			String author = selectionAuthor.getEditor().getItem().toString().replace('*', '%');

			// "Nothing" is treated as "display all".
			if(author.length() == 0) {
				author = "%";
			}
			author = author.toLowerCase().trim();

			// Collection to search for. The database is case sensitive.
			// However, the search should by case insensitive.
			String collectionTitle = selectionCollection.getText().length() == 0 ? "%" : selectionCollection.getText().replace('*', '%').toLowerCase().trim();
			String collectionComment = selectionComment.getText().length() == 0 ? "%" : selectionComment.getText().replace('*', '%').toLowerCase();

			// Get the collection data.
			String statement =
				"SELECT * 			   " +
				"from collectionData c " +
				"inner join authorData a on c.authorID = a.authorID " +
				"where LCASE(collectionTitle)   like ? and " +
				"      LCASE(collectionComment) like ? and " +
				"      LCASE(name)              like ?     " +
				"ORDER BY collectionTitle                  ";

			PreparedStatement p = database.prepareStatement(statement);

			try {
				int index = 1;
				p.clearParameters();
				p.setString(index++, collectionTitle);
				p.setString(index++, collectionComment);
				p.setString(index++, author);
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
							result.getInt("authorID"),
							result.getString("collectionTitle"),
							new ComboBoxEntry(result.getString("name"), result.getInt("authorID")),
							result.getString("collectionComment"),
							result.getTimestamp("lastChanged")
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

			// If the data hasn't changed return immediately.
			if(rowData[col].equals(value)) {
				return;
			}

			// Determine the new timestamp.
			Timestamp newTimestamp = new Timestamp(System.currentTimeMillis());

			// Set the new value and a new timestamp.
			rowData[col] = value;
			rowData[LAST_CHANGED_INDEX]   = newTimestamp;
			fireTableRowsUpdated(row, row);

			// Set the flag that a relevant change for the other views
			// has been done if the title has been changed.
			if(col == COLLECTION_TITLE_INDEX) {
				DatabaseChangesInViews.collectionNamesChanged();
			}

			// If a new author has been set update the authorID
			if(value instanceof ComboBoxEntry) {
				rowData[AUTHORID_INDEX] = Integer.valueOf(((ComboBoxEntry) value).getID());
			}

			// Extract the data from the table row.
			String title     = (String) rowData[COLLECTION_TITLE_INDEX];
			int authorID     = (Integer) rowData[1];
			String comment   = (String) rowData[COLLECTION_TITLE_INDEX];
			int collectionID = (Integer) rowData[COLLECTIONID_INDEX];

			database.updateCollectionData(collectionID, title, authorID, comment, newTimestamp);

			// If the name of a collection has changed
			// the corresponding ComboBox has to be updated.
			if(columnNames[col].equals(Texts.getText("collection"))) {
				updateComboBoxCollections(comboBoxCollections);
			}
		}
	}
}