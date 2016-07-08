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
package de.sokoban_online.jsoko.leveldata.solutions;

import java.awt.BorderLayout;
import java.awt.Component;
import java.awt.Dialog.ModalityType;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.Window;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.ClipboardOwner;
import java.awt.datatransfer.Transferable;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.sql.Timestamp;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JTable;
import javax.swing.JTextArea;
import javax.swing.ListSelectionModel;
import javax.swing.event.EventListenerList;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.table.AbstractTableModel;
import javax.swing.table.JTableHeader;
import javax.swing.table.TableCellRenderer;
import javax.swing.table.TableRowSorter;

import de.sokoban_online.jsoko.gui.BoardDisplay;
import de.sokoban_online.jsoko.gui.ColumnVisibility;
import de.sokoban_online.jsoko.gui.DateTimeRenderer;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.IntStack;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.JScrollPaneOSSpecific;



/**
 * This class manages all solutions of a level.
 * A solution is unique regarding its lurd representation.
 * If another identical solution occurs it is not saved to avoid conflicts
 * due to duplicate solutions.
 *
 * Here we also implement the dialog for the DB interface.
 */
public final class SolutionsManager
	implements ActionListener, MouseListener, ListSelectionListener, ClipboardOwner {
	/**
	 * Types of solutions.
	 * This enum represents the different types of a solution, as they occur
	 * from an attempt to add a new solution.  It is mainly used to decide
	 * for the appropriate user feedback.
	 */
	public enum SolutionType {
		/** Constant for: "invalid solution" */
		INVALID_SOLUTION,

		/** Constant for: "duplicate solution" */
		DUPLICATE_SOLUTION,

		/** Constant for: "new best pushes solution found" */
		NEW_BEST_PUSHES_SOLUTION,

		/** Constant for: "new best moves solution found" */
		NEW_BEST_MOVES_SOLUTION,

		/** Constant for: "new best moves and new best pushes solution" */
		NEW_BEST_SOLUTION,

		/** Constant for: "new solution is first solution" */
		NEW_FIRST_SOLUTION,

		/** Constant for: "new solution" */
		NEW_SOLUTION;

		/**
		 * Returns whether the the current solution type is a valid
		 * and new solution.
		 * @return <code>true</code> if the solution is a valid new solution,
		 *   or<br><code>false</code> otherwise
		 */
		public boolean isValidNewSolution() {
			return this != INVALID_SOLUTION && this != DUPLICATE_SOLUTION;
		}
	}

	/** JDialog for displaying and editing the solutions of this class. */
	private JDialog solutionDialog;

	/** Direct reference to the database for easier access. */
	protected Database database;

    /** ArrayList holding all solutions of a level. */
    protected ArrayList<Solution> solutions;

    /** The level these solutions belong to. */
    protected Level level;

    /** Current best pushes solution. */
    private Solution currentBestPushesSolution = null;
    /** Current best moves solution. */
    private Solution currentBestMovesSolution  = null;

    /** JLabel for displaying the name of the level. */
    private JLabel levelName;

    /**
     * The table used for displaying (some columns) of the solutions
     */
    JTable tableSolutionData;
    /**
     * The table model containing the raw data of the solutions to be shown
     */
    TableModelSolutionData tableModelSolutionData;

    /** Textarea where the LURD-String of a solution is displayed. */
    JTextArea lurdString;

    // Textarea where the comment of a solution is displayed and the button for saving the comment.
    JTextArea solutionComment;
    JButton saveCommentButton;

    // The board of the level this Solutions object belongs to.
    private InitialBoard initialBoard;

    /** A list of event listeners for this component. */
    private EventListenerList listenerList = new EventListenerList();


	/**
	 * Creates a new object for managing solutions.
	 *
	 * @param level the data of the level this objects belongs to
	 */
	public SolutionsManager(Database database, Level level) {

		this.database = database;

		// Create an ArrayList for storing the solution information.
		solutions = new ArrayList<Solution>();

		// Set the boardData of the level.
		this.level = level;
	}


	/**
	 * Displays the solutions in a <code>JDialog</code>.
	 *
	 * @param offerSolAsHistory  indicating whether the "take solution as history" button
	 * 							 is to be displayed
	 * @param parentWindow  the window this dialog is to be shown in
	 */
	public final void displaySolutionsInDialog(Window parentWindow) {

		// Create the modal JDialog for displaying the solutions and set the location and size of the application.
		solutionDialog = new JDialog(parentWindow, Texts.getText("theSokobanGame")+": "+Texts.getText("solutions"), ModalityType.APPLICATION_MODAL);
		solutionDialog.setBounds(parentWindow.getBounds());
		Utilities.setEscapable(solutionDialog);

		// The panel which will contain all elements of the solution view.
		JPanel mainPanel = new JPanel(new BorderLayout());

        // Create a sortable table for showing the solution data.
        tableModelSolutionData = new TableModelSolutionData();
        tableSolutionData = new JTable(tableModelSolutionData);
        tableSolutionData.setShowGrid(false);
		tableSolutionData.setOpaque(true);

		// Set a row sorter.
		tableSolutionData.setRowSorter(new TableRowSorter<TableModelSolutionData>(tableModelSolutionData));

        // Prepare correct rendering of model data of type Date/Timestamp
        new DateTimeRenderer().enterMeForTypeDate(tableSolutionData);

		// Edits should automatically be finished when cells loose the focus.
        tableSolutionData.putClientProperty("terminateEditOnFocusLost", Boolean.TRUE);

		// Add a mouseListener to the headers.
		tableSolutionData.getTableHeader().addMouseListener(this);
        tableSolutionData.addMouseListener(this);
        tableSolutionData.getSelectionModel().addListSelectionListener(this);

		// Hide all columns that are not to be displayed
        Utilities.tableHideInvisibleColumns(tableSolutionData, tableModelSolutionData);

		/*
		 * Add the buttons for the actions and the label for the level title.
		 */
		// Create and add a panel for the buttons and level title.
		JPanel northPanel = new JPanel(new GridBagLayout());
		mainPanel.add(northPanel, BorderLayout.NORTH);

		// Create constraints for setting the constraints for every object.
		GridBagConstraints constraints = new GridBagConstraints();
        constraints.insets = new Insets(2, 2, 2, 2);
        constraints.anchor = GridBagConstraints.LINE_START;

        // Add a filler so the first line is blank.
		constraints.gridx = 0;
		constraints.gridy = 0;
		northPanel.add(new JLabel(" "), constraints);

        // JLabel for displaying the title of the level.
        levelName = new JLabel(Texts.getText("level"));
		constraints.gridy++;
		northPanel.add(levelName, constraints);

		// Add a Panel showing the level.
		constraints.gridx++;
		constraints.gridy=0;
		constraints.weightx = 1;
		constraints.anchor = GridBagConstraints.CENTER;
		constraints.fill   = GridBagConstraints.BOTH;
		constraints.gridheight = 5;
		northPanel.add(new BoardDisplay(level), constraints);

		// Import solution button
		JButton addButton = new JButton(Texts.getText("newSolutionFromClipboard"));
		addButton.setToolTipText(Texts.getText("newSolutionTooltip"));
		addButton.setActionCommand("newSolutionFromClipboard");
		addButton.addActionListener(this);
		constraints.anchor = GridBagConstraints.LINE_END;
		constraints.fill   = GridBagConstraints.HORIZONTAL;
		constraints.gridx++;
		constraints.gridy = 1;
		constraints.gridheight = 1;
		constraints.weightx = 0;
		northPanel.add(addButton, constraints);

        // Delete button
		JButton deleteButton = new JButton(Texts.getText("deleteSolution"));
		deleteButton.setToolTipText(Texts.getText("deleteSolutionTooltip"));
		deleteButton.setActionCommand("deleteSolution");
		deleteButton.addActionListener(this);
		constraints.gridy++;
		northPanel.add(deleteButton, constraints);

		// Separator
		constraints.gridy++;
		northPanel.add(new JLabel(" "), constraints);


		// Add the scrollpane containing the JTable showing the solution data to the main panel.
		mainPanel.add(JScrollPaneOSSpecific.getJScrollPane(tableSolutionData), BorderLayout.CENTER);

		// In the south of the view there is this panel for showing additional information
		// about the solution.
		JPanel southPanel = new JPanel(new GridBagLayout());
		mainPanel.add(southPanel, BorderLayout.SOUTH);

		// Add objects for displaying the LURD string of a solution.
		constraints.gridx = 0;
		constraints.gridy = 0;
		constraints.fill = GridBagConstraints.NONE;
		constraints.anchor = GridBagConstraints.LINE_START;
		southPanel.add(new JLabel(" "), constraints); // separator

		constraints.gridy++;
		southPanel.add(new JLabel("LURD String:"), constraints);
		constraints.gridx++;
		constraints.gridheight = 4;
		constraints.weightx = 1;
		constraints.fill = GridBagConstraints.BOTH;
		lurdString = new JTextArea(2, 1);
		lurdString.setEditable(false);
		lurdString.setLineWrap(true);
		southPanel.add(JScrollPaneOSSpecific.getJScrollPane(lurdString), constraints);

		// The button for copying the lurd string to the clipboard.
		constraints.gridx=0;
		constraints.gridy++;
		constraints.gridheight = 1;
		constraints.weightx = 0;
		constraints.fill = GridBagConstraints.NONE;
		constraints.anchor = GridBagConstraints.LINE_END;
		JButton exportToClipboard = new JButton(Utilities.getIcon("edit-copy.png", null));
		exportToClipboard.setToolTipText(Texts.getText("exportLurdToClipboard"));
		exportToClipboard.setActionCommand("exportLurdToClipboard");
		exportToClipboard.addActionListener(this);
		exportToClipboard.setPreferredSize(new Dimension(18, 18));
		southPanel.add(exportToClipboard, constraints);

		constraints.gridx=0;
		constraints.gridy+=4;
		constraints.anchor = GridBagConstraints.LINE_START;
		southPanel.add(new JLabel(" "), constraints); // separator

		// A textfield for displaying the comment of a solution.
		constraints.gridy++;
		southPanel.add(new JLabel(Texts.getText("comment")+":"), constraints);
		constraints.gridx++;
		constraints.gridheight = 4;
		constraints.weightx = 1;
		constraints.fill = GridBagConstraints.BOTH;
		solutionComment = new JTextArea(5, 1);
		solutionComment.setLineWrap(true);
		solutionComment.setEnabled(false);
		southPanel.add(JScrollPaneOSSpecific.getJScrollPane(solutionComment), constraints);

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

		// A comment can only be there when the level is stored in the database.
		if(level.isConnectedWithDB() == false) {
			solutionComment.setEnabled(false);
			saveCommentButton.setEnabled(false);
		}

		// Add the panel for displaying the solution information.
		solutionDialog.getContentPane().add(mainPanel);

		// Load the solution data.
		tableModelSolutionData.refreshData();

		// Set the first solution as selected
//		if(tableModelSolutionData.getRowCount() > 0) {
//			tableSolutionData.getSelectionModel().setSelectionInterval(0, 0);
//		}
		setSelectedViewRow(0);

		// Display this JDialog.
		solutionDialog.setVisible(true);
		solutionDialog.toFront();
	}

	/**
	 * Makes a single row "selected".
	 * When the passed view row index is negative (e.g. as result of a
	 * failed conversion) the current selection remains unchanged.
	 *
	 * @param viewrowindex the view index of the row to be selected
	 */
	private void setSelectedViewRow(int viewrowindex) {
		if (       (viewrowindex >= 0)
				&& (tableModelSolutionData != null)
				&& (tableModelSolutionData.getRowCount() > 0)
				&& (tableSolutionData != null)) {
			ListSelectionModel lsm = tableSolutionData.getSelectionModel();
			lsm.setSelectionInterval(viewrowindex, viewrowindex);
		}
	}

	/* (non-Javadoc)
	 * @see java.awt.event.ActionListener#actionPerformed(java.awt.event.ActionEvent)
	 */
	@Override
	public void actionPerformed(ActionEvent e) {

		// Get the action string.
		String actionCommand = e.getActionCommand();

		// Save the comment
		if(actionCommand.startsWith("saveComment")) {

			// Immediately return if no row is selected.
			if(tableSolutionData.getSelectedRow() == -1) {
				return;
			}

			// Set the new comment.
			tableModelSolutionData.setValueAt( solutionComment.getText().intern(),
					                           tableSolutionData.getSelectedRow(),
					                           TableModelSolutionData.COMMENT_INDEX );
			return;
		}

		// Import a solution from the clipboard.
		if(actionCommand.startsWith("newSolutionFromClipboard")) {
			importSolutionFromClipboard();
			// ... go on ...
		}

		// Delete the selected solutions from the table.
		if(actionCommand.startsWith("delete")) {

			// Get the numbers of the selected rows.
			int[] selectedRowsNumbers = tableSolutionData.getSelectedRows();

			// Return if no row is selected.
			if(selectedRowsNumbers.length == 0) {
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(solutionDialog, Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			// Delete all selected rows.
			for(int rowCount=selectedRowsNumbers.length; --rowCount>=0; ) {

				// Delete the solution from the main vector holding all solutions.
				Solution solution = (Solution) tableModelSolutionData.getValueAt(selectedRowsNumbers[rowCount], TableModelSolutionData.SOLUTION_INDEX);
				deleteSolution(solution);

				// Delete the solution from the tableModel
				tableModelSolutionData.tableData.remove(selectedRowsNumbers[rowCount]);
			}

			// Fire event to refresh the display.
			tableModelSolutionData.fireTableDataChanged();

			return;
		}

		// Export solution lurd to the clipboard.
		if(actionCommand.equals("exportLurdToClipboard")) {
			Utilities.putStringToClipboard(lurdString.getText(), this);
		}
	}


	/**
	 * This methods sets the information about a solution in some extra fields.
	 *
	 * @param evt the event indicating that a value has changed
	 */
	@Override
	public void valueChanged(ListSelectionEvent evt) {

		// Ignore adjusting events (there are more events to come).
		if (evt.getValueIsAdjusting()) {
			return;
		}

		ListSelectionModel lsm = (ListSelectionModel) evt.getSource();

		// If no row is selected delete all additional information displays.
		if (lsm.isSelectionEmpty()) {

			// Disable the comment text area and the save button.
			saveCommentButton.setEnabled(false);
			solutionComment.setEnabled(false);

			solutionComment.setText("");
			lurdString.setText("");
			return;
		}

		// Get the number of the first selected row.
		int selectedRow = tableSolutionData.convertRowIndexToModel(lsm.getMinSelectionIndex());

		// Set the comment.
		solutionComment.setText(tableModelSolutionData.getValueAt(selectedRow, TableModelSolutionData.COMMENT_INDEX).toString());
		solutionComment.setCaretPosition(0);

		// Set the solution LURD.
		lurdString.setText(tableModelSolutionData.getValueAt(selectedRow, TableModelSolutionData.LURD_INDEX).toString());
		lurdString.setCaretPosition(0);

		// Enable the comment text area and the save button. This is only done
		// if the level has an ID so the comment can be saved in the database.
		if(level.isConnectedWithDB()){
			saveCommentButton.setEnabled(true);
			solutionComment.setEnabled(true);
		}
	}

	/**
	 * Imports a solution from the clipboard.
	 *
	 * @return one of: DUPLICATE_SOLUTION, INVALID_SOLUTION, or 0 or any of
	 * 				   NEW_BEST_PUSHES_SOLUTION, NEW_BEST_MOVES_SOLUTION,
	 * 				   NEW_FIRST_SOLUTION.
	 */
	public SolutionType importSolutionFromClipboard() {

		// The imported solution.
		String newSolutionLURD = Utilities.getStringFromClipboard();

		if(newSolutionLURD == null) {
			MessageDialogs.showFailureTextKey(solutionDialog, "noValidSolutionFound");
			return SolutionType.INVALID_SOLUTION;
		}

		// Create a new solution object.
		Solution solution = new Solution(newSolutionLURD);

		// Set the name of the solution.
		solution.name = Texts.getText("solution.importedFromClipboard");

		// Add the solution (to this object and store it on the database)
		SolutionType solutionType = addSolution(solution);
		// We leave in the NEW_FIRST_SOLUTION bit, effectively suppressing
		// the congratulations for the first solution.

		switch(solutionType) {
			case DUPLICATE_SOLUTION:
				MessageDialogs.showInfoNoteString(solutionDialog, Texts.getText("solutionIsADuplicate")+".");
				break;
			case INVALID_SOLUTION:
				MessageDialogs.showFailureTextKey(solutionDialog, "noValidSolutionFound");
				break;
			case NEW_BEST_PUSHES_SOLUTION:
				MessageDialogs.showCongratsTextKey(solutionDialog, "newBestPushesSolution");
				break;
			case NEW_BEST_MOVES_SOLUTION:
				MessageDialogs.showCongratsTextKey(solutionDialog, "newBestMovesSolution");
				break;
			case NEW_BEST_SOLUTION:
				MessageDialogs.showCongratsTextKey(solutionDialog, "newBestSolution");
				break;
			case NEW_FIRST_SOLUTION:
				break;
			case NEW_SOLUTION:
				break;
		}

		// Add the new solution to the table model if it is displayed
		// and it is a new valid solution.
		if(solutionDialog != null && solution.lurd.length() != 0 && solutionType.isValidNewSolution()) {
			int modelrowindex = tableModelSolutionData.addSolution(solution);

			// "select" the new solution in the table, e.g. to add a comment
			int viewrowindex = tableSolutionData.convertRowIndexToView(modelrowindex);
			setSelectedViewRow(viewrowindex);
		}

		return solutionType;
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

				// Determine on which side of the separator the user has clicked
				// and determine the corresponding column index.
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


	@Override
	public void mouseEntered(MouseEvent e) {}
	@Override
	public void mouseExited(MouseEvent e) {}
	@Override
	public void mousePressed(MouseEvent e) {}
	@Override
	public void mouseReleased(MouseEvent e) {}


	/**
	 * This method resizes the width of a column to an "optimal" value.
	 *
	 * @param table	the table which columns are to be resized
	 * @param columnNo the number of the column to resize
	 */
	protected final void optimizeColumnWidths(JTable table, int columnNo) {

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
	 * Returns the the best moves solution.
	 *
	 * @return LURD-String of the moves solution
	 */
	public final Solution getBestMovesSolution() {

		Solution bestSolution = null;

		for(Solution solution : solutions) {
			if(solution.isBetterMovesSolutionThan(bestSolution)) {
				bestSolution = solution;
			}
		}

		return bestSolution;
	}


	/**
	 * Returns the best pushes solution.
	 *
	 * @return LURD-String of the pushes solution or null, if there is no solution
	 */
	public final Solution getBestPushesSolution() {

		Solution bestSolution = null;

		for(Solution solution : solutions) {
			if(solution.isBetterPushesSolutionThan(bestSolution)) {
				bestSolution = solution;
			}
		}

		return bestSolution;
	}


	/**
	 * Adds a solution. The passed solution is verified and all metrics are calculated.
	 * An already set {@code lastChanged} data will survive, a {@code null}
	 * will be replaced by "now".
	 * <p>
	 * This method is synchronized because the optimizer or solver
	 * may add a solution at the same time the user does.<br>
	 * If the level is stored in the database the new solution is
	 * saved in the database, too.
	 *
	 * @param solution solution to be added
	 * @return  information about the added solution. See: {@link SolutionType}.
	 */
	synchronized public SolutionType addSolution(Solution solution) {

		// Check whether there is already any solution stored.
		boolean isASolutionStored = (solutions == null) || solutions.isEmpty();

		// Check whether the passed solution is null.
		if(solution == null) {
			if(Debug.isDebugModeActivated) {
				System.out.println("null passed as solution");
			}
			return SolutionType.INVALID_SOLUTION;
		}

		// Verify solution and calculate the metrics.
		if(solution.isSolutionVerified == false) {
			verifySolution(solution);
		}

		// If no valid solution has been found return with an error code.
		if(solution.lurd.isEmpty()) {
			if(Debug.isDebugModeActivated) {
				System.out.println("Invalid solution found");
			}
			return SolutionType.INVALID_SOLUTION;
		}

		// Check if the solution is already stored.
		if(solutions.contains(solution)) {
			return SolutionType.DUPLICATE_SOLUTION;
		}

		// Add the new solution to the solution array.
		solution.chkSetLastChangedToNow();
		solutions.add(solution);

		// The initial return code is "NEW_SOLUTION" = "everything is ok, just a normal new solution".
		SolutionType solutionType = SolutionType.NEW_SOLUTION;

		// Check whether the new solution is a new best pushes solution.
		if(solution.isBetterPushesSolutionThan(currentBestPushesSolution)) {
			currentBestPushesSolution = solution;
			solutionType = SolutionType.NEW_BEST_PUSHES_SOLUTION;
		}

		// Check whether the new solution is a new best moves solution.
		if(solution.isBetterMovesSolutionThan(currentBestMovesSolution)) {
			currentBestMovesSolution = solution;
			// It's either a new best moves solution or a new best solution at all (best pushes and best moves solution).
			solutionType = (solutionType == SolutionType.NEW_BEST_PUSHES_SOLUTION) ? SolutionType.NEW_BEST_SOLUTION : SolutionType.NEW_BEST_MOVES_SOLUTION;
		}

		if (isASolutionStored) {
			// It's the very first solution, at all. This implies that it's the best moves and the best pushes solution, of course.
			solutionType = SolutionType.NEW_FIRST_SOLUTION;
		}

		// Store the solution on the database, if the level has a connection to the database.
		// Thereby the unique solutionsID from the database for the new solution is stored in the solution.
		if(level.isConnectedWithDB()) {
			database.insertSolution(solution, level.getDatabaseID());
		}

		fireSolutionEvent(new SolutionEvent(this, Solution.class, SolutionEvent.EventAction.INSERT, solution));

		return solutionType;
	}

	/**
	 * Deletes the passed <code>Solution</code> from the level.
	 * <p>
	 * If the solution is stored in the database for this level it's also deleted there.
	 *
	 * @param solution the <code>Solution</code> to be deleted
	 */
	 public void deleteSolution(Solution solution) {

		if(!solutions.remove(solution)) {
			return;
		}

		// Delete the solution from the database if it is stored there.
		if(solution.databaseID != Database.NO_ID) {
			database.deleteSolution(solution.databaseID);
		}

		// Determine the new best pushes and new best moves solution.
		currentBestMovesSolution  = getBestMovesSolution();
		currentBestPushesSolution = getBestPushesSolution();

		fireSolutionEvent(new SolutionEvent(this, Solution.class, SolutionEvent.EventAction.DELETE, solution));
	}

	/**
	 * Deletes all solutions of this level.
	 */
	public void deleteAllSolutions() {
		for(Solution solution : solutions) {
			deleteSolution(solution);
		}
	}

	/**
	 * This methods checks if the given solution is valid.
	 * The solution is corrected in the following way:
	 * <ol>
	 *  <li> trailing unnecessary moves/pushes and invalid movements are erased,
	 *  <li> moves are represented by lower case letters and
	 *  <li> pushes are represented by upper case letters.
	 * </ol>
	 * The correct solution is set in the <code>Solution</code> object.
	 * <p>
	 * This is also the main code to compute the secondary solution metrics.
	 *
	 * @param solution Solution to be validated
	 */
	public void verifySolution(Solution solution) {

		// If there is no solution simply return.
		if(solution == null || solution.lurd.isEmpty()) {
			return;
		}
		if(solution.lurd.trim().isEmpty()) {
			solution.lurd = "";
			return;
		}

		// Direction offsets up, down, left, right.
		final int deltaUP    = -level.getWidth();
		final int deltaDOWN  = -deltaUP;
		final int deltaLEFT  = -1;
		final int deltaRIGHT =  1;

		// Corrected solution that bases on the given solution string.
		StringBuilder validatedSolution = new StringBuilder();

		// New box position after a push.
		int newBoxPosition = 0;

		// Flag indicating whether the last movement was a move.
		// For the secondary metrics we need a "move" before the start.
		boolean lastMovementWasMove = true;

		// Position of the last pushed box, initialized to impossible value.
		int lastPushedBoxPosition = 0;

		// Last "stepDelta", initialized to impossible value.
		int lastStepDelta = 0;

		// Initialize the metric values of the solution.
		solution.pushesCount 	 = 0;
		solution.movesCount  	 = 0;
		solution.boxLines    	 = 0;
		solution.boxChanges  	 = 0;
		solution.pushingSessions = 0;
		int      playerLines     = 0;		// FFS: to be implemented in DB

		// Backup the initial board for the next runs.
		if(initialBoard == null) {
			initialBoard = getActiveBoard(level);
		}

		// Local copy of the initial board.
		int[] board = initialBoard.board.clone();
		int boxesNotOnGoalCount = initialBoard.boxesNotOnGoalCount;
		int playerPosition      = initialBoard.playerPosition;

		// If the level is no valid level set an empty solution lurd.
		if(playerPosition == 0 || board.length < 12) { // 12 is minimum level size
			solution.lurd = "";
			return;
		}

		// The solution gets the status "verified" even if it is proven
		// to be invalid, in which case it gets am empty "lurd".
		solution.isSolutionVerified = true;

		// Go through solution and check if every movement is valid.
		for(int index=0; index<solution.lurd.length(); index++) {

			final char inChar = solution.lurd.charAt(index);
			final int  stepDelta;

			// Calculation of the new player and potential new box position
			switch( inChar ) {
				case 'u':
				case 'U':
					stepDelta = deltaUP;
					break;

				case 'd':
				case 'D':
					stepDelta = deltaDOWN;
					break;

				case 'l':
				case 'L':
					stepDelta = deltaLEFT;
					break;

				case 'r':
				case 'R':
					stepDelta = deltaRIGHT;
					break;

				default:
					continue;
			}
			playerPosition += stepDelta;
			newBoxPosition  = playerPosition + stepDelta;

			// Increase the number of moves.
			solution.movesCount++;

			// Check if there is a box on the new player position.
			int atPlayer = board[playerPosition];

			if(atPlayer == '$' || atPlayer == '*') {
				// Shall be a push...

				int atNewBox = board[newBoxPosition];

				// If the box can't be pushed the solution is invalid.
				if(atNewBox != ' ' && atNewBox != '.') {
					break;
				}

				// Do push
				atPlayer = ((atPlayer == '*') ? '.' : ' ');
				atNewBox = ((atNewBox == '.') ? '*' : '$');
				board[playerPosition] = atPlayer;
				board[newBoxPosition] = atNewBox;

				// Construct new solution string (appe3nd push in upper case)
				validatedSolution.append(Character.toUpperCase(inChar));

				// Update boxes on goals value.
				if(atPlayer == '.') {			// pushed box away from goal
					boxesNotOnGoalCount++;
				}
				if(atNewBox == '*') {			// pushed box onto goal
					boxesNotOnGoalCount--;
				}

				// Increase the number of pushes.
				solution.pushesCount++;

				// Update push oriented secondary solution metrics values.
				if(playerPosition != lastPushedBoxPosition || lastMovementWasMove) {
					solution.boxLines++;
				}
				if(playerPosition != lastPushedBoxPosition) {
					solution.boxChanges++;
				}
				if(lastMovementWasMove) {
					solution.pushingSessions++;
				}

				// Update move oriented secondary solution metrics values.
				if (stepDelta != lastStepDelta) {
					playerLines++;
				}

				// Save position of the pushed box for secondary metrics.
				lastPushedBoxPosition = newBoxPosition;

				lastMovementWasMove = false;
				lastStepDelta = stepDelta;

				// If all boxes are pushed to goals the solution is valid.
				if(boxesNotOnGoalCount == 0) {
					solution.lurd = validatedSolution.toString();
					return;
				}
			} else {
				// Shall be a "move"...
				// If the player can't move the solution is invalid
				if(atPlayer != ' ' && atPlayer != '.') {
					break;
				}

				// Construct new solution string (append move in lower case)
				validatedSolution.append(Character.toLowerCase(inChar));

				// Update move oriented secondary solution metrics values.
				if (stepDelta != lastStepDelta) {
					playerLines++;
				}

				lastMovementWasMove = true;
				lastStepDelta = stepDelta;
			}
		}

		// If the solution had been a valid solution, the method
		// would have been already left.  Hence, this is not a valid solution.
		solution.lurd = "";
	}


	/**
	 * Returns the solution specified by the passed solution number.
	 *
	 * @param solutionNumber the index of the solution in the ArrayList to return
	 * @return the solution
	 */
	public final Solution getSolution(int solutionNumber) {
		if(solutionNumber < 0 || solutionNumber >= solutions.size()) {
			return null;
		}

		// Get the relevant solution.
		Solution solution = solutions.get(solutionNumber);

		// Check if the solution has been verified.
		if(solution.isSolutionVerified == false) {
			verifySolution(solution);
		}

		return solution;
	}

	/**
	 * Returns all {@code Solution}s stored.
	 *
	 * @return the {@code Solution}s stored in this {@code SolutionsManager}
	 */
	public ArrayList<Solution> getSolutions() {
		ArrayList<Solution> verifiedSolutions = new ArrayList<Solution>(solutions.size());
		for(Solution solution : solutions) {
			if(!solution.isSolutionVerified) {
				verifySolution(solution);
			}
			verifiedSolutions.add(solution);
		}
		return verifiedSolutions;
	}

	/**
	 * Returns the number of solutions stored in this object.
	 *
	 * @return the number of solutions of this level
	 */
	public final int getSolutionCount() {
		return solutions.size();
	}

	/**
	 * This method is the leading object for adding and deleting solutions of a level.
	 * However, the SolutionsGUI is the main object for the order of the solutions
	 * in a level. This method is called when the solutions have been reordered
	 * in the SolutionsGUI (by the use).
	 * The order is represented by the "order value" of every solution.
	 */
	public void sortAccordingOrderValue() {

		// Sort the solutions according to their order value.
		Collections.sort(solutions, Solution.ORDER_VALUE_COMPARATOR);
	}


	/**
	 * Returns the "active" board of the level.
	 * <p>
	 * "Active" means that only the player reachable board is taken into account.
	 *
	 * @param level the level containing the relevant board data
	 * @return the board
	 */
	private InitialBoard getActiveBoard(Level level) {

		int boardWidth  = level.getWidth();
		int boardHeight = level.getHeight();
		int boardSize   = boardWidth * boardHeight;

		InitialBoard board 	= new InitialBoard();
		board.board = new int[boardSize];

		// Set the starting board position in the local board array and calculate the number of boxes on a goal.
		for(int y = 0; y < boardHeight; y++) {
			for(int x = 0; x < boardWidth; x++) {

				int squareCharacter = level.getSquareCharacter(x, y);

				// Copy the board data in the local array.
				board.board[x + y * boardWidth] = squareCharacter;

				if(squareCharacter == '@' || squareCharacter == '+') {
					board.playerPosition = x + boardWidth*y;

					// The player needn't to be set in the board data.
					board.board[board.playerPosition] = (squareCharacter == '@') ? ' ' : '.';
				}
			}
		}

		// Determine the area the player can reach.
		boolean[] playerReachableSquares = new boolean[boardSize];

		final IntStack positionsToBeAnalyzed = new IntStack(boardSize);

		int currentPlayerPosition;
		int[] offset = new int[] {-boardWidth, +boardWidth, -1, +1};

		positionsToBeAnalyzed.add(board.playerPosition);

		while (!positionsToBeAnalyzed.isEmpty()) {
			currentPlayerPosition = positionsToBeAnalyzed.remove();

			// Ensure the position is a valid one.
			if (       currentPlayerPosition < boardWidth
					|| currentPlayerPosition > boardSize - boardWidth
					|| currentPlayerPosition % boardWidth == 0
					|| currentPlayerPosition % boardWidth == boardWidth - 1) {
				continue;
			}

			playerReachableSquares[currentPlayerPosition] = true;

			for(int direction = 0; direction < 4; direction++) {
				int newPlayerPosition = currentPlayerPosition + offset[direction];
				if (board.board[newPlayerPosition] != '#' && playerReachableSquares[newPlayerPosition] == false) {
					positionsToBeAnalyzed.add(newPlayerPosition);
				}
			}
		}

		// Determine the number of boxes not on a goal in the player reachable area (= active board).
		// Furthermore add a wall at all squares not reachable by the player.
		for(int position=0; position<boardSize; position++) {
			if(board.board[position] == '$' && playerReachableSquares[position] == true) {
				board.boxesNotOnGoalCount++;
			}
			if(playerReachableSquares[position] == false) {
				board.board[position] = '#';
			}
		}

		return board;
	}

	/**
	 * Table model for the table showing the solution data.
	 */
	@SuppressWarnings("serial")
	private class TableModelSolutionData extends AbstractTableModel
		implements ColumnVisibility
	{

		// The indices of the columns.
		protected static final int SOLUTION_ID_INDEX 	    =  0;
		protected static final int NAME_INDEX 			    =  1;
		protected static final int LURD_INDEX 			    =  2;
		protected static final int MOVES_INDEX 	 	  		=  3;
		protected static final int PUSHES_INDEX	       		=  4;
		protected static final int BOX_LINES_INDEX 	   		=  5;
		protected static final int BOX_CHANGES_INDEX 	  	=  6;
		protected static final int PUSHING_SESSIONS_INDEX 	=  7;
		protected static final int IS_OWN_SOLUTION_INDEX 	=  8;
		protected static final int COMMENT_INDEX 		    =  9;
		protected static final int ORDER_VALUE_INDEX	    = 10;
		protected static final int HIGHLIGHT_DATA_INDEX     = 11;
		protected static final int LAST_CHANGED_INDEX       = 12;
		protected static final int SOLUTION_INDEX       	= 13;

		// The names of the columns.
		private String[] columnNames = {"SOLUTIONID",Texts.getText("name"),  		   "LURD",
				Texts.getText("moves"), 		     Texts.getText("pushes"),          Texts.getText("boxLines"),
				Texts.getText("boxChanges"), 		 Texts.getText("pushingSessions"), Texts.getText("isOwnSolution"),
				"COMMENT", 							 "orderValue",					   "highLightData",
				Texts.getText("lastChanged"),		 Texts.getText("solution") };

		// Determination of the editable fields.
		private boolean[] editable = {false, true, false, false, false, false, false, false, true, false, false, false, false, false};

		// Determination of the visible columns.
		private boolean[] isVisible = {false, true, false, true, true, true, true, true, true, false, false, false, true, false};

		// The data of the table.
		protected ArrayList<Object[]> tableData = new ArrayList<Object[]>();


		/**
		 * Creates a table model for the level data table.
		 */
		public TableModelSolutionData() {}


		/**
		 * Loads the data of the levels specified by the passed search pattern
		 * into this table model.
		 */
		public final void refreshData() {

			// Delete the old data.
			tableData.clear();

			for(int solutionNo=0; solutionNo < solutions.size(); solutionNo++) {

				Solution solution = getSolution(solutionNo);

				// Create a data row for the table.
				Object[] dataRow = {
						solution.databaseID,
						solution.name,
						solution.lurd,
						solution.movesCount,
						solution.pushesCount,
						solution.boxLines,
						solution.boxChanges,
						solution.pushingSessions,
						solution.isOwnSolution,
						solution.comment,
						solution.orderValue,
						solution.highLightData,
						solution.getLastChanged(),
						solution };

				// Add the solution data to the table.
				tableData.add(dataRow);
			}

			// Set the name of this level in the JLabel displaying the name.
			levelName.setText(Texts.getText("level") + ": " + level.getTitle());

			// Notify the view that the model data has been changed.
			fireTableDataChanged();

			// Set optimal widths.
			for(int i=0; i<getColumnCount(); i++) {
				optimizeColumnWidths(tableSolutionData, i);
			}
		}


		/**
		 * Adds a (new) solution to this model.
		 * The {@code lastChanged} data of the argument is ignored
		 * and replaced by "now".
		 *
		 * @param solution the solution to add
		 * @return the model row index of the new row
		 */
		public final int addSolution(Solution solution) {

			// Create a data row holding all relevant information.
			Object[] dataRow = {solution.databaseID,
								solution.name,
								solution.lurd,
								solution.movesCount,
								solution.pushesCount,
								solution.boxLines,
								solution.boxChanges,
								solution.pushingSessions,
								solution.isOwnSolution,
								solution.comment,
								solution.orderValue,
								solution.highLightData,
								new Date(),
								solution
							   };
			tableData.add(dataRow);
			final int newrowindex = getRowCount() - 1;
			fireTableRowsInserted(newrowindex, newrowindex);

			return newrowindex;
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
			rowData[LAST_CHANGED_INDEX] = newTimestamp;

			// Fire events because the values have changed.
			fireTableCellUpdated(row, col);
			fireTableCellUpdated(row, LAST_CHANGED_INDEX);

			Solution solution 		= (Solution) rowData[SOLUTION_INDEX];
			solution.name 			= (String) rowData[NAME_INDEX];
			solution.isOwnSolution 	= ((Boolean) rowData[IS_OWN_SOLUTION_INDEX]).booleanValue();
			solution.comment 		= (String) rowData[COMMENT_INDEX];
			solution.setLastChanged(newTimestamp);

			// Update the solution data on the database.
			if(solution.databaseID != Database.NO_ID) {
				database.updateSolution(solution);
			};

			// Inform the listeners (so the change is also stored in the database).
			fireSolutionEvent(new SolutionEvent(this, Solution.class, SolutionEvent.EventAction.CHANGE, solution));
		}
	}

	/* (non-Javadoc)
	 * @see java.awt.datatransfer.ClipboardOwner#lostOwnership(java.awt.datatransfer.Clipboard, java.awt.datatransfer.Transferable)
	 */
	@Override
	final public void lostOwnership(Clipboard clipboard, Transferable contents) {}

	/**
	 * Class for storing the data of the board.
	 */
	private class InitialBoard {
		public int[] board = new int[0];
		public int playerPosition = 0;
		public int boxesNotOnGoalCount = 0;

		public InitialBoard() {}

		@Override
		public InitialBoard clone() {
			InitialBoard newBoard = new InitialBoard();
			if(board != null) {
				newBoard.board = board.clone();
			}
			newBoard.playerPosition = playerPosition;
			newBoard.boxesNotOnGoalCount = boxesNotOnGoalCount;

			return newBoard;
		}
	}


	/**
	 * Adds an <code>SolutionEventListener</code>.
	 * @param l the <code>SolutionEventListener</code> to be added
	 */
	public void addSolutionEventListener(SolutionEventListener l) {
		listenerList.add(SolutionEventListener.class, l);
	}

	/**
	 * Removes an <code>SolutionEventListener</code>.
	 *
	 * @param l the listener to be removed
	 */
	public void removeSolutionEventListener(SolutionEventListener l) {
		listenerList.remove(SolutionEventListener.class, l);
	}

	/**
	 * Notifies all listeners that have registered interest for
	 * notification on this event type.
	 *
	 * @param event  the <code>SolutionEventListener</code> object
	 */
	protected void fireSolutionEvent(SolutionEvent event) {
		// Guaranteed to return a non-null array
		Object[] listeners = listenerList.getListenerList();
		// Process the listeners last to first, notifying
		// those that are interested in this event
		for (int i = listeners.length-2; i>=0; i-=2) {
			if (listeners[i]==SolutionEventListener.class) {
				((SolutionEventListener)listeners[i+1]).solutionEventFired(event);
			}
		}
	}

}