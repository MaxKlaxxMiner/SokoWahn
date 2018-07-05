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
import java.awt.Dimension;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.GridLayout;
import java.awt.event.ActionEvent;
import java.awt.event.WindowEvent;
import java.util.ArrayList;
import java.util.Arrays;

import javax.swing.BorderFactory;
import javax.swing.BoxLayout;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTextArea;
import javax.swing.JTextField;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.gui.MessageDialogs;
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
 * This class is used to find solutions for modified levels.
 * <p>
 * The user is offered to change the current level, for instance by adding walls.
 * Then a solution for this new level is searched by taking the old solution
 * for the unmodified level as guide.<br>
 * This class uses the "Optimizer" class to search for a solution
 * in the vicinity of the old solution.
 */
@SuppressWarnings("serial")
public final class RemodelSolverGUI extends OptimizerGUISuperClass {

	// The object displaying the board of the level.
	BoardDisplayRemodelSolver boardDisplay;



	/**
	 * Creates an object for searching a solution of a modified level.
	 * <p>
	 * Although this class is a solver it internally uses the optimizer class to solve the level.
	 *
	 * @param application  reference to the main object that holds references to all other objects
	 */
	public RemodelSolverGUI(final JSoko application) {

		// Reference to the main object of this program holding all references.
		this.application = application;

		// Save the reference to the level.
		currentLevel = application.currentLevel;

		// The vicinity settings fields.
		vicinitySettings = new ArrayList<NumberInputTF>(3);

		// Display the GUI of this remodel solver.
		createGUI();

		// Show the GUI.
		setVisible(true);
	}

	/**
	 * Displays the GUI of the remodel solver.
	 */
	final private void createGUI() {

		// Set the title.
		setTitle(Texts.getText("remodelSolver.JSokoRemodelSolver")+"- "+currentLevel.getTitle());

		// Set the bounds of this Frame corresponding to the bounds of the main application window.
		setBounds(application.getBounds());

		// Set BorderLayout for this GUI.
		setLayout(new BorderLayout());

		// Create an object for displaying the level and add it at the center.
		boardDisplay = new BoardDisplayRemodelSolver(currentLevel);
		add(boardDisplay, BorderLayout.CENTER);

		// In the south a new JPanel is added containing all elements for setting up the solver.
		JPanel southPanel = new JPanel(new GridBagLayout());
		southPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createCompoundBorder(BorderFactory.createEmptyBorder(), BorderFactory.createEmptyBorder(5, 0, 0, 0)), southPanel.getBorder()));
		add(southPanel, BorderLayout.SOUTH);

		// Set constraints.
		GridBagConstraints constraints = new GridBagConstraints();

		/*
		 * Create a panel for the vicinity settings and add 3 number fields for letting the user set the distance how far the boxes may be repositioned.
		 */
		JPanel vicinitySettingsPanel = new JPanel(new GridLayout(3, 1));
		vicinitySettingsPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("vicinitySquares")), BorderFactory.createEmptyBorder(5, 5, 5, 5)), vicinitySettingsPanel.getBorder()));

		// Add the input fields for the vicinity settings.
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox1Enabled, Texts.getText("box") + " 1", 1, 999, Settings.vicinitySquaresBox1, true));
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox2Enabled, Texts.getText("box") + " 2", 1, 999, Settings.vicinitySquaresBox2, true));
		vicinitySettings.add(new NumberInputTF(Settings.vicinitySquaresBox3Enabled, Texts.getText("box") + " 3", 1, 999, Settings.vicinitySquaresBox3, true));

		// Add the input fields to the panel.
		for (NumberInputTF numberField : vicinitySettings) {
			vicinitySettingsPanel.add(numberField);
		}

		// Add the vicinity settings panel to the main panel.
		constraints.gridx = 1;
		constraints.gridy++;
		constraints.gridheight = 2;
		constraints.anchor = GridBagConstraints.NORTH;
		constraints.fill = GridBagConstraints.NONE;
		constraints.insets.set(0, 0, 0, 0);
		southPanel.add(vicinitySettingsPanel, constraints);


		/*
		 * Button for starting the solver.
		 */
		startButton = new StartStopButton( "solver.startSolver", "start",
				                           "solver.stopSolver",  "stop"  );
		startButton.addActionListener(this);
		constraints.gridheight = 1;
		constraints.gridwidth  = 1;
		constraints.gridx++;
		constraints.gridy+=2;
		southPanel.add(startButton, constraints);


		/*
		 * List showing the metrics of all solutions of the level.
		 */
		// Create a GUI for showing the current solutions of the level.
		solutionsGUI = new SolutionsGUI(application, false, false);
		solutionsGUI.setLevel(currentLevel);

		// Put the list into a scroll pane.
		JScrollPane listScroller = new JScrollPane(solutionsGUI);
		listScroller.setAlignmentX(LEFT_ALIGNMENT);
		listScroller.setPreferredSize(new Dimension(200, 130));
		listScroller.setMinimumSize(listScroller.getPreferredSize());

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
		infoText = new JTextField(80);
		infoText.setEditable(false);
		southPanel.add(infoText, constraints);

		/*
		 * Add two extra labels so the components are shown centered.
		 */
		constraints.gridx = 0;
		constraints.gridy = 1;
		constraints.gridheight = 1;
		constraints.gridwidth = 1;
		constraints.insets.set(0, 0, 0, 0);
		constraints.fill = GridBagConstraints.NONE;
		JLabel labelLeft = new JLabel("          ");
		labelLeft.setForeground(getBackground());
		southPanel.add(labelLeft, constraints);

		constraints.gridx = 5;
		JLabel labelRight = new JLabel("           ");
		labelRight.setForeground(getBackground());
		southPanel.add(labelRight, constraints);

		// Set the JSoko icon.
		setIconImage(Utilities.getJSokoIcon());
	}

	@Override
	protected void processWindowEvent(WindowEvent e) {

		// If the user closes the Frame check whether the optimizer is still running and save the settings.
		if (e.getID() == WindowEvent.WINDOW_CLOSING) {

			// Ask the user whether the running optimizer is really to be closed.
			if(isOptimizerRunning == true) {
				if(JOptionPane.showConfirmDialog(this, Texts.getText("remodelSolver.closeWhileRunning"), Texts.getText("warning"), JOptionPane.YES_NO_OPTION, JOptionPane.WARNING_MESSAGE) != JOptionPane.YES_OPTION) {
					return;
				}
				optimizer.stopOptimizer();
			}

			// The optimizer itself isn't used anymore.
			optimizer = null;
		}

		// Process the window event.
		super.processWindowEvent(e);
	}


	/* (non-Javadoc)
	 * @see java.awt.event.ActionListener#actionPerformed(java.awt.event.ActionEvent)
	 */
	@Override
	public final void actionPerformed(ActionEvent evt) {

		String action = evt.getActionCommand();

		// The "solver" is to be started.
		if (action.equals("start")) {

			// Ensure at least on solution has been selected.
			if (solutionsGUI.getSelectedIndex() == -1) {
				return;
			}

			// Be sure that the optimizer isn't running at the moment.
			if(isOptimizerRunning == true) {
				return;
			}

			// Save the current time.
			startTimestamp = System.currentTimeMillis();

			// Get the selected solutions and convert them in the Optimizer solution class.
			Object[] selectedSolutions = solutionsGUI.getSelectedValues();
			ArrayList<OptimizerSolution> solutionsToBeOptimized = new ArrayList<OptimizerSolution>(selectedSolutions.length);
			for (Object selectedSolution : selectedSolutions) {
				solutionsToBeOptimized.add(new OptimizerSolution((Solution) selectedSolution));
			}

			// Get the values of the box settings.
			int relevantBoxesCount = 0;
			int[] searchDepth = new int[vicinitySettings.size()];
			for(NumberInputTF numberField : vicinitySettings) {
				Integer value = numberField.getValueAsInteger();
				if(value != null) {
					searchDepth[relevantBoxesCount++] = value;
				}
			}

			if(relevantBoxesCount == 0) {
				JOptionPane.showMessageDialog(this, Texts.getText("selectAtLeastOneBox"));
				return;
			}

			// The array shouldn't be longer than it has to be.
			if(searchDepth.length > relevantBoxesCount) {
				int[] temp = new int[relevantBoxesCount];
				System.arraycopy(searchDepth, 0, temp, 0, relevantBoxesCount);
				searchDepth = temp;
			}

			// Create a board from the board shown in this GUI.
			// Thereby the simple deadlock squares are identified.
			Board board = boardDisplay.getBoard();

			StringBuilder validityMessage = new StringBuilder();
			if(board.isValid(validityMessage) == false) {
				MessageDialogs.showErrorString(this, validityMessage.toString());
				//JOptionPane.showMessageDialog(this, validityMessage, Texts.getText("error"), JOptionPane.ERROR_MESSAGE);
				return;
			}

			// The board is valid -> prepare the board (determine deadlock squares, ...)
			board.prepareBoard();

			// Check whether the level is a deadlock.
//			if(application.deadlockDetection.isDeadlock()) {
//				MessageDialogs.showErrorTextKey(this, "notsolvableanymore");
//				return;
//			}

			// Mark all squares as relevant squares.
			boolean[] relevantSquares = new boolean[board.size];
			Arrays.fill(relevantSquares, true);

			// Create the optimizer.
			optimizer = new Optimizer(board, this, null);

			// Start the optimizer.
			optimizer.startOptimizer(searchDepth, relevantSquares, solutionsToBeOptimized, OptimizationMethod.PUSHES_MOVES, -1, false, false, false, 1, 0, 0, 0, 0, false);

			// The optimizer is running.
			isOptimizerRunning = true;

			// Rename the start button to "stop optimizer" and set a new action command.
			startButton.setToStop();

			return;
		}

		// The optimizer is to be stopped.
		if (action.equals("stop")) {
			optimizer.stopOptimizer();
			return;
		}
	}

	/**
	 * Sets the status bar text.
	 *
	 * @param text
	 *            the text to be shown in the status bar
	 */
	@Override
	final public void setInfoText(String text) {
		infoText.setText(text);
	}

	/**
	 * This method is called when the optimizer thread has ended.
	 *
	 * @param solution the found solution
	 */
	@Override
	public void optimizerEnded(OptimizerSolution solution) {

		// Set the new status of the optimizer thread.
		isOptimizerRunning = false;

		// Display a message to inform the user about the found solution.
		if(solution == null) {
			setInfoText(Texts.getText("noNewSolutionFound")+" "+Texts.getText("time")+": "+ ((System.currentTimeMillis() - startTimestamp)/1000f)+" "+Texts.getText("seconds"));
		} else {

			// The box changes optimizing considers moves, pushes and box changes. Hence, display all values.
			setInfoText(Texts.getText("foundSolution")+" "+
			Texts.getText("moves")+" = "+solution.movesCount+", "+
			Texts.getText("pushes")+" = " +solution.pushesCount+", "+
		    Texts.getText("boxChanges")+" = "+solution.boxChanges+", "+
		    Texts.getText("time")+": "+((System.currentTimeMillis() - startTimestamp)/1000f)+" "+Texts.getText("seconds"));

			// Display the solution in a text area.
			JTextArea textarea = new JTextArea(solution.getLURD());
			textarea.setAutoscrolls(true);
			textarea.setLineWrap(true);
			textarea.setEditable(false);
			JOptionPane.showMessageDialog(this, textarea, Texts.getText("solution"), JOptionPane.PLAIN_MESSAGE);

		}

		// Rename the start button to "start solver" and set a new action command.
		startButton.setToStart();
	}

	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.optimizer.OptimizerGUISuperClass#addLogText(java.lang.String)
	 */
	@Override
	public void addLogText(String text) {
		// not implemented
	}

	@Override
	public void addLogTextDebug(String text) {
		// not implemented
	}

	/**
	 * This method is called from the optimizer every time it has found a new solution.
	 *
	 * @param bestFoundSolution  the best found solution
	 */
	@Override
	public Solution newFoundSolution(OptimizerSolution bestFoundSolution) {
		return null;
	}

	/**
	 * Catches all uncaught exceptions of all threads the optimizer uses.
	 */
	@Override
	public void uncaughtException(final Thread t, final Throwable e) {

		// Stop the optimizer.
		if(isOptimizerRunning) {
			optimizer.stopOptimizer();
		}

		// Only outOfMemory is caught.
		if (e instanceof OutOfMemoryError) {

			JOptionPane.showMessageDialog(this, Texts.getText("outOfMemory"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);

			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		} else {
			// Display a stack trace.
			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}
	}
}
