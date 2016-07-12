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

import java.awt.Component;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.util.List;

import javax.swing.JComboBox;
import javax.swing.JPanel;
import javax.swing.JTable;
import javax.swing.table.TableCellRenderer;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.leveldata.Author;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * The "LevelManagementDialog" contains several Views on the data.
 * This class is the super class of all these views.
 */
@SuppressWarnings("serial")
public abstract class LevelManagementView extends JPanel implements ActionListener, MouseListener {

	/** Maximal length of combo box entry texts. */
	protected final int maximumLengthComboBoxTexts = 40;

	/** Width of all the fields for a selection. */
	protected final int widthOfSelectionFields = 20;

	/** Reference to the database of the program. */
	protected Database database;

	/** Reference to the main object which holds all references. */
    protected JSoko application;

	/** Dialog the views are added to. */
    protected DatabaseGUI levelManagementDialog;

    /**
     * Indicates whether actions are handled or not. If the content of combo
     * boxes is updated this flag is set to "true" to avoid refreshing
     * the views at that time.
	 */
    protected volatile boolean doNotFireActions = false;


	/**
     * This method resizes the width of one column to an "optimal" value.
     * For this we set its preferred width to the maximum over all cells
     * of this column (including the header).
     *
     * @param table	the table which columns are to be resized
     * @param columnNo the number of the column to resize
     */
    protected void optimizeColumnWidths(JTable table, int columnNo) {

        // The maximum width of the column over all rows.
        int maxWidth = 0;
        int borderWidth = table.getColumnModel().getColumnMargin();

        // Check if the header is the largest row.
        TableCellRenderer renderer = table.getColumnModel().getColumn(columnNo).getHeaderRenderer();
        if(renderer == null) {
            renderer = table.getTableHeader().getDefaultRenderer();
        }
        Component c = renderer.getTableCellRendererComponent(table, table.getColumnName(columnNo), false, false, -1, columnNo);
        maxWidth = c.getPreferredSize().width;

        // Check if another row is larger.
        for(int row = 0; row < table.getRowCount(); row++) {
            renderer = table.getCellRenderer(row, columnNo);
            c = renderer.getTableCellRendererComponent(table, table.getValueAt(row, columnNo), false, false, row, columnNo);
            maxWidth = Math.max(c.getPreferredSize().width + borderWidth, maxWidth);
        }

        // Set the new preferred width.
        table.getColumnModel().getColumn(columnNo).setPreferredWidth(maxWidth);
    }

	/**
	 * Reload the data of this view from the database.
	 */
	protected void refreshView() {
		actionPerformed(new ActionEvent(this, 0, "refreshView"));
	}

	/**
	 * Adds all author names from the database to the provided <code>ComboBox</code>es.
	 */
	protected void updateComboBoxAuthors(JComboBox... comboBoxes) {

		// Select all author names.
		List<Author> authors = database.getAllAuthors();

		// When adding new items the combo boxes should not fire actions.
		// (If this flag isn't set the combo boxes refresh the views
		//  every time their content changes)
		doNotFireActions = true;

		// Delete the current authors from the ComboBoxes.
		for(JComboBox comboBox : comboBoxes) {
			comboBox.removeAllItems();
		}

		for(Author author : authors) {

			// Too long names result in a bad optic in the screen layout.
			String authorName = Utilities.clipToEllipsis(author.getName(), maximumLengthComboBoxTexts);
			int authorID      = author.getDatabaseID();
			ComboBoxEntry entry = new ComboBoxEntry(authorName, authorID);

			// Add entry to all ComboBoxes.
			for(JComboBox comboBox : comboBoxes) {
				comboBox.addItem(entry);
			}
		}

		// Actions may be fired again.
		doNotFireActions = false;
	}

	/**
	 * Adds all collection names to the provided <code>ComboBox</code>es.
	 */
	protected void updateComboBoxCollections(JComboBox... comboBoxes) {

		// Select all collection names.
		List<LevelCollection> collectionInfos = database.getCollectionsInfo();

		// When adding new items the combo boxes should not fire actions.
		doNotFireActions = true;

		// Delete the current collections from the ComboBoxes.
		for(JComboBox comboBox : comboBoxes) {
			comboBox.removeAllItems();
		}

		for(LevelCollection collectionInfo : collectionInfos) {

			// Too long titles result in a bad optic in the screen layout.
			String collectionTitle = Utilities.clipToEllipsis(collectionInfo.getTitle(), maximumLengthComboBoxTexts);
			int collectionID = collectionInfo.getDatabaseID();

			ComboBoxEntry entry = new ComboBoxEntry( collectionTitle, collectionID);

			// Add entry to all ComboBoxes.
			for(JComboBox comboBox : comboBoxes) {
				comboBox.addItem(entry);
			}
		}

		// Actions may be fired again.
		doNotFireActions = false;
	}

	/* (non-Javadoc)
	 * @see java.awt.event.MouseListener#mouseClicked(java.awt.event.MouseEvent)
	 */
	@Override
	public void mouseClicked(MouseEvent e) {}
	@Override
	public void mouseEntered(MouseEvent e) {}
	@Override
	public void mouseExited(MouseEvent e) {}
	@Override
	public void mousePressed(MouseEvent e) {}
	@Override
	public void mouseReleased(MouseEvent e) {}


	/**
	 * Class whose objects are stored in a <code>JComboBox</code>.
	 * A string and an ID from the database is stored.
	 * This makes it possible to store two authors with the same name.
	 */
	protected class ComboBoxEntry {

		/**
		 * The string to be shown in the combobox.
		 * Must not be {@code null}.
		 */
		protected String string;
		/**
		 * The database ID referencing the database entry,
		 * which is represented (named) by the {@link #string}.
		 */
		protected int ID;

		ComboBoxEntry(String string, int ID) {
			this.string = string;
			this.ID 	= ID;
		}

		/* (non-Javadoc)
		 * @see java.lang.Object#toString()
		 */
		@Override
		public String toString() {
			return string;
		}

		/**
		 * Return the ID of the stored string.
		 *
		 * @return the ID
		 */
		public int getID() {
			return ID;
		}

		/* (non-Javadoc)
		 * @see java.lang.Object#clone()
		 */
		@Override
		protected Object clone() throws CloneNotSupportedException {
			return new ComboBoxEntry(string, ID);
		}

		/* (non-Javadoc)
		 * @see java.lang.Object#equals(java.lang.Object)
		 */
		@Override
		public boolean equals(Object compareObject) {
			return compareObject != null
				&& getClass() == compareObject.getClass()
				&& string.equals(compareObject.toString())
				&& ((ComboBoxEntry) compareObject).ID == ID;
		}

		@Override
		public int hashCode() {
			return ID + string.hashCode();
		}
	}
}