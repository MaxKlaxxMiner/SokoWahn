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
import java.awt.Dimension;
import java.awt.Font;
import java.awt.GridLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;

import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTabbedPane;
import javax.swing.JTextArea;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * Creates a dialog displaying information about this program.
 */
@SuppressWarnings("serial")
public class JSokoAboutBox extends JDialog implements ActionListener {

	/**
	 * Creates a JDialog for displaying the about information.
	 * 
	 * @param application Reference to the main object which holds all references
	 */
	public JSokoAboutBox(JSoko application) {

		// Set properties
		setTitle(Texts.getText("jSoko_about"));

		// Create tabbedPane for the topic "About", "Author" and "License"
		JTabbedPane tabbedPane = new JTabbedPane();

		// For a better layout in the panes this grid is used
		GridLayout gridLayout = new GridLayout();
		gridLayout.setRows(5);
		gridLayout.setColumns(2);

		// The two elements for the tabbed Pane
		JPanel authorPanel  = new JPanel();
		JPanel licensePanel = new JPanel();

		
		/*
		 * Create and add author panel
		 */
		JTextArea textArea = new JTextArea();
		textArea.setEditable(false);
		for (int textRow = 1; textRow<=19; textRow++) {
			String text = Texts.getText("authortab_" + textRow);
			textArea.append(text + "\n");
		}
		textArea.setCaretPosition(0);     // First line should be displayed at the top
		authorPanel.add(textArea);
		textArea.setOpaque(false);
		textArea.setFont(new Font("SansSerif", Font.BOLD, 12));
		textArea.setBorder(BorderFactory.createEmptyBorder());
		
		JScrollPane scrollPaneAuthor = new JScrollPane();
		scrollPaneAuthor.setWheelScrollingEnabled(true);
		scrollPaneAuthor.getViewport().add(authorPanel);
		tabbedPane.add(scrollPaneAuthor,  Texts.getText("authors"));
		

		/*
		 * Create and add license panel
		 */
		// Due to the size of the text a scroll pane is used
		JScrollPane scrollPane = new JScrollPane();
		scrollPane.setPreferredSize(new Dimension(565, 300));
		scrollPane.setWheelScrollingEnabled(true);

		licensePanel.setLayout(new BorderLayout());
		licensePanel.add(scrollPane);
		scrollPane.getViewport().add(getLicenseText());
		
		// Add license panel to tabbed pane		
		tabbedPane.add(licensePanel, Texts.getText("license"));

		
		
		// A title is displayed above the tabbed pane 
		JLabel textTitle = new JLabel(Texts.getText("theSokobanGame"));
		textTitle.setFont(new Font("Arial", 0, 18));
		textTitle.setHorizontalAlignment(0);

		JLabel textVersion = new JLabel("Version " + Settings.PROGRAM_VERSION);
		textVersion.setHorizontalAlignment(0);

		// Creation of a panel for the title
		JPanel titelPanel = new JPanel();
		titelPanel.add(textTitle);
		titelPanel.add(textVersion);
		titelPanel.setLayout(gridLayout);

		// Creation of a ok-button for closing the window and adding it to the main panel
		JButton okButton = new JButton("OK");
		okButton.addActionListener(this);
		okButton.setActionCommand("okbutton");
		JPanel jpanel4 = new JPanel();
		jpanel4.add(okButton);

		// Add the three elements to the dialog window.
		getContentPane().add(titelPanel, "North");
		getContentPane().add(tabbedPane, "Center");
		getContentPane().add(jpanel4,    "South");
		pack();

		// Set the AboutBox in front of the main panel.
		setLocation((int) application.getLocationOnScreen().getX(),
				   (int) application.getLocationOnScreen().getY());
		setSize(application.getWidth(), getHeight());

		// Set the JSoko icon.
		setIconImage(Utilities.getJSokoIcon());
		
		// Display the Information
		setVisible(true);
		toFront();
	}

	/**
	 * Method for closing the window after the ok-button has been clicked
	 * 
	 * @param actionevent the action event to be analyzed
	 */
	public void actionPerformed(ActionEvent actionevent) {
		if (actionevent.getActionCommand() == "okbutton") {
			dispose();
		}
	}

	/**
	 * Reads in the license text and returns it in a text area.
	 * 
	 * @return	Textarea containing the license text
	 */
	public final JTextArea getLicenseText() {

		JTextArea licenseText = new JTextArea("No licensetext found!");
		licenseText.setEditable(false);
		licenseText.setFont(new Font("Courier New", 0, 12));

		// Transfer of the license text from the file into the text area
		try {
			InputStream licenseFileInputStream = Utilities.getInputStream("/License JSoko.txt");
			BufferedReader licenseFile = new BufferedReader(new InputStreamReader(licenseFileInputStream, "UTF8"));
			
			String dataString;
			String wholeText = "";
			while ((dataString = licenseFile.readLine()) != null)
				wholeText = wholeText + "\n  " + dataString;

			licenseText.setText(wholeText);

			licenseFile.close();
		} catch (IOException ioexception) {
			System.out.println("IO Error:" + ioexception.getMessage());

			return licenseText;
		} catch (NullPointerException e) {
			return licenseText;
		}

		// Due to the insert of the text to the text area the cursor is located at the end
		// of the text.  As default the text should be displayed from the start.
		licenseText.setCaretPosition(0);

		return licenseText;
	}
}