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
import java.awt.Color;
import java.awt.Component;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.Point;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.ClipboardOwner;
import java.awt.datatransfer.DataFlavor;
import java.awt.datatransfer.Transferable;
import java.awt.datatransfer.UnsupportedFlavorException;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.ComponentAdapter;
import java.awt.event.ComponentEvent;
import java.awt.event.FocusAdapter;
import java.awt.event.FocusEvent;
import java.awt.event.KeyEvent;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.beans.PropertyChangeEvent;
import java.beans.PropertyChangeListener;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Enumeration;
import java.util.Hashtable;
import java.util.WeakHashMap;

import javax.swing.AbstractAction;
import javax.swing.BorderFactory;
import javax.swing.BoxLayout;
import javax.swing.DefaultListCellRenderer;
import javax.swing.DefaultListModel;
import javax.swing.DropMode;
import javax.swing.ImageIcon;
import javax.swing.JButton;
import javax.swing.JComponent;
import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JMenu;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JScrollPane;
import javax.swing.ListCellRenderer;
import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;
import javax.swing.TransferHandler;
import javax.swing.border.BevelBorder;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.RunLengthFormat;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager.SolutionType;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.GraphicUtilities;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * This class is used for displaying solutions.
 * It is the GUI for the class "Solutions" (which manages the solution set
 * of a level).  It is used for multiple views to the solution list,
 * e.g. in the MainBoard and in the optimizer.
 */
@SuppressWarnings("serial")
public class SolutionsGUI extends JList<Solution> implements ActionListener, ClipboardOwner, SolutionEventListener {

	public enum Sorting {
		MOVES_PUSHES_SORTING, // Constant for the sorting style: major moves, minor pushes
		PUSHES_MOVES_SORTING; // Constant for the sorting style: major pushes, minor moves
		// FFS/hm: more sorting options to be added
	}

	/** Reference to the main object which holds all references.
	 *  The main object is needed e.g. for setting a solution as new history in the game.
	 */
	protected JSoko application;

	/** The model of this JList. */
	protected DefaultListModel<Solution> listModel = null;

	/** Level the displayed solutions belong to. */
	private Level level = null;

	/** The Solutions object managing the solutions that are displayed by this class. */
	protected SolutionsManager solutionsManager = null;

	/** Flag, indicating whether the solutions view is pinned, that is: it isn't
	 *  set invisible when the mouse leaves the view.
	 */
	protected boolean isViewPinned = false;

	/** Button for pinning the view. */
	protected JButton pinViewButton = null;

	/** Button which opens the solution view when pressed. */
	protected JButton openSolutionsViewButton = null;

	/** Panel displaying the solutions in a list. */
	protected JPanel pinablePanel = null;

	/** The scroll pane this list is shown in. */
	protected JScrollPane solutionsListScrollPane = null;

	/** Panel containing the solutionsListPanel and the button for opening the panel. */
	protected JPanel solutionsViewPanel = null;

	/**
	 * In the GUI the user can remove some of the solutions. These "hidden" solutions
	 * are stored in this ArrayList. If the solutions are reloaded from the database
	 * all solutions are checked for being in this list of hidden solutions.
	 * This way none of the hidden solutions is shown again.
	 */
	protected ArrayList<Solution> hiddenSolutions = new ArrayList<Solution>();

	/**
	 * Solutions that are displayed in a specific color. Solutions colored using this
	 * functionality are just shown colored but don't save the color themselves.
	 * Therefore solutions that are displayed colored due to this array aren't colored
	 * in other SolutionsGUIs.
	 */
	protected WeakHashMap<Solution, Color> coloredSolutions = new WeakHashMap<Solution, Color>();

	// Menu item in the context menu for the solutions list. If a solution is removed
	// from the GUI this menu item is set "enabled" so the user can let the list
	// show all solutions again.
	protected JMenuItem showRemovedSolutionsAgain;

	/** Best pushes solution from the currently displayed solutions.
	 *  This list entry shall be marked with a special icon.
	 *  @see #bestMovesSolution
	 */
	protected Solution bestPushesSolution = null;

	/** Best moves solution from the currently displayed solutions.
	 *  This list entry shall be marked with a special icon.
	 *  @see #bestPushesSolution
	 */
	protected Solution bestMovesSolution  = null;

	/** Thread that highlights solutions. */
	protected HighlighterThread solutionHighlighterThread = null;

	/**
	 * Flag, indicating whether this is the main solutions view where
	 * the user can reorder solutions. Some other views (for instance
	 * in the optimizer) also offer reordering, but this isn't saved
	 * in the level or database.
	 */
	protected boolean isMainSolutionView = false;

	/** Flag, indicating whether the "take solution as history" menu item
	 *  is to be shown in the context menu.
	 */
	boolean isTakeSolutionAsHistoryEnabled = true;


	/**
	 * Variables for drag&drop.
	 */
	// The mime type of the transferable.
	private static String flavorMimeType = DataFlavor.javaJVMLocalObjectMimeType
	                                     + ";class=\"" + TransferableData.class.getName()
	                                     + "\"";

	// Data flavor.
	static DataFlavor dataFlavor = new DataFlavor(flavorMimeType, "Transferable");
	public static DataFlavor[] supportedFlavors = { dataFlavor };


	/**
	 * Create the GUI for displaying the solutions.
	 *
	 * @param application Reference to the main object which holds all references
	 * @param showTakeSolutionMenuItem flag, indicating whether the menu item for taking
	 *                                 a solution as solution is to be displayed
	 * @param isMainSolutionView flag, indicating whether reordering of the solutions
	 *                           is to be saved
	 */
	public SolutionsGUI(final JSoko application, boolean showTakeSolutionMenuItem, boolean isMainSolutionView) {

		this.application = application;
		this.isMainSolutionView = isMainSolutionView;
		isTakeSolutionAsHistoryEnabled = showTakeSolutionMenuItem;

		// If the model changes the reference must be updated.
		addPropertyChangeListener("model", new PropertyChangeListener() {
			@Override
			public void propertyChange(PropertyChangeEvent evt) {
				listModel = (DefaultListModel<Solution>)  evt.getNewValue();
			}
		});

		// Set a default list model.
		setModel((listModel = new DefaultListModel<Solution>()));

		// Create the context menu for this list.
		createContextMenu();

		// The solutions are to be displayed with text and a icon.
		// Hence, set a new cell renderer.
		setCellRenderer(getListCellRenderer());

		// Add a mouse listener for the solutions, so a solution can be taken
		// as history with a double click.
		addMouseListener(getMouseListener());

		// Create a thread which highlights solutions that have been added
		// or have been moved.
		(solutionHighlighterThread = new HighlighterThread()).start();

		// This list should support drag and drop.
		// Hence at a ListTransferHandler and enable drag&drop.
		setTransferHandler(new ListTransferHandler());
		setDragEnabled(true);
		setDropMode(DropMode.INSERT);

		// A "paste" action should be treated as importing a solution from the clipboard.
		getActionMap().put("paste", new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				SolutionsGUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "importSolution"));
			}
		});

		addFocusListener(new FocusAdapter() {
			@Override
			public void focusGained(FocusEvent e) {
				// Select first solution if none is selected.
				if(isSelectionEmpty()) {
					setSelectedIndex(0);
				}
			}
		});
	}


	/**
	 * Returns the mouse listener for this class.
	 *
	 * @return the mouse listener
	 */
	private MouseListener getMouseListener() {
		return new MouseAdapter() {
			@Override
			public void mouseClicked(MouseEvent e) {
				if(e.getClickCount() > 1) {
					int index = locationToIndex(e.getPoint());
					if (index >= 0) {

						// This action can be disabled (for instance when the solver is running).
						if(!isTakeSolutionAsHistoryEnabled) {
							return;
						}

						Solution clickedSolution = listModel.getElementAt(index);

						// Set the selected solution as new history in the game.
						application.takeSolutionForHistory(clickedSolution);
					}
				}
			}
		};
	}

	/**
	 * Returns the cell renderer for this list.
	 * <p>
	 * The solutions aren't displayed as plain string text but as StyledLabels.
	 *
	 * @return the cell renderer
	 */
	private ListCellRenderer<? super Solution> getListCellRenderer() {

		// Class for rendering the solutions in the JList.
		class SolutionListCellRenderer extends DefaultListCellRenderer {

			@Override
			public Component getListCellRendererComponent(JList<?> list, Object value, int index, boolean isSelected, boolean cellHasFocus) {

				// Get the default renderer.
				JLabel renderer = (JLabel) super.getListCellRendererComponent(list, value, index, isSelected, cellHasFocus);

				// The icon should be displayed at the right side.
				renderer.setHorizontalTextPosition(SwingConstants.LEFT);

				// Set a border for a better look.
				setBorder(BorderFactory.createEmptyBorder(0, 2, 0, 10));

				// If this is the best pushes or best moves solution
				// "highlight" it: use a bold font, set tooltip and add an icon.
				Solution solution = (Solution) value;
				boolean  wanticon = false;
				String   ttkey    = null;
				String   iconkey  = null;
				char     iconchar = '?';

				if(solution.equals(bestMovesSolution) ) {
					wanticon = true;
					ttkey    = "bestMovesSolution";
					iconkey  = "movesSolutionCharacter";
					if(solution.equals(bestPushesSolution)) {
						ttkey    = "bestSolution";
						iconkey  = "solutionCharacter";
					}
				}
				else if(solution.equals(bestPushesSolution)) {
					wanticon = true;
					ttkey    = "bestPushesSolution";
					iconkey  = "pushesSolutionCharacter";
				}

				if (wanticon) {
					renderer.setFont(new Font(getFont().getFontName(), Font.BOLD, getFont().getSize()));
					renderer.setToolTipText(Texts.getText(ttkey));

					// Set an icon for marking the kind of solution.
					if (iconkey != null) {
						String icontxt = Texts.getText(iconkey);
						if (icontxt != null && (icontxt.length() >= 1)) {
							iconchar = Texts.getText(iconkey).charAt(0);
						}
					}
					renderer.setIcon(Utilities.getIconWithCharacter(16, iconchar, new Color(28, 134, 238)));
				}
				else {
					// Delete the tool tip text and the icon.
					renderer.setToolTipText(null);
					renderer.setIcon(null);
				}

				Integer i = solutionHighlighterThread.getHighlightIntensity(solution);
				if(i != null) {
					renderer.setForeground(GraphicUtilities.changeLightness(Color.blue, 20*i));
				}

				// Set the color of the solution.
				Color color = coloredSolutions.get(solution);
				if(color != null) {
					renderer.setBackground(color);
				}

				// In debug mode display the order value.
//				if(Debug.isDebugModeActivated) {
//					renderer.setText(renderer.getText().concat(", order: "+solution.orderValue));
//				}

				if(Debug.debugShowLevelIsInDBStatus) {
					renderer.setText(renderer.getText().concat(" ID: "+solution.databaseID));
				}

				return renderer;
			}
		}

		return new SolutionListCellRenderer();
	}

	/**
	 * Creates the context menu for this list.
	 *
	 * @param showTakeSolutionMenuItem flag, indicating whether the menu item for taking
	 *                                 a solution as solution is to be displayed
	 */
	private void createContextMenu() {

		// Menu for selecting a solution for playing.
		final JMenuItem takeSolution = new JMenuItem(Texts.getText("takeSolutionAsHistory"), Utilities.getIcon("tranformation icon.png", null));
		takeSolution.setToolTipText(Texts.getText("takeSolutionTooltip"));
		takeSolution.addActionListener(this);
		takeSolution.setActionCommand("takeSolutionAsHistory");

		// Add a JPopupMenu to the list offering several possible actions for the user.
		final JPopupMenu popupMenu = new JPopupMenu(Texts.getText("solutions")) {

			@Override
			public void show(Component invoker, int x, int y) {

				super.show(invoker, x, y);

				// Get the index of the item under the mouse click position.
				int index = locationToIndex(new Point(x, y));

				// Select the solution the popup is shown for if it isn't already selected.
				if(!isSelectedIndex(index)) {
					setSelectedIndex(index);
				}

				// The "Take solution as history" menu is only shown
				// when only one solution is selected.
				// Otherwise it wouldn't be clear which solutions is taken.
				takeSolution.setEnabled(getSelectedIndices().length == 1);

				// Only show the menu item for taking a solution as history if this is set.
				takeSolution.setVisible(isTakeSolutionAsHistoryEnabled);
			}
		};

		// Ensure the popup changes its look&feel when the user changes the look&feel.
		Utilities.addComponentToUpdateUI(popupMenu);

		// Menu item for easy cancel of popup.
		JMenuItem cancelMenuItem = new JMenuItem(Texts.getText("cancel"));
		cancelMenuItem.setEnabled(false);
		popupMenu.add(cancelMenuItem);


		popupMenu.addSeparator();

		// Add the "take solution as history" menu to the popup.
		popupMenu.add(takeSolution);

		// Menu for adding a new solution.
		JMenuItem importFromClipboard = new JMenuItem(Texts.getText("importSolution"), Utilities.getIcon("edit-paste.png", null));
		importFromClipboard.addActionListener(this);
		importFromClipboard.setActionCommand("importSolution");
		popupMenu.add(importFromClipboard);

		// Menu item for copying the solution to the clipboard.
		JMenuItem copyToClipboard = new JMenuItem(Texts.getText("solutionList.popupCopy"), Utilities.getIcon("edit-copy.png", null));
		copyToClipboard.addActionListener(this);
		copyToClipboard.setActionCommand("copySolutionToClipBoard");
		popupMenu.add(copyToClipboard);

		// Menu item for copying the solution to the clipboard in run length encoded format.
		JMenuItem copyToClipboardRLE = new JMenuItem(Texts.getText("solutionList.popupCopyRLE"), Utilities.getIcon("edit-copy.png", null));
		copyToClipboardRLE.addActionListener(this);
		copyToClipboardRLE.setActionCommand("copySolutionToClipBoardRLE");
		popupMenu.add(copyToClipboardRLE);

		// Menu item for showing the lurd representation of the solution.
		JMenuItem showLURD = new JMenuItem(Texts.getText("solutionList.displayMoves"), Utilities.getIcon("system-search.png", null));
		showLURD.addActionListener(this);
		showLURD.setActionCommand("showLURD");
		popupMenu.add(showLURD);


		popupMenu.addSeparator();


		// Menu item for removing the selected solutions from the list.
		JMenuItem removeFromList = new JMenuItem(Texts.getText("solutionsList.removeFromList"), Utilities.getIcon("list-remove.png", null));
		removeFromList.addActionListener(this);
		removeFromList.setActionCommand("removeFromList");
		popupMenu.add(removeFromList);

		// Menu item for showing the removed solutions again.
		showRemovedSolutionsAgain = new JMenuItem(Texts.getText("solutionsList.showAllRemovedSolutions"), Utilities.getIcon("list-add.png", null));
		showRemovedSolutionsAgain.addActionListener(this);
		showRemovedSolutionsAgain.setActionCommand("showRemovedSolutions");
		showRemovedSolutionsAgain.setEnabled(false); // it's first enabled when a solution is removed from the list
		popupMenu.add(showRemovedSolutionsAgain);

//		TODO "color solution" menu
//		Color newColor = JColorChooser.showDialog(this, "Choose Background Color", this.getForeground());
//		System.out.printf("color: "+newColor);

		popupMenu.addSeparator();


		// Delete button
		JMenuItem deleteSolution = new JMenuItem(Texts.getText("deleteSolution"), Utilities.getIcon("edit-delete.png", null));
		deleteSolution.setToolTipText(Texts.getText("deleteSolutionTooltip"));
		deleteSolution.addActionListener(this);
		deleteSolution.setActionCommand("deleteSolutions");
		popupMenu.add(deleteSolution);


		popupMenu.addSeparator();


		// Menu for activating that the best moves and pushes solutions are always shown at the top of the list.
		JMenuItem bestSolutionsOnTop = new JMenuItem(Texts.getText("solutionList.bestSolutionsOnTop"));
		bestSolutionsOnTop.addActionListener(this);
		bestSolutionsOnTop.setActionCommand("bestSolutionsOnTop");
		popupMenu.add(bestSolutionsOnTop);

		// Sorting menu.
		JMenu sortingMenu = new JMenu(Texts.getText("sorting"));

		JMenuItem movesPushesSorting = new JMenuItem(Texts.getText("solutionList.moves/pushes"));
		movesPushesSorting.addActionListener(this);
		movesPushesSorting.setActionCommand("sortMovesPushes");
		sortingMenu.add(movesPushesSorting);

		JMenuItem pushesMovesSorting = new JMenuItem(Texts.getText("solutionList.pushes/moves"));
		pushesMovesSorting.addActionListener(this);
		pushesMovesSorting.setActionCommand("sortPushesMoves");
		sortingMenu.add(pushesMovesSorting);

		// FFS/hm: more sorting options to be added

		popupMenu.add(sortingMenu);

		// Set the popup for the list.
		setComponentPopupMenu(popupMenu);
	}

	/**
	 * Returns a <code>JPanel</code> displaying solutions.
	 *
	 * @param parentPanel {@code JPanel} this GUI is docked on
	 * @return the <code>JPanel</code> displaying the solutions
	 */
	public JPanel getDockingGUI(JPanel parentPanel) {

		// Set the help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "solutions-sidebar", null);

		// Get the pinned status from the settings. This status is only used in the main
		// game view. Hence it can just be read from the settings although this GUI
		// may be used in different views (like in the optimizer).
		isViewPinned = Settings.getBool("isSolutionViewPinned", false);

		// Create a panel which will contain this list and a button for showing the panel.
		solutionsViewPanel = new JPanel(new BorderLayout());
		solutionsViewPanel.setBorder(BorderFactory.createBevelBorder(BevelBorder.RAISED));
		solutionsViewPanel.setFocusable(false);

		/**
		 * Create the menu bar for the solutions view.
		 */
		ImageIcon pinIcon = Utilities.getIcon("pin (oxygen).png", "pin view");
		pinViewButton = new JButton(pinIcon);
		pinViewButton.setToolTipText(Texts.getText("general.pinButtonTooltip"));
		pinViewButton.setMaximumSize(new Dimension(pinIcon.getIconWidth(), pinIcon.getIconHeight()));
		pinViewButton.setPreferredSize(pinViewButton.getMaximumSize());
		pinViewButton.setContentAreaFilled(false);
		pinViewButton.setBorderPainted(false);
		pinViewButton.setActionCommand("pinView");
		pinViewButton.setFocusable(false);	// Avoid bad look when NimRod shows a border
		pinViewButton.addActionListener(this);

		/**
		 * Create the panel showing the solutions.
		 */
		// Put the list into a scroll pane.
		solutionsListScrollPane = new JScrollPane(this);

		// Add a listener so the scroll pane resizes when the list entries width changes.
		addComponentListener(new ComponentAdapter() {
			@Override
			public void componentResized(ComponentEvent e) {
				resizeGUI();
			}
		});

		// A panel containing
		pinablePanel = new JPanel(new BorderLayout());

		// A panel for the pin button and the title of the solution view.
		JPanel headerPanel = new JPanel(new BorderLayout());
		JLabel label = new JLabel(Texts.getText("solutions"));
		headerPanel.add(label, BorderLayout.WEST);
		headerPanel.add(pinViewButton, BorderLayout.EAST);
		pinablePanel.add(headerPanel, BorderLayout.NORTH);

		// A panel for showing the solutions in a list.
		JPanel solutionsListPanel = new JPanel();
		solutionsListPanel.setLayout(new BoxLayout(solutionsListPanel, BoxLayout.Y_AXIS));
		solutionsListPanel.add(solutionsListScrollPane);
		solutionsListPanel.setBorder(BorderFactory.createEmptyBorder(0, 2, 0, 0));
		pinablePanel.add(solutionsListPanel, BorderLayout.CENTER);

		solutionsViewPanel.add(pinablePanel, BorderLayout.CENTER);


		/**
		 * Create the button for opening the solutions view.
		 */
		ImageIcon openSolutionsViewIcon = Utilities.getIcon("tranformation icon.png", "open solutions view");
		openSolutionsViewButton = new JButton(openSolutionsViewIcon);
		openSolutionsViewButton.setPreferredSize(new Dimension(6, openSolutionsViewIcon.getIconHeight()));
		openSolutionsViewButton.setMaximumSize(new Dimension(6, pinIcon.getIconHeight()));
		openSolutionsViewButton.setPreferredSize(new Dimension(6, pinIcon.getIconHeight()));
		openSolutionsViewButton.setContentAreaFilled(false);
		openSolutionsViewButton.setBorderPainted(false);
		openSolutionsViewButton.setActionCommand("openView");
		openSolutionsViewButton.setMnemonic(KeyEvent.VK_F11);
		openSolutionsViewButton.setFocusable(false);
		openSolutionsViewButton.addActionListener(this);
		openSolutionsViewButton.addMouseListener(new MouseAdapter() {
			@Override
			public void mouseEntered(MouseEvent e) {
				openSolutionsViewButton.doClick();
			}
		});

		pinablePanel.setVisible(isViewPinned);
		openSolutionsViewButton.setVisible(!isViewPinned);
		if(isViewPinned == false) {
			// Set a 50% brighter icon when the view isn't pinned.
			pinViewButton.setIcon(GraphicUtilities.createBrigtherIconImage(pinViewButton.getIcon(), 50));
		}

		// Add the button and the panel for showing the solutions to the main panel.
		solutionsViewPanel.add(openSolutionsViewButton, BorderLayout.EAST);

		parentPanel.addMouseListener(new MouseAdapter() {
			@Override
			public void mousePressed(MouseEvent e) {
				// If the view is pinned it never closes. Hence, this thread can be stopped.
				if(isViewPinned) {
					return;
				}

				// The mouse has been clicked in the parent panel and the solutions view isn't pinned.
				// Therefore this view is closed.
				openSolutionsViewButton.setVisible(true);
				pinablePanel.setVisible(false);
			}
		});

		//TODO: add a text area for showing the solution comments.
		//		solutionsViewPanel.add(new JScrollPane(new AutoResizingTextArea("Test", 2, 10, 20)), BorderLayout.SOUTH);

		return solutionsViewPanel;
	}

	/**
	 * Sets the solutions of the passed level as new solutions to be displayed.
	 *
	 * @param level  the level whose solutions are to be displayed
	 */
	public void setLevel(final Level level) {

		// Remove the current stored solutions.
		// This automatically sets the best solutions to null in the data listener.
		listModel.removeAllElements();

		// No solutions to be highlighted anymore.
		solutionHighlighterThread.removeAllSolutionsToBeHighlighted();

		// No solutions are hidden when the solutions of a level are to be added.
		hiddenSolutions.clear();

		// Remove this GUI as action listener from the previous solutions manager.
		if(this.solutionsManager != null) {
			this.solutionsManager.removeSolutionEventListener(this);
		}

		// Save the references to the solutions and level object.
		this.solutionsManager = level.getSolutionsManager();
		this.level = level;

		// Add this GUI as ActionListener so every change of the solutions
		// is immediately displayed in this GUI.
		solutionsManager.addSolutionEventListener(this);

		// Add all solutions to this list. There neededn't be a duplicate check
		// because the solutions in the main solutions object are unique.
		for(Solution solution : solutionsManager.getSolutions()) {
			listModel.addElement(solution);
		}

		// Determine the best solutions.
		determineBestSolutions(null);

		// Update the order values of every solution.
		updateSolutionOrderValues();

		// If there is already an item selected in the list the new added item is
		// also selected.  This selection is deleted.  The caller method has to decide
		// whether or not this new item is to be selected.
		getSelectionModel().removeSelectionInterval(0, 0);

		// Determine the best solutions.
		determineBestSolutions(null);

		// Update the order values of every solution.
		updateSolutionOrderValues();

		// Resize this GUI so the solutions fit in the scroll pane.
		resizeGUI();

		// Update the UI. Otherwise getPreferredSize returns a wrong size for example
		// when a level has no solution and then a level having solutions is loaded
		// from the database.
		updateUI();
	}

	/**
	 * Adds the passed solution to the displayed list of solutions.
	 * <p>
	 * If the solution is already in the list the solution isn't added.
	 *
	 * @param solution the solution to be added
	 */
	private void addSolution(final Solution solution, boolean isSolutionToBeHighlighted) {

		// If the solution is already stored in the list immediately return.
		if(listModel.indexOf(solution) != -1) {
			return;
		}

		// Add the solution as last solution.
		listModel.addElement(solution);

		// Determine whether the added solution is a new best solution.
		determineBestSolutions(solution);

		// Update the order values of every solution.
		updateSolutionOrderValues();

		// Update the UI. Otherwise getPreferredSize returns a wrong size if there is
		// only one solution in the list.
		if(listModel.getSize() == 1) {
			updateUI();
		}
	}

	/**
	 * Returns the <code>Solution</code> stored in this JList at the passed index.
	 *
	 * @param index  index of the <code>Solution</code> to be returned
	 * @return the <code>Solution</code>
	 */
	Solution getSolution(int index) {
		return listModel.get(index);
	}

	/**
	 * Sets a color for the passed <code>Solution</code> in this GUI.
	 * <p>
	 * If "null" is passed as color the solution is set back to the default color.
	 *
	 * @param solution the <code>Solution</code> to be colored
	 * @param color the <code>Color</code>
	 */
	public void setSolutionColor(Solution solution, Color color) {
		if(color == null) {
			coloredSolutions.remove(solution);
		} else {
			coloredSolutions.put(solution, color);
		}
	}

	/**
	 * Sets the background color for all solutions to the default color.
	 */
	public void setAllSolutionsUncolored() {
		coloredSolutions.clear();
	}

	/**
	 * Sorts the solutions according to the passed sorting method.
	 *
	 * @param sortMethod sorting order. Value should be passed using the constant values
	 *                   for sorting of this class.
	 */
	private void sortSolutions(Sorting sortMethod) {

		// Sorting will invalid the selection. Hence the selection is removed.
		clearSelection();

		// Add all removed solutions again so they are sorted, too.
		for(Solution solution : hiddenSolutions) {
			listModel.addElement(solution);
		}

		// Get solutions as array, sort array and set sorted solutions in list.
		Solution[] solutions = new Solution[listModel.getSize()];
		int index = 0;
		for(Object o : listModel.toArray()) {
			solutions[index++] = (Solution) o;
		}
		Arrays.sort(solutions, sortMethod == Sorting.MOVES_PUSHES_SORTING? Solution.MOVES_COMPARATOR : Solution.PUSHES_COMPARATOR);
		while(--index != -1) {
			listModel.setElementAt(solutions[index], index);
		}

		// Every solution holds its position in the level in an own variable.
		// These variables have to be updated now.
		updateSolutionOrderValues();

		// Hide the solutions again.
		for(Solution solution : hiddenSolutions) {
			listModel.removeElement(solution);
		}
	}

	/**
	 * Shows an information dialog with translated title "note" and the message text
	 * for the text key "solutionList.noSolutionSelected".
	 */
	protected void showNoSolutionsSelected() {
		MessageDialogs.showInfoNoteTextKey(getParent(), "solutionList.noSolutionSelected");
	}

	/* (non-Javadoc)
	 * @see java.awt.event.ActionListener#actionPerformed(java.awt.event.ActionEvent)
	 */
	@Override
	public void actionPerformed(ActionEvent evt) {

		// Get the action string.
		String actionCommand = evt.getActionCommand();

		/**
		 * Actions fired from the context menu.
		 */
		// The solution is selected for playing.
		if(actionCommand.equals("takeSolutionAsHistory")) {

			// Get the selected solution.
			Solution solution = getSelectedValue();

			// The user must have selected a solution otherwise a message is displayed.
			if(solution == null) {
				showNoSolutionsSelected();
				return;
			}

			// Set the selected solution as new history in the game.
			application.takeSolutionForHistory(solution);

			return;
		}

		// A solution is to be imported from the clipboard.
		if(actionCommand.equals("importSolution")) {

			// The imported solution.
			String newSolutionLURD = Utilities.getStringFromClipboard();

			if(newSolutionLURD == null) {
				MessageDialogs.showFailureTextKey(getParent(), "noValidSolutionFound");
				return;
			}

			// Create a new solution object.
			Solution solution = new Solution(newSolutionLURD);

			// Set the name of the solution.
			solution.name = Texts.getText("solution.importedFromClipboard");

			// Add the solution (to this object and store it on the database)
			SolutionType solutionType = level.addSolution(solution);

			// Give feedback to the user
			switch(solutionType) {

				case DUPLICATE_SOLUTION:
					MessageDialogs.showInfoNoteString(getParent(), Texts.getText("solutionIsADuplicate")+".");

					// Mark the solution so the user can see the duplicate.
					setSelectedValue(solution, true);
					break;

				case INVALID_SOLUTION:
					MessageDialogs.showFailureTextKey(getParent(), "noValidSolutionFound");
					break;

				case NEW_FIRST_SOLUTION:
					// No congratulations for the first solution.
					break;

				case NEW_BEST_SOLUTION:
					MessageDialogs.showCongratsTextKey(getParent(), "newBestSolution");
					break;

				case NEW_BEST_PUSHES_SOLUTION:
					MessageDialogs.showCongratsTextKey(getParent(), "newBestPushesSolution");
					break;

				case NEW_BEST_MOVES_SOLUTION:
					MessageDialogs.showCongratsTextKey(getParent(), "newBestMovesSolution");
					break;

				case NEW_SOLUTION:
					break;
			}

			return;
		}

		// A solution is to be exported to the clipboard.
		if(actionCommand.equals("copySolutionToClipBoard")) {

			// Get the indices of all selected solutions.
			int[] selectedIndices = getSelectedIndices();

			// The user must have selected a solution otherwise a message is displayed.
			if(selectedIndices.length == 0) {
				showNoSolutionsSelected();
				return;
			}

			// Create a string buffer containing the lurd-representations of the selected solutions.
			StringBuilder lurds = new StringBuilder();
			for(int index : selectedIndices) {
				lurds.append(getSolution(index).lurd);
				lurds.append("\n\n");
			}

			// Copy the LURD representation of the solutions to the clipboard.
			Utilities.putStringToClipboard(lurds.toString(), this);
			return;
		}

		// A solution is to be exported to the clipboard in run length format.
		if(actionCommand.equals("copySolutionToClipBoardRLE")) {

			// Get the indices of all selected solutions.
			int[] selectedIndices = getSelectedIndices();

			// The user must have selected a solution otherwise a message is displayed.
			if(selectedIndices.length == 0) {
				showNoSolutionsSelected();
				return;
			}

			// Create a string buffer containing the lurd-representations of the selected solutions.
			StringBuilder lurds = new StringBuilder();
			for(int index : selectedIndices) {
				lurds.append(RunLengthFormat.runLengthEncode(getSolution(index).lurd));
				lurds.append("\n\n");
			}

			// Copy the LURD representation of the solutions to the clipboard.
			Utilities.putStringToClipboard(lurds.toString(), this);
			return;
		}

		// Displays the lurd representation of the selected solution.
		if(actionCommand.equals("showLURD")) {

			// Get the selected solution.
			Solution solution = getSelectedValue();

			// The user must have selected a solution otherwise a message is displayed.
			if(solution == null) {
				showNoSolutionsSelected();
				return;
			}

			// Show the lurd representation.
			application.applicationGUI.showPopupTextarea("LURD: ", solution.lurd);
		}

		// All selected solutions are removed from the list (but not deleted from the level!).
		if(actionCommand.equals("removeFromList")) {

			// Get the indices of all selected solutions.
			int[] selectedIndices = getSelectedIndices();

			// The user must have selected a solution otherwise a message is displayed.
			if(selectedIndices.length == 0) {
				showNoSolutionsSelected();
				return;
			}

			// Remove all selected solutions.
			for(int index=selectedIndices.length; --index != -1; ) {
				Solution removedSolution = listModel.remove(selectedIndices[index]);
				hiddenSolutions.add(removedSolution);
			}

			// Determine the new best solutions of the displayed solutions.
			determineBestSolutions(null);

			// At least one solution has been removed from the GUI.
			// Therefore enable the button to show all solutions again.
			showRemovedSolutionsAgain.setEnabled(true);

			// Update the UI. Otherwise getPreferredSize() returns a wrong size
			// if there is only one solution in the list.
			if(listModel.getSize() == 1) {
				updateUI();
			}

			return;
		}

		// All removed solutions are shown in the GUI again.
		if(actionCommand.equals("showRemovedSolutions")) {

			while(hiddenSolutions.size() > 0) {

				Solution hiddenSolution = hiddenSolutions.remove(0);

				// Add the solution according to its order value.
				listModel.add(getOrderValuePosition(hiddenSolution), hiddenSolution);

				// Determine the best solutions.
				determineBestSolutions(null);
			}

			// All solutions are shown again.
			// Hence, disable the button for showing all solutions.
			showRemovedSolutionsAgain.setEnabled(false);

			return;
		}

		// Delete all selected solutions.
		if(actionCommand.equals("deleteSolutions")) {

			// Get the indices of all selected solutions.
			int[] selectedIndices = getSelectedIndices();

			// The user must have selected a solution otherwise a message is displayed.
			if(selectedIndices.length == 0) {
				showNoSolutionsSelected();
				return;
			}

			// Let the user confirm the deletion.
			if(JOptionPane.showConfirmDialog(getParent(), Texts.getText("reallyDelete"), Texts.getText("question"), JOptionPane.YES_NO_OPTION) == JOptionPane.NO_OPTION) {
				return;
			}

			// Delete all selected rows. Start at the end so the indices of the other
			// solutions aren't changed when a solution is deleted.
			for(int index = selectedIndices.length; --index != -1; ) {
				solutionsManager.deleteSolution(getSolution(selectedIndices[index]));
			}

			// Determine the new best solutions of the displayed solutions.
			determineBestSolutions(null);

			// Update the UI. Otherwise getPreferredSize returns a wrong size
			// if there is only one solution in the list.
			if(listModel.getSize() == 1) {
				updateUI();
			}
		}

		// Sort solutions by moves then by pushes.
		if(actionCommand.equals("sortMovesPushes")) {
			sortSolutions(Sorting.MOVES_PUSHES_SORTING);
			repaint();
			return;
		}

		// Sort solutions by pushes then by moves.
		if(actionCommand.equals("sortPushesMoves")) {
			sortSolutions(Sorting.PUSHES_MOVES_SORTING);
			repaint();
			return;
		}
		// FFS/hm: more sorting options to be added

		// Put the best moves and best pushes solution at the top of the list.
		if(actionCommand.equals("bestSolutionsOnTop")) {
			moveBestSolutionsToTopOfList();
			repaint();
			return;
		}


		/**
		 * Actions from the GUI elements.
		 */
		// If the pin button has been pressed change the pinned status.
		if(actionCommand.equals("pinView")) {
			isViewPinned = !isViewPinned;

			// Save the new value in the settings for restoring this value
			// for the next time the program is started.
			// Currently this class is used in more than one views. However, the docking
			// version is only used for the main game view. Hence, there needn't to be
			// a check whether this is the main solutionsGUI of the program.
			Settings.set("isSolutionViewPinned", Boolean.toString(isViewPinned));

			// Depending on the pin status set a brighter icon or the original icon.
			if(isViewPinned) {
				pinViewButton.setIcon(Utilities.getIcon("pin (oxygen).png", "pin view"));
			} else {
				pinViewButton.setIcon(GraphicUtilities.createBrigtherIconImage(pinViewButton.getIcon(), 50));
			}

			return;
		}

		// If the button for opening the solutions view has been pressed then show the solutions view.
		if(actionCommand.equals("openView")) {
			pinablePanel.setVisible(true);
			openSolutionsViewButton.setVisible(false);
			return;
		}
	}


	@Override
	public void solutionEventFired(SolutionEvent event) {

		/**
		 * All coming actions have been fired by the model (that is: from class "Solutions").
		 */
		// A solution has been added to the model. It has to be added in this GUI, too.
		if(event.getEventAction() == SolutionEvent.EventAction.INSERT) {

			Solution solution = event.getSolution();

			// If the solution to be added isn't "marked" as removed, then add it as
			// new solution. This can only happen if the user deleted the solution
			// in the database and opened the database again or the solutions menu.
			// The reason is that the deletion of a solution in the database doesn't check
			// whether the level the solution belongs to is currently loaded for playing
			// in the game. Hence, at this time the solutions are "out of sync" with the
			// database. However, the other way works: if a solution is added/deleted
			// in a solution GUI for a level having a database ID the corresponding data
			// on the database are updated accordingly.
			if(!hiddenSolutions.contains(solution)) {
				addSolution(solution, true);
			}

			return;
		}

		// A solution has been deleted from the model. Hence delete it from this GUI, too.
		if(event.getEventAction() == SolutionEvent.EventAction.DELETE) {
			listModel.removeElement(event.getSolution());

			// Determine the new best solutions of the displayed solutions.
			determineBestSolutions(null);

			// Update the UI. Otherwise getPreferredSize returns a wrong size
			// if there is only one solution in the list.
			if(listModel.getSize() == 1) {
				updateUI();
			}

			return;
		}

		// All solutions have been deleted from the model. Hence, remove them from this GUI, too.
//		if(event.getEventAction() == SolutionEvent.EventAction.DELETE_ALL) {
//			listModel.removeAllElements();
//
//			// No best solutions available.
//			bestPushesSolution = null;
//			bestMovesSolution  = null;
//
//			// No solutions to be highlighted anymore.
//			solutionHighlighterThread.removeAllSolutionsToBeHighlighted();
//
//			// No solutions are hidden anymore.
//			hiddenSolutions.clear();
//
//			return;
//		}

	}


	/**
	 * Moves the best moves and best pushes solution to the start of the list.
	 */
	void moveBestSolutionsToTopOfList() {

		// Reordering will invalidate the selection. Hence, remove the selection.
		clearSelection();

		// Move the best solutions to the beginning of the list.
		if(bestPushesSolution != null) {
			listModel.removeElement(bestPushesSolution);
			listModel.add(0, bestPushesSolution);
		}
		if(bestMovesSolution != null && !bestMovesSolution.equals(bestPushesSolution)) {
			listModel.removeElement(bestMovesSolution);
			listModel.add(0, bestMovesSolution);
		}

		// Every solution holds its position in the level in an own variable.
		// These variables have to be updates now.
		updateSolutionOrderValues();
	}

	/**
	 * Updates the current order of the solutions.
	 * <p>
	 * The solutions have an internal order value representing the position
	 * in the list.
	 * If solutions are moved or added this value must be adjusted.
	 */
	protected void updateSolutionOrderValues() {

		// Flag, indicating whether the order of the solutions has changed.
		boolean isReordered = false;

		int lowestPossibleOrderValue = -1;

		for(int index=0; index < listModel.getSize(); index++) {
			Solution solution = getSolution(index);

			// This solution must have at least this order value.
			lowestPossibleOrderValue++;

			// Check whether the solution must get a new order value.
			if( solution.orderValue < lowestPossibleOrderValue ) {
				solution.orderValue = lowestPossibleOrderValue;

				isReordered = true;

				// Check whether the solution has to be updated in the database.
				// This has only to be done if this is the main view.
				if(isMainSolutionView == true && solution.databaseID != Database.NO_ID) {
					application.levelIO.database.updateSolution(solution);
				}
			} else {
				lowestPossibleOrderValue = solution.orderValue;
			}
		}

		// If the order of the solutions has changed => the main solutions object
		// has to be reordered, too.
		// (This list is only the GUI having the solutions stored in an own model).
		// However, only reordering the solutions in the main view is saved.
		// Other views (like the solutions list in the optimizer) don't influence
		// the main solutions object.
		if(isMainSolutionView && isReordered) {
			solutionsManager.sortAccordingOrderValue();
		}
	}

	/**
	 * Determines the best pushes and the best moves solution and stores them
	 * in the corresponding class variables.
	 * <p>
	 * This method must always be called when the model changes to ensure
	 * the best solutions are always referenced.
	 */
	private void determineBestSolutions(Solution addedSolution) {

		// If a new solution has been added just this solution is checked
		// for being new best solution.
		if(addedSolution != null) {

			if(bestPushesSolution == null || addedSolution.isBetterPushesSolutionThan(bestPushesSolution)) {
				bestPushesSolution = addedSolution;
			}
			if(bestMovesSolution == null || addedSolution.isBetterMovesSolutionThan(bestMovesSolution)) {
				bestMovesSolution = addedSolution;
			}

			return;
		}

		// A solution has been removed or multiple solutions have been added
		// -> determine the new best solutions.
		bestPushesSolution = null;
		bestMovesSolution  = null;

		// We iterate forwards to favor old solutions, as elsewhere
		final int siz = listModel.getSize();
		for(int index=0 ; index < siz ; ++index ) {
			Solution solution = listModel.get(index);

			if(bestPushesSolution == null || solution.isBetterPushesSolutionThan(bestPushesSolution)) {
				bestPushesSolution = solution;
			}
			if(bestMovesSolution == null || solution.isBetterMovesSolutionThan(bestMovesSolution)) {
				bestMovesSolution = solution;
			}
		}
	}

	/**
	 * Search the position of the solution in the list according to its order value.
	 *
	 * @param solution the solution to search the insert position for
	 * @return the position of the solution
	 */
	int getOrderValuePosition(Solution solution) {

		int low  = 0;
		int high = listModel.size() - 1;

		// Perform a binary search to calculate the position for the solution.
		while(low <= high) {
			int middle = low + ((high - low) >>> 1);

			if(getSolution(middle).orderValue < solution.orderValue) {
				low  = middle + 1;
			} else if(getSolution(middle).orderValue > solution.orderValue) {
				high = middle - 1;
			} else {
				return middle;
			}
		}

		// Return the index the solution must be inserted to preserve the sorting order.
		return low;
	}

	/**
	 * This list has changed its size. The scroll pane shouldn't be horizontally scrollable.
	 * Hence, it has to be resized corresponding to this JList.
	 */
	void resizeGUI() {

		// Only if there is a scroll pane it can be resized (only the dockable view has this).
		if(solutionsListScrollPane == null) {
			return;
		}

		// Get the size the solutions list needs for showing itself.
		Dimension preferredViewportSizeOfList = getPreferredScrollableViewportSize();

		// If the list is empty set a minimum size of 128 pixel (otherwise the list default is 256).
		// Furthermore ensure a minimum size of 150 pixel.
		if(listModel.getSize() == 0 || preferredViewportSizeOfList.width < 150) {
			preferredViewportSizeOfList.width = 150;
		}
//System.out.printf("\nsize: "+preferredViewportSizeOfList);
//System.out.printf("\ncurrent size: "+solutionsListScrollPane.getSize());
//System.out.printf("\nviewport size: "+solutionsListScrollPane.getViewport().getSize());

		// Set the view port size to the requested size of this solution list.
		// This ensures the whole texts of the solutions is shown.
		solutionsListScrollPane.getViewport().setPreferredSize(preferredViewportSizeOfList);

		// Ensure the panel sizes are adjusted
		application.applicationGUI.revalidate();
	}

	/**
	 * Sets the visibility of the "take solution as history" menu item.
	 *
	 * @param isVisible <code>true</code> sets the "take solution as history" menu item visible
	 * 				   <code>false</code> sets the "take solution as history" menu item invisible
	 */
	public void setTakeSolutionAsHistoryVisible(boolean isVisible) {
		isTakeSolutionAsHistoryEnabled = isVisible;
	}

	@Override
	public void lostOwnership(Clipboard clipboard, Transferable contents) {}

	/**
	 * A <code>Thread</code> for highlighting solutions that have been added to this list
	 * or changed their index in this list.
	 */
	private class HighlighterThread extends Thread {

		// Solutions to be highlighted. Key = solution to be highlighted,
		// value = Counter for how much time the highlighting is done.
		private Hashtable<Solution, Integer> highlightedSolutions = new Hashtable<Solution, Integer>();

		/**
		 * Creates a <code>Thread</code> for highlighting solutions.
		 */
		public HighlighterThread() {}

		/**
		 * Add the passed solution as solution to be highlighted.
		 *
		 * @param solution <code>Solution</code> to be highlighted
		 */
		public void addSolutionToBeHighlighted(Solution solution) {

			// Inform this thread that new solutions are to be highlighted.
			synchronized(this) {
				highlightedSolutions.put(solution, 10);
				notify();
			}
		}

		/**
		 * Removes all solutions to be highlighted.
		 */
		public void removeAllSolutionsToBeHighlighted() {
			highlightedSolutions.clear();
		}

		/**
		 * Returns the intensity the passed solution is to be highlighted.
		 *
		 * @param solution the <code>Solution</code> whose highlight intensity is returned
		 * @return the highlight intensity or null if the passed solution isn't to be highlighted
		 */
		public Integer getHighlightIntensity(Solution solution) {
			return highlightedSolutions.get(solution);
		}

		@Override
		public void run() {

			while(true) {
				try {
					// If there are no more solutions to be highlighted wait for new solutions.
					synchronized(Thread.currentThread()) {
						if(highlightedSolutions.size() == 0) {
							Thread.currentThread().wait();
						}
					}

					// Get every solution which is to be highlighted.
					for(Enumeration<Solution> e = highlightedSolutions.keys(); e.hasMoreElements(); ) {

						Solution solution = e.nextElement();

						// Get the counter of the solution.
						Integer i = highlightedSolutions.get(solution);

						// Decrease the counter of the solution.
						highlightedSolutions.put(solution, --i);

						// If the counter is 0 the solution can be removed.
						if(i == 0) {
							highlightedSolutions.remove(solution);
						}
					}

					// Redraw the new status of the solution. Every time the counter is changed the
					// ListCellRenderer draws the solution in a different color.
					SwingUtilities.invokeAndWait(new Runnable() {
						@Override
						public void run() {
							repaint();
						}
					});

					Thread.sleep(90);
				} catch (Exception e1) {e1.printStackTrace();}
			}
		}
	}


	/**
	 * Transfer handler for this JList.
	 */
	protected class ListTransferHandler extends TransferHandler {

		@Override
		public boolean importData(javax.swing.TransferHandler.TransferSupport support) {

			// Check if this really is a drop.
			if (support.isDrop() == false) {
				return false;
			}

			// Get the index of the drop in the list.
			JList.DropLocation dl = (JList.DropLocation) support.getDropLocation();
			int dropIndex = dl.getIndex();

			// Get the list and its model.
			JList<Object> list = (JList<Object>) support.getComponent();
			DefaultListModel<Object> listModel = (DefaultListModel<Object>)list.getModel();

			// Get the dragged data.
			Object[] draggedData = new Object[0];
			try {
				draggedData = ((TransferableData) support.getTransferable().getTransferData(dataFlavor)).dragData;
			} catch (Exception e) {
				e.printStackTrace();
				return false;
			}

			// Move the dragged data in the list.
			for(int i=draggedData.length; --i != -1; ) {

				Object data = draggedData[i];

				// Get the current index of the data in the list.
				int index = listModel.indexOf(data);

				// Remove the data.
				listModel.remove(index);

				// If the old data index is lower than the drop index the drop index
				// has to be adjusted.
				if(index < dropIndex) {
					dropIndex--;
				}

				// Add the data at the drop index.
				listModel.add(dropIndex, data);

				// All dropped solutions are to be highlighted so the user can see
				// which solutions have been dropped.
				solutionHighlighterThread.addSolutionToBeHighlighted((Solution) data);
			}

			return true;
		}

		@Override
		protected void exportDone(JComponent source, Transferable data, int action) {
			super.exportDone(source, data, action);

			// After moving the data the selection is cleared.
			clearSelection();

			// Update the order values in the solutions because some solutions have been
			// moved to other positions.
			updateSolutionOrderValues();

			// The drop has ended. The hidden solutions are to be hidden again.
			for(Solution solution : hiddenSolutions) {
				listModel.remove(getOrderValuePosition(solution));
			}
		}


		@Override
		public int getSourceActions(JComponent c){
			return TransferHandler.MOVE;
		}

		@Override
		protected Transferable createTransferable(JComponent c) {

			// Add all hidden solutions according to their order value so the user
			// can see them while dragging.
			for(Solution solution : hiddenSolutions) {
				listModel.add(getOrderValuePosition(solution), solution);
			}

			return new TransferableData(((JList<?>)c).getSelectedValues());
		}

		@Override
		public boolean canImport(javax.swing.TransferHandler.TransferSupport support) {
			if( !support.isDrop() || !support.isDataFlavorSupported(dataFlavor)) {
				return false;
			}

			return true;
		}
	}

	/**
	 * The data to be transfered by a drag&drop within this list.
	 */
	protected class TransferableData implements Transferable {

		// The dragged data.
		private Object[] dragData;

		protected TransferableData(Object[] d ){
			dragData = d;
		}

		@Override
		public Object getTransferData(DataFlavor flavor) throws UnsupportedFlavorException, IOException {
			if (flavor.equals(dataFlavor)) {
				return TransferableData.this;
			}
			return null;
		}

		@Override
		public DataFlavor[] getTransferDataFlavors() {
			return supportedFlavors;
		}

		@Override
		public boolean isDataFlavorSupported(DataFlavor flavor) {
			return true;
		}
	}
}