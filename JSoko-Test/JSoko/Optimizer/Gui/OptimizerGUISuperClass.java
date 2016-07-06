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

import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.ClipboardOwner;
import java.awt.datatransfer.Transferable;
import java.awt.event.ActionListener;
import java.lang.Thread.UncaughtExceptionHandler;
import java.util.ArrayList;

import javax.swing.JFrame;
import javax.swing.JTextField;
import javax.swing.JTextPane;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.gui.NumberInputTF;
import de.sokoban_online.jsoko.gui.StartStopButton;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsGUI;
import de.sokoban_online.jsoko.optimizer.Optimizer;
import de.sokoban_online.jsoko.optimizer.OptimizerSolution;


/**
 * Super class of all classes that display a GUI for the optimizer class.
 * This ensures that the optimizer GUI and the GUI for the "Remodel solver"
 * can use the same optimizer class because the optimizer class can cast the passed GUI
 * to "OptimizerGUISuperClass".
 */
@SuppressWarnings("serial")
public abstract class OptimizerGUISuperClass extends JFrame implements DirectionConstants, ActionListener, ClipboardOwner, UncaughtExceptionHandler {

	/** Reference to the main object of this program. */
	JSoko application;

	/** Text area for displaying log information while the optimizer is running. */
	JTextPane optimizerLog;

	/** JTextField for info texts. */
	JTextField infoText;

	/** The optimizer doing the search. */
	Optimizer optimizer = null;

	/** Flag indicating whether the optimizer is running or not. */
	boolean isOptimizerRunning = false;

	/** The level this remodel solver is opened for. */
	Level currentLevel = null;

	/** The board of the level. */
	Board board = null;

	/** JList holding all solutions of the level. */
	SolutionsGUI solutionsGUI;

	/** Timestamp from the moment the optimizer is started. */
	long startTimestamp;

	/** Start/stop button for the optimizer. */
	StartStopButton startButton;

	/** NumberInputTF's for the box settings. */
	ArrayList<NumberInputTF> vicinitySettings;


	/**
	 * Sets the status bar text.
	 *
	 * @param text
	 *            the text to be shown in the status bar
	 */
	public void setInfoText(String text) {
		infoText.setText(text);
	}

	/**
	 * Adds the passed <code>String</code> to the log texts of the optimizer
	 * to inform the user about the progress of the optimizer.
	 *
	 * @param text  text to be added to the log
	 * @see #addLogTextDebug(String)
	 */
	abstract public void addLogText(final String text);

	/**
	 * Adds the passed <code>String</code> to the log texts of the optimizer
	 * in a "Monospaced" font.
	 * This is mainly intended for debugging output.
	 *
	 * @param text  text to be added to the log
	 * @see #addLogText(String)
	 */
	abstract public void addLogTextDebug(final String text);

	/**
	 * This method is called from the optimizer every time it has found
	 * a new solution in the middle of its work.
	 *
	 * @param bestFoundSolution  the best found solution so far
	 * @return the solution that has been added to the level
	 */
	abstract public Solution newFoundSolution(OptimizerSolution bestFoundSolution);

	/**
	 * This method is called when the optimizer thread has ended.
	 * This is the main main way to announce the overall optimization result.
	 *
	 * @param bestFoundSolution the best found solution
	 */
	abstract public void optimizerEnded(OptimizerSolution bestFoundSolution);

	@Override
	public void lostOwnership(Clipboard clipboard, Transferable contents) {}

}
