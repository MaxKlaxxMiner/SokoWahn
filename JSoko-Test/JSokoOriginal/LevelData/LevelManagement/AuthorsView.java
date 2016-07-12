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
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextField;
import javax.swing.table.AbstractTableModel;
import javax.swing.table.TableRowSorter;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.ColumnVisibility;
import de.sokoban_online.jsoko.gui.DateTimeRenderer;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.leveldata.Author;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseGUI.DatabaseChangesInViews;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * This class displays the "AuthorsView" in the level management dialog
 * and handles all actions in this view.
 * This Panel contains all elements of the "AuthorsView" and is displayed
 * in a tabbed pane in the level management.
 */
@SuppressWarnings("serial")
public class AuthorsView extends LevelManagementView {

	// There are several data that can be filtered. For every data element there is an own selection field.
	protected JTextField selectionAuthor;
	protected JTextField selectionComment;

	/**
	 * The ComboBox holding all author names.
	 * It is used for selecting an author in a table cell.
	 */
	protected JComboBox comboBoxAuthors = new JComboBox();

	// The table model and the table for showing the author data.
	private JTable tableAuthorData;
	private TableModelAuthorData tableModelAuthorData;


	/**
	 * Creates the <code>JPanel</code> for displaying the data of the authors
	 * in the <code>LevelManagement</code>.
	 *
	 * @param database the database of the program
	 * @param levelManagementDialog the <code>JDialog</code> all views are displayed in
	 * @param application Reference to the main object which holds all references
	 */
	public AuthorsView(Database database, DatabaseGUI levelManagementDialog, JSoko application) {

		this.database = database;
		this.levelManagementDialog = levelManagementDialog;
		this.application = application;

		// Create all things this panel needs.
		createPanel();

		// Initial refresh.
		refreshView();

		// Set the help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "database.authors-tab", null);
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

		// For better optic a filler above.
		constraints.gridy++;
		selectionPanel.add(new JLabel(" "), constraints);


		// Author
		constraints.gridy++;
		constraints.gridx=0;
		selectionPanel.add(new JLabel(Texts.getText("author")+":"), constraints);
		constraints.gridx++;
		selectionAuthor = new JTextField("*", widthOfSelectionFields);
		selectionAuthor.addActionListener(this);
		selectionAuthor.setActionCommand("refreshView");
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

		// Button for inserting an author.
		JButton insertButton = new JButton(Texts.getText("insertDataset"));
		insertButton.setActionCommand("insertView");
		insertButton.addActionListener(this);
		constraints.gridx=4;
		constraints.gridy = 1;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.HORIZONTAL;
		selectionPanel.add(insertButton, constraints);

		// Button for deleting an author.
		JButton deleteButton = new JButton(Texts.getText("deleteAuthor"));
		deleteButton.setToolTipText(Texts.getText("deleteAuthorTooltip"));
		deleteButton.setActionCommand("deleteAuthor");
		deleteButton.addActionListener(this);
		constraints.gridy++;
		selectionPanel.add(deleteButton, constraints);

		// For better optic and the correct alignment add a filler below.
		constraints.gridx=3;
		constraints.gridy++;
		constraints.weightx = 1;
		selectionPanel.add(new JLabel(" "), constraints);


		// Add the selection panel to the author view panel.
		add(selectionPanel, BorderLayout.NORTH);


		// Create a sortable table for showing the author data.
		tableModelAuthorData = new TableModelAuthorData();
		tableAuthorData = new JTable(tableModelAuthorData);

		// Set a sorter for the table.
		tableAuthorData.setRowSorter(new TableRowSorter<TableModelAuthorData>(tableModelAuthorData));

		// Prepare correct conversion of Date values
		new DateTimeRenderer().enterMeForTypeDate(tableAuthorData);

		// Edits should automatically be finished when cells loose the focus.
		tableAuthorData.putClientProperty("terminateEditOnFocusLost", Boolean.TRUE);

		// Add a mouseListener to the headers.
		tableAuthorData.getTableHeader().addMouseListener(this);
		tableAuthorData.addMouseListener(this);

		// Hide all columns that are not to be displayed
		Utilities.tableHideInvisibleColumns(tableAuthorData, tableModelAuthorData);

		// Add the scroll pane containing the JTable showing the author data to the main panel.
		add(new JScrollPane(tableAuthorData), BorderLayout.CENTER);
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
			tableModelAuthorData.refreshData();
		}

		// Insert a new author to the database.
		if(actionCommand.startsWith("insert")) {

			Author newAuthor = new Author();
			int authorID = database.insertAuthor(newAuthor);

			// Add the author data to the data of this model.
			Object[] dataRow = {authorID,
					newAuthor.getName(),
					newAuthor.getEmail(),
					newAuthor.getWebsiteURL(),
					newAuthor.getComment(),
					new Timestamp(System.currentTimeMillis())
			};

			// Add the author data to the table data.
			tableModelAuthorData.tableData.add(0, dataRow);

			// There is a new author, hence the author ComboBox has to be updated.
			updateComboBoxAuthors(comboBoxAuthors);

			// Notify the model that the data have been changed.
			tableModelAuthorData.fireTableRowsInserted(tableModelAuthorData.getRowCount()-1, tableModelAuthorData.getRowCount()-1);

			// Set the flag that a relevant change for the other views has been done.
			DatabaseChangesInViews.authorsNamesChanged();

		}


		// Delete the selected row from the table and delete the author from the database.
		if(actionCommand.startsWith("delete")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = tableAuthorData.getSelectedRows();
			for (int i = 0; i < selectedRowsNumbers.length; i++) {
				selectedRowsNumbers[i] = tableAuthorData.convertRowIndexToModel(selectedRowsNumbers[i]);
			}

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(this, Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			ArrayList<Integer> deletedAuthorIDs = new ArrayList<Integer>();

			// Delete all selected rows.
			for(int rowCount=selectedRowsNumbers.length; --rowCount>=0;) {

				// Get the authorID.
				int authorID = (Integer) tableModelAuthorData.getValueAt(selectedRowsNumbers[rowCount], tableModelAuthorData.AUTHORID_INDEX);

				// The author must not be deleted when the author is still assigned to a level or collection.
				try {
					ResultSet result = database.query("SELECT levelTitle FROM levelData WHERE authorID = "+authorID);
					if(result.next()) {
						MessageDialogs.showFailureString(this, Texts.getText("can'tDeleteAuthorLevel")
								                         +result.getString("levelTitle") );
						continue;
					}
					result = database.query("SELECT collectionTitle FROM collectionData WHERE authorID = "+authorID);
					if(result.next()) {
						MessageDialogs.showFailureString(this, Texts.getText("can'tDeleteAuthorCollection")
								                         +result.getString("collectionTitle") );
						continue;
					}

				}catch(SQLException exception) {
					exception.printStackTrace();
				}

				// Delete the author from the database.
				database.deleteAuthor(authorID);

				deletedAuthorIDs.add(authorID);

				// Delete the author from the tableModel.
				tableModelAuthorData.tableData.remove(selectedRowsNumbers[rowCount]);
			}

			if(!deletedAuthorIDs.isEmpty()) {
				// Set the flag that a relevant change for the other views has been done.
				DatabaseChangesInViews.authorsNamesChanged();

				// An author has been deleted, hence the author ComboBox has to be updated.
				updateComboBoxAuthors(comboBoxAuthors);

				// Notify the model that the data have been changed.
				tableModelAuthorData.fireTableDataChanged();
			}
		}
	}

	/**
	 * Table model for the table showing the author data.
	 */
	private class TableModelAuthorData extends AbstractTableModel
		implements ColumnVisibility
	{

		// The indices of the columns.
		final protected int AUTHORID_INDEX 	       = 0;
		final protected int NAME_INDEX 		       = 1;
		final protected int EMAIL_INDEX 		   = 2;
		final protected int HOMEPAGE_INDEX 	 	   = 3;
		final protected int COMMENT_INDEX 		   = 4;
		final protected int LAST_CHANGED_INDEX     = 5;

		// The names of the columns. "AUTHORID" is a hidden column which is just for internal use.
		private String[] columnNames = {"AUTHORID", Texts.getText("name"), Texts.getText("email"),
				Texts.getText("homepage"), Texts.getText("comment"), Texts.getText("lastChanged")};

		// Determination of the editable fields.
		private boolean[] editable = {false, true, true, true, true, false};

		// Determination of the visible columns.
		private boolean[] isVisible = {false, true, true, true, true, true};

		// The data of the table.
		protected ArrayList<Object[]> tableData = new ArrayList<Object[]>();


		/**
		 * Creates a table model for the author data table.
		 */
		public TableModelAuthorData() {}


		/**
		 * Loads the data of the authors specified by the passed search pattern into
		 * this table model.
		 */
		public final void refreshData() {

			// Get the author data.
			String statement = "SELECT *		 		 	          " +
							   "from authorData 		              " +
							   "where LCASE(name)          like ? and " +
							   " 	  LCASE(authorComment) like ?	  " +
							   "ORDER BY name";

			PreparedStatement p = database.prepareStatement(statement);

			try {
				int parameterIndex = 1;
				p.clearParameters();
				p.setString(parameterIndex++, selectionAuthor .getText().replace('*', '%').toLowerCase());
				p.setString(parameterIndex++, selectionComment.getText().replace('*', '%').toLowerCase());
				ResultSet result = p.executeQuery();

				// Immediately return if an error occurred.
				if(result == null) {
					return;
				}

				// Delete the old data.
				tableData.clear();

				// Add the author data to the data of this model.
				while(result.next()) {
					Object[] dataRow = {result.getInt("authorID"),
										result.getString("name"),
										result.getString("email"),
										result.getString("homepage"),
										result.getString("authorComment"),
										result.getTimestamp("lastChanged") };
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

			// Determine the new timestamp.
			Timestamp newTimestamp = new Timestamp(System.currentTimeMillis());

			// Set the new value and a new timestamp.
			rowData[col] = value;
			rowData[LAST_CHANGED_INDEX]  = newTimestamp;
			fireTableRowsUpdated(row, row);

			// If the name has changed, set the flag that a relevant change for the other views has been done.
			if(col == NAME_INDEX) {
				DatabaseChangesInViews.authorsNamesChanged();
			}

			int authorID = ((Integer) rowData[AUTHORID_INDEX]).intValue();
			String name = (String) rowData[NAME_INDEX];
			String email = (String) rowData[EMAIL_INDEX];
			String homepage = (String) rowData[HOMEPAGE_INDEX];
			String comment = (String) rowData[HOMEPAGE_INDEX];

			database.updateAuthor(authorID, name, email, homepage, comment, newTimestamp);

			// If the name of an author has changed the corresponding ComboBox has to be updated.
			if(columnNames[col].equals(Texts.getText("name"))) {
				updateComboBoxAuthors(comboBoxAuthors);
			}
		}
	}
}
