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
package de.sokoban_online.jsoko.solver;

import java.awt.BorderLayout;
import java.awt.Cursor;
import java.awt.Dimension;
import java.awt.FileDialog;
import java.awt.FlowLayout;
import java.awt.Font;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.GridLayout;
import java.awt.Insets;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.ItemEvent;
import java.awt.event.ItemListener;
import java.io.File;
import java.io.FilenameFilter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.List;
import java.util.Timer;
import java.util.TimerTask;
import java.util.concurrent.CancellationException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

import javax.swing.BorderFactory;
import javax.swing.ButtonGroup;
import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JDialog;
import javax.swing.JFileChooser;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JRadioButton;
import javax.swing.JScrollPane;
import javax.swing.JSpinner;
import javax.swing.JTextArea;
import javax.swing.JTextField;
import javax.swing.SpinnerNumberModel;
import javax.swing.SwingUtilities;
import javax.swing.SwingWorker;
import javax.swing.border.EtchedBorder;
import javax.swing.filechooser.FileFilter;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.gui.NumberInputTF;
import de.sokoban_online.jsoko.gui.StartStopButton;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager.SolutionType;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.solver.AnySolution.BoardPositionPackingSequence;
import de.sokoban_online.jsoko.solver.AnySolution.PackingSequenceSearch;
import de.sokoban_online.jsoko.solver.AnySolution.SolverAnySolution;
import de.sokoban_online.jsoko.solver.solverEvolutionary.SolverEvolutionary;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific.OSType;



/**
 * GUI for the solver.
 */
@SuppressWarnings("serial")
public final class SolverGUI extends JPanel implements ActionListener {

	/** Reference to the main object of this program.  It is only needed
	 *  to display box configurations for debugging.
	 */
	JSoko application;

	// Buttons for the optimization methods.
	JRadioButton anySolution;
	JRadioButton pushOptimalSolution;
	JRadioButton pushesMovesSolution;
	JRadioButton movesPushesSolution;
	public JRadioButton evolutionarySolver; // currently just for experimental tests
	public JRadioButton BFSSolver; 			// currently just for experimental tests

	/** Start/stop button for the solver. */
	StartStopButton startSolverButton;

	// Radio buttons for switching between:
	// - "solve only current level",
	// - "solve whole collection"
	// - and "solve levels from x to y".
	private JRadioButton isWholeCollectionToBeSolved;
	private JRadioButton isOnlyCurrentLevelToBeSolved;
	private JRadioButton isRangeOfLevelsToSolveSelected;
	private JSpinner     levelsToBeSolvedFromValue;
	private JSpinner     levelsToBeSolvedToValue;

	/** Check box for selecting whether the solver results are to be saved to a file. */
	JCheckBox isSaveToFileEnabled;

	/** Check box for selecting whether the found solutions are to be displayed */
	JCheckBox isDisplaySolutionsEnabled;

	/** File name of the file the solver results are to be saved to. */
    JTextField solverResultsFileName;

	/** Time limit for the solver. */
	NumberInputTF timeLimit;


	/** JTextField for info texts. */
	private JTextField infoText;

	/**
	 * JTextField for displaying the time the solver has already spend
	 * on the current level.
	 */
	JTextField timeTextField;

	/** The SwingWorker that is used for solving the levels. */
	SwingWorker<?,?> solvingProcess = null;

	/** The solver that is currently used to solve a level. */
	Solver solver = null;

	// Reasons why the solver has been canceled and a variable
	// holding the canceling reason.
	enum SolverStatus { RUNNING, CANCELED_DUE_TO_STOP_OF_SOLVING, CANCELED_DUE_TO_SKIP_OF_LEVEL }
	volatile SolverStatus solverStatus = SolverStatus.RUNNING;

	// Debug:
	public JCheckBox isShowBoardPositionsActivated = new JCheckBox();
	public JButton justPackingOrder;

	/**
	 * Creates an object for solving a level.
	 *
	 * @param application  reference to the main object that holds references to all other objects
	 */
	public SolverGUI(JSoko application) {

		// Reference to the main object of this program holding all references.
		this.application = application;

		// Display the GUI for the solver.
		displayGUI();
	}

	/**
	 * Displays the GUI for the solver.
	 */
	final private void displayGUI() {

		// Set a BorderLayout for this panel.
		setLayout(new BorderLayout());

		/*
		 * This panel contains two other panels:
		 * - one panel for the close button and
		 * - another panel for the rest of the GUI.
		 */


		/*
		 * Create a button allowing the user to close the solver.
		 */
		JPanel closeButtonPanel = new JPanel(new BorderLayout());
			JButton closeButton = new JButton(Utilities.getIcon("process-stop.png", "Close"));
			closeButton.setRolloverIcon(Utilities.getIcon("process-stop selected.png", "Close"));
			closeButton.setToolTipText(Texts.getText("close"));
			closeButton.setActionCommand("closeSolver");
			closeButton.addActionListener(this);
			closeButton.setContentAreaFilled(false);
			closeButton.setMinimumSize(new Dimension(closeButton.getIcon().getIconWidth()  + 4,
					                                 closeButton.getIcon().getIconHeight() + 4));
			closeButton.setPreferredSize(closeButton.getMinimumSize());
			closeButton.setBorder(BorderFactory.createEmptyBorder(4, 0, 0, 4));
			closeButton.setBorderPainted(false);
			closeButtonPanel.add(closeButton, BorderLayout.LINE_END);

		add(closeButtonPanel, BorderLayout.NORTH);


		// Create the panel for the main solver GUI and add it to the GUI of the solver.
		JPanel mainSolverGUI = new JPanel(new GridBagLayout());
		add(mainSolverGUI, BorderLayout.CENTER);


		// Create constraints for using the GridBag-Layout.
		GridBagConstraints constraints = new GridBagConstraints();
		constraints.gridheight = 1;
		constraints.gridwidth  = 1;
		constraints.anchor = GridBagConstraints.NORTHEAST;
		constraints.insets = new Insets(4, 0, 0, 4);

		JPanel solvingMethodAndStartButton = new JPanel(new BorderLayout());

		/*
		 * Panel for the optimization method.
		 */
		JPanel solvingMethod = new JPanel(new GridLayout(4, 1));
		Texts.helpBroker.enableHelpKey(solvingMethod, "solver.SolvingMethod", null); // Enable help
		solvingMethod.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(
										Texts.getText("solver.solvingMethod")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						solvingMethod.getBorder())
		);

		ButtonGroup optimizationType = new ButtonGroup();

		anySolution = new JRadioButton(Texts.getText("solver.solvingMethod.anySolution"), true);
		optimizationType.add(anySolution);
		solvingMethod.add(anySolution, 0);

		pushOptimalSolution = new JRadioButton(Texts.getText("solver.solvingMethod.pushOptimal"), false);
		optimizationType.add(pushOptimalSolution);
		solvingMethod.add(pushOptimalSolution, 1);

		pushesMovesSolution = new JRadioButton(Texts.getText("solver.solvingMethod.pushesMoves"), false);
		optimizationType.add(pushesMovesSolution);
		solvingMethod.add(pushesMovesSolution, 2);

		movesPushesSolution = new JRadioButton(Texts.getText("solver.solvingMethod.movesPushes"));
		optimizationType.add(movesPushesSolution);
		solvingMethod.add(movesPushesSolution, 3);

		// Special solver for testing evolutionary / genetic algorithms.
		if(Debug.isDebugModeActivated) {
			evolutionarySolver = new JRadioButton("Evolutionary solver", false);
			optimizationType.add(evolutionarySolver);
			solvingMethod.add(evolutionarySolver, 4);

			BFSSolver = new JRadioButton("BFS solver", false);
			optimizationType.add(BFSSolver);
			solvingMethod.add(BFSSolver, 5);
		}

		constraints.gridheight = 2;
		constraints.gridx = 0;
		constraints.gridy = 1;
		solvingMethodAndStartButton.add(solvingMethod, BorderLayout.NORTH);

		solvingMethodAndStartButton.add(new JLabel(""), BorderLayout.CENTER);

		/*
		 * Button for starting the solver.
		 */
		startSolverButton = new StartStopButton( "solver.startSolver", "startSolver",
				                           "solver.stopSolver",  "stopSolver" );
		startSolverButton.addActionListener(this);
		constraints.gridheight = 1;
		constraints.gridy++;
		solvingMethodAndStartButton.add(startSolverButton, BorderLayout.SOUTH);

		constraints.gridy = 1;
		constraints.gridheight = 2;
		mainSolverGUI.add(solvingMethodAndStartButton, constraints);


		/*
		 * Panel for selecting the levels to be solved.
		 */
		JPanel levelsToBeSolvedPanel = new JPanel(new BorderLayout());
		Texts.helpBroker.enableHelpKey(levelsToBeSolvedPanel, "solver.LevelsToSolve", null); // Enable help
		levelsToBeSolvedPanel.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(
										Texts.getText("solver.levelsToBeSolved")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						levelsToBeSolvedPanel.getBorder())
		);

			ButtonGroup levelsToBeSolved = new ButtonGroup();

			isOnlyCurrentLevelToBeSolved = new JRadioButton(Texts.getText("solver.solverOnlyCurrentLevel"), true);
			levelsToBeSolved.add(isOnlyCurrentLevelToBeSolved);
			levelsToBeSolvedPanel.add(isOnlyCurrentLevelToBeSolved, BorderLayout.NORTH);

			// A CheckBox for selecting "solve whole collection".
		    isWholeCollectionToBeSolved = new JRadioButton(Texts.getText("solver.solveWholeCollection"));
		    isWholeCollectionToBeSolved.setToolTipText(Texts.getText("solver.solveWholeCollection.tooltip"));
		    levelsToBeSolved.add(isWholeCollectionToBeSolved);
		    levelsToBeSolvedPanel.add(isWholeCollectionToBeSolved, BorderLayout.CENTER);

		    JPanel solvingRangePanel = new JPanel(new FlowLayout(FlowLayout.LEADING));
		    isRangeOfLevelsToSolveSelected = new JRadioButton("");
		    levelsToBeSolved.add(isRangeOfLevelsToSolveSelected);
		    solvingRangePanel.add(isRangeOfLevelsToSolveSelected);
		    solvingRangePanel.add(new JLabel(Texts.getText("solver.solveLevelFrom")));
		    SpinnerNumberModel model1 = new SpinnerNumberModel(1, 1, application.currentLevelCollection.getLevelsCount(), 1);
		    levelsToBeSolvedFromValue = new JSpinner( model1 );
		    solvingRangePanel.add(levelsToBeSolvedFromValue);
		    solvingRangePanel.add(new JLabel(Texts.getText("solver.solveLevelTo")));
		    SpinnerNumberModel model2 = new SpinnerNumberModel(
		    			application.currentLevelCollection.getLevelsCount(),
		    			1,
		    			application.currentLevelCollection.getLevelsCount(),
		    			1);
		    levelsToBeSolvedToValue = new JSpinner( model2 );
		    solvingRangePanel.add(levelsToBeSolvedToValue);
		    levelsToBeSolvedPanel.add(solvingRangePanel, BorderLayout.SOUTH);

		constraints.gridheight = 1;
		constraints.gridy = 1;
		constraints.gridx++;
		constraints.fill = GridBagConstraints.NONE;
		mainSolverGUI.add(levelsToBeSolvedPanel, constraints);

		/*
		 * Panel for additional settings for the solver.
		 */
		JPanel settingsPanel = new JPanel(new BorderLayout());
		Texts.helpBroker.enableHelpKey(settingsPanel, "solver.Settings", null); // Enable help
		settingsPanel.setBorder(
				BorderFactory.createCompoundBorder(
						BorderFactory.createCompoundBorder(
								BorderFactory.createTitledBorder(
										Texts.getText("settings")),
								BorderFactory.createEmptyBorder(5, 5, 5, 5)),
						settingsPanel.getBorder())
		);

		    // Also display the found solutions?
		    isDisplaySolutionsEnabled = new JCheckBox(Texts.getText("solver.displaySolutions"), Settings.isDisplaySolutionsEnabled);
		    isOnlyCurrentLevelToBeSolved.addItemListener(new ItemListener() {
				@Override
				public void itemStateChanged(ItemEvent e) {
					// This check box is only enabled if more than one level is selected to be solved.
					isDisplaySolutionsEnabled.setEnabled(e.getStateChange() == ItemEvent.DESELECTED);
				}
			});
		    settingsPanel.add(isDisplaySolutionsEnabled, BorderLayout.NORTH);


		    JPanel saveToFilePanel = new JPanel(new BorderLayout());
		    saveToFilePanel.setBorder(BorderFactory.createEtchedBorder(EtchedBorder.RAISED));

		    	// Check box for selecting "save solver results to file".
			    isSaveToFileEnabled = new JCheckBox(Texts.getText("solver.saveStatsToFile"));
			    saveToFilePanel.add(isSaveToFileEnabled, BorderLayout.NORTH);

			    // Text "File"
			    saveToFilePanel.add(new JLabel(Texts.getText("solver.file")+":"), BorderLayout.WEST);

			    // The name of the file the results are to be saved to.
			    solverResultsFileName = new JTextField(20);
			    solverResultsFileName.setEnabled(false);
			    saveToFilePanel.add(solverResultsFileName, BorderLayout.CENTER);

			    // Button for opening a file chooser for selecting the file.
			    final JButton selectFileButton = new JButton(Utilities.getIcon("system-search.png", "selectFileIcon"));
			    selectFileButton.addActionListener(this);
			    selectFileButton.setActionCommand("selectFileForSaving");
			    selectFileButton.setEnabled(false);
			    selectFileButton.setMinimumSize(new Dimension(selectFileButton.getIcon().getIconWidth(),
			    		                                      selectFileButton.getIcon().getIconHeight()));
			    selectFileButton.setPreferredSize(selectFileButton.getMinimumSize());
			    selectFileButton.setContentAreaFilled(false);
			    saveToFilePanel.add(selectFileButton, BorderLayout.EAST);

			    isSaveToFileEnabled.addActionListener(new MyActionListener() {
					@Override
					public void actionPerformed(ActionEvent actionevent) {
						// Set the text field and button enabled when the check box is enabled.
						solverResultsFileName.setEnabled(isSaveToFileEnabled.isSelected());
						selectFileButton.setEnabled(isSaveToFileEnabled.isSelected());
					}});

	    settingsPanel.add(saveToFilePanel, BorderLayout.CENTER);

		 // NumberInputField for setting the limit limit.
	    timeLimit = new NumberInputTF( Settings.isSolverTimeLimited,
	    		                       Texts.getText("solver.timeLimit"),
	    		                       1, 9999, Settings.solverTimeLimitInSeconds,
	    		                       true );
	    settingsPanel.add(timeLimit, BorderLayout.SOUTH);

		constraints.gridheight = 1;
		constraints.gridy = 1;
		constraints.gridx++;
		constraints.fill = GridBagConstraints.NONE;
		mainSolverGUI.add(settingsPanel, constraints);


		/*
		 * Panel for debugging.
		 */
		if(Debug.isDebugModeActivated) {
			JPanel debugPanel = new JPanel(new GridLayout(3,1));
			debugPanel.setBorder(
					BorderFactory.createCompoundBorder(
							BorderFactory.createCompoundBorder(
									BorderFactory.createTitledBorder("Debug"),
									BorderFactory.createEmptyBorder(5, 5, 5, 5)),
							debugPanel.getBorder())
			);

				// A CheckBox for selecting "show all board positions while solving".
			    isShowBoardPositionsActivated = new JCheckBox("show board positions while solving", false);
			    debugPanel.add(isShowBoardPositionsActivated);

			    justPackingOrder = new JButton("Just packing order");
			    justPackingOrder.setActionCommand("justPackingOrder");
			    justPackingOrder.addActionListener(this);
			    debugPanel.add(justPackingOrder);

		    constraints.gridx++;
		    mainSolverGUI.add(debugPanel, constraints);

			// Debug: During solving a level the path to a board position can be displayed by pressing "S".
			JButton showPathToBoardPosition = new JButton("Show path");
			showPathToBoardPosition.addActionListener(new ActionListener() {

				@Override
				public void actionPerformed(ActionEvent e) {
					Debug.isDisplayPathToCurrentBoardPositionActivated = true;
				}
			});
			constraints.gridy++;
			mainSolverGUI.add(showPathToBoardPosition, constraints);

		}


		/*
		 * Text fields for showing status information.
		 */
		JPanel southPanel = new JPanel(new GridBagLayout());
		constraints.gridx = 0;
		constraints.gridy = 4;
		constraints.gridheight = 1;
		constraints.gridwidth  = 8;
		constraints.weightx = 1;
		constraints.insets.set(5, 10, 5, 10);
		constraints.fill   = GridBagConstraints.HORIZONTAL;
		constraints.anchor = GridBagConstraints.CENTER;
		mainSolverGUI.add(southPanel, constraints);

			infoText = new JTextField(10);
			infoText.setEditable(false);
			constraints.gridx = 0;
			constraints.gridy = 0;
			constraints.gridheight = 1;
			constraints.gridwidth  = 1;
			constraints.weightx = 0.5;
			constraints.insets.set(0, 0, 0, 0);
			constraints.anchor = GridBagConstraints.LINE_START;
			southPanel.add(infoText, constraints);

			timeTextField = new JTextField(10);
			timeTextField.setText(Texts.getText("time")+": 00:00:00");
			timeTextField.setEditable(false);
			constraints.gridx++;
			constraints.gridwidth  = 1;
			constraints.weightx = 0;
			constraints.fill   = GridBagConstraints.NONE;
			constraints.anchor = GridBagConstraints.LINE_END;
			southPanel.add(timeTextField, constraints);


		/*
		 * Add two extra labels so the components are shown centered.
		 */
		constraints.gridx = 0;
		constraints.gridy = 1;
		constraints.gridheight = 1;
		constraints.gridwidth  = 1;
		constraints.weightx = 1;
		constraints.insets.set(0, 0, 0, 0);
		constraints.fill = GridBagConstraints.NONE;
		JLabel labelLeft = new JLabel("          ");
		labelLeft.setForeground(getBackground());
		mainSolverGUI.add(labelLeft, constraints);

		constraints.gridx = 5;
		JLabel labelRight = new JLabel("          ");
		labelRight.setForeground(getBackground());
		mainSolverGUI.add(labelRight, constraints);

		// Set the solver help for this GUI.
		Texts.helpBroker.enableHelpKey(this, "solver", null);
	}


	/**
	 * Handles all action events.
	 *
	 * @param evt
	 *            the action event to be handled.
	 */
	@Override
	public final void actionPerformed(ActionEvent evt) {

		String action = evt.getActionCommand();

		// The solver is to be started.
		if (action.equals("startSolver")) {

			// Be sure that the solver isn't running at the moment.
			if(solvingProcess != null) {
				return;
			}

			// If a log is to be saved check whether the file can be created.
			if(isSaveToFileEnabled.isSelected()) {

				// Check for any file being specified at all.
				if(solverResultsFileName.getText().equals("")) {
					MessageDialogs.showErrorTextKey(this, "solver.noFileForSaving");
					solverResultsFileName.requestFocus();
					return;
				}

				// Check for correct file.
				try {
					new File(solverResultsFileName.getText()).createNewFile();
				} catch (IOException e) {
					MessageDialogs.showErrorTextKey(this, "solver.fileCannotBeCreated");
					solverResultsFileName.requestFocus();
					return;
				}
			}

			// Rename the start button to "stop solver" and set a new action command.
			startSolverButton.setToStop();

			// Initialize the solverCancelledReason.
			solverStatus = SolverStatus.RUNNING;

			// Start the solver.
			if(isOnlyCurrentLevelToBeSolved.isSelected()) {
				solveLevels(application.currentLevel.getNumber(), application.currentLevel.getNumber());
			} else {
				if(isWholeCollectionToBeSolved.isSelected()) {
					solveLevels(1, application.currentLevelCollection.getLevelsCount());
				} else {
					solveLevels(((Integer) levelsToBeSolvedFromValue.getValue()), ((Integer) levelsToBeSolvedToValue.getValue()));
				}
			}

			return;
		}

		// The solver is to be stopped.
		if (action.equals("stopSolver")) {

			// The user is now asked whether he really wants to stop the solver.
			// If more than one level is to be solved the user can also
			// skip the current level and continue with the next one.

			// Only current level to be solved or only one specific level to be solved?
			if(isOnlyCurrentLevelToBeSolved.isSelected() || (isWholeCollectionToBeSolved.isSelected() == false &&
					((Integer) levelsToBeSolvedFromValue.getValue()).equals((levelsToBeSolvedToValue.getValue())))) {

				// Only one level is to be solved. Ask: Stop solver or cancel this dialog.
				int answer = JOptionPane.showOptionDialog(this,
	                    Texts.getText("solver.pleaseChooseAnOption"),
	                    Texts.getText("question"),
	                    JOptionPane.OK_CANCEL_OPTION,
	                    JOptionPane.QUESTION_MESSAGE,
	                    null,
	                    new Object[] { Texts.getText("solver.stopSolver"), Texts.getText("cancel") },
						Texts.getText("solver.stopSolver"));

				// If the user has canceled the InputDialog return immediately.
				if (answer == JOptionPane.NO_OPTION) {
					return;
				}

				// User wants to cancel the solver.
				if(answer == JOptionPane.YES_OPTION) {
					solverStatus = SolverStatus.CANCELED_DUE_TO_STOP_OF_SOLVING;
				}
			}
			else {
				// More than one level is to be solved: Ask: stop solver, skip level or cancel this dialog.
				int answer = JOptionPane.showOptionDialog(this,
	                    Texts.getText("solver.pleaseChooseAnOption"),
	                    Texts.getText("question"),
	                    JOptionPane.YES_NO_CANCEL_OPTION,
	                    JOptionPane.QUESTION_MESSAGE,
	                    null,
	                    new Object[] {
								Texts.getText("solver.stopSolver"),
								Texts.getText("solver.skipLevel"),
								Texts.getText("cancel")},
						Texts.getText("solver.stopSolver"));

				// If the user has canceled the InputDialog return immediately.
				if (answer == JOptionPane.CANCEL_OPTION) {
					return;
				}

				// User wants to cancel the solver.
				if(answer == JOptionPane.YES_OPTION) {
					solverStatus = SolverStatus.CANCELED_DUE_TO_STOP_OF_SOLVING;
				}

				// User skips the current level.
				if(answer == JOptionPane.NO_OPTION) {
					solverStatus = SolverStatus.CANCELED_DUE_TO_SKIP_OF_LEVEL;
					if(solver != null) {
						solver.cancel(true);
					}
					return;
				}
			}

			// Cancel the solver in a new Thread because we have to wait for the EDT
			// having reached the "done" method of the SwingWorker.
			new Thread() {
				@Override
				public void run() {

					// If the solver is still running stop it.
					if(solver != null) {
						solver.cancel(true);

						setCursor(new Cursor(Cursor.WAIT_CURSOR));

						// Wait for the solver having finished canceling.
						for(int i=0; i<100 && solver != null; i++) {
							try {
								sleep(30);
							} catch (InterruptedException e) {}
						}

						setCursor(new Cursor(Cursor.DEFAULT_CURSOR));
					}

					// No solving process running anymore.
					solvingProcess = null;
				}
			}.start();

			return;
		}


		// The solver is to be closed.
		if (action.equals("closeSolver")) {

			if(solvingProcess != null) {
				final int confirmResult = JOptionPane.showConfirmDialog( this,
												Texts.getText("solver.closeWhileRunning"),
												Texts.getText("warning"),
												JOptionPane.YES_NO_OPTION,
												JOptionPane.WARNING_MESSAGE );
				if(confirmResult != JOptionPane.YES_OPTION) {
					return;
				}
			}

			// We use a new Thread because we have to wait for the EDT having
			// reached the "done" method of the Swingworker of the solver.
			new Thread() {
				@Override
				public void run() {

					// Cancel the solving process by setting the stop reason.
					solverStatus = SolverStatus.CANCELED_DUE_TO_STOP_OF_SOLVING;

					// If the solver is still running stop it.
					if(solver != null) {
						solver.cancel(true);

						setCursor(new Cursor(Cursor.WAIT_CURSOR));

						// Wait for the solver having finished canceling.
						for(int i=0; i<100 && solver != null; i++) {
							try {
								sleep(30);
							} catch (InterruptedException e) {}
						}

						setCursor(new Cursor(Cursor.DEFAULT_CURSOR));
					}

					// Save the settings.
					Settings.isSolverTimeLimited = timeLimit.isFieldActive();
					if(Settings.isSolverTimeLimited) {
						Settings.solverTimeLimitInSeconds = timeLimit.getValueAsInteger();
					}
					Settings.isDisplaySolutionsEnabled = isDisplaySolutionsEnabled.isSelected();

					// Inform the application that the solver is to be closed.
					SwingUtilities.invokeLater(new Runnable() {
						@Override
						public void run() {
							application.actionPerformed(new ActionEvent(this, 0, "closeSolver"));
						}
					});
				}
			}.start();

			return;
		}

		// Let the user choose a file for saving the solving results to.
		if(action.equals("selectFileForSaving")) {

			// Let the user choose a file.
			String file = getFileDataForSave(new File(solverResultsFileName.getText()).getParent());

			// Set the new file if the user has chosen one.
			if(file != null) {
				solverResultsFileName.setText(file);
			}

			return;
		}


		// For debugging the packing order search can be started without solving the level.
		if(action.equals("justPackingOrder")) {
			final SolverGUI j = this;
			new Thread() {
				@Override
				public void run() {
					PackingSequenceSearch p = new PackingSequenceSearch(application.board, j, new SolverAnySolution(application, j));
					p.debugSearchPackingSequence();

					int i = 0;
					for(BoardPositionPackingSequence b : p.getPackingSequence()) {
						System.out.println( "Push " + (++i)
								          + ": "   + b.getStartBoxPosition()
								          + " -> " + b.getTargetBoxPosition() );
					}
				}
			}.start();
			return;
		}
	}

	/**
	 * Sets the status bar text.
	 *
	 * @param text
	 *            the text to be shown in the status bar
	 */
	final public void setInfoText(String text) {
		infoText.setText(text);
	}


	/**
	 * Solves a range of levels of the current collection and displays a statistic about
	 * the pushes and number of board positions needed to solve a specific level.
	 *
	 * @param firstLevelToBeSolved the level to start solving at
	 * @param lastLevelToBeSolved  the last level to be solved
	 */
	private void solveLevels(final int firstLevelToBeSolved, final int lastLevelToBeSolved) {

		// Backup the current level number.
		final int backupLevelNo = application.currentLevel.getNumber();

		// Structure for storing the solving statistics of a level.
		class LevelData {
			int levelNo;
			int pushesCount;
			int movesCount;
			float timeForSolving;
			int boardPositionsCount;
			String solution = null;
			String status = "";
		}

		JDialog solvingProgress = new JDialog(application, Texts.getText("solver.solvingResults"));
		Utilities.setEscapable(solvingProgress);
		final JTextArea textArea = new JTextArea();
		textArea.setFont(new Font(Font.MONOSPACED, Font.PLAIN, 12));
		solvingProgress.add(new JScrollPane(textArea));
		solvingProgress.setSize(application.applicationGUI.getSize());

		// If multiple levels are to be solved display the solving process.
		if(firstLevelToBeSolved != lastLevelToBeSolved) {
			solvingProgress.setVisible(true);
		}

		// Final reference to this GUI for the SwingWorker.
		final SolverGUI solverGUI = this;

		// Start the solver in a new thread in order to not block the event dispatching
		// thread of Swing while waiting for the result of the solver.
		solvingProcess = new SwingWorker<Void, LevelData>() {

			// Statistic values.
			int solvedLevelsCount    = 0;
			int notSolvedLevelsCount = 0;

			int totalPushes 		= 0;
			int totalMoves 		    = 0;
			long totalTime 		    = 0;

			@Override
			protected Void doInBackground() throws Exception {

		    	// The found solution or null if no solution has been found.
		    	Solution solution = null;

				// Save current debug status.
				boolean isDebugModeActivated = Debug.isDebugModeActivated;

				// For better performance the debug informations aren't displayed during
				// solving (except for the case where only one level is to be solved).
				if(firstLevelToBeSolved < lastLevelToBeSolved) {
					Debug.isDebugModeActivated = false;
				}

				final int maxTime = timeLimit.getValueAsInteger() == null ? Integer.MAX_VALUE : timeLimit.getValueAsInteger();

				// Solve all levels or stop if the user has stopped the solving process.
				for( int levelNo = firstLevelToBeSolved
				   ;    levelNo <= lastLevelToBeSolved
				     && solverStatus != SolverStatus.CANCELED_DUE_TO_STOP_OF_SOLVING
				   ; levelNo++ )
				{

					// Load the level. If only the currently loaded level is to be solved
					// the level needn't to be set. This has the advantage that the user
					// can make some pushes and then let the solver try to solve
					// the level beginning at the current board position instead of at the
					// start position of the level.
					if(firstLevelToBeSolved != lastLevelToBeSolved || firstLevelToBeSolved != application.currentLevel.getNumber()) {
						application.setLevelForPlaying(levelNo);
					}

					// Collect new data for the new level.
					LevelData levelData = new LevelData();
					levelData.levelNo = levelNo;

					// Disable the undo / redo button.
					application.applicationGUI.setUndoButtonsEnabled(false);
					application.applicationGUI.setRedoButtonsEnabled(false);

					// Setting the level for playing has set the play mode.
					// Disable again the objects not needed for the solver.
					application.applicationGUI.setSolverDependentObjectsEnabled(false);

					// Jump over invalid levels.
					if(application.isLevelValid() == false) {

						// Display a message for this level in the log.
						levelData.status = Texts.getText("solver.invalidLevel");
						publish(levelData);

						// Continue with next level.
						continue;
					}

					// Redraw the new board immediately.
					try {
						SwingUtilities.invokeAndWait(new Runnable() {
							@Override
							public void run() {
								application.applicationGUI.paintImmediately(
										0, 0,
										application.applicationGUI.getWidth(),
										application.applicationGUI.getHeight() );
							}
						});
					} catch (Exception e) {}

					// Create the needed solver.
					if(anySolution.isSelected()) {
						solver = new SolverAnySolution(application, solverGUI);
					} else if(pushOptimalSolution.isSelected()) {
						solver = new SolverAStar(application, solverGUI);
					} else if(pushesMovesSolution.isSelected()) {
						solver = new SolverAStarPushesMoves(application, solverGUI);
					} else if(movesPushesSolution.isSelected()) {
						solver = new SolverAStarMovesPushes(application, solverGUI);
					} else if(evolutionarySolver.isSelected()) {
						solver = new SolverEvolutionary(application, solverGUI);
					} else if(BFSSolver.isSelected()) {
						solver = new SolverBFS(application, solverGUI);
					}

					// Get current time
					long timeStamp = System.currentTimeMillis();

					// Start the solver.
					// This is done in an own executor instead of solver.execute()
					// because of a bug since JDK 6 update 18 (Bug ID: 6880336).
					Utilities.executor.execute(solver);

					// TimeTask for showing the time the solver spends on the current level.
					TimerTask showTimeTask = new TimerTask() {

						int hours   = 0;
						int minutes = 0;
						int seconds = 0;

						@Override
						public void run() {

							// Set/display the current (old) time as string.
							timeTextField.setText(String.format("%s: %02d:%02d:%02d",
									                            Texts.getText("time"),
									                            hours, minutes, seconds));

							// Calculate the next time to display.
							if(++seconds == 60) {
								seconds = 0;
								if(++minutes == 60) {
									minutes = 0;
									hours++;
								}
							}
						}
					};
					// Create daemon timer.
					Timer showTimeTimer = new Timer(true);

					// The time is to be shown every second.  Since we want to start with
					// displaying 00:00:00, we start with a zero delay.
					showTimeTimer.scheduleAtFixedRate(showTimeTask, 0, 1000);

					// Wait at most "maxTime" seconds before the result is returned.
					try {
						// If the solving process isn't canceled wait for the solver having finished.
						if(isCancelled() == false) {
							solution = null;
							solverStatus = SolverStatus.RUNNING;
							solution = solver.get(maxTime, TimeUnit.SECONDS);
						}
					}catch (CancellationException e) {

						// Check for "out of memory".
						if(solver.isSolverStoppedDueToOutOfMemory) {
							setInfoText(Texts.getText("solver.failedDueToOutOfMemory"));
							levelData.status = Texts.getText("solver.failedDueToOutOfMemory");
						}

						// If the solver has been canceled by the user stop the whole solver.
						if(solverStatus == SolverStatus.CANCELED_DUE_TO_STOP_OF_SOLVING) {
							setInfoText(Texts.getText("solver.solverStopped"));
							levelData.status = Texts.getText("solver.failedDueToSolverStopped");
							publish(levelData);
							break;
						}

						// If the solver has been canceled for skipping the level just display a message.
						if(solverStatus == SolverStatus.CANCELED_DUE_TO_SKIP_OF_LEVEL) {
							setInfoText(Texts.getText("solver.levelSkipped"));
							levelData.status = Texts.getText("solver.failedDueToSkipedLevel");
						}
					}
					catch (TimeoutException e2) {
						// If the solver has been interrupted because of a time out display a message.
						setInfoText(Texts.getText("solver.timeout"));
						levelData.status = Texts.getText("solver.failedDueToOutOfTime");
					}
					 catch (Exception e) {
						 if(Debug.isDebugModeActivated) {
							 e.printStackTrace();
						 }
					 }
					finally {
						// Cancel the solver.
						solver.cancel(true);

						// Cancel the thread displaying the time.
						showTimeTimer.cancel();

						// Wait for the solver having finished canceling.
						for(int i=0; i<300 && solver.getProgress() != 100; i++) {
							try {
								Thread.sleep(30);
							} catch (InterruptedException e) {}
						}
					}

					// Add the new solution to the solutions of the level
					// and check the return code.
					if (solution != null) {
						final SolutionType solutionType = application.currentLevel.addSolution(solution);
						if (solutionType == SolutionType.INVALID_SOLUTION) {
							solution = null;
						}
					}
					// Fill the solving data for the level.
					levelData.timeForSolving =  Math.round((System.currentTimeMillis() - timeStamp)/100f)/10f;
					if(solution != null) {
						levelData.status	  		  = Texts.getText("solver.levelSolved");
						levelData.solution	  		  = solution.lurd;
						levelData.pushesCount 		  = application.movesHistory.getPushesCount();
						levelData.movesCount  		  = application.movesHistory.getMovementsCount();
						levelData.boardPositionsCount = solver.boardPositionsCount;
					} else {
						// If no reason for failing is stored simply display "not solved".
						if(levelData.status.equals("")) {
							levelData.status  = Texts.getText("solver.notSolved");
						}
					}

					// Display the solver statistic for the level.
					publish(levelData);
				}

				// Restore the debug status from the saved value
				Debug.isDebugModeActivated = isDebugModeActivated;

				return null;
			}

			@Override
			protected void process(List<LevelData> data) {

				for(LevelData levelData : data) {

					// Level No
					String levelNoString = String.format("%3d", levelData.levelNo);

					// Level title (40 characters at most).
					String levelTitle = application.currentLevelCollection.getLevel(levelData.levelNo).getTitle();
					levelTitle = String.format("%-40s", levelTitle);

					// Number of pushes
					String pushesCountString = String.format("%3d", levelData.pushesCount);

					// Number of moves
					String movesCountString = String.format("%4d", levelData.movesCount);

					// Total time
					String totalTimeString = String.format("%5.1f", levelData.timeForSolving);

					textArea.append(Texts.getText("level")        		  + ": " + levelNoString     + " "     +
								    Texts.getText("levelTitle")   		  + ": " + levelTitle		 + "  | "  +
								    Texts.getText("moves")		  		  + ": " + movesCountString	 + "  | "  +
								    Texts.getText("pushes")		  		  + ": " + pushesCountString + "   | " +
								    Texts.getText("time")		  		  + ": " + totalTimeString	 + Texts.getText("solver.seconds") + "  | " +
								    Texts.getText("solver.solvingStatus") + ": " + levelData.status  +
								    (Debug.isSettingsDebugModeActivated ? "  |   "+String.format("%s %12d", Texts.getText("numberofpositions"), levelData.boardPositionsCount) : "") +   //Misuse of "isSettingsDebugModeActivated": added for Michael (see mails 22.04.2013).
							        "\n");

					// If requested also display the found solution as lurd string.
					if(isDisplaySolutionsEnabled.isSelected()) {
						if(levelData.solution != null) {
							textArea.append(Texts.getText("solver.solution")+":\n"+levelData.solution+"\n\n");
						} else {
							textArea.append(Texts.getText("solver.solution")+":\n\n");
						}
					}

					// Calculate the total statistics.
					totalPushes 		+= levelData.pushesCount;
					totalMoves			+= levelData.movesCount;
					totalTime       	+= levelData.timeForSolving;

					// If there is a solution the level has been solved.
					if(levelData.solution != null) {
						solvedLevelsCount++;
					} else {
						notSolvedLevelsCount++;
					}
				}
			}


			/**
			 * This method is called when the solver has finished solving all levels.
			 */
			@Override
			protected void done() {

				// Display the total sums of the data.
				textArea.append("\n"+String.format("%4d ", solvedLevelsCount+notSolvedLevelsCount)+Texts.getText("solver.totalLevelsProcessed"));
				textArea.append("\n"+String.format("%4d ", solvedLevelsCount)+Texts.getText("solver.totalSolvedLevels"));
				textArea.append("\n"+String.format("%4d ", notSolvedLevelsCount)+Texts.getText("solver.totalFailedLevels"));
				textArea.append("\n\n" + Texts.getText("moves") +": "+totalMoves
						        + ", " + Texts.getText("pushes")+": "+totalPushes
						        + ", " + Texts.getText("time")  +": "+totalTime
						                                        + Texts.getText("solver.seconds"));

				// Save the log to a file if requested.
				if(isSaveToFileEnabled.isSelected()) {

					// Boolean indicating whether saving the data has failed.
					boolean isFileSavingFailed = false;

					// The PrintWriter for saving the data.
					PrintWriter outputFile = null;

					try {
						// Create PrintWriter for saving the data.
						outputFile = new PrintWriter(solverResultsFileName.getText());

						// Get the text of the text Area as String array.
						String[] texts = textArea.getText().split("\n");

						// Write the text into the file.
						for(String s : texts) {
							outputFile.write(s);
							outputFile.println();
						}

						// Check the error status.
						isFileSavingFailed = outputFile.checkError();

					} catch (IOException e1) {
						isFileSavingFailed = true;
					}
					finally {
						// Close the file.
						if(outputFile != null) {
							outputFile.close();
						}
					}

					// Display a message if saving the data has failed.
					if(isFileSavingFailed) {
						JOptionPane.showMessageDialog( solverGUI,
								Texts.getText("solver.savingResultsFailed"),
								Texts.getText("warning"),
								JOptionPane.WARNING_MESSAGE );
					}
				}

				// Rename the start button to "start solver" and set a new action command.
				startSolverButton.setToStart();

				// Show the level that has been shown before the solver has been started
				// (only necessary if another than the currently loaded level has been solved).
				if(firstLevelToBeSolved != lastLevelToBeSolved || firstLevelToBeSolved != application.currentLevel.getNumber()) {
					application.setLevelForPlaying(backupLevelNo);
				}

				// Disable the undo / redo button.
				application.applicationGUI.setUndoButtonsEnabled(false);
				application.applicationGUI.setRedoButtonsEnabled(false);

				// Setting the level for playing has set the play mode. Disable again the objects not needed for the solver.
				application.applicationGUI.setSolverDependentObjectsEnabled(false);

				// Repaint the board. It may contain new information like the numbers of the packing order.
				application.applicationGUI.mainBoardDisplay.repaint();

				// Set: no solver running at the moment.
				solver         = null;
				solvingProcess = null;
			}
		};

		// Start the SwingWorker for solving the levels.
		solvingProcess.execute();
	}


	/**
	 * Displays a <code>JFileChooser</code> dialog for letting the user choose
	 * a file (name) for saving the solver log.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @return name and location of the chosen file
	 */
	private String getFileDataForSave(String directoryPath) {

		// Set current path as default if needed.
		if(directoryPath == null) {
			directoryPath = "./";
		}

		// Set the passed path as current directory.
		// If it doesn't exist take the current directory as default.
		File startDirectory = new File(directoryPath);
		if (startDirectory.isDirectory() == false) {
			startDirectory = new File(OSSpecific.getUserHomeDirectory());
		}

		if(OSType.isMac) {
			return getFileDataForSaveMacOS(directoryPath);
		}

		// Create JFileCooser.
		JFileChooser fc = new JFileChooser(startDirectory);
		fc.setDialogTitle(Texts.getText("solver.chooseFileForSolverLog"));
		fc.setDialogType(JFileChooser.SAVE_DIALOG);
		Dimension d = new Dimension(700, 400);
		fc.setMinimumSize(d);
		fc.setPreferredSize(d);

		//	Filter files for: *.txt
		fc.setFileFilter(new FileFilter() {
			@Override
			public boolean accept(File f) {
				return  f.getName().toLowerCase().endsWith(".txt") || f.isDirectory();
			}

			@Override
			public String getDescription() {
				return "*.txt";
			}
		});

		// If the JFileChooser has been canceled or an error occurred return immediately.
		if (fc.showSaveDialog(this) != JFileChooser.APPROVE_OPTION) {
			return null;
		}

		// Get the chosen file name.
		String fileName = fc.getCurrentDirectory() + System.getProperty("file.separator")
		                + fc.getSelectedFile().getName();

		// Add ".txt" if the file doesn't have a file extension, yet.
		if (fileName.lastIndexOf('.') == -1) {
			fileName += ".txt";
		}

		// Handle the case there already is a file with that name.
		if (new File(fileName).exists()) {
			switch (JOptionPane.showConfirmDialog(this, Texts.getText("file_exists_overwrite"),
					Texts.getText("solver.confirmOverwrite"), JOptionPane.YES_NO_CANCEL_OPTION, JOptionPane.WARNING_MESSAGE)) {
			case JOptionPane.YES_OPTION:
				break;
			case JOptionPane.NO_OPTION:
				return getFileDataForSave(fc.getCurrentDirectory().toString());
			case JOptionPane.CANCEL_OPTION:
				return null;
			}
		}

		return fileName;
	}

	/**
	 * Displays a <code>JFileChooser</code> dialog for letting the user choose
	 * a file (name) for saving the solver log.
	 *
	 * @param directoryPath path to the directory the user may choose the file from
	 * @return name and location of the chosen file
	 */
	private String getFileDataForSaveMacOS(String directoryPath) {

		FileDialog fileDialog = new FileDialog(application, Texts.getText("solver.chooseFileForSolverLog"), FileDialog.SAVE);
		fileDialog.setFile(".txt");
		fileDialog.setDirectory(directoryPath);
		fileDialog.setFilenameFilter(new FilenameFilter(){
		    @Override
			public boolean accept(File dir, String name){
		      return name.endsWith(".txt");
		    }
		 });
		fileDialog.setVisible(true);

		String filename = fileDialog.getFile();
		if (filename != null) {

			filename = fileDialog.getDirectory() + System.getProperty("file.separator") + filename;

			// Add ".txt" if the file doesn't have a file extension, yet.
			if (filename.lastIndexOf('.') == -1) {
				filename += ".txt";
			}
		}
		fileDialog.dispose();

		return filename;
	}

	// ActionListener class for adding an ActionListener.
	abstract class MyActionListener implements ActionListener {}

}
