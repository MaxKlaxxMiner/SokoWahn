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
package de.sokoban_online.jsoko.gui;

import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Component;
import java.awt.Cursor;
import java.awt.Dimension;
import java.awt.FileDialog;
import java.awt.GradientPaint;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.GraphicsEnvironment;
import java.awt.Rectangle;
import java.awt.RenderingHints;
import java.awt.Toolkit;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.FocusAdapter;
import java.awt.event.FocusEvent;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.awt.event.MouseEvent;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.awt.geom.AffineTransform;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FilenameFilter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Hashtable;
import java.util.List;
import java.util.Locale;
import java.util.Properties;
import java.util.TreeSet;

import javax.help.CSH;
import javax.swing.AbstractAction;
import javax.swing.Action;
import javax.swing.ActionMap;
import javax.swing.ButtonGroup;
import javax.swing.ImageIcon;
import javax.swing.InputMap;
import javax.swing.JButton;
import javax.swing.JCheckBoxMenuItem;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JDialog;
import javax.swing.JFileChooser;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JRadioButtonMenuItem;
import javax.swing.JScrollPane;
import javax.swing.JSeparator;
import javax.swing.JSlider;
import javax.swing.JTextArea;
import javax.swing.JToggleButton.ToggleButtonModel;
import javax.swing.JToolBar;
import javax.swing.KeyStroke;
import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;
import javax.swing.UIManager;
import javax.swing.UIManager.LookAndFeelInfo;
import javax.swing.UnsupportedLookAndFeelException;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;
import javax.swing.event.EventListenerList;
import javax.swing.event.MenuEvent;
import javax.swing.event.MenuListener;
import javax.swing.event.PopupMenuEvent;
import javax.swing.event.PopupMenuListener;
import javax.swing.filechooser.FileFilter;
import javax.swing.plaf.basic.BasicSliderUI;

import com.jidesoft.list.StyledListCellRenderer;
import com.jidesoft.plaf.LookAndFeelFactory;
import com.jidesoft.swing.ComboBoxSearchable;
import com.jidesoft.swing.JideSplitButton;
import com.nilo.plaf.nimrod.NimRODLookAndFeel;
import com.nilo.plaf.nimrod.NimRODMain;
import com.nilo.plaf.nimrod.NimRODTheme;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.leveldata.Database;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.leveldata.SelectableLevelCollectionComboBoxModel;
import de.sokoban_online.jsoko.leveldata.SelectableLevelCollectionComboBoxModel.SelectableLevelCollection;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsGUI;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.translator.Translator;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.USpinner;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific.OSType;
import de.sokoban_online.jsoko.workInProgress.newSettingsPanel.SettingsFrameNew;


/**
 * This class paints the main panel containing the board and the status bar.
 * It also handles some of the events regarding the graphical output
 * (e.g. rotating the display of the board).
 */

@SuppressWarnings("serial")
public final class GUI extends JPanel implements ActionListener {

	/** Reference to the main object of this program. */
	JSoko application;

	/** The panel showing the board graphics, the info bar and the editor elements. */
	public MainBoardDisplay mainBoardDisplay;

	/** The tool bar of the program. */
	JToolBar toolBar;

	/** The menu bar of this program. */
	JMenuBar menuBar;

	/** JComboBox holding all available collections. */
	private JComboBox<SelectableLevelCollection> collectionsComboBox;

	/** JComboBox holding all levels of the currently played collection. */
	private JComboBox<Level> levelsComboBox;

	/**
	 * A reference to the menu item for the editor.  We need it to set it inactive
	 * in case the level is invalid.
	 */
	private JMenuItem editorMenuItem;

	/** ArrayList collecting all the Objects, which shall be active in play mode, only. */
	ArrayList<Object> playModeDependentObjects = new ArrayList<Object>();

	/**
	 * ArrayList collecting all Objects which are to be activated when the editor mode is activated.
	 */
	ArrayList<JMenuItem> editorModeDependentObjects = new ArrayList<JMenuItem>();

	/** ArrayList containing all objects that have to be disabled when the editor is activated */
	ArrayList<Object> solverModeDependentObjects = new ArrayList<Object>();

	/** ArrayList containing all objects that are relevant for the debug mode.
	 *  These objects are only visible in debug mode.
	 */
	private ArrayList<Component> debugModeDependentObjects = new ArrayList<Component>();

	/** ArrayList containing all objects that must be disabled if the current level is invalid */
	private ArrayList<Object> invalidLevelModeDependentObjects = new ArrayList<Object>();

	/** ArrayList containing the objects for undo functionality. */
	ArrayList<Object> undoActions = new ArrayList<Object>();

	/** ArrayList containing the objects for redo functionality. */
	ArrayList<Object> redoActions = new ArrayList<Object>();

    /** A list of event listeners for this component. */
    private EventListenerList listenerList = new EventListenerList();

	/** JPanel for displaying the solutions of the current level.
	 *  This panel is displayed next to the main level display.
	 */
	SolutionsGUI solutionsGUI = null;


	/**
	 * A reference to the menu entry "Save level".
	 * We need this reference to deactivate this menu entry, in case
	 * the current level has not yet been stored with an associated name.
	 */
	private JMenuItem saveLevelMenuItem;

	/**
	 * Button for showing important information in the GUI.
	 * This button is directly inserted into the menu of JSoko.
	 */
	private JButton infoButton;

	/**
	 * Popup for displaying information.
	 */
	private JPopupMenu popup;

	/** <code>JCheckBox</code> indicating whether the simple deadlock detection is enabled. */
	public JCheckBoxMenuItem detectSimpleDeadlocks;

	/** <code>JCheckBox</code> indicating whether the freeze deadlock detection is enabled. */
	public JCheckBoxMenuItem detectFreezeDeadlocks;

	/** <code>JCheckBox</code> indicating whether the bipartite deadlock detection is enabled. */
	public JCheckBoxMenuItem detectBipartiteDeadlocks;

	/** <code>JCheckBox</code> indicating whether the closed diagonal deadlock detection is enabled. */
	public JCheckBoxMenuItem detectClosedDiagonalDeadlocks;


	/**
	 * Creates a new object for displaying the GUI of this program.
	 *
	 * @param application reference to the main Object which holds all references
	 *                             of all loaded levels
	 */
	public GUI(JSoko application) {

		this.application = application;

		// Set the border layout for this GUI.
		setLayout(new BorderLayout());

		// Add the display of the board.
		mainBoardDisplay = new MainBoardDisplay(application);
		add(mainBoardDisplay, BorderLayout.CENTER);

		// Create a GUI for displaying the solutions and save the reference.
		// Then add add a docked version of the GUI at the left.
		solutionsGUI = new SolutionsGUI(application, true, true);
		add(solutionsGUI.getDockingGUI(mainBoardDisplay), BorderLayout.WEST);

		// Create a tool bar for this program.
		createToolBar();

		add(toolBar, BorderLayout.NORTH);

		application.setJMenuBar(createMenuBar());

		setKeyActions();
	}

	/**
	 * Creates a button for the tool bar.
	 *
	 * @param iconName		the name of the icon for the button
	 * @param actionCommand the action command of the button
	 * @param toolTipText 	the tool tip text for the button
	 * @return the created button
	 * @see #createToolBarButtonByKey(String, String, String)
	 */
	final private JButton createToolBarButton(String iconName, String actionCommand, String toolTipText) {

		// Create and initialize the button.
		JButton button = new JButton(Utilities.getIcon(iconName, null));
		button.setActionCommand(actionCommand);
		button.setToolTipText(toolTipText);
		button.addActionListener(this);

		// The buttons must be clicked with the mouse.
		button.setFocusable(false);

		return button;
	}

	/**
	 * Creates a button for the tool bar.
	 *
	 * @param iconName       the name of the icon for the button
	 * @param actionCommand  the action command of the button
	 * @param toolTipTextKey the text key for the tool tip text for the button
	 * @return the created button
	 * @see #createToolBarButton(String, String, String)
	 */
	final private JButton createToolBarButtonByKey(String iconName, String actionCommand, String toolTipTextKey) {
		String toolTipText = Texts.getText(toolTipTextKey);
		return createToolBarButton(iconName, actionCommand, toolTipText);
	}

	/**
	 * Creates a tool bar for this program.
	 */
	final private void createToolBar() {

		// Create a tool bar if there isn't already one.
		if (toolBar == null) {
			toolBar = new JToolBar(Texts.getText("toolbar"));
			toolBar.setFocusable(false);
			toolBar.setRollover(true);
		} else {
			toolBar.removeAll();
		}

		// Add the buttons to the tool bar.
		toolBar.add(createToolBarButtonByKey("document-open.png", "loadIndividualLevel", "toolbarButton.loadlevel"));

		toolBar.addSeparator();

		// Add the buttons to the tool bar.
		toolBar.add(createToolBarButtonByKey("go-previous.png", "previousLevel", "previouslevel"));

		// Add the buttons to the tool bar.
		toolBar.add(createToolBarButtonByKey("go-next.png", "nextLevel", "nextlevel"));

		// Add the "open Graphical Level Browser" button to the tool bar
		toolBar.add(createToolBarButtonByKey("find (oxygen).png", "openGraphicalLevelBrowser", "graphicalLevelBrowser.open"));

		toolBar.addSeparator();

		// Add the undo/redo buttons to the tool bar.
		JButton button;

		button = createToolBarButtonByKey("edit-undo-all.png", "undoAll", "undoAll");
		undoActions.add(button);
		toolBar.add(button);

		button = createToolBarButtonByKey("edit-undo.png", "undo", "undo");
		undoActions.add(button);
		toolBar.add(button);

		// Button for automatic replay.
		final JideSplitButton replayButton = getReplayButton();
		redoActions.add(replayButton);
		toolBar.add(replayButton);

		button = createToolBarButtonByKey("edit-redo.png", "redo", "redo");
		redoActions.add(button);
		toolBar.add(button);

		button = createToolBarButtonByKey("edit-redo-all.png", "redoAll", "redoAll");
		redoActions.add(button);
		toolBar.add(button);

		toolBar.addSeparator();

		// Combo box for selecting the collection to be played.
		collectionsComboBox = getLevelCollectionCombobox();
		toolBar.add(collectionsComboBox);

		// Combo box for selecting the level to be played.
		levelsComboBox = getLevelsComboBox();
		toolBar.add(levelsComboBox);

		toolBar.addSeparator();

		// Button showing the current transformation status.
		toolBar.add(getTransformationButton());

		toolBar.addSeparator();

		// Button for switching between moves and pushes optimized path finding.
		toolBar.add(getPathFindingMethodSelector());

		// All components of the the tool bar must be disabled while the solver
		// is running or the editor is activated.
		// For the editor this is important because the checks whether the
		// level has been transformed are made when the editor has been left.
		// If the user loads a new level while the editor is still activated
		// the program wouldn't recognize changes of the level due to
		// transformations. However, the redo and undo buttons must be
		// handled separately because their status depends on the history.
		for (Component component : toolBar.getComponents()) {
			// The undo and the redo buttons are handled separately,
			// because their status also depends on the history.
			if (component instanceof JButton) {
				String actionCommand = ((JButton) component).getActionCommand();
				if (actionCommand.indexOf("undo") != -1 || actionCommand.indexOf("redo") != -1) {
					continue;
				}
			}
			solverModeDependentObjects.add(component);
			// must be set inactive when editor is activated
			playModeDependentObjects.add(component);
		}
	}

	/**
	 * Returns a {@code JButton} for selecting the method for
	 * path finding.
	 * <p>
	 * The player moves are either optimized for moves or for pushes.<br>
	 * This button allows the user to switch between these to methods.
	 *
	 * @return the button
	 */
	private JButton getPathFindingMethodSelector() {

		JButton button = new JButton(Settings.movesVSPushes == 0 ? Texts.getText("moves") : Texts.getText("pushes"));
		button.setToolTipText(Texts.getText("toolbarButton.pathfindingToolTip"));
		button.setFocusable(false);
		button.setActionCommand("pathfindingOptimization");
		button.addActionListener(this);

		return button;
	}

	/**
	 * Returns the button for the replay functionality.
	 *
	 * @return the replay button
	 */
	private JideSplitButton getReplayButton() {

		final JideSplitButton replayButton = new JideSplitButton(Utilities.getIcon("1rightarrow (oxygen).png", null));
		replayButton.setActionCommand("replay");
		replayButton.setToolTipText(Texts.getText("replay"));
		replayButton.addActionListener(this);
		replayButton.setFocusable(false);
		final ReplaySpeedSlider replaySpeedSlider = new ReplaySpeedSlider(0, 250, 250 - Settings.delayValueUndoRedo);
		replaySpeedSlider.addChangeListener(new ChangeListener() {
			@Override
			public void stateChanged(ChangeEvent e) {
				Settings.delayValueUndoRedo = (short) (250 - replaySpeedSlider.getValue());
			}
		});
		// The initial value has to be set every time the slider is
		// set visible because it may have been change by the user.
		replayButton.addMenuListener(new MenuListener() {
			@Override
			public void menuSelected(MenuEvent e) {
				replaySpeedSlider.setValue(250 - Settings.delayValueUndoRedo);
			}
			@Override
			public void menuDeselected(MenuEvent e) {}
			@Override
			public void menuCanceled(MenuEvent e) {}
		});
		replayButton.add(replaySpeedSlider);

		return replayButton;
	}

	/**
	 * Returns a {@code JComboBox} the user can select the level
	 * to be played from.
	 *
	 * @return the created {@code JComboBox}
	 */
	private JComboBox<Level> getLevelsComboBox() {

		// Add a comboBox for quick selection of a specific level.
		// The focus must stay on the main panel for the key events. The user must use the mouse here.
		final JComboBox<Level> levelsComboBox = new JComboBox<Level>();
		final ComboBoxSearchable searchable = new ComboBoxSearchable(levelsComboBox) {
			@Override
			protected String convertElementToString(Object object) {
				Level level = (Level) object;
				return level.getNumber()+" - "+level.getTitle();
			}
		};
		searchable.setSearchingDelay(10);

		final Level[] selectedLevelWhenPopupBecameVisible = new Level[1];
		levelsComboBox.setActionCommand("loadLevelComboBox");
		levelsComboBox.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(final ActionEvent e) {
				// Only fire when popup is visible that is: not JSoko itself
				// has changed the items but the user using the popup.
				if(levelsComboBox.isPopupVisible()) {
					SwingUtilities.invokeLater(new Runnable() {
						@Override
						public void run() {
							// Only fire event when the user has finally selected a level.
							// Otherwise typing letters fires events, too, although the searchable
							// functionality just searches corresponding items.
							if(!levelsComboBox.isPopupVisible()) {
								mainBoardDisplay.requestFocusInWindow(); // Set focus back so the user can immediately use the arrow keys for playing
								selectedLevelWhenPopupBecameVisible[0] = null; // user has selected a new level, hence don't set back the previous level
								GUI.this.actionPerformed(e);
							}
						}
					});
				}
			}
		});

		levelsComboBox.addFocusListener(new FocusAdapter() {
			@Override
			public void focusGained(FocusEvent e) {
				// Avoid that the user can use page down / page up
				// because that way the selected item could be changed without a popup.
				// If the user cancels the popup the focus is removed from the combo box!
				levelsComboBox.setPopupVisible(true);
			}
		});

		levelsComboBox.addPopupMenuListener(new PopupMenuListener() {

			@Override
			public void popupMenuWillBecomeVisible(PopupMenuEvent e) {
				GUI.this.setEnabled(false);  // the user might use the searchable
				menuBar.setEnabled(false);   // feature by pressing keys which shouldn't be handled anywhere else

				selectedLevelWhenPopupBecameVisible[0] = (Level) levelsComboBox.getSelectedItem();
			}

			@Override
			public void popupMenuWillBecomeInvisible(PopupMenuEvent e) {
				// The user may have changed the selected level by using keys. "selectedLevelWhen..." is null,
				// when the user has really selected a new level by pressing enter. Otherwise the remembered level is set again.
				SwingUtilities.invokeLater(new Runnable() {
					@Override
					public void run() {
						if(selectedLevelWhenPopupBecameVisible[0] != null) {
							levelsComboBox.setSelectedItem(selectedLevelWhenPopupBecameVisible[0]);
						}
						menuBar.setEnabled(true);	// enable key actions for other
						GUI.this.setEnabled(true);  // actions again
					}
				});
			}

			@Override
			public void popupMenuCanceled(PopupMenuEvent e) {
				// Set the focus back to the board so the user can play using the arrow keys
				// and key events are handled normally (like pressing "l" for opening the level collections combo box).
				mainBoardDisplay.requestFocusInWindow();
			}
		});

		levelsComboBox.setRenderer(new StyledListCellRenderer() {
			@Override
			protected void customizeStyledLabel(JList list, Object value, int index, boolean isSelected,  boolean cellHasFocus) {
				super.customizeStyledLabel(list, value, index, isSelected, cellHasFocus);
				setHorizontalTextPosition(SwingConstants.LEFT);
				Level level = (Level) value;
				if (level.getSolutionsManager().getSolutionCount() > 0) {
					setIcon(Utilities.getIcon("apply (oxygen).png", ""));
				}
				String levelString = " " + level.getNumber() + " - " + Utilities.clipToEllipsis(level.getTitle(), 50);
				setText(levelString);
				setToolTipText(level.getTitle());
			}
		});

		return levelsComboBox;
	}

	/**
	 * Returns a {@code JComboBox} the user can select the level collection
	 * to be played from.
	 *
	 * @return the created {@code JComboBox}
	 */
	private JComboBox<SelectableLevelCollection> getLevelCollectionCombobox() {

		final JComboBox<SelectableLevelCollection> levelCollectionsComboBox = new JComboBox<SelectableLevelCollection>(application.getSelectableLevelCollectionsModel());
		ComboBoxSearchable searchable = new ComboBoxSearchable(levelCollectionsComboBox) {
			@Override
			protected String convertElementToString(Object object) {
				return ((SelectableLevelCollection) object).title;
			}
		};
		searchable.setSearchingDelay(10);

		final SelectableLevelCollection[] selectedCollectionWhenPopupBecameVisible = new SelectableLevelCollection[1];
		levelCollectionsComboBox.setActionCommand("loadCollectionComboBox");
		levelCollectionsComboBox.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(final ActionEvent e) {
				//Only fire when popup is visible that is: not JSoko itself
				// has changed the items but the user using the popup.
				if(levelCollectionsComboBox.isPopupVisible()) {
					SwingUtilities.invokeLater(new Runnable() {
						@Override
						public void run() {
							// Only fire event when the user has finally selected a collection.
							// Otherwise typing letters fires events, too, although the searchable
							// functionality just searches corresponding items.
							if(!levelCollectionsComboBox.isPopupVisible()) {
								mainBoardDisplay.requestFocusInWindow(); // Set focus back so the user can immediately use the arrow keys for playing
								selectedCollectionWhenPopupBecameVisible[0] = null; // user has selected a new collection, hence don't set back the previous level
								GUI.this.actionPerformed(e);
							}
						}
					});
				}
			}
		});

		levelCollectionsComboBox.addFocusListener(new FocusAdapter() {
			@Override
			public void focusGained(FocusEvent e) {
				// Avoid that the user can use page down / page up
				// because that way the selected item could be changed without a popup.
				// If the user cancels the popup the focus is removed from the combo box!
				levelCollectionsComboBox.setPopupVisible(true);
			}
		});

		levelCollectionsComboBox.addPopupMenuListener(new PopupMenuListener() {

			@Override
			public void popupMenuWillBecomeVisible(PopupMenuEvent e) {
				GUI.this.setEnabled(false);  // the user might use the searchable
				menuBar.setEnabled(false);   // feature by pressing keys which shouldn't be handled anywhere else

				selectedCollectionWhenPopupBecameVisible[0] = (SelectableLevelCollection) levelCollectionsComboBox.getSelectedItem();
			}

			@Override
			public void popupMenuWillBecomeInvisible(PopupMenuEvent e) {
				// The user may have changed the selected collection by using keys. "selectedCollectionWhen..." is null,
				// when the user has really selected a new collection by pressing enter. Otherwise the remembered collection is set again.
				SwingUtilities.invokeLater(new Runnable() {
					@Override
					public void run() {
						if(selectedCollectionWhenPopupBecameVisible[0] != null) {
							levelCollectionsComboBox.setSelectedItem(selectedCollectionWhenPopupBecameVisible[0]);
						}
						menuBar.setEnabled(true);	// enable key actions for other
						GUI.this.setEnabled(true);  // actions again
					}
				});
			}

			@Override
			public void popupMenuCanceled(PopupMenuEvent e) {
				// Set the focus back to the board so the user can play using the arrow keys
				// and key events are handled normally (like pressing "l" for opening the level combo box).
				mainBoardDisplay.requestFocusInWindow();
			}
		});

		/**
		 * Create a renderer for the items showing the level collection titles.
		 *
		 */
		levelCollectionsComboBox.setRenderer(new StyledListCellRenderer() {

			@Override
			protected void customizeStyledLabel(JList list, Object value, int index, boolean isSelected,  boolean cellHasFocus) {
				super.customizeStyledLabel(list, value, index, isSelected, cellHasFocus);
				setHorizontalTextPosition(SwingConstants.LEFT);
				SelectableLevelCollection collection = (SelectableLevelCollection) value;
				String collectionTitle = Utilities.clipToEllipsis(collection.title, 40);
				setText(collectionTitle);
			}

		    private final JPanel separatorPanel = new JPanel(new BorderLayout());
		    private final JSeparator separator = new JSeparator();

		    @Override
			public Component getListCellRendererComponent(JList list, Object value, int index, boolean isSelected, boolean cellHasFocus) {
		    	Component comp = super.getListCellRendererComponent(list, value, index, isSelected, cellHasFocus);

		    	SelectableLevelCollection levelCollection = (SelectableLevelCollection) value;

		    	/* Display the file path for collections loaded from the disk. */
		    	if(isSelected) {
		    		// Priority for displaying: 1. database, 2. hard disk file, 3. don't display any info
		    		String infoText = levelCollection.databaseID != Database.NO_ID ? Texts.getText("collection")+": "+levelCollection.title + " ("+Texts.getText("database") +")" :
		    						  !levelCollection.file.isEmpty() ? Texts.getText("collection")+": "+levelCollection.file :
		    						  "";

		    		if(Debug.isDebugModeActivated) {
		    			infoText += "     databaseID: "+levelCollection.databaseID;
		    		}

		            setToolTipText(infoText);
		    		mainBoardDisplay.displayInfotext(infoText);
		    	}

		    	// Display the text right from the icon.
				setHorizontalTextPosition(SwingConstants.RIGHT);

		    	if(levelCollection.databaseID != Database.NO_ID) {
					setIcon(Utilities.getIcon("Sean Poon - Database-3-icon.png", "database"));
				}
		    	else if(!levelCollection.file.isEmpty()) {
					setIcon(Utilities.getIcon("drive_harddisk (oxygen).png", "harddisk"));
		    	}

		    	if(index!=-1 && index == ((SelectableLevelCollectionComboBoxModel) levelCollectionsComboBox.getModel()).getSeparatorIndex() && isSelected == false){
		            separatorPanel.removeAll();
		            separatorPanel.add(comp, BorderLayout.CENTER);
		            separatorPanel.add(separator, BorderLayout.SOUTH);
		            return separatorPanel;
		        }

		        return comp;
		    }
		});


		return levelCollectionsComboBox;
	}

	private final void addOneDebugVar(JMenu menu, Debug.DebugField debugField) {
		JCheckBoxMenuItem checkBoxMenuItem;

		checkBoxMenuItem = new JCheckBoxMenuItem(debugField.menuText, debugField.getValue());
		checkBoxMenuItem.setActionCommand(debugField.actionName);
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);
	}

	/**
	 * Add a possibly restricted order range of the debug variables.
	 *
	 * @param menu     add menu entries to this menu
	 * @param hasLower whether to obey the {@code minOrder} value
	 * @param minOrder minimum oder value to use, inclusive
	 * @param hasUpper whether to obey the {@code maxOrder} value
	 * @param maxOrder maximal order value to use, exclusive
	 */
	private final void addDebugVars( JMenu menu,
			boolean hasLower, double minOrder,
			boolean hasUpper, double maxOrder )
	{
		for (Debug.DebugField debugField : Debug.getMenuDebugFields()) {
			if (hasLower && (debugField.anno.menuOrder() < minOrder)) {
				continue;
			}
			if (hasUpper && (debugField.anno.menuOrder() >= maxOrder)) {
				continue;
			}
			addOneDebugVar(menu, debugField);
		}
	}

	private final void addDebugVarsFromTo(JMenu menu, double minOrder, double maxOrder) {
		addDebugVars(menu, true, minOrder, true, maxOrder);
	}

	private final void addDebugVarsFrom(JMenu menu, double minOrder) {
		addDebugVars(menu, true, minOrder, false, 0.0);
	}

	private final void addDebugVarsTo(JMenu menu, double maxOrder) {
		addDebugVars(menu, false, 0.0, true, maxOrder);
	}

	@SuppressWarnings("unused")
	private final void addDebugVars(JMenu menu) {
		addDebugVars(menu, false, 0.0, false, 0.0);
	}

	/**
	 *  Create a menu bar.
	 *  <p>
	 *  The menu bar is created using the global variable "menuBar".
	 *
	 * @return the menu bar of this program
	 */
	public final JMenuBar createMenuBar() {

		// Variables holding the components to be added to the menu bar.
		JCheckBoxMenuItem checkBoxMenuItem;
		JMenu menu;
		JMenuItem menuItem;

		// Remove all the components from the menu bar.
		if (menuBar == null) {
			menuBar = new JMenuBar();
			Texts.helpBroker.enableHelpKey(menuBar, "menu", null); // Enable help
		} else {
			menuBar.removeAll();
		}

		/*
		 * Level menu
		 */
		// "Level"
		menu = new JMenu(Texts.getText("level"));
		menu.setMnemonic(KeyEvent.VK_L);
		menuBar.add(menu);
		solverModeDependentObjects.add(menu); // must be disabled when the solver is started

		// Load level
		JMenu loadLevelMenu = new JMenu(Texts.getText("menu.loadLevel"));
		menu.add(loadLevelMenu);
		playModeDependentObjects.add(loadLevelMenu); // set inactive when editor is activated

		// Load individual level.
		menuItem = new JMenuItem(Texts.getText("menu.loadIndividualLevel"));
		menuItem.setActionCommand("loadIndividualLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_O, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		loadLevelMenu.add(menuItem);

		// Import level from clipboard
		menuItem = new JMenuItem(Texts.getText("menu.loadFromClipboard"));
		menuItem.setActionCommand("importLevelFromClipboard");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_V, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		loadLevelMenu.add(menuItem);

		// Add the built-in level collections to the menu.
		for (int authorNo = 1; Settings.get("levelCollection" + authorNo + "author") != null; authorNo++) {

			// Create a menu item for the author.
			JMenu levelMenuItem = new JMenu(Settings.get("levelCollection" + authorNo + "author"));
			loadLevelMenu.add(levelMenuItem);

			// Add the collections of the author.
			for (int collectionNo = 1;; collectionNo++) {

				// Get next collection of author. If there isn't any more collection of the author jump to the next author.
				String levelFileName = Settings.get("levelCollection" + authorNo + "_" + collectionNo);
				if (levelFileName == null) {
					break;
				}

				// Get the title of the collection.
				String collectionTitle = Settings.get("levelCollection" + authorNo + "_" + collectionNo + "_Title");
				if (collectionTitle == null) {
					collectionTitle = "Collection" + collectionNo;
				}

				// Add the collection to the menu of the author.
				menuItem = new JMenuItem(collectionTitle);
				menuItem.setActionCommand("loadLevel_" + levelFileName);
				menuItem.addActionListener(this);
				levelMenuItem.add(menuItem);
			}
		}

		// Add additional level collections (not build in) to the menu.
		//	    ArrayList<String> additionalCollections = application.levelManagement.getAdditionalCollectionNames();
		//	    JMenu levelMenuItem = levelMenu;
		//	    for(int i=0; additionalCollections != null && i<additionalCollections.size(); i++) {
		//
		//	            String collectionName = additionalCollections.get(i);
		//
		//	            // Structure of collection name is: NumberOfFile + "!" + FolderName + "/" + LevelName
		//
		//	            // All collections are added to the main level menu unless they were saved in a separate folder.
		//	            int index = collectionName.indexOf("/");
		//	            if(index != -1) {
		//	            	String folderName = collectionName.substring(collectionName.indexOf("!")+1, index);
		//	            	if(folderName.equals(levelMenuItem.getText()) == false) {
		//	                	levelMenuItem = new JMenu(folderName);
		//	                	levelMenu.add(levelMenuItem);
		//	            	}
		//	            }
		//	            else {
		//	            	levelMenuItem = levelMenu;
		//	            	index = collectionName.indexOf("!");
		//	            }
		//
		//	            // Add the levelcollection to the menu
		//	            menuItem = new JMenuItem(collectionName.substring(index+1, collectionName.length()-4)); // ohne Endung ".sok"
		//	            menuItem.setActionCommand("loadLevel_zip!"+collectionName);
		//	            menuItem.addActionListener(this);
		//	            levelMenuItem.add(menuItem);
		//	    }

		// Save level menu.
		JMenu saveLevelMenu = new JMenu(Texts.getText("menu.saveLevel"));
		menu.add(saveLevelMenu);
		playModeDependentObjects.add(loadLevelMenu); // set inactive when editor is activated

		// Create the "save" menu item.
		if (saveLevelMenuItem == null) {
			saveLevelMenuItem = new JMenuItem(Texts.getText("menu.saveLevel"));
			saveLevelMenuItem.setActionCommand("saveLevel");
			saveLevelMenuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_S, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
			saveLevelMenuItem.addActionListener(this);
		} else {
			// The language may have changed, so the text has to be set.
			saveLevelMenuItem.setText(Texts.getText("menu.saveLevel"));
		}
		saveLevelMenu.add(saveLevelMenuItem);

		// "Save level as ..."
		menuItem = new JMenuItem(Texts.getText("menu.saveLevelAs"));
		menuItem.setActionCommand("saveLevelAs");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_S, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask() | InputEvent.SHIFT_DOWN_MASK));
		menuItem.addActionListener(this);
		saveLevelMenu.add(menuItem);

		// "Save collection as ..."
		menuItem = new JMenuItem(Texts.getText("menu.saveCollectionAs"));
		menuItem.setActionCommand("saveCollectionAs");
		menuItem.addActionListener(this);
		saveLevelMenu.add(menuItem);

		// Export level to clipboard.
		menuItem = new JMenuItem(Texts.getText("menu.exportLevelToClipboard"));
		menuItem.setActionCommand("exportLevelToClipboard");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_C, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		saveLevelMenu.add(menuItem);

		// Export run length encoded level to clipboard.
		menuItem = new JMenuItem(Texts.getText("menu.exportLevelToClipboardRLE"));
		menuItem.setActionCommand("exportLevelToClipboard(RLE)");
		menuItem.addActionListener(this);
		saveLevelMenu.add(menuItem);

		// Export level to clipboard regarding the transformation.
		menuItem = new JMenuItem(Texts.getText("menu.exportLevelToClipboardWithTransformation"));
		menuItem.setToolTipText(Texts.getText("menu.exportLevelToClipboardWithTransformationToolTip"));
		menuItem.setActionCommand("exportLevelToClipboardWithTransformation");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_C, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask() | InputEvent.SHIFT_DOWN_MASK));
		menuItem.addActionListener(this);
		saveLevelMenu.add(menuItem);

		menu.addSeparator();

		// "Next level"
		menuItem = new JMenuItem(Texts.getText("nextlevel"));
		menuItem.setActionCommand("nextLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_PAGE_DOWN, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		// "Previous level"
		menuItem = new JMenuItem(Texts.getText("previouslevel"));
		menuItem.setActionCommand("previousLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_PAGE_UP, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		menu.addSeparator();

		// "Next unsolved level"
		menuItem = new JMenuItem(Texts.getText("menu.nextUnsolvedLevel"));
		menuItem.setActionCommand("nextUnsolvedLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_PAGE_DOWN, InputEvent.SHIFT_DOWN_MASK));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		// "Previous unsolved level"
		menuItem = new JMenuItem(Texts.getText("menu.previousUnsolvedLevel"));
		menuItem.setActionCommand("previousUnsolvedLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_PAGE_UP, InputEvent.SHIFT_DOWN_MASK));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		menu.addSeparator();

		// "Any level"
		menuItem = new JMenuItem(Texts.getText("anylevel"));
		menuItem.setActionCommand("anyLevel");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_A, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated


		/*
		 * Solver menu
		 */
		// "Solver"
		menu = new JMenu(Texts.getText("solver"));
		menu.setMnemonic(KeyEvent.VK_S);
		menuBar.add(menu);

		// The solver menu must be set inactive when ...
		playModeDependentObjects.add(menu);          // the editor is activated
		invalidLevelModeDependentObjects.add(menu);  // or the level is invalid.
		solverModeDependentObjects.add(menu);        // or the solver has been opened.

		// Create menu item for starting and stopping the solver.
		menuItem = new JMenuItem(Texts.getText("solver.openCloseSolver"));
		menuItem.setActionCommand("openSolver");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F4, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Create menu item for starting and stopping the solver.
		if(Debug.isDebugModeActivated) {
			menuItem = new JMenuItem(Texts.getText("remodelSolver.openCloseSolver"));
			menuItem.setActionCommand("openRemodelSolver");
			menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F9, 0));
			menuItem.addActionListener(this);
			menu.add(menuItem);}

		/*
		 * Optimizer menu
		 */
		// "Optimizer". The optimizer menu must be disabled while the solver is running.
		menu = new JMenu(Texts.getText("optimizer"));
		menu.setMnemonic(KeyEvent.VK_O);

		// If the level is invalid the optimizer must be disabled.
		invalidLevelModeDependentObjects.add(menu);

		// This menu is only active when then solver and the editor are inactive.
		solverModeDependentObjects.add(menu);
		playModeDependentObjects.add(menu);

		menuItem = new JMenuItem(Texts.getText("openOptimizer"));
		menuItem.setActionCommand("openOptimizer");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F5, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		menuBar.add(menu);


		/*
		 * Editor menu
		 */
		// "Editor"
		menu = new JMenu(Texts.getText("editor"));
		menu.setMnemonic(KeyEvent.VK_E);

		// Add the editor menu to the menu bar.
		menuBar.add(menu);

		solverModeDependentObjects.add(menu); // If the solver is running the editor must be disabled.

		// A JCheckboxMenuItem would be the better choice,
		// but has sometimes performance problems,
		// in case the program is started outside Eclipse :-(
		editorMenuItem = new JMenuItem(Texts.getText("editormode"));
		editorMenuItem.setActionCommand("activateEditor");
		editorMenuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F6, 0));
		editorMenuItem.addActionListener(this);
		menu.add(editorMenuItem);

		menuItem = new JMenuItem(Texts.getText("newlevel"));
		menuItem.setActionCommand("newLevel");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("cancel"));
		menuItem.setActionCommand("cancelEditor");
		menuItem.addActionListener(this);
		menu.add(menuItem);
		// This menu item should only be enabled in editor mode.
		editorModeDependentObjects.add(menuItem);

		/*
		 * Database menu
		 */
		// Create the database menu and add it to the menu bar.
		// It must only be enabled when the solver isn't running -> solver mode dependent.
		menu = new JMenu(Texts.getText("database"));
		menu.setMnemonic(KeyEvent.VK_D);
		menuBar.add(menu);
		solverModeDependentObjects.add(menu);

		// The menu is disabled while the editor is activated.
		playModeDependentObjects.add(menu);

		// Browse database
		menuItem = new JMenuItem(Texts.getText("browseDatabase"));
		menuItem.setActionCommand("browseDatabase");
		menuItem.addActionListener(this);
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F7, 0));
		menuItem.setEnabled(Database.isBlockedByAnotherProcess() == false);
		menu.add(menuItem);

		// Import level to database
		menuItem = new JMenuItem(Texts.getText("importLevelToDB"));
		menuItem.setActionCommand("importLevelToDB");
		menuItem.addActionListener(this);
		menuItem.setEnabled(Database.isBlockedByAnotherProcess() == false);
		menu.add(menuItem);

		// Import collection to database
		menuItem = new JMenuItem(Texts.getText("importCollectionToDB"));
		menuItem.setActionCommand("importCollectionToDB");
		menuItem.addActionListener(this);
		menuItem.setEnabled(Database.isBlockedByAnotherProcess() == false);
		menu.add(menuItem);

		// Import collections of folder.
		menuItem = new JMenuItem(Texts.getText("menu.importCollectionsOfFolder"));
		menuItem.setActionCommand("importCollectionsOfFolder");
		menuItem.addActionListener(this);
		menuItem.setEnabled(Database.isBlockedByAnotherProcess() == false);
		menu.add(menuItem);

		// Import other database data.
		menuItem = new JMenuItem(Texts.getText("menu.importDataFromOtherDatabase"));
		menuItem.setActionCommand("importDataFromOtherDatabase");
		menuItem.addActionListener(this);
		menuItem.setEnabled(Database.isBlockedByAnotherProcess() == false);
		menu.add(menuItem);

		/*
		 * Moves menu
		 */
		// Create the moves menu and add it to the menu bar.
		// It must only be enabled when the solver isn't running -> solver mode dependent.
		menu = new JMenu(Texts.getText("menu.moves"));
		menu.setMnemonic(KeyEvent.VK_M);
		menuBar.add(menu);
		solverModeDependentObjects.add(menu);

		// The moves menu is disabled while ...
		playModeDependentObjects.add(menu);          // FFS/hm the editor is activated
		invalidLevelModeDependentObjects.add(menu);  // the level is invalid

		// "Undo all"
		menuItem = new JMenuItem(Texts.getText("undoAll"));
		menuItem.setActionCommand("undoAll");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_HOME, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated
		undoActions.add(menuItem); // set inactive when there isn't any preceding movement in the history

		// "Undo movement"
		menuItem = new JMenuItem(Texts.getText("undo"));
		menuItem.setActionCommand("undo");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_DELETE, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated
		undoActions.add(menuItem); // set inactive when there isn't any preceding movement in the history

		// "Replay"
		menuItem = new JMenuItem(Texts.getText("replay"));
		menuItem.setActionCommand("replay");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_R, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated
		redoActions.add(menuItem); // set inactive when there isn't any successor movement in the history

		// "Redo movement"
		menuItem = new JMenuItem(Texts.getText("redo"));
		menuItem.setActionCommand("redo");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_INSERT, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated
		redoActions.add(menuItem); // set inactive when there isn't any successor movement in the history

		// "Redo all"
		menuItem = new JMenuItem(Texts.getText("redoAll"));
		menuItem.setActionCommand("redoAll");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_END, 0));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated
		redoActions.add(menuItem); // set inactive when there isn't any successor movement in the history

		// "Go to move..."
		menuItem = new JMenuItem(Texts.getText("menu.goToMove"));
		menuItem.setActionCommand("goToMove");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_J, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		menu.addSeparator();

		// "Show LURD string"
		menuItem = new JMenuItem(Texts.getText("showlurd"));
		menuItem.setActionCommand("showlurd");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_L, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		// "Copy moves to clipbard"
		menuItem = new JMenuItem(Texts.getText("menu.copyMovesToClipboard"));
		menuItem.setActionCommand("copyMovesToClipboard");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_M, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		// "Copy moves x to y to clipbard"
		menuItem = new JMenuItem(Texts.getText("menu.copyMovesXToYToClipboard"));
		menuItem.setActionCommand("copyMovesXToYToClipboard");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_M, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask() | ActionEvent.SHIFT_MASK));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated

		// "Paste moves from clipboard"
		menuItem = new JMenuItem(Texts.getText("menu.pasteMovesFromClipboard"));
		menuItem.setActionCommand("pasteMovesFromClipboard");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_P, Toolkit.getDefaultToolkit().getMenuShortcutKeyMask()));
		menuItem.addActionListener(this);
		menu.add(menuItem);
		playModeDependentObjects.add(menuItem); // set inactive when editor is activated


		/*
		 * Debug menu
		 */
		// Debug
		menu = new JMenu("Debug");
		menuBar.add(menu);
		debugModeDependentObjects.add(menu);

		// Negative order values come first
		addDebugVarsTo(menu, 0.0);

		checkBoxMenuItem = new JCheckBoxMenuItem("Draw own skin graphics");
		checkBoxMenuItem.setActionCommand("debugDrawOwnSkin");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		addDebugVarsFromTo(menu,  0.0, 10.0);

		// Menu for settings the balance between pushes and moves.
		menuItem = new JMenuItem("Set push penalty");
		menuItem.setActionCommand("setPushPenalty");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		addDebugVarsFromTo(menu, 10.0, 20.0);

		menuItem = new JMenuItem("Show corralforcer situations");
		menuItem.setActionCommand("showCorralForcerSituations");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show boxdata");
		checkBoxMenuItem.setActionCommand("showBoxData");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show lower bound forward");
		checkBoxMenuItem.setActionCommand("showLowerboundForward");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show lower bound backward");
		checkBoxMenuItem.setActionCommand("showLowerboundBackward");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		menuItem = new JMenuItem("Show lower bound of all levels");
		menuItem.setActionCommand("showLowerboundAllLevels");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		addDebugVarsFromTo(menu, 20.0, 50.0);


		checkBoxMenuItem = new JCheckBoxMenuItem("Show box distance forward");
		checkBoxMenuItem.setActionCommand("showBoxDistanceForward");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show box distance backward");
		checkBoxMenuItem.setActionCommand("showBoxDistanceBackward");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);


		// Create menu items from annotation "DebugVar"
		addDebugVarsFrom(menu, 50.0);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show penaltysquares");
		checkBoxMenuItem.setActionCommand("showPenaltySquares");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show penaltysituations separately");
		checkBoxMenuItem.setActionCommand("showPenaltySituationsSeparately");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Show areas (box count vs. goal count)");
		checkBoxMenuItem.setActionCommand("showLevelAreas");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		checkBoxMenuItem = new JCheckBoxMenuItem("Mark closed diagonal deadlock boxes", false);
		checkBoxMenuItem.setActionCommand("markClosedDiagonalDeadlockBoxes");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		menuItem = new JMenuItem("Show not identified deadlockpositions");
		menuItem.setActionCommand("showNotIdentifiedDeadlockPositions");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		menuItem = new JMenuItem("Image extractor");
		menuItem.setActionCommand("startImageExtractor");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		//  MenuItem for firing an event for testing anything
		menuItem = new JMenuItem("Test");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_T, 0));
		menuItem.setActionCommand("test");
		menuItem.addActionListener(this);
		menu.add(menuItem);



		/*
		 * Solutions menu
		 */
		JMenu solutionMenu = new JMenu(Texts.getText(("solutions")));
		solutionMenu.setMnemonic(KeyEvent.VK_U);

		menuItem = new JMenuItem(Texts.getText("importSolution"));
		menuItem.setActionCommand("importSolution");
		menuItem.addActionListener(this);
		solutionMenu.add(menuItem);

		solutionMenu.addSeparator();

		menuItem = new JMenuItem(Texts.getText("solutionManagement"));
		menuItem.setActionCommand("solutionManagement");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F8, 0));
		menuItem.addActionListener(this);
		solutionMenu.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("solutionList.solutionSidebar"));
		menuItem.setActionCommand("solutionSidebar");
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F11, 0));
		menuItem.addActionListener(this);
		solutionMenu.add(menuItem);

		// The solutions menu is set inactive when ...
		playModeDependentObjects.add(solutionMenu);          // editor is activated
		invalidLevelModeDependentObjects.add(solutionMenu);  // level is invalid
		menuBar.add(solutionMenu);


		/*
		 * View menu.
		 */
		JMenu viewMenu = new JMenu(Texts.getText("view"));
		viewMenu.setMnemonic(KeyEvent.VK_V);
		solverModeDependentObjects.add(viewMenu); // must be disabled when the solver is started


		/*
		 * Skin menu
		 */
		JMenu skinMenu = new JMenu(Texts.getText("skins"));

		// Only one skin can be displayed at a time, hence a button group is used.
		ButtonGroup buttonGroup = new ButtonGroup();

		// Get the properties of all available skins.
		TreeSet<Properties> availableSkinsProperties = SkinManagement.getAvailableSkins();

		Hashtable<String, JMenu> authorMenus = new Hashtable<String, JMenu>(50);

		// Add every skin to the menu. Every author gets his own menu for his skins.
		for (final Properties skinSettings : availableSkinsProperties) {

			// Get the author of the skin.
			String author = skinSettings.getProperty("author");
			if(author == null)
			 {
				author = skinSettings.getProperty("Copyright"); // for skn-format
			}
			if(author == null) {
				author = Texts.getText("Unknown", "Unknown");
			}

			// Get the menu for the author. If there isn't one yet, create it.
			JMenu authorMenu = authorMenus.get(author);
			if(authorMenu == null) {
				authorMenu = new JMenu(author);
				skinMenu.add(authorMenu);
				authorMenus.put(author, authorMenu);
			}

			// Get the name of the skin.
			String skinName = skinSettings.getProperty("name");
			if(skinName == null)
			 {
				skinName = skinSettings.getProperty("Title"); // for skn-format
			}
			if(skinName == null) {
				skinName = Texts.getText("Unknown", "Unknown");
			}

			// Radiobutton for the skin. If it is armed then the skin is shown.
			final JRadioButtonMenuItem skinRadioButton = new JRadioButtonMenuItem(skinName, skinSettings.getProperty("settingsFilePath").equals(Settings.currentSkin));
			skinRadioButton.getModel().addChangeListener(new ChangeListener() {
				@Override
				public void stateChanged(ChangeEvent e) {

					// Get the model.
					ToggleButtonModel toggleButtonmodel = (ToggleButtonModel) e.getSource();

					// Get the path to the skin settings file.
					String settingsFilePathToSet = skinSettings.getProperty("settingsFilePath");

					// If the menu item is armed then set the new skin. This allows the user to browse through the skins.
					// Attention: "stateChanged" is called several times. Hence, ensure that the skin isn't set again,
					// when already set.
					if(toggleButtonmodel.isArmed() && mainBoardDisplay.skin.settingsFilePath.equals(settingsFilePathToSet) == false) {
						mainBoardDisplay.setSkin(settingsFilePathToSet);

						// If the skin hasn't been successfully been set, disable the button because setting the skin results in a failure.
						if(mainBoardDisplay.skin.settingsFilePath.equals(settingsFilePathToSet) == false) {
							skinRadioButton.setEnabled(false);
						}
					}
				}
			});
			skinRadioButton.setActionCommand("setSkin"+skinSettings.getProperty("settingsFilePath"));
			skinRadioButton.addActionListener(this);
			authorMenu.add(skinRadioButton);
			buttonGroup.add(skinRadioButton);
		}

		skinMenu.addSeparator();

		// Checkbox for enabling / disabling animations of the skin (player and box animations).
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("menu.showSkinAnimations"), Settings.getBool("showSkinAnimations", true));
		checkBoxMenuItem.setToolTipText(Texts.getText("menu.showSkinAnimationsToolTip"));
		checkBoxMenuItem.setActionCommand("showSkinAnimations");
		checkBoxMenuItem.addActionListener(this);
		skinMenu.add(checkBoxMenuItem);

		// Menu item for setting the animation delay.
		menuItem = new JMenuItem(Texts.getText("menu.skinAnimationDelay"));
		menuItem.setActionCommand("setSkinAnimationDelay");
		menuItem.addActionListener(this);
		skinMenu.add(menuItem);

		skinMenu.addSeparator();

		// Menu for the background images.
		final ButtonGroup backgroundPictureGroup = new ButtonGroup();
		final JMenu backgroundMenu = new JMenu(Texts.getText("backgroundgraphics"));
		ArrayList<String> backgroundImageNames = new ArrayList<String>();
		File[] backgroundImagesFiles = Utilities.getFileFromClassPath(Settings.get("backgroundImagesFolder")).listFiles(new FilenameFilter(){
			@Override
			public boolean accept(File file, String name){
				return new File(file, name).isFile() && (
						name.toLowerCase().endsWith(".bmp")  ||
						name.toLowerCase().endsWith(".jpg")  ||
						name.toLowerCase().endsWith(".jpeg") ||
						name.toLowerCase().endsWith(".gif")  ||
						name.toLowerCase().endsWith(".png"));
			}
		});
		for (File file : backgroundImagesFiles) {
			backgroundImageNames.add(file.getName());
		}

		// Add all background images to the menu.
		for (String backgroundImageName : backgroundImageNames) {

			// Ensure to jump over invalid names.
			if(backgroundImageName.lastIndexOf(".") == -1) {
				continue;
			}

			// Get the name of the image without the filename extension.
			String backgroundImageLabel = backgroundImageName.substring(0, backgroundImageName.lastIndexOf("."));

			// Get the translated text of the image name.
			String translation = Texts.getText("backgroundgraphic."+backgroundImageLabel.replace(" ", "_"));
			if(translation != null) {
				backgroundImageLabel = translation;
			}

			final String backgroundImagePath = Settings.get("backgroundImagesFolder")+backgroundImageName;

			final JRadioButtonMenuItem backgroundRadioButton = new JRadioButtonMenuItem(backgroundImageLabel, backgroundImagePath.equals(Settings.get("backgroundImageFile")));
			backgroundRadioButton.setActionCommand("setBackground" + backgroundImagePath);
			backgroundRadioButton.getModel().addChangeListener(new ChangeListener() {
				@Override
				public void stateChanged(ChangeEvent e) {

					// Get the model.
					ToggleButtonModel toggleButtonmodel = (ToggleButtonModel) e.getSource();

					// If the menu item is armed then set the new background. This allows the user to browse through the graphics.
					// The state of the button changes several times, hence ensure the new image is only set once.
					if(toggleButtonmodel.isArmed() && Settings.get("backgroundImageFile").equals(backgroundImagePath) == false) {
						try {
							mainBoardDisplay.setBackgroundGraphic(backgroundImagePath);
						} catch (FileNotFoundException e1) {
							// Background image can't be set. Disable the button so no further attempts can be activated.
							backgroundRadioButton.setEnabled(false);
						}
					}
				}
			});
			backgroundRadioButton.addActionListener(this);
			backgroundMenu.add(backgroundRadioButton);
			backgroundPictureGroup.add(backgroundRadioButton);
		}
		skinMenu.add(backgroundMenu);

		// If the skin menu is deselected the last skin and background is set back again.
		skinMenu.addMenuListener(new MenuListener() {

			// While browsing the skins / background images they are set in the game. However, after
			// the browsing the skin/background image before browsing has to be set again. Therefore
			// the skin and background image are saved when the menu opens.
			private Skin skinBackup;
			private String backgroundImageFilePathBackup;


			@Override
			public void menuCanceled(MenuEvent e) {}
			@Override
			public void menuDeselected(MenuEvent e) {

				// If the skins menu is left the skin before entering the menu is set back.
				if(mainBoardDisplay.skin.settingsFilePath.equals(skinBackup.settingsFilePath) == false) {
					mainBoardDisplay.setSkin(skinBackup.settingsFilePath);
				}

				// Set the old background image if the user has set another by browsing through the background images.
				// If the user has really selected another background (not just browsing) the action command will set
				// the selected background image after this method is called.
				if(Settings.get("backgroundImageFile").equals(backgroundImageFilePathBackup) == false) {

					// Fire the action to set the background image. Get the action command from the button.
					// The selection may be null when JSoko tried to load an image that isn't stored anymore on the
					// hard disk when it starts.
					if(backgroundPictureGroup.getSelection() != null) {
						actionPerformed(new ActionEvent(this, 0, backgroundPictureGroup.getSelection().getActionCommand()));
					}
				}
			}
			@Override
			public void menuSelected(MenuEvent e) {
				// Backup the currently set skin. This is done because browsing through the skins automatically
				// sets them so the user can see how they look like. However, when the browsing is finished the
				// skin set before the browsing started has to be set back.
				skinBackup = mainBoardDisplay.skin;

				// Backup the background image.
				backgroundImageFilePathBackup = Settings.get("backgroundImageFile");
			}
		});

		viewMenu.add(skinMenu);


		/*
		 * Transformations
		 */
		// Menu for transformations
		JMenu transformationMenu = new JMenu(Texts.getText("transformation"));
		solverModeDependentObjects.add(transformationMenu); // must be disabled when the solver is started

		// 90° rotation clockwise.
		menuItem = new JMenuItem(Texts.getText("rotate_90_degrees"));
		menuItem.setActionCommand("rotate_90");
		menuItem.addActionListener(this);
		transformationMenu.add(menuItem);

		// 90° rotation anti-clockwise.
		menuItem = new JMenuItem(Texts.getText("rotate_270_degrees"));
		menuItem.setActionCommand("rotate_270");
		menuItem.addActionListener(this);
		transformationMenu.add(menuItem);

		transformationMenu.addSeparator();

		// Flip horizontally.
		menuItem = new JMenuItem(Texts.getText("flip_horizontally"));
		menuItem.setActionCommand("flipHorizontally");
		menuItem.addActionListener(this);
		transformationMenu.add(menuItem);

		// Flip vertically.
		menuItem = new JMenuItem(Texts.getText("flip_vertically"));
		menuItem.setActionCommand("flipVertically");
		menuItem.addActionListener(this);
		transformationMenu.add(menuItem);

		transformationMenu.addSeparator();

		// Reset all transformations.
		menuItem = new JMenuItem(Texts.getText("reset_transformations"));
		menuItem.setActionCommand("resetTransformations");
		menuItem.addActionListener(this);
		transformationMenu.add(menuItem);

		viewMenu.add(transformationMenu);


		// Menu for setting the maximal scaling.
		JMenu scalingMenu = new JMenu(Texts.getText("max_scale"));
		viewMenu.add(scalingMenu);

		ButtonGroup buttonGroupZoom = new ButtonGroup();

		// Get the maximum scaling factor from the settings.
		int maximumScaling = Settings.getInt("maximumScaling", 3);

		// Add the scaling factors.
		for (int i = 1; i <= 3; i++) {
			JRadioButtonMenuItem zoomRadioButton = new JRadioButtonMenuItem(i + "x", i == maximumScaling);
			zoomRadioButton.setActionCommand("zoom" + i + "x");
			zoomRadioButton.addActionListener(this);
			buttonGroupZoom.add(zoomRadioButton);
			scalingMenu.add(zoomRadioButton);
		}

		/*
		 * Show walls checkbox
		 */
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("menu.showWalls"), Settings.getBool("showWalls", true));
		checkBoxMenuItem.setActionCommand("showWalls");
		checkBoxMenuItem.addActionListener(this);
		viewMenu.add(checkBoxMenuItem);

		menuBar.add(viewMenu);


		/*
		 * Settings menu
		 */
		menu = new JMenu(Texts.getText("settings"));
		menu.setMnemonic(KeyEvent.VK_T);
		menuBar.add(menu);
		solverModeDependentObjects.add(menu); // must be disabled when the solver is started
		playModeDependentObjects.add(menu);   // must be disabled when the editor is started

		/*
		 *  Global JSoko settings
		 */
		JMenuItem settingsFrameMenu = new JMenuItem(Texts.getText("settings"));

		settingsFrameMenu.addActionListener(new ActionListener() {

			@Override
			public void actionPerformed(ActionEvent e) {
				SettingsFrame settingsFrame = new SettingsFrame(GUI.this);
				settingsFrame.setLocationRelativeTo(null); // set position to the middle
				settingsFrame.setVisible(true);
			}
		});
		if(Debug.isDebugModeActivated) {
			menu.add(settingsFrameMenu);
		}



		JMenuItem settingsFrameMenu2 = new JMenuItem(Texts.getText("settings")+"2");

		settingsFrameMenu2.addActionListener(new ActionListener() {

			@Override
			public void actionPerformed(ActionEvent e) {
				SettingsFrameNew settingsFrame = new SettingsFrameNew(application);
			}
		});
		if(Debug.isDebugModeActivated) {
			menu.add(settingsFrameMenu2);
		}


		// Language menu
		JMenu languageMenu = new JMenu(Texts.getText("language"));
		languageMenu.setMnemonic(KeyEvent.VK_S);

		// Get the available language name (translated to the user language) and add them to the menu.
		for(String languageName : Translator.getAvailableLanguageNames()) {
			String languageCode = Translator.getLanguageCode(languageName);
			Locale locale = new Locale(languageCode);
			menuItem = new JMenuItem(locale.getDisplayLanguage(locale), KeyEvent.VK_D);
			menuItem.setActionCommand("language" + languageCode);
			menuItem.addActionListener(this);
			languageMenu.add(menuItem);
		}

		languageMenu.addSeparator();

		menuItem = new JMenuItem(Texts.getText("translate"));
		menuItem.setActionCommand("translate");
		menuItem.addActionListener(this);
		languageMenu.add(menuItem);

		menu.add(languageMenu);

		// "Look and Feel" menu
		JMenu lookAndFeel = new JMenu(Texts.getText("lookAndFeel"));

		// NimROD L&F
		JMenu nimRod = new JMenu("NimROD L&F");
		lookAndFeel.add(nimRod);

		menuItem = new JMenuItem("NimROD L&F");
		menuItem.setActionCommand("nimRODLookAndFeel");
		menuItem.addActionListener(this);
		nimRod.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("menu.nimROD.customizeNimRODL&F"));
		menuItem.setActionCommand("customizeNimRODL&F");
		menuItem.addActionListener(this);
		nimRod.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("menu.nimROD.loadThemeNimRODL&F"));
		menuItem.setActionCommand("loadThemeNimRODL&F");
		menuItem.addActionListener(this);
		nimRod.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("menu.nimROD.setDefaultTheme"));
		menuItem.setActionCommand("setDefaultThemeNimRODL&F");
		menuItem.addActionListener(this);
		nimRod.add(menuItem);

		// Standard L&F
		LookAndFeelInfo[] info = UIManager.getInstalledLookAndFeels();
		for (LookAndFeelInfo lfInfo : info) {
			menuItem = new JMenuItem(lfInfo.getName());
			menuItem.setActionCommand(lfInfo.getClassName());
			menuItem.addActionListener(this);
			lookAndFeel.add(menuItem);
		}

		menu.add(lookAndFeel);

		// Deadlock menu
		JMenu deadlockMenu = new JMenu(Texts.getText("deadlocks"));
		Texts.helpBroker.enableHelpKey(deadlockMenu, "sokoban.deadlocks", null); // Enable help

		// "Detect simple deadlocks"
		detectSimpleDeadlocks = new JCheckBoxMenuItem(Texts.getText("detectSimpleDeadlocks"),Settings.detectSimpleDeadlocks);
		Texts.helpBroker.enableHelpKey(detectSimpleDeadlocks, "sokoban.deadlocks.DeadSquareDeadlocks", null); // Enable help
		detectSimpleDeadlocks.setActionCommand("detectSimpleDeadlocks");
		detectSimpleDeadlocks.addActionListener(this);
		deadlockMenu.add(detectSimpleDeadlocks);

		// "Detect freeze deadlocks"
		detectFreezeDeadlocks = new JCheckBoxMenuItem(Texts.getText("detectFreezeDeadlocks"), Settings.detectFreezeDeadlocks);
		Texts.helpBroker.enableHelpKey(detectFreezeDeadlocks, "sokoban.deadlocks.FreezeDeadlocks", null); // Enable help
		detectFreezeDeadlocks.setActionCommand("detectFreezeDeadlocks");
		detectFreezeDeadlocks.addActionListener(this);
		deadlockMenu.add(detectFreezeDeadlocks);

		// "Detect corral deadlocks"
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("detectCorralDeadlocks"), Settings.detectCorralDeadlocks);
		Texts.helpBroker.enableHelpKey(checkBoxMenuItem, "sokoban.deadlocks.CorralDeadlocks", null); // Enable help
		checkBoxMenuItem.setActionCommand("detectCorralDeadlocks");
		checkBoxMenuItem.addActionListener(this);
		deadlockMenu.add(checkBoxMenuItem);

		// "Detect bipartite deadlocks"
		detectBipartiteDeadlocks = new JCheckBoxMenuItem(Texts.getText("detectBipartiteDeadlocks"), Settings.detectBipartiteDeadlocks);
		Texts.helpBroker.enableHelpKey(detectBipartiteDeadlocks, "sokoban.deadlocks.BipartiteDeadlocks", null); // Enable help
		detectBipartiteDeadlocks.setActionCommand("detectBipartiteDeadlocks");
		detectBipartiteDeadlocks.addActionListener(this);
		deadlockMenu.add(detectBipartiteDeadlocks);

		// "Detect closed diagonal deadlocks"
		detectClosedDiagonalDeadlocks = new JCheckBoxMenuItem(Texts.getText("detectClosedDiagonalDeadlocks"), Settings.detectClosedDiagonalDeadlocks);
		Texts.helpBroker.enableHelpKey(detectClosedDiagonalDeadlocks, "sokoban.deadlocks.ClosedDiagonalDeadlocks", null); // Enable help
		detectClosedDiagonalDeadlocks.setActionCommand("detectClosedDiagonalDeadlocks");
		detectClosedDiagonalDeadlocks.addActionListener(this);
		deadlockMenu.add(detectClosedDiagonalDeadlocks);

		menu.add(deadlockMenu);

		// "Show deadlock squares"
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("showdeadlocksquares"), Settings.showDeadlockFields);
		Texts.helpBroker.enableHelpKey(checkBoxMenuItem, "sokoban.deadlocks.DeadSquareDeadlocks", null); // Enable help
		checkBoxMenuItem.setActionCommand("showDeadlockSquares");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// "Show minimum solution length"
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("showMinimumSolutionLength"), Settings.showMinimumSolutionLength);
		checkBoxMenuItem.setActionCommand("showMinimumSolutionLength");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Flag, indicating whether undo/redo has to be done step by step (or as long as the same box is pushed).
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("singlestepUndoRedo"), Settings.singleStepUndoRedo);
		checkBoxMenuItem.setActionCommand("singleStepUndoRedo");
		checkBoxMenuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_S, 0));
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Flag, indicating whether reverse moves should be treated as undo.
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("treat_reverse_moves_as_undo"),Settings.treatReverseMovesAsUndo);
		checkBoxMenuItem.setActionCommand("reverseMoves");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Flag, indicating whether moves between pushes should be optimized automatically.
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("optimize_moves"), Settings.optimizeMovesBetweenPushes);
		checkBoxMenuItem.setActionCommand("optimizeBetweenMoves");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Flag, indicating whether the reachable positions for the selected box are to be highlighted.
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("showReachableBoxPositions"), Settings.showReachableBoxPositions);
		checkBoxMenuItem.setActionCommand("showReachableBoxPositions");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Sound on/off
		checkBoxMenuItem = new JCheckBoxMenuItem(Texts.getText("sound"), Settings.soundEffectsEnabled);
		checkBoxMenuItem.setActionCommand("sound");
		checkBoxMenuItem.addActionListener(this);
		menu.add(checkBoxMenuItem);

		// Animation speed
		menuItem = new JMenuItem(Texts.getText("animationsspeed"));
		menuItem.setActionCommand("animationSpeed");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Animation speed for undo / redo
		menuItem = new JMenuItem(Texts.getText("animationsspeedundoredo"));
		menuItem.setActionCommand("animationSpeedUndoRedo");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Add the settings menu to the menu bar.
		menuBar.add(menu);

		/*
		 * Info menu
		 */
		// "Info"
		menu = new JMenu(Texts.getText("info"));
		menu.setMnemonic(KeyEvent.VK_I);
		menuBar.add(menu);
		solverModeDependentObjects.add(menu); // must be disabled when the solver is started

		// About menu item.
		menuItem = new JMenuItem(Texts.getText("aboutJSoko"));
		menuItem.setActionCommand("aboutJSoko");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Website.
		menuItem = new JMenuItem(Texts.getText("menu.website"));
		menuItem.setActionCommand("openWebsite");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Check for update.
		menuItem = new JMenuItem(Texts.getText("menu.updateCheck"));
		menuItem.setActionCommand("updateCheck");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		// Flag, indicating whether the program checks for new updates at every program start.
		final JCheckBoxMenuItem checkBoxMenuItemUpdate = new JCheckBoxMenuItem(Texts.getText("menu.automaticUpdateCheck"), Settings.getBool("automaticUpdateCheck"));
		checkBoxMenuItemUpdate.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				// Set the new status in the settings.
				Settings.set("automaticUpdateCheck", ""+checkBoxMenuItemUpdate.isSelected());
			}
		});
		menu.add(checkBoxMenuItemUpdate);

		/*
		 * Help menu
		 */
		menu = new JMenu(Texts.getText("menu.help"));
		menuBar.add(menu);

		menuItem = new JMenuItem(Texts.getText("menu.JSokoHelp"));
		menuItem.addActionListener(new CSH.DisplayHelpFromSource(Texts.helpBroker));
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F1, 0));
		menu.add(menuItem);

		// Create a menu item for selecting any object to show the help for.
		menuItem = new JMenuItem(Texts.getText("menu.selectHelpObject"));
		menuItem.setAccelerator(KeyStroke.getKeyStroke(KeyEvent.VK_F1, InputEvent.SHIFT_DOWN_MASK));
		menuItem.addActionListener(new CSH.DisplayHelpAfterTracking(Texts.helpBroker));
		menu.add(menuItem);

		// Set the correct status of the objects.
		setModeDependentObjectStatus();
		setSolverDependentObjectsEnabled(true);
		setDebugMenuVisible(Debug.isDebugModeActivated);

		infoButton = new JButton();
		infoButton.setBorderPainted(false);
		infoButton.setContentAreaFilled(false);
		infoButton.setPreferredSize(new Dimension(80, menuBar.getPreferredSize().height)); // should not be higher than the menu bar
		infoButton.setVisible(false);
		menuBar.add(infoButton);

		return menuBar;
	}

	/**
	 * Sets the action events to be fired by typing keys on the keyboard.
	 */
	private void setKeyActions() {

		InputMap inputMap   = getInputMap(JComponent.WHEN_ANCESTOR_OF_FOCUSED_COMPONENT);
		ActionMap actionmap = getActionMap();

		/*
		 * Cursor key "left"
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_LEFT, 0), "moveLeft");
		AbstractAction moveLeftAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "moveLeft"));
			}
		};
		actionmap.put("moveLeft", moveLeftAction);


		/*
		 * Cursor key "right"
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_RIGHT, 0), "moveRight");
		AbstractAction moveRightAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "moveRight"));
			}
		};
		actionmap.put("moveRight", moveRightAction);


		/*
		 * Cursor key "up"
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_UP, 0), "moveUp");
		AbstractAction moveUpAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "moveUp"));
			}
		};
		actionmap.put("moveUp", moveUpAction);


		/*
		 * Cursor key "down"
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_DOWN, 0), "moveDown");
		AbstractAction moveDownAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "moveDown"));
			}
		};
		actionmap.put("moveDown", moveDownAction);


		/*
		 * C for opening the combo box for selecting a level collection.
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_C, 0), "openCollectionsComboBox");
		AbstractAction openCollectionsComboBoxAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "openCollectionsComboBox"));
			}
		};
		actionmap.put("openCollectionsComboBox", openCollectionsComboBoxAction);


		/*
		 * L for opening the combo box for selecting a level.
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_L, 0), "openLevelsComboBox");
		AbstractAction openLevelsComboBoxAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "openLevelsComboBox"));
			}
		};
		actionmap.put("openLevelsComboBox", openLevelsComboBoxAction);


		/*
		 * Undo can be activated by backspace and by "z".
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_Z, 0), "undo");
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_BACK_SPACE, 0), "undo");
		AbstractAction undoAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "undo"));
			}
		};
		actionmap.put("undo", undoAction);
		undoActions.add(undoAction);


		/*
		 * Undo all can be activated by ctrl + z.
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_Z, InputEvent.CTRL_DOWN_MASK), "undoAll");
		AbstractAction undoAllAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "undoAll"));
			}
		};
		actionmap.put("undoAll", undoAllAction);
		undoActions.add(undoAllAction);


		/*
		 * Redo can be activated by "y".
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_Y, 0), "redo");
		AbstractAction redoAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "redo"));
			}
		};
		actionmap.put("redo", redoAction);
		redoActions.add(redoAction);


		/*
		 * Redo all can be activated by ctrl + y.
		 */
		inputMap.put(KeyStroke.getKeyStroke(KeyEvent.VK_Y, InputEvent.CTRL_DOWN_MASK), "redoAll");
		AbstractAction redoAllAction = new AbstractAction() {
			@Override
			public void actionPerformed(ActionEvent e) {
				GUI.this.actionPerformed(new ActionEvent(e.getSource(), e.getID(), "redoAll"));
			}
		};
		actionmap.put("redoAll", redoAllAction);
		redoActions.add(redoAllAction);


		// All key actions in one list.
		List<AbstractAction> keyActions = Arrays.asList(moveLeftAction, moveRightAction, moveUpAction, moveDownAction, undoAction, undoAllAction, redoAction, redoAllAction);

		playModeDependentObjects.addAll(keyActions);
		solverModeDependentObjects.addAll(keyActions);
		invalidLevelModeDependentObjects.addAll(keyActions);
	}

	/**
	 * Sets the cursor image to the specified cursor.
	 *
	 * @param cursor The value of the cursor to be set
	 */
	final public void setCursor(int cursor) {

		// Set the specified predefined cursor.
		mainBoardDisplay.setCursor(Cursor.getPredefinedCursor(cursor));
	}

	/**
	 * This method sets the currently set language in all menu bar
	 * and tool bar components.
	 */
	final public void setNewLanguage() {

		// Create a new menu bar and and a new tool bar according to the new language.
		application.setJMenuBar(createMenuBar());
		createToolBar();
		updatedSelectableLevels();
	}

	/**
	 * Displays a FileChooser dialog for letting the user choose a file and returns its name.
	 * <p>
	 * The passed path to the file is overwritten with the path of the new chosen file.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @param dialogTitle the title of the dialog
	 * @param fileFilter the filter to be used for filtering by file name
	 * @return name and location of the chosen file
	 */
	public String[] getFileDataForLoading(String directoryPath, String dialogTitle, FileFilter fileFilter) {

		// A FileDialog looks better on Mac OS.
		if(OSType.isMac) {
			return getFileDataForLoadingMacOS(directoryPath, dialogTitle, fileFilter);
		}

		// String array containing: path to the file, the filename, concatenation of path and filename.
		String[] fileData = new String[2];

		// Create a JFileCooser for letting the user choose a file.
		JFileChooser fc = new JFileChooser();
		fc.setDialogTitle(dialogTitle);
		ShortCutsAccessory shortcuts = new ShortCutsAccessory(fc);
		fc.setAccessory(shortcuts);
		Dimension d = new Dimension(700, 400);
		fc.setMinimumSize(d);
		fc.setPreferredSize(d);

		// Set the last known file path if there hasn't been passed a specific one.
		if (directoryPath == null) {
			directoryPath = Settings.lastFilePath;
		}

		// Set the passed path as current directory. If it doesn't exist take the current directory as default.
		File file = new File(directoryPath);
		if (file.isDirectory() == false) {
			file = new File(Utilities.getBaseFolder()+Settings.get("levelFolder"));
		}
		fc.setCurrentDirectory(file);

		//	Set a filter for the supported file types. (so far: *.sok, *.xsb and *.txt)
		fc.setFileFilter(fileFilter);

		// If the user hasn't chosen a file or an error occurred return null.
		if (fc.showOpenDialog(this) != JFileChooser.APPROVE_OPTION) {
			return null;
		}

		// Set the return data.
		fileData[0] = fc.getSelectedFile().getName();  // Only the file name
		fileData[1] = fc.getCurrentDirectory()
				+ System.getProperty("file.separator")
				+ fileData[0];                     // filename + path

		// Save the selected path as default path.
		Settings.lastFilePath = fc.getCurrentDirectory().getPath();

		return fileData;
	}

	/**
	 * Displays a {@code FileDialog} for letting the user choose a file and returns its name.
	 * <p>
	 * The passed path to the file is overwritten with the path of the new chosen file.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @param dialogTitle the title of the dialog
	 * @param fileFilter the filter to be used for filtering by file name
	 * @return name and location of the chosen file
	 */
	private String[] getFileDataForLoadingMacOS(String directoryPath, String dialogTitle, final FileFilter fileFilter) {

		// String array containing: path to the file, the filename, concatenation of path and filename.
		String[] fileData = new String[2];

		// Set the last known file path if there hasn't been passed a specific one.
		if (directoryPath == null) {
			directoryPath = Settings.lastFilePath;
		}

		FileDialog fileDialog = new FileDialog(application, dialogTitle, FileDialog.LOAD);
		fileDialog.setFile("");
		fileDialog.setDirectory(directoryPath);
		fileDialog.setFilenameFilter(new FilenameFilter(){
		    @Override
			public boolean accept(File dir, String name){
		      return fileFilter.accept(new File(name));
		    }
		 });
		fileDialog.setVisible(true);

		if (fileDialog.getFile() != null) {
			// Save the selected path as default path.
			Settings.lastFilePath = fileDialog.getDirectory();

			fileDialog.dispose();

			// Set the return data.
			fileData[0] = fileDialog.getFile();  // Only the file name
			fileData[1] = fileDialog.getDirectory() + System.getProperty("file.separator") + fileDialog.getFile();              // filename + path

			// Save the selected path as default path.
			Settings.lastFilePath = fileDialog.getDirectory();

			return fileData;
		}

		fileDialog.dispose();

		return null;

	}

	/**
	 * Displays a <code>JFileChooser</code> dialog for letting the user
	 * choose a file and returns its name.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @return name and location of the chosen file
	 */
	public String getFileDataForSaving(String directoryPath) {

		// Set the last known file path if there hasn't been passed a specific one.
		if (directoryPath == null) {
			directoryPath = Settings.lastFilePath;
		}

		// Set the passed path as current directory.
		// If it doesn't exist take the current directory as default.
		File startDirectory = new File(directoryPath);
		if (startDirectory.isDirectory() == false) {
			startDirectory = new File(OSSpecific.getUserHomeDirectory());
		}

		// When the program runs in Mac OS the FileDialog looks better.
		if(OSType.isMac) {
			return getFileDataForSavingMacOS(directoryPath);
		}

		// Create JFileCooser.
		JFileChooser fc = new JFileChooser(startDirectory);
		fc.setDialogTitle(Texts.getText("choose_level"));
		fc.setDialogType(JFileChooser.SAVE_DIALOG);
		ShortCutsAccessory shortcuts = new ShortCutsAccessory(fc);
		fc.setAccessory(shortcuts);
		Dimension d = new Dimension(700, 400);
		fc.setMinimumSize(d);
		fc.setPreferredSize(d);

		//	Filter files for: *.sok, *.xsb and *.txt
		fc.setFileFilter(new FileFilter() {
			@Override
			public boolean accept(File f) {
				final String lowerCaseName = f.getName().toLowerCase();
				return     lowerCaseName.endsWith(".sok")
						|| lowerCaseName.endsWith(".xsb")
						|| lowerCaseName.endsWith(".txt")
						|| f.isDirectory();
			}

			@Override
			public String getDescription() {
				return Texts.getText("supported_leveltypes");
			}
		});

		// If the JFileChooser has been canceled or an error occurred return immediately.
		if (fc.showSaveDialog(this) != JFileChooser.APPROVE_OPTION) {
			return null;
		}

		// Get the chosen file name.
		String fileName = fc.getCurrentDirectory() + System.getProperty("file.separator") + fc.getSelectedFile().getName();

		// Add ".sok" if the file doesn't have a file extension, yet.
		if (fileName.lastIndexOf('.') == -1) {
			fileName += ".sok";
		}

		// Handle the case that there is a file with that name, already.
		if (new File(fileName).exists()) {
			switch (JOptionPane.showConfirmDialog(this, Texts.getText("file_exists_overwrite"),
					Texts.getText("menu.saveLevel"), JOptionPane.YES_NO_CANCEL_OPTION, JOptionPane.WARNING_MESSAGE)) {
					case JOptionPane.YES_OPTION:
						break;
					case JOptionPane.NO_OPTION:
						return getFileDataForSaving(fc.getCurrentDirectory().toString());
					case JOptionPane.CANCEL_OPTION:
						return null;
			}
		}

		// Save the selected path as default path.
		Settings.lastFilePath = fc.getCurrentDirectory().getPath();

		return fileName;
	}

	/**
	 * Displays a <code>FileDialog</code> for letting the user
	 * choose a file to save to and returns its name.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @return name and location of the chosen file
	 */
	private String getFileDataForSavingMacOS(String directoryPath) {

		FileDialog fileDialog = new FileDialog(application, Texts.getText("choose_level"), FileDialog.SAVE);
		fileDialog.setFile(".sok");
		fileDialog.setDirectory(directoryPath);
		fileDialog.setFilenameFilter(new FilenameFilter(){
		    @Override
			public boolean accept(File dir, String name){
		      return (name.endsWith(".sok") || name.endsWith(".xsb") || name.endsWith(".txt"));
		    }
		 });
		fileDialog.setVisible(true);

		String filename = fileDialog.getFile();
		if (filename != null) {

			filename = fileDialog.getDirectory() + System.getProperty("file.separator") + filename;

			// Add ".sok" if the file doesn't have a file extension, yet.
			if (filename.lastIndexOf('.') == -1) {
				filename += ".sok";
			}

			// Save the selected path as default path.
			Settings.lastFilePath = fileDialog.getDirectory();
		}
		fileDialog.dispose();

		return filename;
	}

	/**
	 *  ActionEvent handling.
	 *
	 *@param  evt action event object
	 */
	@Override
	public void actionPerformed(ActionEvent evt) {

		// Get the action command.
		String action = evt.getActionCommand();

		/*
		 * Solution sidebar
		 */
		if (action.equals("solutionSidebar")) {
			solutionsGUI.actionPerformed(new ActionEvent(this, 0, "openView"));
			return;
		}

		/*
		 * View menu events
		 */
		// Set new delay for the animations.
		if (action.equals("setSkinAnimationDelay")) {

			// Create a spinner for setting the animation delay.
			USpinner delaySpinner = new USpinner(Settings.getInt("skinAnimationDelay", 35), 1, 250, 1);

			// Adjust the delay for the animation when the spinner value changes.
			delaySpinner.addChangeListener(new ChangeListener() {

				@Override
				public void stateChanged(ChangeEvent e) {

					int delay = ((USpinner) e.getSource()).getValueAsInteger();
					mainBoardDisplay.setSkinAnimationDelay(delay);
				}

			});

			// Create a panel containing a text message and the spinner for setting
			// the delay and show it in an option pane.
			JPanel p = new JPanel();
			p.add(new JLabel(Texts.getText("message.skinAnimationDelay")+" (1-250):"));
			p.add(delaySpinner);

			// Remember the current animation delay.
			int animationDelayBackup = Settings.getInt("skinAnimationDelay", 40);

			// Let the user set a new animation delay.
			int answer = JOptionPane.showConfirmDialog(application, p, Texts.getText("menu.skinAnimationDelay"), JOptionPane.OK_CANCEL_OPTION, JOptionPane.PLAIN_MESSAGE, null);

			// If the user canceled the dialog the old value is set back.
			if(answer == JOptionPane.CANCEL_OPTION) {
				mainBoardDisplay.setSkinAnimationDelay(animationDelayBackup);
			}

			return;
		}

		// Set a new skin.
		if (action.startsWith("setSkin")) {

			// The path to the settings file of the skin is appended after the action command.
			String skinSettingsFilePath = action.substring(7);

			// Set the skin. The path to the skin`s settings file is appended after the text "setSkin".
			mainBoardDisplay.setSkin(skinSettingsFilePath);

			// If the skin hasn't been successfully been set, disable the component which fired the event,
			// because setting the skin results in a failure.
			if(mainBoardDisplay.skin.settingsFilePath.equals(skinSettingsFilePath) == false) {
				((JComponent) evt.getSource()).setEnabled(false);
			}

			return;
		}

		// Background picture
		if (action.startsWith("setBackground")) {

			// The path to the image is appended after the text "setBackground".
			String backgroundFile = action.substring(13);

			try {
				mainBoardDisplay.setBackgroundGraphic(backgroundFile);
			} catch (FileNotFoundException e) {
				// Display a message because the background image couldn't be loaded.
				MessageDialogs.showExceptionError(this, e);

				return;
			}

			return;
		}

		// Enable / disable showing animations.
		if (action.startsWith("showSkinAnimations")) {

			// Get the selected status of the checkbox.
			JCheckBoxMenuItem checkbox = (JCheckBoxMenuItem) evt.getSource();

			// Save the setting in the global settings object.
			Settings.set("showSkinAnimations", Boolean.toString(checkbox.isSelected()));

			// Repaint the GUI so the setting can influence the GUI.
			repaint();

			return;
		}

		// 90° rotation of the board.
		if (action == "rotate_90") {
			mainBoardDisplay.transformBoard(Transformation.ROTATION_BY_90_DEGREES);

			return;
		}

		// 270° rotation of the board.
		if (action == "rotate_270") {
			mainBoardDisplay.transformBoard(Transformation.ROTATION_BY_270_DEGREES);

			return;
		}

		// Horizontal flipping of the board.
		if (action == "flipHorizontally") {
			mainBoardDisplay.transformBoard(Transformation.FLIP_HORIZONTALLY);

			return;
		}

		// Vertical flipping of the board.
		if (action == "flipVertically") {
			mainBoardDisplay.transformBoard(Transformation.FLIP_VERTICALLY);

			return;
		}

		// Board display without any transformation (the original state).
		if (action == "resetTransformations") {
			mainBoardDisplay.transformBoard(Transformation.NO_TRANSFORMATION);

			return;
		}

		// Maximal zoom factor
		if (action.startsWith("zoom")) {
			int maximumScaling = Integer.parseInt(action.substring(4, 5));
			mainBoardDisplay.setMaximumScalingFactor(maximumScaling);

			return;
		}

		// Show walls setting.
		if (action.equals("showWalls")) {

			// Get the selected status of the checkbox.
			JCheckBoxMenuItem checkbox = (JCheckBoxMenuItem) evt.getSource();

			// Save the setting in the global settings object.
			Settings.set("showWalls", Boolean.toString(checkbox.isSelected()));

			// Show the GUI regarding the new setting.
			mainBoardDisplay.recalculateGraphicSizes();
			repaint();

			return;
		}

		// If a popup has been displayed the user now wants to close it.
		if (action == "show popup") {
			popup.setVisible(false);
			popup = null;

			return;
		}

		if (action.equals("translate")) {

			// Set dialog for 'Translator' tool.
			JDialog translatorDialog = new JDialog(application, "JSoko - "+Texts.getText("translate"), true);
			translatorDialog.setResizable(true);
			Utilities.setEscapable(translatorDialog);

			Rectangle bounds = application.getBounds();
			translatorDialog.setLocation(bounds.x + 20, bounds.y + 30);
			translatorDialog.setSize(bounds.width - 20, bounds.height - 30);

			Translator translator = new Translator(translatorDialog);
			translator.setVisible(true);
			translatorDialog.add(translator);

			translatorDialog.setJMenuBar(translator.createMenuBar());

			translatorDialog.setVisible(true);
			translatorDialog.pack();

			return;
		}

		// Set Look and Feel
		if (action.endsWith("LookAndFeel")) {

			// Save the new Look&Feel in the settings.
			Settings.currentLookAndFeel = action;

			JFrame.setDefaultLookAndFeelDecorated(true);
			JDialog.setDefaultLookAndFeelDecorated(true);

			// Set "nimROD"-Look and Feel if requested.
			if (action.startsWith("nimROD")) {
				setNimRODLookAndFeel(null);
				return;
			}

			//	Set the chosen Look and Feel.
			try {
				UIManager.setLookAndFeel(action);
			} catch (Exception e2) {
				setNimRODLookAndFeel(null);
			}

			// Update the look&feel of all swing components.
			Utilities.updateUI();

			return;
		}

		// Customizing of the NimROD Look and Feel.
		// NimROD has own functionality for this which is called.
		if(action.equals("customizeNimRODL&F")) {

			// Set nimROD L&F for this application, because the change in NimRODMain directly influences this application.
			setNimRODLookAndFeel(null);

			// Call the NimROD method for showing the customizing frame.
			NimRODMain nimrod = new NimRODMain();

			// Add info text for the user.
			nimrod.getContentPane().add(new JTextArea(Texts.getText("nimrod.helpText")), BorderLayout.EAST);

			// Make some room for the info text.
			nimrod.setSize( 850, 700 );

			// Ensure that no parts of the program will be off-screen and the program is displayed centered.
			// Get the maximum size available for the program.
			Rectangle maximumWindowBounds = GraphicsEnvironment.getLocalGraphicsEnvironment().getMaximumWindowBounds();

			// If the current windows is larger than the maximum available size then shrink it to the maximum size.
			if(    nimrod.getSize().width  > maximumWindowBounds.width
					|| nimrod.getSize().height > maximumWindowBounds.height )
			{
				nimrod.setSize(maximumWindowBounds.width, maximumWindowBounds.height);
			}

			// The new position should be in the higher middle of the screen:
			int xCoord = (maximumWindowBounds.width  / 2  -  getSize().width  / 2);
			int yCoord = (maximumWindowBounds.height / 4  -  getSize().height / 4);
			nimrod.setLocation(xCoord, yCoord);

			// When the user closes the window the current NimROD theme has to be set back.
			nimrod.addWindowListener(new WindowAdapter() {
				@Override
				public void windowClosing(WindowEvent e) {
					setNimRODLookAndFeel(null);
				};
			});

			return;
		}

		// Load and set a theme the user selects.
		if(action.startsWith("loadThemeNimRODL&F")) {

			JFileChooser jfilechooser = new JFileChooser();
			jfilechooser.setDialogTitle(Texts.getText("nimRod.loadThemeTitle"));

			// Set the last used path as directory. If this path should not be valid the
			// file chooser automatically chooses the home directory of the user.
			jfilechooser.setCurrentDirectory(new File(Settings.get("nimRODThemeLastFilePath")));


			// Filter files for: *.theme
			jfilechooser.setFileFilter(new FileFilter() {
				@Override
				public boolean accept(File f) {
					return     f.getName().toLowerCase().endsWith(".theme")
							|| f.isDirectory();
				}

				@Override
				public String getDescription() {
					return "*.theme";
				}
			});

			// If the JFileChooser has been canceled or an error occurred return immediately.
			if(jfilechooser.showOpenDialog(this) != JFileChooser.APPROVE_OPTION) {
				return;
			}

			// Get the location of the chosen file.
			String configurationFile = jfilechooser.getSelectedFile().getPath();

			// Save the path to the theme in the settings.
			Settings.set("nimRODThemeLastFilePath", jfilechooser.getSelectedFile().getParent());

			// Set the NimROD look and feel.
			setNimRODLookAndFeel(configurationFile);

			return;
		}

		// Load and set the standard theme.
		if(action.equals("setDefaultThemeNimRODL&F")) {

			// Empty theme file means: default theme.
			Settings.set("nimRODThemeFile", "");

			// Set default NimROD look and feel.
			setNimRODLookAndFeel(null);
			return;
		}

		/*
		 * Open the combo box for selecting a level.
		 */
		if(action.equals("openLevelsComboBox")) {
			levelsComboBox.setPopupVisible(true);
			levelsComboBox.requestFocusInWindow();

			return;
		}

		/*
		 * Load from ComboBox: load the selected level.
		 */
		if (action.equals("loadLevelComboBox")) {

			if(levelsComboBox.getSelectedIndex() != -1) {
				Level selectedLevel = (Level) levelsComboBox.getSelectedItem();
				fireActionPerformed(new ActionEvent(selectedLevel, 0, "levelSelected_"+levelsComboBox.getSelectedIndex()));
			}

			return;
		}

		/*
		 * Open the combo box for selecting a collection.
		 */
		if(action.equals("openCollectionsComboBox")) {
			collectionsComboBox.setPopupVisible(true);
			collectionsComboBox.requestFocusInWindow();

			return;
		}

		/*
		 * Load from combo box: load the selected level collection.
		 */
		if (action.equals("loadCollectionComboBox")) {
			SelectableLevelCollection selectedCollection = (SelectableLevelCollection) collectionsComboBox.getSelectedItem();
			if(selectedCollection != null) {
				fireActionPerformed(new ActionEvent(selectedCollection, 0, "collectionSelected"));
			}

			return;
		}

		// This action command can't be handled here
		// -> inform the listeners.
		fireActionPerformed(evt);
	}

	/**
	 * Displays a popup containing a text area with corresponding label.
	 *
	 * @param labelText	    the text of the label
	 * @param textAreaText	the text in the text area
	 */
	final public void showPopupTextarea(String labelText, String textAreaText) {

		// Create a Textarea for the text content
		JTextArea textarea = new JTextArea(textAreaText);
		textarea.setAutoscrolls(true);
		textarea.setLineWrap(true);
		textarea.setEditable(false);

		// Create a Label for the "name"
		JLabel label = new JLabel(labelText);

		JButton button = new JButton("OK");
		button.addActionListener(this);
		button.setActionCommand("show popup");

		// Wrap a Panel around label, text and button
		JPanel panel = new JPanel(new BorderLayout());
		panel.add(label, "North");
		panel.add(new JScrollPane(textarea), "Center");
		panel.add(button, "South");

		// Wrap a Popup around the panel
		popup = new JPopupMenu();
		popup.add(panel);
		popup.setPreferredSize(new Dimension(getWidth() * 3 / 4, getHeight() * 3 / 4));

		// Show the Popup centered into the board
		popup.show(this, (getWidth()  - (int) popup.getPreferredSize().getWidth() ) / 2,
				(getHeight() - (int) popup.getPreferredSize().getHeight()) / 2);
	}

	/**
	 * Sets whether the editor menu item is enabled.
	 * @param enabled
	 */
	final public void setEditorMenuItemEnabled(boolean enabled) {
		editorMenuItem.setEnabled(enabled);
	}

	/**
	 * Returns the menu item for saving a level.
	 *
	 * @return the <code>JMenuItem</code> for saving a level
	 */
	final public JMenuItem getSaveLevelMenuItem() {
		return saveLevelMenuItem;
	}

	/**
	 * Returns the button for showing info in the menu bar.
	 * <p>
	 * The caller can fully access this button.
	 *
	 * @return the <code>JMenuItem</code> for saving a level
	 */
	final public JButton getInfoButton() {
		return infoButton;
	}


	/**
	 * Sets the objects enabled or disabled depending on the current mode
	 * (play or editor mode).
	 */
	final public void setModeDependentObjectStatus() {

		// Ensure to change status on the EDT (event dispatcher thread),
		// because this method may be called from a background thread.
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				boolean isPlayModeActivated = application.isPlayModeActivated();
				boolean isEditorModeActivated = application.isEditorModeActivated();

				for (Object object : playModeDependentObjects) {
					if(object instanceof Component) {
						((Component) object).setEnabled(isPlayModeActivated);
					} else if(object instanceof Action) {
						((Action) object).setEnabled(isPlayModeActivated);
					}
				}

				for (Object object : editorModeDependentObjects) {
					if(object instanceof Component) {
						((Component) object).setEnabled(isEditorModeActivated);
					} else if(object instanceof Action) {
						((Action) object).setEnabled(isEditorModeActivated);
					}
				}
			}
		});
	}

	/**
	 * Sets the status of the solver mode dependent objects.
	 *
	 * @param enabledStatus <code>true</code>, if the objects are enabled, and
	 * 						<code>false</code> if the objects are disabled
	 */
	final public void setSolverDependentObjectsEnabled(final boolean enabledStatus) {

		// Ensure to change status on the EDT (event dispatcher thread),
		// because this method may be called from a background thread.
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				for (Object object : solverModeDependentObjects) {
					if(object instanceof Component) {
						((Component) object).setEnabled(enabledStatus);
					} else if(object instanceof Action) {
						((Action) object).setEnabled(enabledStatus);
					}
				}
			}
		});
	}

	/**
	 * Sets the enabled status of the undo buttons.
	 *
	 * @param enabledStatus <code>true</code>, if the objects are enabled, and
	 * 						<code>false</code> if the objects are disabled
	 */
	final public void setUndoButtonsEnabled(final boolean enabledStatus) {

		// Ensure to change status on the EDT, because this method may be called from a background thread.
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				for (Object object : undoActions) {
					if(object instanceof Component) {
						((Component) object).setEnabled(enabledStatus);
					} else if(object instanceof Action) {
						((Action) object).setEnabled(enabledStatus);
					}
				}
			}
		});
	}

	/**
	 * Sets the enabled status of the redo buttons.
	 *
	 * @param enabledStatus <code>true</code> if the objects are enabled, and
	 * 					   <code>false</code> if the objects are disabled
	 */
	final public void setRedoButtonsEnabled(final boolean enabledStatus) {

		// Ensure to change status on the EDT, because this method may be called from a background thread.
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				for (Object object : redoActions) {
					if(object instanceof Component) {
						((Component) object).setEnabled(enabledStatus);
					} else if(object instanceof Action) {
						((Action) object).setEnabled(enabledStatus);
					}
				}
			}
		});
	}

	/**
	 * This methods sets the enabled status of specific GUI elements.
	 * <p>
	 * If the current loaded level is invalid then some of the GUI elements
	 * have to be disabled.
	 *
	 * @param enabledStatus  whether the GUI elements are to be enabled
	 */
	final public void setInvalidLevelModeDependentObjectsEnabled(boolean enabledStatus) {

		for (Object object : invalidLevelModeDependentObjects) {
			if(object instanceof Component) {
				((Component) object).setEnabled(enabledStatus);
			} else if(object instanceof Action) {
				((Action) object).setEnabled(enabledStatus);
			}
		}

		// An invalid level can't have a movement history.
		// Hence, disable the undo/redo buttons.
		setUndoButtonsEnabled(enabledStatus);
		setRedoButtonsEnabled(enabledStatus);

		// Enable the editor menu item so the user can open the editor.
		setEditorMenuItemEnabled(true);
	}

	/**
	 * Sets the debug menu visible or invisible.
	 *
	 * @param isToBeVisible <code>true</code>, if the debug menu shall be visible, and
	 * 						<code>false</code> if the debug menu shall be invisible
	 */
	final public void setDebugMenuVisible(boolean isToBeVisible) {
		for (Component component : debugModeDependentObjects) {
			component.setVisible(isToBeVisible);
		}
	}

	/**
	 * Sets the look and feel for JSoko.
	 * <p>
	 * The look and feel can be changed by the user and is saved in the settings file.
	 */
	public static void setLookAndFeel() {

		// If this program is started for the first time ("showHelp" is true) and it's
		// a Mac OS then use the Mac OS default look and feel instead of NimRod.
		if(Settings.getBool("showHelp") == true && OSType.isMac) {
			// Save the default look and feel name as standard.
			Settings.currentLookAndFeel = UIManager.getLookAndFeel().getName();
			return;
		}

		// Set the Look&Feel read from the settings file.
		try {
			if(Settings.currentLookAndFeel.startsWith("nimROD")) {
				GUI.setNimRODLookAndFeel(null);
			} else {
				UIManager.setLookAndFeel(Settings.currentLookAndFeel);
			}
		} catch (Exception e2) {
			try {
				UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
			} catch (Exception e) {}
		}
	}

	/**
	 * Loads and sets the NimROD look and feel in this program.
	 * <p>
	 * If a configuration file is passed the theme settings are read from that file.
	 *
	 * @param configurationFile the property file with the theme data
	 */
	public static void setNimRODLookAndFeel(String configurationFile) {

		// Theme for NimROD.
		NimRODTheme nimrodTheme = null;

		// If no configuration file has been passed use the one in the settings, if any.
		if(configurationFile == null) {
			configurationFile = Settings.get("nimRODThemeFile");
		}

		// User specific colors may be loaded from a theme file.
		if(configurationFile != null) {
			try {
				Properties properties = new Properties();
				properties.load(new FileInputStream(configurationFile));

				nimrodTheme = new NimRODTheme();

				// Set the data from the read property file.
				if(properties.getProperty("nimrodlf.selection") != null) {
					nimrodTheme.setPrimary(Color.decode(properties.getProperty("nimrodlf.selection")));
				}
				if(properties.getProperty("nimrodlf.background") != null) {
					nimrodTheme.setSecondary(Color.decode(properties.getProperty("nimrodlf.background")));
				}
				if(properties.getProperty("nimrodlf.p1") != null) {
					nimrodTheme.setPrimary1(Color.decode(properties.getProperty("nimrodlf.p1")));
				}
				if(properties.getProperty("nimrodlf.p2") != null) {
					nimrodTheme.setPrimary2(Color.decode(properties.getProperty("nimrodlf.p2")));
				}
				if(properties.getProperty("nimrodlf.p3") != null) {
					nimrodTheme.setPrimary3(Color.decode(properties.getProperty("nimrodlf.p3")));
				}
				if(properties.getProperty("nimrodlf.s1") != null) {
					nimrodTheme.setSecondary1(Color.decode(properties.getProperty("nimrodlf.s1")));
				}
				if(properties.getProperty("nimrodlf.s2") != null) {
					nimrodTheme.setSecondary2(Color.decode(properties.getProperty("nimrodlf.s2")));
				}
				if(properties.getProperty("nimrodlf.s3") != null) {
					nimrodTheme.setSecondary3(Color.decode(properties.getProperty("nimrodlf.s3")));
				}
				if(properties.getProperty("nimrodlf.w") != null) {
					nimrodTheme.setWhite(Color.decode(properties.getProperty("nimrodlf.w")));
				}
				if(properties.getProperty("nimrodlf.b") != null) {
					nimrodTheme.setBlack(Color.decode(properties.getProperty("nimrodlf.b")));
				}
				if(properties.getProperty("nimrodlf.menuOpacity") != null) {
					nimrodTheme.setMenuOpacity(Integer.parseInt(properties.getProperty("nimrodlf.menuOpacity")));
				}
				if(properties.getProperty("nimrodlf.frameOpacity") != null) {
					nimrodTheme.setFrameOpacity(Integer.parseInt(properties.getProperty("nimrodlf.frameOpacity")));
				}
			}
			catch (FileNotFoundException e) {
				nimrodTheme = null;
				configurationFile = null;
			} catch (IOException e) {
				nimrodTheme = null;
				configurationFile = null;
			}
		}

		// If no theme has been created, yet, set the default colors for this program.
		if(nimrodTheme == null) {
			nimrodTheme = new NimRODTheme();
			nimrodTheme.setPrimary1(Color.decode("#71B9F3"));
			nimrodTheme.setPrimary2(Color.decode("#A4D4FA"));
			nimrodTheme.setPrimary3(Color.decode("#EFEFFB"));
			nimrodTheme.setSecondary1(Color.decode("#CADBF7"));
			nimrodTheme.setSecondary2(Color.decode("#DFE0F9"));
			nimrodTheme.setSecondary3(Color.decode("#EDF5FF"));
			nimrodTheme.setWhite(Color.decode("#FFFFFF"));
			nimrodTheme.setBlack(Color.decode("#000000"));
			nimrodTheme.setMenuOpacity(235);
			nimrodTheme.setFrameOpacity(180);
		}

		// Create a new instance of the look and feel.
		NimRODLookAndFeel nimRODLF = new NimRODLookAndFeel();

		// Set the theme.
		NimRODLookAndFeel.setCurrentTheme(nimrodTheme);

		// Set static variables in the NimROD look and feel. This is important
		// if later the customize frame of NimROD is opened.
		// This way the current theme is used there, too, so the user
		// immediately gets the settings also set in JSoko.
		NimRODMain.nf = nimRODLF;
		NimRODMain.nt = nimrodTheme;

		// Set the look and feel.
		try {
			UIManager.setLookAndFeel(nimRODLF);
		} catch (UnsupportedLookAndFeelException e2) {
			return;
		}

		// Install the JIDE extension without destroying Nimbus look and feel
		// (-> xerto without menu).
		LookAndFeelFactory.installJideExtension(LookAndFeelFactory.XERTO_STYLE_WITHOUT_MENU);

		// Update the look&feel of all swing components.
		Utilities.updateUI();

		// Save the new Look&Feel in the settings.
		Settings.currentLookAndFeel = "nimRODLookAndFeel";

		// Save which theme file is used.
		Settings.set("nimRODThemeFile", configurationFile == null ? "" : configurationFile);
	}

	/**
	 * Returns the solutions view.
	 * <p>
	 * The solutions are shown in an own JPanel at the left of the main GUI.
	 *
	 * @return the <code>solutionsGUI</code> displaying the solutions
	 */
	public SolutionsGUI getSolutionsView() {
		return solutionsGUI;
	}

	/**
	 * Updates the combo box the levels can be selected from.
	 */
	public void updatedSelectableLevels() {

		LevelCollection levelCollection = application.currentLevelCollection;

		if(levelCollection == null) {
			return;
		}

		// Remove all currently set items.
		levelsComboBox.removeAllItems();

		// Add the levels to the combo box.
		for(Level level : levelCollection) {
			levelsComboBox.addItem(level);
		}

		// We add the "name" of the collection as a tool tip
		// Other ways to get displayed the current collections info
		// could be even better.
		String collname = levelCollection.getTitle();
		if (collname != null && !collname.equals("")) {
			levelsComboBox.setToolTipText( Texts.getText("selectLevelTooltip") + " " + collname );
		}

		levelsComboBox.setSelectedItem(application.currentLevel);

	}

	/**
	 * Sets the passed level as selected level in the level combo box.
	 *
	 * @param level {@code Level} to be set in the level combo box
	 */
	public void selectLevelInLevelComboBox(Level level) {

		if(level != null) {
			levelsComboBox.setSelectedItem(level);
		}
	}

	/**
	 * Returns a button for showing and changing the transformation
	 * of the displayed board in the game.
	 *
	 * @return the <code>JideSplitButton</code> for the transformation actions
	 */
	private JideSplitButton getTransformationButton() {

		final JideSplitButton transformationButton = new JideSplitButton(Utilities.getIcon("transformation status (own).png", null));
		transformationButton.setActionCommand("rotate_90");
		transformationButton.setToolTipText(Texts.getText("toolbarButton.transformationToolTip"));
		transformationButton.setFocusable(false);
		transformationButton.addActionListener(this);

		Transformation.addChangeEventListener(new ChangeListener() {
			@Override
			public void stateChanged(ChangeEvent e) {
				ImageIcon icon = Utilities.getIcon("transformation status (own).png", null);
				if(icon == null) {
					return;
				}

				int angle = 0;
				switch(Transformation.getRotationValue()) {
				case Transformation.NO_TRANSFORMATION:
					break;
				case Transformation.ROTATION_BY_90_DEGREES:
					angle = 90;
					break;
				case Transformation.ROTATION_BY_180_DEGREES:
					angle = 180;
					break;
				case Transformation.ROTATION_BY_270_DEGREES:
					angle = 270;
					break;
				}

				// Draw the original icon without the flip arrow into the new image.
				int w = icon.getIconWidth();
				int h = icon.getIconHeight();
				BufferedImage image1 = new BufferedImage(h, w, BufferedImage.TYPE_INT_ARGB);
				Graphics2D g2D = image1.createGraphics();
				g2D.drawImage(icon.getImage(), 0, 0, w, h-6, 0, 0, w, h-6, null);
				g2D.dispose();

				// Rotate the image according to the current settings.
				double x = (h - w) / 2.0;
				double y = (w - h) / 2.0;
				AffineTransform at = AffineTransform.getTranslateInstance(x, y);
				at.rotate(Math.toRadians(angle), w/2.0, h/2.0-3);
				BufferedImage image2 = new BufferedImage(h, w, BufferedImage.TYPE_INT_ARGB);
				g2D = image2.createGraphics();
				g2D.drawImage(image1, at, null);

				// Draw the arrows indicating that the board is displayed horizontally flipped.
				if(Transformation.isLevelFlippedHorizontally()) {
					g2D.drawImage(icon.getImage(), 0, h-6, w, h, 0, h-6, w, h, null);
				}

				g2D.dispose();

				// Set the new icon.
				transformationButton.setIcon(new ImageIcon(image2));
			}
		});

		// 90° rotation clockwise.
		JMenuItem menuItem = new JMenuItem(Texts.getText("rotate_90_degrees"));
		menuItem.setActionCommand("rotate_90");
		menuItem.addActionListener(this);
		transformationButton.add(menuItem);

		// 90° rotation anti-clockwise.
		menuItem = new JMenuItem(Texts.getText("rotate_270_degrees"));
		menuItem.setActionCommand("rotate_270");
		menuItem.addActionListener(this);
		transformationButton.add(menuItem);

		// Flip horizontally.
		menuItem = new JMenuItem(Texts.getText("flip_horizontally"));
		menuItem.setActionCommand("flipHorizontally");
		menuItem.addActionListener(this);
		transformationButton.add(menuItem);

		// Flip vertically.
		menuItem = new JMenuItem(Texts.getText("flip_vertically"));
		menuItem.setActionCommand("flipVertically");
		menuItem.addActionListener(this);
		transformationButton.add(menuItem);

		// Reset all transformations.
		menuItem = new JMenuItem(Texts.getText("reset_transformations"));
		menuItem.setActionCommand("resetTransformations");
		menuItem.addActionListener(this);
		transformationButton.add(menuItem);

		return transformationButton;
	}


	/**
	 * Own slider class for adjusting the speed of the replay.
	 */
	private class ReplaySpeedSlider extends JSlider {

		public ReplaySpeedSlider(int min, int max, int value) {
			super(min, max, value);
			setOrientation(SwingConstants.HORIZONTAL);
			setPreferredSize(new Dimension(200, 15));

			// The slider should have an own UI.
			setUI(new ReplaySpeedSliderUI(this));
		}

		@Override
		public void updateUI() {
			super.updateUI();
			// Always set the own UI.
			setUI(new ReplaySpeedSliderUI(this));
		}


		/**
		 * An own UI class for the history slider.
		 */
		private class ReplaySpeedSliderUI extends BasicSliderUI {

			// The left and right border.
			private ReplaySpeedSlider slider;

			// Colors the slider is drawn with.
			private Color lowValueColor  = new Color(118, 238, 198, 150);  //Aquamarine2
			private Color highValueColor = new Color(238,  44,  44, 150);  //Firebrick2

			public ReplaySpeedSliderUI(ReplaySpeedSlider slider) {
				super(slider);
				this.slider = slider;
				setOpaque(false);
			}

			@Override
			public void paint(Graphics g, JComponent c){
				Graphics2D g2 = (Graphics2D) g;

				int sliderPosition = xPositionForValue(slider.getValue());
				g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
				g2.setPaint(new GradientPaint(0, 0, lowValueColor, slider.getWidth(), 0, highValueColor));
				g2.fillRect(0, 0, sliderPosition , slider.getHeight());
			}

			@Override
			protected void calculateTrackBuffer() {
				// No gap at the top and the bottom of the slider but a border left and right.
				trackBuffer = 0;
			}
			@Override
			protected void scrollDueToClickInTrack(int direction) {

				int value = slider.getValue();
				if (slider.getOrientation() == SwingConstants.HORIZONTAL) {
					value = this.valueForXPosition(slider.getMousePosition().x);
				} else if (slider.getOrientation() == SwingConstants.VERTICAL) {
					value = this.valueForYPosition(slider.getMousePosition().y);
				}
				slider.setValue(value);

				// Simulate a click in the thumb area to start dragging.
				trackListener.mousePressed(new MouseEvent(slider, 0, 0L, 0, thumbRect.x, thumbRect.y, 1, false));
			}

			@Override
			protected Dimension getThumbSize() {
				return new Dimension(10, 15);
			}
		}
	}

    /**
     * Adds an <code>ActionListener</code> to this GUI.
     * @param l the <code>ActionListener</code> to be added
     */
    public void addActionListener(ActionListener l) {
        listenerList.add(ActionListener.class, l);
    }

    /**
     * Removes an <code>ActionListener</code> from this GUI.
     *
     * @param l the listener to be removed
     */
    public void removeActionListener(ActionListener l) {
        listenerList.remove(ActionListener.class, l);
    }

    /**
     * Notifies all listeners that have registered interest for
     * notification on this event type.
     *
     * @param event  the <code>ActionEvent</code> object
     */
    protected void fireActionPerformed(ActionEvent event) {
    	// Guaranteed to return a non-null array
    	Object[] listeners = listenerList.getListenerList();
    	// Process the listeners last to first, notifying
    	// those that are interested in this event
    	for (int i = listeners.length-2; i>=0; i-=2) {
    		if (listeners[i]==ActionListener.class) {
    			((ActionListener)listeners[i+1]).actionPerformed(event);
    		}
    	}
    }
}