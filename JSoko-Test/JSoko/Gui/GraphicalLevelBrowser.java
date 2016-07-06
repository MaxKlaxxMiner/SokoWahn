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
import java.awt.Dialog.ModalityType;
import java.awt.Dimension;
import java.awt.GridLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.util.ArrayList;
import java.util.Hashtable;

import javax.swing.BorderFactory;
import javax.swing.ImageIcon;
import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JSlider;
import javax.swing.SwingConstants;
import javax.swing.border.BevelBorder;
import javax.swing.border.TitledBorder;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.LevelCollection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * Class for graphically displaying levels for choosing one level for playing.
 * The graphical browser is a modal dialog.  If no level is to be chosen,
 * the dialog has to be exited (killed).
 */
@SuppressWarnings("serial")
public class GraphicalLevelBrowser extends JPanel implements MouseListener, ActionListener, ChangeListener {

	/** Main object holding references to the main objects of the whole application */
	protected JSoko application;

	/** Object holding reference to the window that is used to contain this level browser */
	protected JDialog dialog = null;

	/** The main panel */
	protected JPanel mainPanel = new JPanel();

	/** The panel for displaying the levels. */
	protected JPanel contentPanel = new JPanel();

	/** Constant indicating the forward traversing direction */
	protected static final int FORWARD = 1;

	/** Constant indicating the backward traversing direction */
	protected static final int BACKWARD = -1;

	/** Numbers of levels to be displayed in one row. */
	protected int levelsPerRow = 3;

	/** Number of levels to be displayed in one page */
	protected int levelsPerPage = levelsPerRow * levelsPerRow;

	/** Maximum number of levels to be displayed in one row */
	protected static final int MAX_LEVELS_PER_ROW = 6;

	/** Minimum number of levels to be displayed in one row */
	protected static final int MIN_LEVELS_PER_ROW = 1;

	/** Maximum number of levels to be displayed in one page */
	protected static final int MAX_LEVELS_PER_PAGE = MAX_LEVELS_PER_ROW * MAX_LEVELS_PER_ROW;

	/** List containing all the displayed level */
	protected ArrayList<Level> displayedLevels;

	/** Left panel */
	protected JPanel leftPanel;

	/** "Next" button */
	protected JButton nextButton;

	/** Right panel */
	protected JPanel rightPanel;

	/** "Previous" button */
	protected JButton prevButton;

	/** The number of the first level that is currently displayed on the browser */
	protected int firstLevelIndex;

	/** The number of the last level that is currently displayed on the browser */
	protected int lastLevelIndex;

	/** The number of levels per page slider */
	protected JSlider levelAmountSlider;

	/**
	 * Creates a new Graphical Level Browser
	 * @param application reference to the main object
	 */
	public GraphicalLevelBrowser(JSoko application) {

		setLayout(new BorderLayout());
		this.application = application;

		// Dialog for the case the user wants to display this panel as dialog.
		dialog = new JDialog();

		// Set the dimension of this level browser.
		setBounds(application.getBounds());

		mainPanel.setLayout(new BorderLayout());

		contentPanel.setLayout(new GridLayout(0, levelsPerRow));

		leftPanel = new JPanel();
		leftPanel.setPreferredSize(new Dimension(getWidth()/8, getHeight()));
		this.add(leftPanel, BorderLayout.WEST);

		rightPanel = new JPanel();
		rightPanel.setPreferredSize(new Dimension(getWidth()/8, getHeight()));
		this.add(rightPanel, BorderLayout.EAST);

		displayedLevels = new ArrayList<Level>();

		nextButton = new JButton(Utilities.getIcon("arrow right (own).png", null));
		nextButton.setPreferredSize(new Dimension(getWidth()/8, getHeight()));
		nextButton.setActionCommand("Next");
		nextButton.addActionListener(this);
		nextButton.setFocusable(false); // Just for a better look
		rightPanel.add(nextButton);

		prevButton = new JButton(Utilities.getIcon("arrow left (own).png", null));
		prevButton.setPreferredSize(new Dimension(getWidth()/8, getHeight()));
		prevButton.setActionCommand("Previous");
		prevButton.addActionListener(this);
		prevButton.setFocusable(false); // Just for a better look
		leftPanel.add(prevButton);

		// Create the levels per page slider
		levelAmountSlider = new JSlider(SwingConstants.HORIZONTAL,1,MAX_LEVELS_PER_ROW,3);
		levelAmountSlider.setPreferredSize(new Dimension(100, 60));// enough for showing the slider and the text in every L&F
		levelAmountSlider.setMajorTickSpacing(1);
		levelAmountSlider.setMinorTickSpacing(1);

		Hashtable<Integer, JLabel> labelTable = new Hashtable<Integer, JLabel>();
		for (int num = 1; num < 7; num++) {
			labelTable.put(new Integer(num), new JLabel((num * num) + ""));
		}
		levelAmountSlider.setLabelTable(labelTable);
		levelAmountSlider.setPaintLabels(true);
		levelAmountSlider.setBorder(
				BorderFactory.createTitledBorder(
						BorderFactory.createEmptyBorder(),
						Texts.getText("graphicalLevelBrowser.numberOfLevelsPerPage"),
						TitledBorder.CENTER, TitledBorder.ABOVE_TOP) );
		levelAmountSlider.addChangeListener(this);
		levelAmountSlider.setSnapToTicks(true);

		add(mainPanel, BorderLayout.CENTER);

		mainPanel.add(levelAmountSlider, BorderLayout.NORTH);
		mainPanel.add(contentPanel, BorderLayout.CENTER);

		firstLevelIndex = 0;
		lastLevelIndex  = 0;
	}


	/**
	 * Adds a level to be displayed
	 * @param level level to be displayed
	 */
	public void addLevel(Level level) {
		displayedLevels.add(level);
		if (displayedLevels.size() < levelsPerPage) {
			lastLevelIndex = displayedLevels.size() - 1;
		} else {
			lastLevelIndex = levelsPerPage - 1;
		}
	}

	/**
	 * Adds a collection of levels to be displayed
	 * @param collection collection of levels
	 * @param startNumber index of the start level in the collection
	 * @param endNumber index of the end level in the collection
	 */
	public void addLevelCollection(final LevelCollection collection, int startNumber, int endNumber) {
		for(int levelNo = startNumber; levelNo <= collection.getLevelsCount() && levelNo <= endNumber; levelNo++) {
			addLevel(collection.getLevel(levelNo));
		}
	}

	/**
	 * Pack all the levels in the list from the indicated start number to the end number
	 * @param startLevelIndex start index of the collection of levels
	 * @param endLevelIndex end index of the collection of levels
	 */
	public void packLevelForDisplay(int startLevelIndex, int endLevelIndex) {

		int maxIndex = displayedLevels.size() - 1;
		if (endLevelIndex > maxIndex) {
			endLevelIndex = maxIndex;
		}
		if (startLevelIndex < 0) {
			startLevelIndex = 0;
		}
        firstLevelIndex = startLevelIndex;
        lastLevelIndex = endLevelIndex;
        int currentLevelsPerRow = (int) Math.sqrt(lastLevelIndex - firstLevelIndex + 1);
        contentPanel.removeAll();
        contentPanel.setLayout(new GridLayout(currentLevelsPerRow, currentLevelsPerRow));
        for (int index = startLevelIndex; index <= endLevelIndex; index++) {
        	int levelNumber = index + 1;
        	Level currentLevel =  displayedLevels.get(index);

            JPanel levelContent = new JPanel(new BorderLayout());
        	JLabel levelLabel = new JLabel( levelNumber + " " + displayedLevels.get(index).getTitle());
            levelContent.add(levelLabel, BorderLayout.NORTH);
            if (currentLevel.getSolutionsManager().getSolutionCount() > 0) {
            	ImageIcon levelSolvedIcon = Utilities.getIcon("apply (oxygen).png", null);
            	levelLabel.setIcon(levelSolvedIcon);
            	levelLabel.setToolTipText(Texts.getText("solved"));
            }
            BoardDisplay levelDisplay = new BoardDisplay(currentLevel);
            levelDisplay.addMouseListener(this);
            levelDisplay.setBorder(BorderFactory.createBevelBorder(BevelBorder.RAISED, Color.LIGHT_GRAY, Color.gray));
            levelDisplay.setPreferredSize(new Dimension(contentPanel.getWidth()/currentLevelsPerRow, contentPanel.getHeight()/currentLevelsPerRow));
            levelContent.add(levelDisplay, BorderLayout.CENTER);

            contentPanel.add(levelContent);
        }
    }

	/**
	 * Traverse through next or previous base on the given number of levels
	 * @param numberOfLevels number of levels to traverse
	 * @param direction number indicating the direction to traverse
	 */
	public void traverse(int numberOfLevels, int direction) {

		if (direction == FORWARD) {
			if ((((displayedLevels.size() - 1) - lastLevelIndex) < numberOfLevels) && (((displayedLevels.size() - 1) - lastLevelIndex) > 0)){
				packLevelForDisplay(lastLevelIndex + 1, displayedLevels.size() - 1);
			}
			else if (lastLevelIndex < (displayedLevels.size() - 1)) {
				contentPanel.removeAll();
				packLevelForDisplay(lastLevelIndex + 1, lastLevelIndex + numberOfLevels);
			}
			this.updateUI();
		}

		if (direction == BACKWARD) {
			if (firstLevelIndex >= numberOfLevels) {
				if ((lastLevelIndex - firstLevelIndex) < numberOfLevels) {
					packLevelForDisplay(firstLevelIndex - numberOfLevels, lastLevelIndex - (lastLevelIndex - firstLevelIndex + 1));
				}
				else {
					packLevelForDisplay(firstLevelIndex - numberOfLevels, firstLevelIndex - 1);
				}
				this.updateUI();
			}
			else if (firstLevelIndex > 0) {
				packLevelForDisplay(0, firstLevelIndex - 1);
				this.updateUI();
			}
		}
	}

	/**
	 * Show the graphical level browser containing the indicated collection of levels as a dialog
	 * @param title title of the dialog
	 */
	public void showAsDialog(String title) {

        dialog.setBounds(application.getBounds());
        dialog.setTitle(title);
        Level currentDisplayedLevel = application.currentLevel;
        int currentLevelIndex = displayedLevels.indexOf(currentDisplayedLevel);
        packLevelForDisplay(currentLevelIndex, currentLevelIndex + levelsPerPage - 1);
        dialog.add(this);
        dialog.setModalityType(ModalityType.APPLICATION_MODAL);
        Utilities.setEscapable(dialog);
        dialog.setVisible(true);
	}

	/**
	 * We want to set a highlighting (yellow) border around the level.
	 * The thickness of this border should be approximately the same for
	 * the different levels we can see, but it should also be no larger
	 * than the graphic size of a square from the skin.  Otherwise we
	 * are going to obscure relevant parts of the graphic.
	 *
	 * @param levelDisplay the Board just entered with mouse
	 * @return highlighting border thickness to represent entering
	 */
	private static int highlightThickness(BoardDisplay levelDisplay) {
		int levelHeight = levelDisplay.getHeight();
		int levelWidth  = levelDisplay.getWidth();
		int levelSize = Math.min(levelWidth, levelHeight);

		int thickness = levelSize / 24;

		int sqWidth  = levelDisplay.getSquareWidth();
		int sqHeight = levelDisplay.getSquareHeight();
		int sqSize   = Math.min(sqWidth, sqHeight);

		if (sqSize > 0) {
			if (thickness > sqSize) {
				thickness = sqSize;
			}
		}
		// FFS/hm this is not yet optimal: the displayed boards have different unused margins

		// We establish a minimum of 1 for the thickness, as else we just see nothing
		if (thickness < 1) {
			thickness = 1;
		}
		return thickness;
	}

	@Override
	public void mouseClicked(MouseEvent e) {
		Object source = e.getSource();
		if (source instanceof BoardDisplay) {
			BoardDisplay clickedBoardDisplay = (BoardDisplay) source;
			Level clickedLevel = clickedBoardDisplay.getDisplayedLevel();
			application.setLevelForPlaying(clickedLevel);
			dialog.dispose();
		}
	}

	@Override
	public void mouseEntered(MouseEvent e) {
		Object source = e.getSource();
		if (source instanceof BoardDisplay) {
			BoardDisplay enteredLevel = (BoardDisplay) source;
			enteredLevel.setBorder(BorderFactory.createLineBorder(Color.YELLOW, highlightThickness(enteredLevel)));
			this.updateUI();
		}
	}

	@Override
	public void mouseExited(MouseEvent e) {
		Object source = e.getSource();
		if (source instanceof BoardDisplay) {
			BoardDisplay enteredLevel = (BoardDisplay) source;
			enteredLevel.setForeground(null);
			enteredLevel.setBorder(BorderFactory.createBevelBorder(BevelBorder.RAISED, Color.LIGHT_GRAY, Color.gray));
			this.updateUI();
		}
	}

	@Override
	public void mousePressed(MouseEvent e) {}

	@Override
	public void mouseReleased(MouseEvent e) {}

	@Override
	public void actionPerformed(ActionEvent e) {
		String command = e.getActionCommand();
		if (command.equals("Next")) {
			traverse(levelsPerPage, FORWARD);
		}

		if (command.equals("Previous")) {
			traverse(levelsPerPage, BACKWARD);
		}
	}

	@Override
	public void stateChanged(ChangeEvent e) {
		JSlider slider = (JSlider) e.getSource();
		if (!slider.getValueIsAdjusting()) {
			int chosenAmount = slider.getValue();
			this.levelsPerPage = chosenAmount * chosenAmount;
			int desiredIndex = firstLevelIndex + levelsPerPage - 1;
			int maxIndex = displayedLevels.size() - 1;
			if (lastLevelIndex >= maxIndex) {
				// Keeps focus the levels at the end of the level list
				this.packLevelForDisplay(maxIndex - levelsPerPage + 1, maxIndex);
			} else {
				this.packLevelForDisplay(firstLevelIndex, desiredIndex);
			}
			this.updateUI();
		}
	}
}