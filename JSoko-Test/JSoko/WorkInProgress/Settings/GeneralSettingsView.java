package de.sokoban_online.jsoko.workInProgress.settings;

import java.awt.BorderLayout;
import java.awt.GridLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;

import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JPanel;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.GUI;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.translator.Translator;



/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2011 by Matthias Meger, Germany
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *
 *	JSoko is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *	GNU General Public License for more details.
 *
 *	You should have received a copy of the GNU General Public License
 *	along with JSoko; if not, write to the Free Software
 *	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

/**
 * This class displays the "LevelsView" in the level management dialog and handles
 * all actions in this view.
 * This Panel contains all elements of the "LevelsView" and is displayed in a
 * tabbed pane in the level management.
 */
@SuppressWarnings("serial")
public class GeneralSettingsView extends JPanel {

	/** Reference to the main object which holds all references. */
	private final JSoko application;

	private final GUI applicationGUI;

	/**
	 * Creates the <code>JPanel</code> for displaying the data of the levels
	 * in the <code>LevelManagement</code>.
	 *
	 * @param database the database of the program
	 * @param levelManagementDialog the <code>JDialog</code> all views are displayed in
	 * @param application Reference to the main object which holds all references
	 */
	public GeneralSettingsView(JSoko application) {

		// Save local references.
		this.application = application;
		this.applicationGUI = application.applicationGUI;

		// Create all things this panel needs.
		createPanel();
	}

	/**
	 * Creates all things this panel needs.
	 */
	private void createPanel() {

		setLayout(new BorderLayout());

		JPanel guiPanel = new JPanel(new GridLayout(0, 1, 0, 10));
		guiPanel.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createRaisedBevelBorder(), BorderFactory.createEmptyBorder(10, 10, 10, 10)));

		/**
		 * Language settings
		 */
		JPanel languageSettings = new JPanel(new BorderLayout());
		languageSettings.setBorder(BorderFactory.createCompoundBorder(BorderFactory.createTitledBorder(Texts.getText("language")), BorderFactory.createEmptyBorder(10, 0, 0, 0)));

//		        jLabel1.setText(Texts.getText("language"));


		        final JComboBox comboBoxLanguages = new JComboBox(Translator.getAvailableLanguageNames().toArray());
		        comboBoxLanguages.addActionListener(new java.awt.event.ActionListener() {
		            @Override
					public void actionPerformed(java.awt.event.ActionEvent evt) {
		                String languageName = (String) comboBoxLanguages.getSelectedItem();
		                application.actionPerformed(new ActionEvent(GeneralSettingsView.this, 0, "language"+languageName));
		            }
		        });
		        languageSettings.add(comboBoxLanguages, BorderLayout.NORTH);

		        JButton translateButton = new JButton(Texts.getText("translate"));
		        translateButton.setActionCommand("translate");
		        translateButton.addActionListener(applicationGUI);
		        languageSettings.add(translateButton, BorderLayout.SOUTH);

//		        JButton applyChanges = new JButton("Apply");
		        languageSettings.add(translateButton, BorderLayout.EAST);
//		        jButton1.addActionListener(new java.awt.event.ActionListener() {
//		            public void actionPerformed(java.awt.event.ActionEvent evt) {
//		                jButton1ActionPerformed(evt);
//		            }
//		        });


		guiPanel.add(languageSettings);


		/**
		 * Game play settings
		 */
		JPanel playGameSettings = new JPanel(new GridLayout(0, 1));
		playGameSettings.setBorder(BorderFactory.createTitledBorder("Playgame"));
		playGameSettings.add(getCheckBox(Texts.getText("treat_reverse_moves_as_undo"), Settings.treatReverseMovesAsUndo, "reverseMoves", applicationGUI));
		playGameSettings.add(getCheckBox(Texts.getText("singlestepundo"), Settings.singleStepUndoRedo, "singleStepUndo", applicationGUI));
		playGameSettings.add(getCheckBox(Texts.getText("optimize_moves"), Settings.optimizeMovesBetweenPushes, "optimizeBetweenMoves", applicationGUI));
		playGameSettings.add(getCheckBox("show reachable player squares (Does not work)", false, "", applicationGUI));
		playGameSettings.add(getCheckBox("show reachable box squares  (Does not work)", false, "", applicationGUI));
		guiPanel.add(playGameSettings, BorderLayout.NORTH);

		/**
		 * Deadlock settings
		 */
		JPanel deadlockSettings = new JPanel(new GridLayout(0, 1));
		deadlockSettings.setBorder(BorderFactory.createTitledBorder(Texts.getText("deadlocks")));
		deadlockSettings.add(getCheckBox(Texts.getText("detectSimpleDeadlocks"),    Settings.detectSimpleDeadlocks,    "detectSimpleDeadlocks",    applicationGUI));
		deadlockSettings.add(getCheckBox(Texts.getText("detectFreezeDeadlocks"),    Settings.detectFreezeDeadlocks,    "detectFreezeDeadlocks",    applicationGUI));
		deadlockSettings.add(getCheckBox(Texts.getText("detectCorralDeadlocks"),    Settings.detectCorralDeadlocks,    "detectCorralDeadlocks",    applicationGUI));
		deadlockSettings.add(getCheckBox(Texts.getText("detectBipartiteDeadlocks"), Settings.detectBipartiteDeadlocks, "detectBipartiteDeadlocks", applicationGUI));
		guiPanel.add(deadlockSettings);

		add(guiPanel, BorderLayout.NORTH);

	}

	private JCheckBox getCheckBox(String text, boolean initialState, String actionCommand, ActionListener actionListener) {
		JCheckBox cb = new JCheckBox(text, initialState);
		cb.setActionCommand(actionCommand);
		if(actionListener != null) {
			cb.addActionListener(actionListener);
		}
		return cb;
	}
}