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
package de.sokoban_online.jsoko.optimizer.GUI;

import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.GridLayout;
import java.awt.Insets;
import java.awt.event.ActionEvent;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.event.WindowEvent;
import java.text.ParseException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

import javax.swing.AbstractButton;
import javax.swing.AbstractSpinnerModel;
import javax.swing.BorderFactory;
import javax.swing.BoxLayout;
import javax.swing.ButtonGroup;
import javax.swing.ButtonModel;
import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JFormattedTextField;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JRadioButton;
import javax.swing.JScrollPane;
import javax.swing.JSpinner;
import javax.swing.JTextField;
import javax.swing.JTextPane;
import javax.swing.SpinnerNumberModel;
import javax.swing.SwingUtilities;
import javax.swing.border.BevelBorder;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.text.BadLocationException;
import javax.swing.text.Style;
import javax.swing.text.StyleConstants;
import javax.swing.text.StyleContext;
import javax.swing.text.StyledDocument;

import de.sokoban_online.jsoko.ExceptionHandler;
import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.gui.NumberInputTF;
import de.sokoban_online.jsoko.gui.StartStopButton;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsGUI;
import de.sokoban_online.jsoko.optimizer.Optimizer;
import de.sokoban_online.jsoko.optimizer.Optimizer.OptimizationMethod;
import de.sokoban_online.jsoko.optimizer.OptimizerSolution;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * GUI for the optimizer.
 */
@SuppressWarnings("serial")
public final class OptimizerGUI extends OptimizerGUISuperClass {

	// Buttons for the optimization method.
	private JRadioButton movesOptimization;
	private JRadioButton pushesOptimization;
	private JRadioButton pushesAllMetricsOptimization;
	private JRadioButton movesAllMetricsOptimization;
	private JRadioButton boxLinesOnlyOptimization;
	private JRadioButton boxChangesOptimization;

	// DEUBG ONLY: check box indicating whether the optimizer should find the best vicinity settings.
	private JCheckBox isOptimizerToFindBestVicinitySettings = new JCheckBox(); // dummy unselected check box

	// CheckBox for turning iterating on/off.
	JCheckBox isIteratingEnabled;

	// CheckBox determining whether only the last solution is saved in iterating mode or all solutions.
	JCheckBox isOnlyLastSolutionToBeSaved;

	// The displayed board in the optimizer.
	OptimizerBoardDisplay boardDisplay = null;

	// GUI elements for setting the maximum number of box configurations to be generated
	// by the optimizer.  This number directly influences the RAM usage of the program
	// and is therefore important to avoid out-of-memory errors.
	private JRadioButton isMaxBoxConfigurationManuallySet;
	private JSpinner 	 maxBoxConfigurationsToBeGenerated;


	/**
	 * Variables for the pushes range optimization.
	 */
	// GUI elements for setting a range of pushes to be considered for optimizing a solution.
	JRadioButton isRangeOfPushesActivated;
	JSpinner     rangeOfPushesFromValue;
	JSpinner     rangeOfPushesToValue;

	// Checkbox for setting the flag indicating whether the player position is fixed
	// for all solutions. That means: the optimizer may only search
	// for solutions where the player ends at the same location as in the basis solution.
	private JCheckBox isPlayerPositionToBePreserved;

	// If the optimizer only optimizes a part of a solution (the user has selected a
	// specific pushes range to be optimized) then these variables hold the prefix and
	// suffix moves to be added to the solution and the number of prefix moves and pushes.
	private String prefixSolutionMoves = "";
	private String suffixSolutionMoves = "";

	/** The range of pushes to be optimized. */
	int optimizeFromPush, optimizeToPush;

	/** Number of threads to be used by the optimizer. */
	private JSpinner threadsCount;


	/** The user may select more than one solution for optimizing.
	 *  This list contains all selected solutions.
	 */
	List<Solution> selectedSolutionsToBeOptimized = new ArrayList<Solution>();

	/** If only a pushes range of a solution is to be optimized a new optimizer is
	 *  created for optimizing a new board created from the relevant part of the solution.
	 *  The old optimizer which is used for optimizing the whole level is then saved
	 *  in this variable.
	 */
	Optimizer optimizerOriginalLevel = null;


	/**
	 * Creates an object for optimizing a solution.
	 *
	 * @param application  reference to the main object that holds references
	 *                     to all other objects
	 */
	public OptimizerGUI(final JSoko application) {

		// Reference to the main object of this program holding all references.
		this.application = application;

		// Save the reference to the currently loaded level the optimizer has been opened for.
		currentLevel = application.currentLevel;

		// Register the optimizer due to stopping the optimizer when an out-of-memory error occurs.
		ExceptionHandler.INSTANCE.addHandler(this);

		// Create a new board with the initial level board position. This is done in order
		// not to change the original board and to ensure the board is in the initial state.
		try {
			board = new Board();
			board.setBoardFromString(currentLevel.getBoardDataAsString());
			if(board.isValid(new StringBuilder()) == false) {
				return;
			}
			board.prepareBoard();
		} catch (Exception e) {
			e.printStackTrace();
			return;
		}

		// The vicinity settings fields.
		vicinitySettings = new ArrayList<NumberInputTF>(4);

		// Display the GUI of the optimizer.
		createGUI();

		// Create a new optimizer object.
		optimizer = new Optimizer(board, this, null);

		// Backup the optimizer for the board. If the user wants to optimize a pushes range
		// of the solution a new board is created and the original optimizer can later
		// be set back quickly without creating a new one.
		optimizerOriginalLevel = optimizer;

		// Adjust settings according to the saved ones.
		setSettings();

		// Show the optimizer GUI.
		setVisible(true);

		// The help is registered on the root pane. It requests the focus so pressing F1 opens the help for the optimizer
		getRootPane().requestFocus();
	}

	/**
	 * Sets all settings in this GUI that haven't been set, yet.
	 * <p>
	 * The settings to be set have been read from the hard disk, see {@link Settings}.
	 */
	private void setSettings() {
		// If no bounds settings have been saved, yet, set the bounds of the main GUI.
		if(Settings.optimizerXCoordinate == -1) {
			setBounds(application.getBounds());
		} else {
			setBounds(Settings.optimizerXCoordinate, Settings.optimizerYCoordinate, Settings.optimizerWidth, Settings.optimizerHeight);
		}

		OptimizationMethod optimizationMethod = OptimizationMethod.values()[Settings.getInt("optimizationMethod", OptimizationMethod.MOVES_PUSHES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS.ordinal())];
		switch (optimizationMethod) {
		case MOVES_PUSHES:
			movesOptimization.setSelected(true);
			break;
		case PUSHES_MOVES:
			pushesOptimization.setSelected(true);
			break;
		case MOVES_PUSHES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS:
			movesAllMetricsOptimization.setSelected(true);
			break;
		case PUSHES_MOVES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS:
			pushesAllMetricsOptimization.setSelected(true);
			break;
		case BOXLINES:
			boxLinesOnlyOptimization.setSelected(true);
			break;
		case BOXCHANGES:
			boxChangesOptimization.setSelected(true);
			break;
		default:
			movesAllMetricsOptimization.setSelected(true);
		}
	}

	/**
	 * Displays the GUI of the optimizer.
	 */
	final private void createGUI() {

		// Set the title.
		setTitle(Texts.getText("optimizer.JSokoOptimizer") + " - " + currentLevel.getNumber() +  " - " + currentLevel.getTitle());

		// A main panel is added to this JFrame. All other GUI elements are added to a scroll pane
		// which is added to the main panel.
		JPanel mainPanel = new JPanel();
		add(new JScrollPane(mainPanel));

		// Set BorderLayout for this GUI.
		mainPanel.setLayout(new BorderLayout());

		// Add the GUI of the level at the center.
		boardDisplay = new OptimizerBoardDisplay(currentLevel);
		boardDisplay.setBorder(BorderFactory.createBevelBorder( BevelBorder.RAISED,
				                                                Color.LIGHT_GRAY,
				                                                Color.gray ));
		Texts.helpBroker.enableHelpKey(boardDisplay, "optimizer.RestrictingTheAreaToBeOptimized", null); // Enable help
		mainPanel.add(boardDisplay, BorderLayout.CENTER);

		// In the south a new JPanel is added containing all optimizer specific elements.
		JPanel southPanel = new JPanel(new GridBagLayout());
		mainPanel.add(southPanel, BorderLayout.SOUTH);

		// Set constraints.
		GridBagConstraints constraints = new GridBagConstraints();

		// Add a dummy label to make some room between the displayed level and the optimizer elements.
		constraints.gridx = 5;
		constraints.gridy = 0;
		constraints.insets = new Insets(4, 0, 0, 4);
		southPanel.add(new JLabel(""), constraints);

		/*
		 * Create a panel for the vicinity settings and add 3 number fields for letting
		 * the user set the distance how far the boxes may be repositioned.
		 */
		JPanel vicinitySettingsPanel = new JPanel(new GridLayout(4, 1));
		vicinitySettingsPanel.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(Texts.getText("vicinitySquares")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						vicinitySettingsPanel.getBorder()));
		Texts.helpBroker.enableHelpKey(vicinitySettingsPanel, "optimizer.RestrictingTheSearch", null); // Enable help

		// Add the input fields for the vicinity settings.
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox1Enabled, Texts.getText("box") + " 1", 1, 999, Settings.vicinitySquaresBox1, true));
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox2Enabled, Texts.getText("box") + " 2", 1, 999, Settings.vicinitySquaresBox2, true));
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox3Enabled, Texts.getText("box") + " 3", 1, 999, Settings.vicinitySquaresBox3, true));
//		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox4Enabled, Texts.getText("box") + " 4", 1, 999, Settings.vicinitySquaresBox4, true)); // in nearly all levels 3 boxes are sufficient

		// Add the input fields to the panel.
		for (NumberInputTF numberField : vicinitySettings) {
			vicinitySettingsPanel.add(numberField);
		}

		if(Debug.isDebugModeActivated || Debug.isFindSettingsActivated) {  		// DEBUG ONLY: find good vicinity settings.
			((GridLayout) vicinitySettingsPanel.getLayout()).setRows(5);
			isOptimizerToFindBestVicinitySettings = new JCheckBox("Finde Settings");
			vicinitySettingsPanel.add(isOptimizerToFindBestVicinitySettings);
		}

		// Add the vicinity settings panel to the main panel.
		constraints.gridx = 1;
		constraints.gridy++;
		constraints.gridheight = 2;
		constraints.anchor = GridBagConstraints.NORTH;
		constraints.insets.set(0, 0, 0, 0);
		southPanel.add(vicinitySettingsPanel, constraints);


		/*
		 * Panel for the optimization method.
		 */
		JPanel mainOptimizerPanel = new JPanel(new BorderLayout());
		JPanel optimizationMethod = new JPanel(new GridLayout(6, 1));
		optimizationMethod.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("optimizationMethod")),
														   BorderFactory.createEmptyBorder(5, 5, 5, 5)), optimizationMethod.getBorder()));
		Texts.helpBroker.enableHelpKey(optimizationMethod, "optimizer.OptimizationMethod", null); // Enable help

		ButtonGroup optimizationType = new ButtonGroup();

		movesOptimization = new JRadioButton(Texts.getText("moves") + "/" + Texts.getText("pushes"), true);
		optimizationType.add(movesOptimization);
		optimizationMethod.add(movesOptimization, 0);

		pushesOptimization = new JRadioButton(Texts.getText("pushes") + "/" + Texts.getText("moves"));
		optimizationType.add(pushesOptimization);
		optimizationMethod.add(pushesOptimization, 1);

		movesAllMetricsOptimization = new JRadioButton(Texts.getText("optimizer.movesAllMetricsOptimizationMethod"));
		movesAllMetricsOptimization.setToolTipText(Texts.getText("moves") + "/" + Texts.getText("pushes") + "/" + Texts.getText("boxLines") + "/" + Texts.getText("boxChanges") + "/" + Texts.getText("pushingSessions"));
		optimizationType.add(movesAllMetricsOptimization);
		optimizationMethod.add(movesAllMetricsOptimization, 2);

		pushesAllMetricsOptimization = new JRadioButton(Texts.getText("optimizer.pushesAllMetricsOptimizationMethod"));
		pushesAllMetricsOptimization.setToolTipText(Texts.getText("pushes") + "/" + Texts.getText("moves") + "/" + Texts.getText("boxLines") + "/" + Texts.getText("boxChanges") + "/" + Texts.getText("pushingSessions"));
		optimizationType.add(pushesAllMetricsOptimization);
		optimizationMethod.add(pushesAllMetricsOptimization, 3);

		boxLinesOnlyOptimization = new JRadioButton(Texts.getText("boxLinesOnly"));
		optimizationType.add(boxLinesOnlyOptimization);
		optimizationMethod.add(boxLinesOnlyOptimization, 4);

		boxChangesOptimization = new JRadioButton(Texts.getText("boxChanges"));
		optimizationType.add(boxChangesOptimization);
		optimizationMethod.add(boxChangesOptimization, 5);

		constraints.gridheight = 2;
		constraints.gridy = 1;
		constraints.gridx++;
		mainOptimizerPanel.add(optimizationMethod, BorderLayout.NORTH);


		/*
		 * Panel for setting a pushes range which is to be optimized.
		 */
		JPanel pushesRangePanel = new JPanel(new BorderLayout());
		pushesRangePanel.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(Texts.getText("optimizer.range")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						pushesRangePanel.getBorder()));
		Texts.helpBroker.enableHelpKey(pushesRangePanel, "optimizer.RestrictingThePushesRange", null); // Enable help

			// Button group for "optimize whole collection" and "optimize range of solution" selection.
			ButtonGroup optimizingRange = new ButtonGroup();

			// Button for selecting "optimize the whole solution".
			final JRadioButton isCompleteSolutionActivated = new JRadioButton(Texts.getText("optimizer.completeSolution"), true);
			optimizingRange.add(isCompleteSolutionActivated);
			pushesRangePanel.add(isCompleteSolutionActivated, BorderLayout.NORTH);

			// Button for selecting "range of solution".
			isRangeOfPushesActivated = new JRadioButton("");
			optimizingRange.add(isRangeOfPushesActivated);
			pushesRangePanel.add(isRangeOfPushesActivated, BorderLayout.WEST);

			// Panel containing the spinners for setting the range to optimize.
			JPanel rangeSelectionPanel = new JPanel(new FlowLayout(FlowLayout.LEADING, 3, 5));
				rangeSelectionPanel.add(new JLabel(Texts.getText("optimizer.rangeFrom")));
			    final SpinnerNumberModel model1 = new SpinnerNumberModel(0, 0, 99999, 1);
			    rangeOfPushesFromValue = new JSpinner( model1 );
			    ((JSpinner.DefaultEditor)rangeOfPushesFromValue.getEditor()).getTextField().setColumns(5);
			    rangeSelectionPanel.add(rangeOfPushesFromValue);

			    rangeSelectionPanel.add(new JLabel(Texts.getText("optimizer.rangeTo")));
			    final SpinnerNumberModel model2 = new SpinnerNumberModel(99999, 1, 99999, 1);
			    rangeOfPushesToValue = new JSpinner( model2 );
			    ((JSpinner.DefaultEditor)rangeOfPushesToValue.getEditor()).getTextField().setColumns(5);
			    rangeSelectionPanel.add(rangeOfPushesToValue);
			pushesRangePanel.add(rangeSelectionPanel, BorderLayout.CENTER);

			// Change listener for the pushes range setting.
			ChangeListener changeListener = new ChangeListener() {

				@Override
				public void stateChanged(ChangeEvent e) {

					/**
					 * This method is called when the "pushes range" radio button, the "complete solution radio button"
					 * or the spinner have been pressed.
					 */

					// Check whether one of the radio buttons has fired a change.
					if(e.getSource() instanceof AbstractButton) {
						AbstractButton aButton = (AbstractButton) e.getSource();
				        ButtonModel aModel = aButton.getModel();

				        // Ensure the button had been deselected and is to be selected now.
				        if(! (aModel.isPressed() == true && aModel.isSelected() == false)) {
				        	return;
				        }

				        // Check whether the button indicating that the whole solution is to be optimized has been pressed.
			        	if(aButton == isCompleteSolutionActivated) {

							// For optimizing the whole solution the normal board is set. Even if the pushes range were set to 0 to "pushes of solution"
			        		// then there may be boxes that are never pushed which are converted to walls.
			        		boardDisplay.setBoardToDisplay(board);

			        		// If the user has entered a new number in the pushes range spinners and then pressed the "isCompleteSolutionActivated"
			        		// button, then a spinner change event is fired, too (after this event) while the button is still "false". Hence,
			        		// ensure the button is already set to "true", so the spinner event is rejected (see the "if" in the following "else" branch).
			        		aButton.setSelected(true);
							return;
			        	}
					}

					// Spinner value changes are only relevant if the pushes range optimization is activated.
					if(e.getSource() instanceof AbstractSpinnerModel) {
						if(isRangeOfPushesActivated.isSelected() == false) {
							return;
						}
					}

					// The pushes range button has been selected or the corresponding spinners have been changed.
					// => Set a new board from the selected pushes range of the solution.
					setBoardToDisplayFromPushesRange();
				}
			};

			// Pressing the "enter" key should fire a change event.
			KeyAdapter keyListener = new KeyAdapter() {
	            @Override
				public void keyTyped(KeyEvent e) {
	            	if(e.getKeyChar() == KeyEvent.VK_ENTER && e.getSource() instanceof JFormattedTextField) {
	            		JFormattedTextField textfield = (JFormattedTextField) e.getSource();
	                	try {
	                		textfield.commitEdit();
						} catch (ParseException e1) {}
	            	}
	            }
	        };
			((JSpinner.DefaultEditor)rangeOfPushesFromValue.getEditor()).getTextField().addKeyListener(keyListener);
			((JSpinner.DefaultEditor)rangeOfPushesToValue.getEditor()).getTextField().addKeyListener(keyListener);

			// If the pushes range has been set to a new value the change is displayed by creating a new board.
			// Switching from "pushes range" to "complete solution" should also fire a change.
			model1.addChangeListener(changeListener);
			model2.addChangeListener(changeListener);
			isCompleteSolutionActivated.addChangeListener(changeListener);
			isRangeOfPushesActivated.addChangeListener(changeListener);

		mainOptimizerPanel.add(pushesRangePanel, BorderLayout.CENTER);


		/*
		 * Button for starting the optimizer.
		 */
		startButton = new StartStopButton( "startOptimizer", "startOptimizer",
				                           "stopOptimizer",  "stopOptimizer"  );

		startButton.setFocusPainted(false);
		startButton.addActionListener(this);
		constraints.gridheight = 1;
		constraints.gridwidth  = 1;
		constraints.gridx = 2;
		constraints.gridy++;
		mainOptimizerPanel.add(startButton, BorderLayout.SOUTH);

		constraints.gridy = 1;
		constraints.gridheight = 2;
		southPanel.add(mainOptimizerPanel, constraints);

		/*
		 * List showing the metrics of all solutions of the level.
		 */
		solutionsGUI = new SolutionsGUI(application, false, false);
		Texts.helpBroker.enableHelpKey(solutionsGUI, "optimizer.SolutionsList", null); // Enable help for the solutions list
		solutionsGUI.setLevel(currentLevel);

		// Put the list into a scroll pane and select the first solution.
		JScrollPane listScroller = new JScrollPane(solutionsGUI);
		listScroller.setAlignmentX(LEFT_ALIGNMENT);
		listScroller.setPreferredSize(new Dimension(200, 130));
		listScroller.setMinimumSize(listScroller.getPreferredSize());
		solutionsGUI.setSelectedIndex(0);

		// Display the whole level again when a new solution has been selected.
		// This has a better performance compared to always creating a new board
		// of the selected pushes range for the new solution.
		solutionsGUI.addListSelectionListener(new ListSelectionListener() {
			@Override
			public void valueChanged(ListSelectionEvent e) {

				if(isCompleteSolutionActivated.isSelected()) {
					// For optimizing the whole solution the normal board is set. Even if the pushes range were set
					// to 0 to "pushes of solution" there may be boxes that are never pushed which are converted to walls.
	        		boardDisplay.setBoardToDisplay(board);
				}

				if(isRangeOfPushesActivated.isSelected()) {
					// Create a new board from the pushes range.
					setBoardToDisplayFromPushesRange();
				}

			}
		});

		// Label for the scroll pane.
		JPanel listPane = new JPanel();
		listPane.setLayout(new BoxLayout(listPane, BoxLayout.PAGE_AXIS));
		JLabel label = new JLabel(Texts.getText("solutions"));
		label.setLabelFor(listScroller);
		listPane.add(label);
		listPane.add(listScroller);
		listPane.setBorder(BorderFactory.createEmptyBorder(0, 2, 0, 0));

		// Add the scroll pane to the panel.
		constraints.gridy = 1;
		constraints.gridx++;
		constraints.fill = GridBagConstraints.BOTH;
		southPanel.add(listPane, constraints);

		// Layout panel for iterating and maximum box configurations
		JPanel layoutPanel = new JPanel(new BorderLayout());

			/*
			 * Panel for additional settings for iterative optimizing.
			 */
			JPanel iterationPanel = new JPanel(new GridLayout(2, 1));
			iterationPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("iteratingOptimizing")), BorderFactory.createEmptyBorder(5, 5, 5, 5)), iterationPanel.getBorder()));
			Texts.helpBroker.enableHelpKey(iterationPanel, "optimizer.IterativeOptimizing", null); // Enable help

				// A CheckBox for turning iteration on/off.
				isIteratingEnabled = new JCheckBox(Texts.getText("activateIterating"), Settings.isIteratingEnabled);
				isIteratingEnabled.setToolTipText(Texts.getText("activateIteratingTooltip"));
				iterationPanel.add(isIteratingEnabled);

				// A CheckBox for turning iteration on/off.
				isOnlyLastSolutionToBeSaved = new JCheckBox(Texts.getText("onlyKeepLastSolution"), Settings.isOnlyLastSolutionToBeSaved);
				isOnlyLastSolutionToBeSaved.setToolTipText(Texts.getText("onlyKeepLastSolutionTooltip"));
				iterationPanel.add(isOnlyLastSolutionToBeSaved);

		layoutPanel.add(iterationPanel, BorderLayout.NORTH);


			/*
			 * Panel for setting the maximum number of generated box configurations.
			 */
			JPanel maxBoxConfigurationsPanel = new JPanel(new BorderLayout());
			maxBoxConfigurationsPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("optimizer.maxBoxConfigurations")), BorderFactory.createEmptyBorder(5, 5, 5, 5)), maxBoxConfigurationsPanel.getBorder()));
			maxBoxConfigurationsPanel.setToolTipText(Texts.getText("optimizer.maxBoxConfigurationPanelTooltip"));
			Texts.helpBroker.enableHelpKey(maxBoxConfigurationsPanel, "optimizer.MaximumNumberOfBoxConfigurations", null); // Enable help

				// Button group for setting the maximum number of box configurations to be generated.
				ButtonGroup maxBoxConfigurationsButtonGroup = new ButtonGroup();

				// Button for selecting "automatically set maximum number". This button isn't really used. It's just there to deselect the next radio button.
				JRadioButton dummy2 = new JRadioButton(Texts.getText("optimizer.calculateAutomatically"), true);
				maxBoxConfigurationsButtonGroup.add(dummy2);
				maxBoxConfigurationsPanel.add(dummy2, BorderLayout.NORTH);

				// Button for selecting "manually set maximum number of box configurations to be generated".
				isMaxBoxConfigurationManuallySet = new JRadioButton("");
				maxBoxConfigurationsButtonGroup.add(isMaxBoxConfigurationManuallySet);
				maxBoxConfigurationsPanel.add(isMaxBoxConfigurationManuallySet, BorderLayout.WEST);

				// Panel containing the spinners for setting the value.
				JPanel valuePanel = new JPanel(new FlowLayout(FlowLayout.LEADING, 3, 5));
					valuePanel.add(new JLabel(Texts.getText("optimizer.setManually")));
				    maxBoxConfigurationsToBeGenerated = new JSpinner(new SpinnerNumberModel(1, 1, Integer.MAX_VALUE, 1));
				    ((JSpinner.DefaultEditor)maxBoxConfigurationsToBeGenerated.getEditor()).getTextField().setColumns(5);
				    valuePanel.add(maxBoxConfigurationsToBeGenerated);
				    valuePanel.add(new JLabel(Texts.getText("general.thousand")));
				maxBoxConfigurationsPanel.add(valuePanel, BorderLayout.CENTER);

		layoutPanel.add(maxBoxConfigurationsPanel, BorderLayout.CENTER);

		/*
		 * Panel for setting the player position fixed for all found solutions.
		 */
		JPanel specialSettingsPanel = new JPanel(new BorderLayout());
		specialSettingsPanel.setBorder(BorderFactory.createCompoundBorder(
				BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("optimizer.specialSettings")),
				BorderFactory.createEmptyBorder(5, 5, 5, 5)), specialSettingsPanel.getBorder()));
			Texts.helpBroker.enableHelpKey(specialSettingsPanel, "optimizer.PreservePlayerEndPosition", null); // Enable help

			// Checkbox for setting the optimizer to preserve the player end position.
			isPlayerPositionToBePreserved = new JCheckBox(Texts.getText("optimizer.playerEndPositionIsFix"), false);
			specialSettingsPanel.add(isPlayerPositionToBePreserved, BorderLayout.NORTH);

			// The user may restrict the optimizer to only use a specific number of threads.
			// The text "CPUs" is used because this is easier to understand for the users.
			JPanel threads = new JPanel(new FlowLayout(FlowLayout.LEADING, 3, 5));
			threads.add(new JLabel(Texts.getText("optimizer.CPUsToUse")));
			int CPUCoresCountInitialValue = Math.min(Runtime.getRuntime().availableProcessors(), Settings.CPUCoresToUse);
			threadsCount = new JSpinner(new SpinnerNumberModel(CPUCoresCountInitialValue, 1, Runtime.getRuntime().availableProcessors(), 1));
			threads.add(threadsCount);
			specialSettingsPanel.add(threads, BorderLayout.SOUTH);

		layoutPanel.add(specialSettingsPanel, BorderLayout.SOUTH);

		constraints.gridheight = 2;
		constraints.gridy = 1;
		constraints.gridx++;
		constraints.fill = GridBagConstraints.NONE;
		constraints.anchor = GridBagConstraints.NORTHWEST;
		southPanel.add(layoutPanel, constraints);


		/*
		 * Panel for showing a TextArea for displaying log info while the optimizer
		 * is running. The log may contain a lot of information.
		 * Hence it is added as east panel for the whole GUI.
		 */
		JPanel logTextPanel = new JPanel(new BorderLayout());
		logTextPanel.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(Texts.getText("optimizer.logText")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						logTextPanel.getBorder()));
		optimizerLog = new JTextPane();
		optimizerLog.setEditable(false);
		Texts.helpBroker.enableHelpKey(optimizerLog, "optimizer.OptimizerLog", null); // Enable help for the optimizer log

		//Initialize some styles.
		// - Style "regular" from the default
		// - Style "italic"  as "regular" with setItalics()
		// - Style "bold"    as "regular" with setBold()
		// FFS/hm@mm: All three are set SansSerif
        Style def = StyleContext.getDefaultStyleContext().getStyle(StyleContext.DEFAULT_STYLE);
        Style regular = optimizerLog.addStyle("regular", def);

        StyleConstants.setFontFamily(def, "SansSerif");

        Style s = optimizerLog.addStyle("italic", regular);
        StyleConstants.setItalic(s, true);

        s = optimizerLog.addStyle("bold", regular);
        StyleConstants.setBold(s, true);

        // - Style "bold"    as "regular" but family "Monospaced" used for display for debug outpout
        s = optimizerLog.addStyle("debug", regular);
        StyleConstants.setFontFamily(s, "Monospaced");
        StyleConstants.setForeground(s, Color.blue);

		logTextPanel.add(new JScrollPane(optimizerLog));
		logTextPanel.setPreferredSize(new Dimension((int) getPreferredSize().getWidth()/2,
				                                    (int) getPreferredSize().getHeight()/2));

		mainPanel.add(logTextPanel, BorderLayout.EAST);

		/*
		 * This is the text field for showing status information.
		 */
		constraints.gridx = 0;
		constraints.gridy = 4;
		constraints.gridheight = 1;
		constraints.gridwidth = 6;
		constraints.weightx = 1;
		constraints.insets.set(5, 10, 5, 10);
		constraints.fill = GridBagConstraints.HORIZONTAL;
		constraints.anchor = GridBagConstraints.LINE_START;
		infoText = new JTextField();
		infoText.setEditable(false);
		southPanel.add(infoText, constraints);

		// ONLY IN DEBUG MODE
		if(Debug.isDebugModeActivated) {
			JButton activeAreaDebug = new JButton("ActiveArea moves/pushes");
			activeAreaDebug.setActionCommand("displayMovesPushesInActiveArea");
			activeAreaDebug.addActionListener(this);
			add(activeAreaDebug, BorderLayout.NORTH);
		}

		// Set the JSoko icon.
		setIconImage(Utilities.getJSokoIcon());

		// Set the optimizer help for this GUI.
		Texts.helpBroker.enableHelpKey(getRootPane(), "optimizer", null);
	}

	/**
	 * Sets a new board to be displayed in the optimizer from the selected pushes range.
	 * <p>
	 * The user can selected a pushes range of a solution in this GUI. The displayed board
	 * is created from the selected pushes range and set as new board to be displayed.
	 */
	private void setBoardToDisplayFromPushesRange() {

		// Try to commit changes by the user that haven't been set in the model.
		try {
			rangeOfPushesFromValue.commitEdit();
			rangeOfPushesToValue.commitEdit();
		}
		catch (ParseException pe) { /* last valid values are used */  }

		/*
		 * Set a new board from the selected pushes range.
		 */
		optimizeFromPush = (Integer) rangeOfPushesFromValue.getValue();
		optimizeToPush   = (Integer) rangeOfPushesToValue.getValue();

		List<Solution> selectedSolutions = Utilities.getSelectedValuesList(solutionsGUI);
		if(!selectedSolutions.isEmpty()) {
			Solution solutionToBeOptimized = getBestSelectedSolution(selectedSolutions, getOptimizationMethod());

			// Ensure valid input.
			if( optimizeToPush > solutionToBeOptimized.pushesCount ) {
				optimizeToPush = solutionToBeOptimized.pushesCount;
			}
			if( optimizeFromPush >= solutionToBeOptimized.pushesCount ) {
				optimizeFromPush =  solutionToBeOptimized.pushesCount - 1;
			}
			if( optimizeToPush <= optimizeFromPush) {
				optimizeToPush =  optimizeFromPush + 1;
			}

			// Create a new board that is to be used by the optimizer.
			Board boardForOptimizer = getBoardFromSolutionPart(optimizeFromPush, optimizeToPush, solutionToBeOptimized);
			boardDisplay.setBoardToDisplay(boardForOptimizer);
		}
	}

	/**
	 * Returns the best {@link Solution} of the passed solutions according to the passed {@link OptimizationMethod}.
	 *
	 * @param solutions  solutions to return the best from
	 * @param optimizationMethod method of optimization (pushes/moves, moves/pushes, ...)
	 * @return the best found {@link Solution}
	 */
	private Solution getBestSelectedSolution(List<Solution> solutions, OptimizationMethod optimizationMethod) {

		if(solutions.isEmpty()) {
			return null;
		}

		Solution bestSolution = solutions.get(0);
		for (Solution solution : solutions) {
			if (optimizationMethod == OptimizationMethod.MOVES_PUSHES || optimizationMethod == OptimizationMethod.MOVES_PUSHES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS) {
				if (solution.isBetterMovesSolutionThan(bestSolution)) {
					bestSolution = solution;
				}
			}
			if (optimizationMethod == OptimizationMethod.PUSHES_MOVES || optimizationMethod == OptimizationMethod.PUSHES_MOVES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS) {
				if (solution.isBetterPushesSolutionThan(bestSolution)) {
					bestSolution = solution;
				}
			}
		}

		return bestSolution;
	}

	/* (non-Javadoc)
	 * @see javax.swing.JFrame#processWindowEvent(java.awt.event.WindowEvent)
	 */
	@Override
	protected void processWindowEvent(WindowEvent e) {

		// If the user closes the Frame check whether the optimizer is still running
		// and save the settings.
		if (e.getID() == WindowEvent.WINDOW_CLOSING) {

			// Ask the user whether the running optimizer is really to be closed.
			if(isOptimizerRunning == true) {
				if(JOptionPane.showConfirmDialog(this, Texts.getText("closeOptimizerWhileRunning"), Texts.getText("warning"), JOptionPane.YES_NO_OPTION, JOptionPane.WARNING_MESSAGE) != JOptionPane.YES_OPTION) {
					return;
				}
			}

			// Stop the optimizer. Even if it is not running the stop is important
			// because the deadlock identification thread already runs when the optimizer is instantiated.
			optimizer.stopOptimizer();

			// The optimizer itself isn't used anymore.
			optimizer = null;

			/*
			 * Save the settings in the global settings file.
			 */
			int fieldCounter = 0;
			for(NumberInputTF numberField : vicinitySettings) {

				Integer value = numberField.getValueAsIntegerNoNull();
				boolean isActive = numberField.isFieldActive();

				switch(++fieldCounter) {
				case 1:
					Settings.vicinitySquaresBox1 = value;
					Settings.vicinitySquaresBox1Enabled = isActive;
					break;
				case 2:
					Settings.vicinitySquaresBox2 = value;
					Settings.vicinitySquaresBox2Enabled = isActive;
					break;
				case 3:
					Settings.vicinitySquaresBox3 = value;
					Settings.vicinitySquaresBox3Enabled = isActive;
					break;
				case 4:
					Settings.vicinitySquaresBox4 = value;
					Settings.vicinitySquaresBox4Enabled = isActive;
					break;
				}
			}
			Settings.isIteratingEnabled = isIteratingEnabled.isSelected();
			Settings.isOnlyLastSolutionToBeSaved = isOnlyLastSolutionToBeSaved.isSelected();
			Settings.CPUCoresToUse = (Integer) threadsCount.getValue();

			Settings.optimizerXCoordinate 	= getX();
			Settings.optimizerYCoordinate 	= getY();
			Settings.optimizerWidth 	  	= getWidth();
			Settings.optimizerHeight		= getHeight();
			Settings.optimizationMethod		= getOptimizationMethod().ordinal();

			// Set the focus back to the main GUI to allow key board usage in the main GUI.
			application.getRootPane().requestFocus();
			application.applicationGUI.mainBoardDisplay.requestFocusInWindow();
		}

		// Process the window event.
		super.processWindowEvent(e);
	}

	/**
	 * Handles all action events.
	 *
	 * @param evt  the action event to be handled.
	 */
	@Override
	public final void actionPerformed(ActionEvent evt) {

		String action = evt.getActionCommand();

		// The optimizer is to be started.
		if (action.equals("startOptimizer")) {

			// Ensure at least on solution has been selected for optimizing.
			if (solutionsGUI.getSelectedIndex() == -1) {
				setInfoText(Texts.getText("optimizer.xSelectedSolutions", 0)); // Display method that no solution has been selected
				return;
			}

			// Be sure that the optimizer isn't running at the moment.
			if(isOptimizerRunning == true) {
				return;
			}

			// Save the current time.
			startTimestamp = System.currentTimeMillis();

    		// The optimizer for the original board has been saved and can now be set back.
			// As default the original level is optimized. Only if the user has selected
			// "pushes range optimization" a new optimizer is created.
    		optimizer = optimizerOriginalLevel;

			// Get the selected solutions and convert them in the Optimizer solution class.
    		selectedSolutionsToBeOptimized = Utilities.getSelectedValuesList(solutionsGUI);
			ArrayList<OptimizerSolution> solutionsToBeOptimized = new ArrayList<OptimizerSolution>(selectedSolutionsToBeOptimized.size());
			for(Solution solution : selectedSolutionsToBeOptimized) {
				solutionsToBeOptimized.add(new OptimizerSolution(solution));
			}

			// If the box changes are to be optimized call the corresponding method.
			if(boxChangesOptimization.isSelected()) {
				isOptimizerRunning = true;

				// Delete the log for the new optimizer run.
				optimizerLog.setText(null);

				// Optimize the first of the selected solutions.
				OptimizerSolution solutionToBeOptimized = solutionsToBeOptimized.get(0);
				OptimizerSolution optimizedSolution = solutionToBeOptimized;
				OptimizerSolution bestFoundSolution = null;
				// Optimize until no further better solution can be found.
				while(optimizedSolution != null) {
					optimizedSolution = optimizer.reduceBoxChanges(optimizedSolution);
					if(optimizedSolution != null) {
						bestFoundSolution = optimizedSolution;
					}
				}

				// The optimizer tries to push the same box again if possible to reduce box changes.
				// However, this "same box push" may result in an additional box change after this
				// new push sequence. Hence, the new found solution may have the same number of
				// box changes as the selected solution. It therefore has to be checked whether the
				// new found solution is really better than the selected solution to be optimized.
				if(bestFoundSolution != null) {

					// Create "real" solutions from the OptimizerSolutions and
					// verify them to ensure all metrics (like box changes) are calculated.
					Solution bestSolution = new Solution(bestFoundSolution.getLURD());
					currentLevel.getSolutionsManager().verifySolution(bestSolution);
					Solution selectedSolution = new Solution(solutionToBeOptimized.getLURD());
					currentLevel.getSolutionsManager().verifySolution(selectedSolution);

					// If the new solution isn't better than the selected solution it is set to "null",
					// so the method "optimizerEnded" will display an appropriate message.
					if(bestSolution.isBetterPushesSolutionThan(selectedSolution) == false) {
						bestFoundSolution = null;
					}
					else {
						// Save the new solution if there is any.
						newFoundSolution(bestFoundSolution);
					}
				}

				// Optimizer has ended.
				optimizerEnded(bestFoundSolution);

				return;
			}

			// Get the values of the box settings. The values are increased by one since this is more logical
			// for the user: a setting of 1 therefore means "1 square in the vicinity of the box square".
			// The generator logic however counts the square of the box as 1 square, too => increase the settings.
			ArrayList<Integer> vicinityRestrictions = new ArrayList<Integer>();
			for(NumberInputTF numberField : vicinitySettings) {
				Integer value = numberField.getValueAsInteger();
				if(value != null) {
					vicinityRestrictions.add(value+1);
				}
			}
			// Sort the values ascending. This is important for the box configuration generator.
			Collections.sort(vicinityRestrictions);

			if(vicinityRestrictions.size() == 0) {
				JOptionPane.showMessageDialog(this, Texts.getText("selectAtLeastOneBox"));
				return;
			}

			// The optimizer is running.
			isOptimizerRunning = true;

			// Delete the log for the new optimizer run.
			optimizerLog.setText(null);

			// Determine the optimization type.
			OptimizationMethod optimizationType = getOptimizationMethod();
			if(optimizationType == OptimizationMethod.BOXLINES && boxLinesOnlyOptimization.isSelected() == false) {
				optimizationType = OptimizationMethod.FIND_VICINITY_SETTINGS; // debug method
			}

			// Flag, indicating whether the new solution must have the same
			// player end position as the original one
			boolean isPlayerEndPositionFixed = isPlayerPositionToBePreserved.isSelected();

			// Start the optimizer. If the pushes range is activated the prefix
			// and suffix values have to be passed, too.
			int prefixMovesCount  = 0;
			int prefixPushesCount = 0;
			int suffixMovesCount  = 0;
			int suffixPushesCount = 0;

			// Check whether the user wants to optimize just a part of the solution.
			if(isRangeOfPushesActivated.isSelected()) {

				// Optimization of ranges of solution can only be done with one solution at a time.
				// Determine the best solution of the passed solutions.
				Solution solutionToBeOptimized = getBestSelectedSolution(selectedSolutionsToBeOptimized, optimizationType);
				selectedSolutionsToBeOptimized.clear();
				selectedSolutionsToBeOptimized.add(solutionToBeOptimized);

				// Fix the player end position if the solution doesn't include the
				// last push of the solution. This must be done to ensure the optimizer
				// doesn't find a new solution with the player at another end position
				// because this would mean the rest of the solution (postfixSolutionMoves)
				// couldn't be added anymore.
				if(optimizeToPush < solutionToBeOptimized.pushesCount) {
					isPlayerEndPositionFixed = true;
				}

				// Ensure the pushes range is within the pushes range of the best solution.
				optimizeFromPush = Math.min(optimizeFromPush, solutionToBeOptimized.pushesCount-1);
				optimizeToPush   = Math.min(optimizeToPush, solutionToBeOptimized.pushesCount);

				//  Split the solution according to the selected range.
				prefixSolutionMoves   = getSolutionRange(solutionToBeOptimized, 0, optimizeFromPush).lurd;
				suffixSolutionMoves   = getSolutionRange(solutionToBeOptimized, optimizeToPush, solutionToBeOptimized.pushesCount).lurd;
				solutionToBeOptimized = getSolutionRange(solutionToBeOptimized, optimizeFromPush, optimizeToPush);

				// Add the solution part as solution to be optimized.
				solutionsToBeOptimized.clear();
				solutionsToBeOptimized.add(new OptimizerSolution(solutionToBeOptimized));

				prefixMovesCount  = prefixSolutionMoves.length();
				prefixPushesCount = optimizeFromPush;
				suffixMovesCount  = suffixSolutionMoves.length();
				suffixPushesCount = selectedSolutionsToBeOptimized.get(0).pushesCount-optimizeToPush; // Get the whole solution again and calculate pushes difference

				// Create a new optimizer for the new board.
				optimizer = new Optimizer(boardDisplay.getBoard(), this, optimizerOriginalLevel);

				// Debug
//				System.out.printf("\nPrefix moves: "+prefixSolutionMoves);
//				System.out.printf("\nSolution to be optimized: "+solutionToBeOptimized.lurd);
//				System.out.printf("\nPostfix moves: "+suffixSolutionMoves);
//				System.out.printf("\n\n");
			}

			// Determine the maximum number of box configurations to be generated.
			// If the user has set the value manually then this value is taken.
			// However, it must at least be high enough so all box configurations
			// of one solution can be generated.
			int userSetMaximumNoOfBoxConfigurations = Optimizer.NONE;
			if(isMaxBoxConfigurationManuallySet.isSelected()) {
				userSetMaximumNoOfBoxConfigurations = 1000 * (Integer) maxBoxConfigurationsToBeGenerated.getValue();
				if(solutionsToBeOptimized.get(0).pushesCount >= userSetMaximumNoOfBoxConfigurations) {
					userSetMaximumNoOfBoxConfigurations = solutionsToBeOptimized.get(0).pushesCount + 1;
				}
			}

			// Color selected solutions so the user knows which solutions are being optimized.
			for(Object s : selectedSolutionsToBeOptimized) {
				solutionsGUI.setSolutionColor((Solution) s, new Color(0x87, 0xCE, 0xFF));
			}

			optimizer.startOptimizer(Utilities.toIntArray(vicinityRestrictions), boardDisplay.getMarkedSquares(), solutionsToBeOptimized, optimizationType,
					  userSetMaximumNoOfBoxConfigurations, isIteratingEnabled.isSelected(), isOnlyLastSolutionToBeSaved.isSelected(),
					  isPlayerEndPositionFixed, (Integer) threadsCount.getValue(),
					  prefixMovesCount, prefixPushesCount,
					  suffixMovesCount, suffixPushesCount,
					  isOptimizerToFindBestVicinitySettings.isSelected());

			// Rename the start button to "stop optimizer" and set a new action command for it.
			startButton.setToStop();

			return;
		}

		// The optimizer is to be stopped.
		if (action.equals("stopOptimizer")) {
			optimizer.stopOptimizer();
			return;
		}

		// DEBUG: Display the number of moves and pushes of the solution in the marked "active area".
		if(action.equals("displayMovesPushesInActiveArea")) {

			int moves=0, pushes=0;
			int playerPosition = board.playerPosition;

			// Get the first selected solution (if any)
			Solution sol= solutionsGUI.getSelectedValue();

			if (sol != null) {
				// Get a boolean array where marked squares have a value of "true".
				boolean[] relevant = boardDisplay.getMarkedSquares();

				// Go through the solution and count the moves and pushes
				// done in the relevant area.
				for (char direction : sol.lurd.toCharArray()) {
					switch (Character.toLowerCase(direction)) {
					case 'u':
						playerPosition = board.getPosition(playerPosition, UP);
						break;
					case 'd':
						playerPosition = board.getPosition(playerPosition, DOWN);
						break;
					case 'l':
						playerPosition = board.getPosition(playerPosition, LEFT);
						break;
					case 'r':
						playerPosition = board.getPosition(playerPosition, RIGHT);
						break;
					}
					if (relevant[playerPosition] == true) {
						moves++;
						if (Character.isUpperCase(direction)) {
							pushes++;
						}
					}
				}
				// Display the result: number of moves and pushes
				// of the selected solution in the relevant area.
				setInfoText(moves + " moves, " + pushes + " pushes in active area.");
			}
			return;
		}
	}

	/**
	 * Returns the {@code OptimizationMethod} currently selected in this GUI.
	 *
	 * @return the {@code OptimizationMethod} currently selected in this GUI
	 */
	private OptimizationMethod getOptimizationMethod() {
		return 	movesOptimization.isSelected()  		  ? OptimizationMethod.MOVES_PUSHES :
				pushesOptimization.isSelected() 		  ? OptimizationMethod.PUSHES_MOVES :
				pushesAllMetricsOptimization.isSelected() ? OptimizationMethod.PUSHES_MOVES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS :
				movesAllMetricsOptimization.isSelected()  ? OptimizationMethod.MOVES_PUSHES_BOXLINES_BOXCHANGES_PUSHINGSESSIONS :
				boxLinesOnlyOptimization.isSelected()     ? OptimizationMethod.BOXLINES :
															OptimizationMethod.BOXCHANGES;
	}

	/**
	 * Adds the passed <code>String</code> to the log texts of the optimizer
	 * to inform the user about the progress of the optimizer,
	 * or to inform the developer about statistical data.
	 *
	 * @param text       text to be added to the log
	 * @param stylename  registered name of style to be used
	 */
	private void addLogTextStyle(final String text, final String stylename) {
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				try {
					StyledDocument doc = optimizerLog.getStyledDocument();
					doc.insertString(doc.getLength(), text+"\n", doc.getStyle(stylename));
				} catch (BadLocationException e) {	/* ignore */ }
			}
		});
	}

	@Override
	public void addLogText(final String text) {
		addLogTextStyle(text, "regular");
	}

	@Override
	public void addLogTextDebug(final String text) {
		addLogTextStyle(text, "debug");

		// Additional output on the console since the log is cleared in the next run.
		System.out.println(text);
	}

	/**
	 * This method is called from the optimizer every time it has found a new solution.
	 *
	 * @param bestFoundSolution  the best found solution
	 * @return the solution that has been added to the level
	 */
	@Override
	public Solution newFoundSolution(OptimizerSolution bestFoundSolution) {

		// The user may have restricted the search to a part of the solution only.
		// Therefore add the moves before this part and after this part here to ensure
		// the solution is a valid solution for the level.
		String solutionLURD = prefixSolutionMoves + bestFoundSolution.getLURD() + suffixSolutionMoves;

		// Add the solution to the solutions of the level.
		final Solution newSolution = new Solution(solutionLURD);
		newSolution.name =       Texts.getText("createdBy")
						 + " " + Texts.getText("optimizer")
						 + " " + Utilities.nowString();
		currentLevel.addSolution(newSolution);

		Runnable runnable = new Runnable() {
			@Override
			public void run() {

				// Color the found solution because it will be optimized next.
				solutionsGUI.setSolutionColor(newSolution, new Color(0x87, 0xCE, 0xFF));

				// Remove the color of the first selected solution.
				solutionsGUI.setSolutionColor(selectedSolutionsToBeOptimized.get(0), null);

				// Remove the first solution and add the new solution to the array. This is exactly what the optimizer
				// does in order to optimize not more solutions at once as the user had selected.
				selectedSolutionsToBeOptimized.remove(0);
				selectedSolutionsToBeOptimized.add(newSolution);

				// Select the new found solution.
				solutionsGUI.setSelectedValue(newSolution, true);
			}
		};

		// Select the solution. Since GUI changes are made this must be done on the EDT.
		if(SwingUtilities.isEventDispatchThread()) {
			runnable.run();
		}
		else {
			SwingUtilities.invokeLater(runnable);
		}

		// Return the solution that has been added to the level.
		return newSolution;
	}

	/**
	 * This method is called when the optimizer thread has ended.
	 *
	 * @param bestFoundSolution the best found solution
	 */
	@Override
	public void optimizerEnded(final OptimizerSolution bestFoundSolution) {

		// Set the new status of the optimizer thread.
		isOptimizerRunning = false;

		// The optimizer has ended. Display the result, rename the button
		// back to "start" and remove the coloring of all solutions.
		// This method is called from the optimizer thread, hence ensure
		// that the swing components are updated on the EDT.
		SwingUtilities.invokeLater(new Runnable() {
			@Override
			public void run() {
				// Display a message to inform the user about the found solution.
				if(bestFoundSolution == null) {
					setInfoText(     Texts.getText("noNewSolutionFound")
							+ " "  + Texts.getText("time")
							+ ": " + ((System.currentTimeMillis() - startTimestamp)/1000f)
							+ " "  + Texts.getText("seconds") );
				} else {
					// The optimizer has ended. Hence, it must be the last solution it has found. If the user selected "only save last
					// solution" this solution hasn't been saved, yet -> save it now.
					Solution newSolutionOfLevel = newFoundSolution(bestFoundSolution);

					// The box changes optimizing considers moves, pushes and box changes. Hence, display all values.
					if(boxChangesOptimization.isSelected()) {
						setInfoText(      Texts.getText("foundSolution")
								+ " "   + Texts.getText("moves")
								+ " = " + newSolutionOfLevel.movesCount
								+ ", "  + Texts.getText("pushes")
								+ " = " + newSolutionOfLevel.pushesCount
								+ ", "  + Texts.getText("boxChanges")
								+ " = " + newSolutionOfLevel.boxChanges
								+ ", "  + Texts.getText("time")
								+ ": "  + ((System.currentTimeMillis() - startTimestamp)/1000f)
								+ " "   + Texts.getText("seconds") );
					} else {
						// The "box lines only" optimization only considers box lines. Hence, just display the new number of box lines.
						if(boxLinesOnlyOptimization.isSelected()) {
							setInfoText(      Texts.getText("foundSolution")
									+ " "   + Texts.getText("boxLines")
									+ " = " + newSolutionOfLevel.boxLines
									+ ", "  + Texts.getText("time")
									+ ": "  + ((System.currentTimeMillis() - startTimestamp)/1000f)
									+ " "   + Texts.getText("seconds") );
						} else {
							// Main metrics have been optimized. Hence, display the main metrics.
							setInfoText(	Texts.getText("foundSolution")
									+ " " + Texts.getText("moves")
									+ " = " + newSolutionOfLevel.movesCount
									+ ", "  + Texts.getText("pushes")
									+ " = " + newSolutionOfLevel.pushesCount
									+ ", "  + Texts.getText("time")
									+ ": "  + ((System.currentTimeMillis() - startTimestamp)/1000f)
									+ " "   + Texts.getText("seconds") );
						}
					}
				}

				// The next run is done using the whole solution.
				prefixSolutionMoves = "";
				suffixSolutionMoves = "";

				// Rename the start button to "start optimizer" and set a new action command.
				startButton.setToStart();

				// Remove the highlighting of solutions currently being optimized.
				solutionsGUI.setAllSolutionsUncolored();
				solutionsGUI.repaint();
			}
		});
	}

	/**
	 * Creates a new board from the passed solution regarding all pushes from the "fromPush" to the "toPush".
	 *
	 * @param fromPush  the first relevant push of the solution
	 * @param toPush  the last relevant push of the solution
	 * @param solution  the solution to create a new board from
	 * @return the created board
	 */
	@SuppressWarnings("fallthrough")
	Board getBoardFromSolutionPart(int fromPush, int toPush, Solution solution) {

		// Create a clone of the current board.
		Board helpBoard = board.clone();
		Board newBoard = helpBoard;

		if( fromPush < 0 || fromPush >= solution.pushesCount) {
			fromPush = 0;
		}
		if( toPush > solution.pushesCount || toPush < fromPush) {
			toPush = solution.pushesCount;
		}

		boolean[] isBoxPushedInSolution = new boolean[helpBoard.boxCount];
		int pushesCount = 0;

		int newPlayerPosition = helpBoard.playerPosition;

		// Go through the solution until the "toPush" is reached.
		for(int i=0; i<solution.lurd.length() && pushesCount < toPush; i++) {

			// If the "fromPush" has been reached this board is the start for the optimizer.
			if(pushesCount == fromPush && newBoard == helpBoard) {
				newBoard = helpBoard.clone();
			}

			switch(solution.lurd.charAt(i)) {
				case 'U':
					pushesCount++;
				case 'u':
					newPlayerPosition = helpBoard.getPosition(newPlayerPosition, UP);
					break;

				case 'D':
					pushesCount++;
				case 'd':
					newPlayerPosition = helpBoard.getPosition(newPlayerPosition, DOWN);
					break;

				case 'L':
					pushesCount++;
				case 'l':
					newPlayerPosition = helpBoard.getPosition(newPlayerPosition, LEFT);
					break;

				case 'R':
					pushesCount++;
				case 'r':
					newPlayerPosition = helpBoard.getPosition(newPlayerPosition, RIGHT);
					break;
			}

			// If a box is reached set a flag that the box is pushed in the solution.
			if(helpBoard.isBox(newPlayerPosition) && pushesCount >= fromPush) {
				isBoxPushedInSolution[helpBoard.getBoxNo(newPlayerPosition)] = true;
			}

			// Push a box if necessary.
			if(helpBoard.isBox(newPlayerPosition)) {
				helpBoard.pushBox(newPlayerPosition, 2*newPlayerPosition-helpBoard.playerPosition);
			}

			// Move the player.
			helpBoard.setPlayerPosition(newPlayerPosition);
		}

		// Remove all goals.
		for(int goalNo=0; goalNo<newBoard.goalsCount; goalNo++) {
			newBoard.removeGoal(newBoard.getGoalPosition(goalNo));
		}

		// Set a wall at all boxes that don't have been pushed in the relevant part
		// of the solution.
		for(int counter=0; counter<helpBoard.boxCount; counter++) {

			int boxPosition = helpBoard.boxData.getBoxPosition(counter);

			if(isBoxPushedInSolution[counter] == false) {
				newBoard.setWall(boxPosition);
				newBoard.removeBox(boxPosition);
			} else {
				// Set new goals at the box positions.
				newBoard.setGoal(boxPosition);
			}
		}

		// Inform the board that the elements of the board have changed (new walls and removed boxes).
		newBoard.isValid(new StringBuilder());
		newBoard.prepareBoard();

		// Set a wall on all new "outer" squares. Due to boxes that have become walls the new board may contain
		// empty squares which can't be reached by the player. For a better look these squares are filled with walls.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare;position++) {
			if(newBoard.isOuterSquareOrWall(position) && !board.isOuterSquareOrWall(position)) {
				newBoard.setWall(position);
			}
		}

		return newBoard;
	}

	/**
	 * Returns the part of a solution between two pushes.
	 * <p>
	 * The new created solution will be exclusive the "fromPush" and inclusive the "toPush".
	 *
	 * @param solution the solution to get a part from
	 * @param fromPush the number of the first relevant push
	 * @param toPush   the number of the last  relevant push
	 * @return
	 */
	private Solution getSolutionRange(Solution solution, int fromPush, int toPush) {
		int startIndex = -1;
		int endIndex = -1;
		int pushesCount = 0;

		if( fromPush < 0 ) {
			fromPush = 0;
		}
		if( toPush > solution.pushesCount ) {
			toPush = solution.pushesCount;
		}
		if(        fromPush == solution.pushesCount
				|| fromPush > solution.pushesCount
				|| toPush == 0
				|| fromPush == toPush
				|| toPush < fromPush  )
		{
			return new Solution("");
		}

		for(int moveNo=0; moveNo<solution.lurd.length(); moveNo++) {

			// The relevant solution path begins at the first move after the previous push.
			if(pushesCount == fromPush && startIndex == -1) {
				startIndex = moveNo;
			}

			if(Character.isUpperCase(solution.lurd.charAt(moveNo))) {
				pushesCount++;
			}

			if(pushesCount == toPush) {
				endIndex = moveNo;
				break;
			}
		}

		// Create a new solution containing only the moves in the range in the pushes range passed to this method.
		solution = new Solution(solution.lurd.substring(startIndex, endIndex+1));
		solution.pushesCount = toPush - fromPush;
		solution.movesCount  = endIndex - startIndex + 1;

		return solution;
	}

	/**
	 * Catches all uncaught exceptions of all threads the optimizer uses.
	 * <p>
	 * This method is called before the default method in class {@code ExceptionHandler} is called.
	 */
	@Override
	public void uncaughtException(final Thread t, final Throwable e) {

		// Stop the optimizer.
		if(isOptimizerRunning) {
			optimizer.stopOptimizer();
		}

		// Only outOfMemory is caught.
		if (e instanceof OutOfMemoryError) {

			// If the user has set the maximal number of box configurations manually then this value
			// must be decreased => display a message for this.
			// Otherwise display a general "out of memory" message.
			if(isMaxBoxConfigurationManuallySet.isSelected()) {
				JOptionPane.showMessageDialog(this, Texts.getText("optimizer.boxConfigurationCountTooHigh"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
			}
			else {
				// Inform the user about the error in the log.
				addLogText("\n"+Texts.getText("outOfMemory"));
			}
		}
	}
}
