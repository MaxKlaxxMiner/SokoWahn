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
package de.sokoban_online.jsoko.translator;

import java.awt.Color;
import java.awt.Dimension;
import java.awt.FileDialog;
import java.awt.Point;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.io.BufferedReader;
import java.io.File;
import java.io.FilenameFilter;
import java.io.IOException;
import java.io.InputStream;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.Date;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.IllegalFormatException;
import java.util.Locale;
import java.util.Properties;

import javax.swing.BoxLayout;
import javax.swing.DefaultComboBoxModel;
import javax.swing.GroupLayout;
import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JDialog;
import javax.swing.JFileChooser;
import javax.swing.JLabel;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextArea;
import javax.swing.ListSelectionModel;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;
import javax.swing.event.TableModelEvent;
import javax.swing.event.TableModelListener;
import javax.swing.filechooser.FileFilter;
import javax.swing.table.DefaultTableModel;
import javax.swing.table.TableModel;

import com.jidesoft.swing.SearchableUtils;

import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific.OSType;


/**
 * This class provides methods for the translation of language dependent texts.
 * Based on the property file for one language, the user can interactively
 * create or change the property file for another language.
 * <p>
 * All texts created or changed by the user are copied to the user preferences
 * directory (see class {@link OSSpecific}) in order not to change the default
 * text files of JSoko.<br>
 * JSoko first considers the user texts, if there is none, then the standard
 * text is loaded and displayed in JSoko.
 */
@SuppressWarnings("serial")
public final class Translator extends JPanel implements ActionListener {

	/** Constants for indexes of columns in the table. */
	final int KEY_NAME_COLUMN      = 0;
	final int NEW_LANGUAGE_COLUMN  = 1;
	final int HINT_LANGUAGE_COLUMN = 2;

	/** Path to the text files. */
	final static String path = "/texts/";

	/** Reference to the main object of this program. */
	private static JDialog parent;

	/** Default key value if no user-defined value was provided. */
	String nullKeyValue = Texts.getText("nullKeyValue");

	/** Abbreviation of currently translated language ("en", "de" etc.). */
	public String newLanguageCode = null;

	/** Properties loaded from .properties file of currently translated language. */
	private Properties newLanguageProperties = null;

	/** Abbreviation of currently translated language ("en", "de" etc.). */
	public String hintLanguageCode = null;

	/** Properties loaded from the project's model file. */
	public static Properties hintLanguageProperties = null;


	/** Stores values of currently translated language properties
	 *  (as in the moment when they have been saved last time).
	 */
	private Properties lastSavedNewLanguageProperties = null;

	/** Variable used to activate save warnings only after all controls
	 *  are created and configured.
	 */
	private boolean warningsEnabled = false;

	/** GUI components */
	private JCheckBox   adminMode;					// Only for internal use to edit the standard JSoko texts
	private JButton 	addKeyButton;				// Only for internal use to add a new key
    private JButton 	removeKeyButton;			// Only for internal use to remove a key
	private JLabel 		currentTextLabel;
    private JScrollPane currentTextScrollPane;
    private JTextArea 	currentTextTextArea;
    public	JComboBox<String> 	hintLanguageComboBox;
    private JLabel 		hintLanguageLabel;
    private JLabel		keyLabel;
    private JLabel 		keyTextLabel;
    public	JComboBox<String> 	newLanguageComboBox;
    private JLabel 		newLanguageLabel;
    private JButton 	restoreButton;
    private JButton 	saveButton;
    private JTable		table;
    private JScrollPane tableScrollPane;
    private JLabel 		translationLabel;
    private JScrollPane translationScrollPane;
    private JTextArea	translationTextArea;
    private JTextArea   exampleOutputText; // Example output of a text where %d and others are filled with example text
    private JScrollPane exampleOutputTextScrollPane;


	/** Handles for GUI components' listeners. */
	private TableModelListener translationTableModelListener = new TranslationTableModelListener();
	private ActionListener 	   newLanguageActionListener     = new NewLanguageActionListener();
	private ActionListener 	   hintLanguageActionListener    = new HintLanguageActionListener();

	//FFS/mm: make the table cells editable

	/**
	 * Creates the translator.
	 */
	public Translator(JDialog dialog) {
		parent = dialog;

		// Configure controls and layout.
        initComponents();

		// Change "Current text" text area's disabled text color for better readability.
		currentTextTextArea.setDisabledTextColor(Color.darkGray);

		// Implement JIDE searchable interface for table.
		SearchableUtils.installSearchable(table);

		// User can select only one row at a time.
		table.setSelectionMode(ListSelectionModel.SINGLE_SELECTION);

		// Make it possible to sort table by clicking on one of header columns.
		table.setAutoCreateRowSorter(true);

		// Add listener which reflects changes made in translation table also in 'newLanguageProperties'.
		table.getModel().addTableModelListener(translationTableModelListener);

		// Add listener which reflects changes in table's row selection in translation text area.
		ListSelectionModel tableSelectionModel = table.getSelectionModel();
		tableSelectionModel.addListSelectionListener(new TranslationTableListSelectionListener());

		// Add listener which reflects translation made in text area also in the table.
		translationTextArea.addKeyListener(new TranslationKeyListener());

		// As all controls have been created and configures, save warnings may now be enabled.
		warningsEnabled = true;

		// Initializes JSoko dependent variables and loads project's content.
		initContent();

		// Set help for the translator.
		Texts.helpBroker.enableHelpKey(parent.getRootPane(), "translator", null);
	}

    /**
	 * This method is called from within the constructor to initialize the form.
     */
    private void initComponents() {
		JPanel upperPanel = new JPanel();
		JPanel lowerPanel = new JPanel();

		table = new JTable();
        newLanguageLabel  	 = new JLabel();
        hintLanguageLabel    = new JLabel();
        newLanguageComboBox  = new JComboBox<String>();
        hintLanguageComboBox = new JComboBox<String>();
        tableScrollPane  	 = new JScrollPane();
        saveButton		 	 = new JButton();
        keyTextLabel 	 	 = new JLabel();
        currentTextLabel 	 = new JLabel();
        translationLabel 	 = new JLabel();
        keyLabel 		 	 = new JLabel();

        currentTextScrollPane 		= new JScrollPane();
        currentTextTextArea   		= new JTextArea();
        translationScrollPane 		= new JScrollPane();
        translationTextArea   		= new JTextArea();
        exampleOutputTextScrollPane = new JScrollPane();
        exampleOutputText 			= new JTextArea();

        addKeyButton    = new JButton();
        removeKeyButton = new JButton();
        restoreButton   = new JButton();

        setMinimumSize(new Dimension(800, 650));

        newLanguageLabel.setText(Texts.getText("newLanguage"));
        newLanguageLabel.setMaximumSize(new Dimension(250, 14));
        newLanguageLabel.setMinimumSize(new Dimension(250, 14));

        hintLanguageLabel.setText(Texts.getText("hintLanguage"));

        newLanguageComboBox.setEnabled(false);
        newLanguageComboBox.addActionListener(newLanguageActionListener);

        hintLanguageComboBox.setModel(new DefaultComboBoxModel<String>());
        hintLanguageComboBox.setEnabled(false);
        hintLanguageComboBox.addActionListener(hintLanguageActionListener);

        table.setModel(
        	new DefaultTableModel(
        		new Object [][] {
        		},
        		new String [] {
        			Texts.getText("textKey"),
        			Texts.getText("newText"),
        			Texts.getText("hintText")
        		}
        	) {
        		Class<?>[] types = new Class [] {
        			String.class,
        			String.class,
        			String.class
        		};
        		boolean[] canEdit = new boolean [] {
        			false, false, false
        		};

        		@Override
        		public Class<?> getColumnClass(int columnIndex) {
        			return types[columnIndex];
        		}

        		@Override
        		public boolean isCellEditable(int rowIndex, int columnIndex) {
        			return canEdit[columnIndex];
        		}
        	}
        );
        table.setMinimumSize(new Dimension(800, 0));
        table.getTableHeader().setReorderingAllowed(false);
        tableScrollPane.setViewportView(table);

        saveButton.setText(Texts.getText("saveComment"));
        saveButton.setEnabled(false);
        saveButton.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent evt) {
                saveButtonActionPerformed();
            }
        });

        keyTextLabel.setText(     Texts.getText("textKey" ) + ":" );
        currentTextLabel.setText( Texts.getText("hintText") + ":" );
        translationLabel.setText( Texts.getText("newText" ) + ":" );

        keyLabel.setText(Texts.getText("nullKeyText"));

        // Text field for showing example output of the currently selected text.
		JLabel exampleOutputLabel = new JLabel(Texts.getText("translator.example")+": ");
		exampleOutputText.setEditable(false);
		exampleOutputText.setEnabled(false);
		exampleOutputText.setRows(4);
		exampleOutputTextScrollPane.setViewportView(exampleOutputText);

		currentTextTextArea.setColumns(20);
        currentTextTextArea.setRows(5);
        currentTextTextArea.setEditable(false);
        currentTextScrollPane.setViewportView(currentTextTextArea);

        translationTextArea.setColumns(20);
        translationTextArea.setRows(5);
        translationTextArea.setEnabled(false);
        translationTextArea.addMouseListener(new MouseAdapter() {
			@Override
            public void mouseClicked(MouseEvent evt) {
                checkForDefaultText();
            }
        });
        translationTextArea.addKeyListener(new KeyAdapter() {
			@Override
			public void keyTyped(KeyEvent e) {
				checkForDefaultText();
			}
		});
        translationScrollPane.setViewportView(translationTextArea);

        // Button for adding a new text key.
        addKeyButton.setText(Texts.getText("addKey.buttonLabel"));
        addKeyButton.setEnabled(false);
        addKeyButton.addActionListener(new ActionListener() {
            @Override
			public void actionPerformed(ActionEvent evt) {
                addKeyButtonActionPerformed();
            }
        });

        // Button for removing a text key.
        removeKeyButton.setText(Texts.getText("removeKey.buttonLabel"));
        removeKeyButton.setDoubleBuffered(true);
        removeKeyButton.setEnabled(false);
        removeKeyButton.addActionListener(new ActionListener() {
            @Override
			public void actionPerformed(ActionEvent evt) {
                removeKeyButtonActionPerformed();
            }
        });

        // Reset button.
        restoreButton.setText(Texts.getText("restore"));
        restoreButton.setEnabled(false);
        restoreButton.addActionListener(new ActionListener() {
            @Override
			public void actionPerformed(ActionEvent evt) {
                restoreButtonActionPerformed(evt);
            }
        });


		final int GAP_MIN  = 10;
		final int GAP_PREF = 60;
		final int GAP_MAX  = GAP_PREF;

		// Create upper panel.
		GroupLayout upperPanelLayout = new GroupLayout(upperPanel);
		upperPanel.setLayout(upperPanelLayout);

		upperPanelLayout.setAutoCreateGaps(true);
		upperPanelLayout.setAutoCreateContainerGaps(true);

		upperPanelLayout.setHorizontalGroup(
			upperPanelLayout.createSequentialGroup()
				.addGroup(upperPanelLayout.createParallelGroup()
					.addComponent(newLanguageLabel, GroupLayout.PREFERRED_SIZE, GroupLayout.PREFERRED_SIZE, GroupLayout.PREFERRED_SIZE)
					.addComponent(hintLanguageLabel, GroupLayout.PREFERRED_SIZE, GroupLayout.PREFERRED_SIZE, GroupLayout.PREFERRED_SIZE))
				.addGroup(upperPanelLayout.createParallelGroup()
					.addComponent(newLanguageComboBox)
					.addComponent(hintLanguageComboBox))
				.addGap(GAP_MIN, GAP_PREF, GAP_MAX)
				.addGroup(upperPanelLayout.createParallelGroup()
					.addComponent(saveButton)
					.addComponent(restoreButton))
				.addGap(GAP_MIN, GAP_PREF, GAP_MAX)
				.addGroup(upperPanelLayout.createParallelGroup()
					.addComponent(addKeyButton)
					.addComponent(removeKeyButton))
		);

		upperPanelLayout.setVerticalGroup(
			upperPanelLayout.createSequentialGroup()
				.addGroup(upperPanelLayout.createBaselineGroup(true, false)
					.addComponent(newLanguageLabel)
					.addComponent(newLanguageComboBox)
					.addComponent(saveButton)
					.addComponent(addKeyButton))
				.addGroup(upperPanelLayout.createBaselineGroup(true, false)
					.addComponent(hintLanguageLabel)
					.addComponent(hintLanguageComboBox)
					.addComponent(restoreButton)
					.addComponent(removeKeyButton))
		);

		upperPanelLayout.linkSize(saveButton, restoreButton);
		upperPanelLayout.linkSize(addKeyButton, removeKeyButton);


		// Create lower panel.
		GroupLayout lowerPanelLayout = new GroupLayout(lowerPanel);
		lowerPanel.setLayout(lowerPanelLayout);

		lowerPanelLayout.setAutoCreateGaps(true);
		lowerPanelLayout.setAutoCreateContainerGaps(true);

		lowerPanelLayout.setHorizontalGroup(
			lowerPanelLayout.createSequentialGroup()
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(keyTextLabel)
					.addComponent(currentTextLabel)
					.addComponent(translationLabel)
					.addComponent(exampleOutputLabel))
				.addGap(GAP_MIN)
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(keyLabel)
					.addComponent(currentTextScrollPane)
					.addComponent(translationScrollPane)
					.addComponent(exampleOutputTextScrollPane))
		);

		lowerPanelLayout.setVerticalGroup(
			lowerPanelLayout.createSequentialGroup()
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(keyTextLabel)
					.addComponent(keyLabel))
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(currentTextLabel)
					.addComponent(currentTextScrollPane))
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(translationLabel)
					.addComponent(translationScrollPane))
				.addGroup(lowerPanelLayout.createParallelGroup()
					.addComponent(exampleOutputLabel)
					.addComponent(exampleOutputTextScrollPane))
		);

		// Create main panel (this).
		BoxLayout mainPanelLayout = new BoxLayout(this, BoxLayout.Y_AXIS);
		setLayout(mainPanelLayout);

		// If selected the whole text file is saved in the user directory instead of just
		// the changes compared to the standard text file. The created file is intended to
		// be used as new standard JSoko text file.
		adminMode = new JCheckBox("Admin mode: save whole texts to file");

		adminMode.addActionListener(new ActionListener() {
			@Override
			public void actionPerformed(ActionEvent e) {
				initContent();
			}
		});
		add(adminMode);

		if(!Debug.isDebugModeActivated) {
			adminMode.setVisible(false);
		}
		add(upperPanel);
		add(tableScrollPane);
		add(lowerPanel);
    }

    /**
	 * Update "Current text" column.
	 *
	 * @param evt fired <code>ActionEvent</code>
	 */
	private void hintLanguageComboBoxActionPerformed(ActionEvent evt) {

		// Change of 'displayed' language should be reflected in "Current text" column.
		loadLanguageTexts(HINT_LANGUAGE_COLUMN);

		// Refresh content of translation corner.
		refreshTranslationTextAreas();
	}

	/**
	 * Handle save button action.
	 */
	private void saveButtonActionPerformed() {

		// Save properties to a file.
		saveProperties(newLanguageProperties, getFilepathFromLanguageCode(newLanguageCode));

		// Make new backup copy of properties.
		lastSavedNewLanguageProperties = (Properties) newLanguageProperties.clone();
	}

	/**
	 * Internal use only - not for users!
	 * Handle "add key" button action.
	 */
	private void addKeyButtonActionPerformed() {

		// Ask user to save or discard recent changes (variable which stores current
		// translation will be reloaded and its previous content will be lost).
		showSaveDialog();

		// Ask user to provide name of new key.
		String newKeyName = JOptionPane.showInputDialog(
                    this,
                    Texts.getText("addKey.message"),
                    Texts.getText("addKey.title"),
                    JOptionPane.PLAIN_MESSAGE);

		// If no name was typed or such a key already exists do nothing.
		if (newKeyName == null || newKeyName.equals("") || hintLanguageProperties.contains(newKeyName)) {
			return;
		}

		// Add appropriate new property key to all projects' locale files.
		for (String languageFilename : getAvailableLanguageFiles()) {
			Properties localProps = null;
			try {
				localProps = loadPropertiesByFilename(path + languageFilename);
				localProps.put(newKeyName, nullKeyValue);
				saveProperties(localProps, path + languageFilename);
			} catch (IOException ex) {
				showMissingFileDialog(languageFilename, JOptionPane.ERROR_MESSAGE);
			}
		}

		// As a new key was added, it is necessary to refresh table content.
		loadKeyTexts();
		loadLanguageTexts(NEW_LANGUAGE_COLUMN);
		loadLanguageTexts(HINT_LANGUAGE_COLUMN);

		// Refresh content of translation corner.
		refreshTranslationTextAreas();

		// As a new key was added it surely may be removed.
		removeKeyButton.setEnabled(true);

		// Search for row with new key and select it.
		for (int i = 0; i < table.getRowCount(); i++) {
			if (table.getValueAt(i, KEY_NAME_COLUMN).equals(newKeyName)) {
				table.changeSelection(i, NEW_LANGUAGE_COLUMN, false, false);
				break;
			}
		}

		// Make new backup copy of properties.
		if (newLanguageProperties != null) {
			lastSavedNewLanguageProperties = (Properties) newLanguageProperties.clone();
		}
	}

	/**
	 * Internal use only - not for users!
	 * Handles "remove key" actions.
	 */
	private void removeKeyButtonActionPerformed() {

		// Make sure user really wants to remove key.
		int n = JOptionPane.showConfirmDialog(
				this,
				Texts.getText("removeKey.message", table.getValueAt(table.getSelectedRow(), KEY_NAME_COLUMN)),
				Texts.getText("removeKey.title"),
				JOptionPane.YES_NO_OPTION,
				JOptionPane.WARNING_MESSAGE);

		// Remove key from all locales.
		if (n == JOptionPane.YES_OPTION) {
			// Ask user to save or discard recent changes (variable which stores current
			// translation will be reloaded and its previous content will be lost).
			showSaveDialog();

			Object keyToRemove = table.getValueAt(table.getSelectedRow(), KEY_NAME_COLUMN);

			for (String languageFilename : getAvailableLanguageFiles()) {
				Properties localProps = null;
				try {
					localProps = loadPropertiesByFilename(path + languageFilename);
					localProps.remove(keyToRemove);
					saveProperties(localProps, path + languageFilename);
				} catch (IOException ex) {
					showMissingFileDialog(languageFilename, JOptionPane.ERROR_MESSAGE);
				}
			}
		}

		// As a key was removed, it is necessary to refresh table content.
		loadKeyTexts();
		loadLanguageTexts(NEW_LANGUAGE_COLUMN);
		loadLanguageTexts(HINT_LANGUAGE_COLUMN);

		// Refresh content of translation text areas.
		refreshTranslationTextAreas();

		// It is impossible to delete a key if no keys are left.
		if (hintLanguageProperties.isEmpty()) {
			removeKeyButton.setEnabled(false);
		}

		// Make new backup copy of properties.
		lastSavedNewLanguageProperties = (Properties) newLanguageProperties.clone();
	}

	/**
	 * Restores the last saved properties.
	 *
	 * @param evt
	 */
	private void restoreButtonActionPerformed(ActionEvent evt) {
		newLanguageProperties = (Properties) lastSavedNewLanguageProperties.clone();

		loadLanguageTexts(NEW_LANGUAGE_COLUMN);
		refreshTranslationTextAreas();
	}

	private void checkForDefaultText() {

		// If key value is blank and user clicked in translation text area, clear this value
		// so that it's not necessary to select it and delete manually.
		if (translationTextArea.getText().equals(nullKeyValue)) {
			translationTextArea.setText("");
		}
	}

	private void newLanguageComboBoxActionPerformed(ActionEvent evt) {

		// Ask user to save or discard recent changes (variable which stores current
		// translation will be reloaded and its previous content will be lost).
		showSaveDialog();

		// Change of 'displayed' language should be reflected in "Current text" column.
		loadLanguageTexts(NEW_LANGUAGE_COLUMN);

		// Refresh content of translation corner.
		refreshTranslationTextAreas();
	}

	/**
	 * Returns path to the file containing translation for the given language.
	 *
	 * @param code language code of the searched file
	 * @return path to the file containing translation for the given language
	 */
	public static String getFilepathFromLanguageCode(String code) {

		return path + "texts_" + code + ".properties";
	}

	/**
	 * Returns the list of all project-available language codes
	 * (in lexicographical order).
	 *
	 * @return list of all project-available language codes (in lexicographical order)
	 */
	public static ArrayList<String> getAvailableLanguageCodes() {

		ArrayList<String> languages = new ArrayList<String>();

		// Make list of all supported languages.
		for (String element : getAvailableLanguageFiles()) {
			int beg = element.lastIndexOf('_');
			int end = element.lastIndexOf('.');
			if (beg != -1) {
				String lang = element.substring(beg + 1, end);
				languages.add(lang);
			}
		}

		Collections.sort(languages);

		return languages;
	}

	/**
	 * Returns list of all project-available language names (in lexicographical order)
	 * translated to the currently set user language.
	 *
	 * @return list of all project-available language names (in lexicographical order)
	 */
	public static ArrayList<String> getAvailableLanguageNames() {

		ArrayList<String> languageData = getAvailableLanguageCodes();
		ArrayList<String> translatedData = new ArrayList<String>();

		// Get the locale of the currently set language in the program.
		Locale currentLanguageLocal = getUserLocale();

		// Make list of all supported language names.
		for (String language : languageData) {
			translatedData.add(new Locale(language).getDisplayLanguage(currentLanguageLocal));
		}

		Collections.sort(translatedData);

		return translatedData;
	}

	/**
	 * Adds appropriate items to newLanguage- and hintLanguageComboBox.
	 */
	public void changeLanguageComboBoxes() {

		ArrayList<String> languageNames = getAvailableLanguageNames();

		// Clear lists.
		newLanguageComboBox.removeActionListener(newLanguageActionListener);
		newLanguageComboBox.removeAllItems();

		hintLanguageComboBox.removeActionListener(hintLanguageActionListener);
		hintLanguageComboBox.removeAllItems();

		// Add supported languages to combo boxes.
		for (String languageName : languageNames) {
			newLanguageComboBox.addItem(languageName);
			hintLanguageComboBox.addItem(languageName);
		}

		newLanguageComboBox.addActionListener(newLanguageActionListener);
		hintLanguageComboBox.addActionListener(hintLanguageActionListener);
	}

	/**
	 * Clears the content of the whole table and fills in "Text key" column.
	 */
	private void loadKeyTexts() {

		table.getModel().removeTableModelListener(translationTableModelListener);

		// Reload default properties.
		try {
			hintLanguageProperties = loadPropertiesByLanguageCode(hintLanguageCode);
		} catch (IOException ex) {
			showErrorMessage(Texts.getText("message.fileMissing", getFilepathFromLanguageCode(hintLanguageCode)), ex);
		}

		// Get current table model.
		DefaultTableModel model = (DefaultTableModel) table.getModel();

		// Clear previous data.
		while (model.getRowCount() > 0) {
			model.removeRow(0);
		}

		// Create list of all keys.
		Enumeration<Object> keys = hintLanguageProperties.keys();
		while(keys.hasMoreElements()) {
			Object[] rowData = {keys.nextElement(), null, null};
			model.addRow(rowData);
		}

		// Set new table model.
		table.setModel(model);

		table.getModel().addTableModelListener(translationTableModelListener);
	}

	/**
	 * Fills in chosen column with translations from appropriate locale.
	 *
	 * @param column	index of the column to be filled
	 */
	private void loadLanguageTexts(int column) {

		warningsEnabled = false;

		String languageName = null;
		Properties properties = null;

		if (column == NEW_LANGUAGE_COLUMN && newLanguageComboBox.getItemCount() > 0) {
			// Get selected language name.
			languageName = newLanguageComboBox.getSelectedItem().toString();
			newLanguageCode = getLanguageCode(languageName);
		}
		if (column == HINT_LANGUAGE_COLUMN && hintLanguageComboBox.getItemCount() > 0) {
			// Get selected language name.
			languageName = hintLanguageComboBox.getSelectedItem().toString();
		}

		// Load properties for selected language.
		try {
			properties = loadPropertiesByLanguageCode(getLanguageCode(languageName));
		} catch (IOException ex) {
			showErrorMessage(Texts.getText("message.fileMissing", getFilepathFromLanguageCode(getLanguageCode(languageName))), ex);
			return;
		}

		if (column == NEW_LANGUAGE_COLUMN) {
			newLanguageProperties = properties;
			if (newLanguageProperties != null) {
				lastSavedNewLanguageProperties = (Properties) newLanguageProperties.clone();
			}
		}

		// Fill in the table.
		table.getModel().removeTableModelListener(translationTableModelListener);

		for (int i = 0; i < table.getRowCount(); i++) {
			// Get value corresponding with the key.
			String data = properties.getProperty((String) table.getValueAt(i, KEY_NAME_COLUMN));

			// If no value for the specified key was provided use blank one.
			if (data == null || data.equals("")) {
				data = nullKeyValue;
			}

			// Set date in appropriate table's cell.
			table.setValueAt(data, i, column);
		}

		table.getModel().addTableModelListener(translationTableModelListener);
	}


	/**
	 * Returns ISO 639 language code for given language.
	 *
	 * @param languageNameToSearch	language name (in user language)
	 * @return	ISO 639 language code of <code>language</code>
	 */
	public static String getLanguageCode(String languageNameToSearch) {

		// Get the locale of the currently set language in the program.
		Locale currentLanguageLocal = getUserLocale();

		// Fill in list of system-supported language names.
		for (String languageCode : Locale.getISOLanguages()) {
			Locale locale = new Locale(languageCode);
			String languageNameOfLocal = locale.getDisplayLanguage(currentLanguageLocal);

			// Search code of passed language name and return it.
			if (languageNameOfLocal.equals(languageNameToSearch)) {
				return languageCode;
			}
		}

		return "";
	}

	/**
	 * Shows confirmation dialog and saves changes made to translation.
	 *
	 * @return	index of chosen option, -1 if warning dialogs are suppressed
	 */
	public int showSaveDialog() {

		// If save dialogs are disabled don't show them.
		if (warningsEnabled == false) {
			return -1;
		}

		// If no changes were applied there is no need to ask to save changes.
		if (       lastSavedNewLanguageProperties == null
				|| lastSavedNewLanguageProperties.equals(newLanguageProperties)) {
			return -1;
		}

		int n = JOptionPane.showConfirmDialog(
				this,
				Texts.getText("save.message"),
				Texts.getText("saveComment"),
				JOptionPane.YES_NO_OPTION,
				JOptionPane.WARNING_MESSAGE);

		if (n == JOptionPane.YES_OPTION) {
			saveProperties(newLanguageProperties, getFilepathFromLanguageCode(newLanguageCode));
		}

		// Make new backup copy of properties.
		lastSavedNewLanguageProperties = (Properties) newLanguageProperties.clone();

		return n;
	}

	/**
	 * Refreshes data in the "translation corner".
	 */
	private void refreshTranslationTextAreas() {

		// If no row was selected use blank values.
		// Otherwise fill in translation corner with appropriate values.
		if (table.getSelectedRow() == -1) {
			keyLabel.setText(Texts.getText("nullKeyValue"));
			currentTextTextArea.setText("");
			translationTextArea.setText("");
			exampleOutputText.setText("");
		} else {
			int carretPosition = translationTextArea.getCaretPosition();

			keyLabel.setText((String) table.getValueAt(table.getSelectedRow(), KEY_NAME_COLUMN));
			currentTextTextArea.setText((String) table.getValueAt(table.getSelectedRow(), HINT_LANGUAGE_COLUMN));
			translationTextArea.setText((String) table.getValueAt(table.getSelectedRow(), NEW_LANGUAGE_COLUMN));

			if (translationTextArea.getText().length() > carretPosition) {
				translationTextArea.setCaretPosition(carretPosition);
			}

			// Replace all place holders with a example value.
			String text = currentTextTextArea.getText();
			text = Texts.substEmbeddedKeys(keyLabel.getText(), text); // Replaces all embedded text keys
			if(text != null) {
				String[] textSplitted = text.split("%");
				String newText = null;
				for(String s : textSplitted) {
					if(newText == null) {
						newText = s;
						continue;
					}

					s = "%" + s;
					try {
						s = String.format(s, "TestText");
					}catch(IllegalFormatException ex) {}
					try {
						s = String.format(s, 10);
					}catch(IllegalFormatException ex) {}
					try {
						s = String.format(s, 20f);
					}catch(IllegalFormatException ex) {}

					newText += s;
				}
				exampleOutputText.setText(newText);
			} else {
				exampleOutputText.setText("");
			}
		}
	}

	/**
	 * Creates menu bar for main application.
	 *
	 * @return	created menu bar
	 */
	public JMenuBar createMenuBar() {

		JMenuBar menuBar = new JMenuBar();
		JMenu menu;
		JMenuItem menuItem;

		menu = new JMenu(Texts.getText("filesMenu"));
		menu.setMnemonic(KeyEvent.VK_F);
		menuBar.add(menu);

		menuItem = new JMenuItem(Texts.getText("menu.addFile"));
		menuItem.setActionCommand("addFile");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		menuItem = new JMenuItem(Texts.getText("translator.menu.export"));
		menuItem.setActionCommand("exportFile");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		//
		// "Options" menu
		//
		menu = new JMenu(Texts.getText("optionsMenu"));
		menu.setMnemonic(KeyEvent.VK_O);
		menuBar.add(menu);

		menuItem = new JMenuItem(Texts.getText("menu.languages"));
		menuItem.setActionCommand("languages");
		menuItem.addActionListener(this);
		menu.add(menuItem);

		return menuBar;
	}

	/**
	 * Handles events fired by clicking on menu items.
	 *
	 * @param e	event
	 */
	@Override
	public void actionPerformed(ActionEvent e) {
		final String action = e.getActionCommand();

		if (action.equals("addFile")) {
			//
			// Build a list of available languages that have not been yet translated.
			//
			String[] localeList = Locale.getISOLanguages();
			ArrayList<String> newLanguages = new ArrayList<String>();
			ArrayList<String> fileList = getAvailableLanguageNames();

			/* Create list of all language names (in English) which has not been
			 * translated yet.
			 */
			for (String element : localeList) {
				Locale locale = new Locale(element);
				String localeName = locale.getDisplayLanguage(getUserLocale());

				if (!fileList.contains(localeName)) {
					newLanguages.add(localeName);
				}
			}

			// Copy list from ArrayList to String[] and sort it so that it can be used
			// to create option pane.
			String[] languageNames = {""};
			languageNames = newLanguages.toArray(languageNames);
			Arrays.sort(languageNames);

			// Show list of available new languages to choose from.
			String newLanguageName = (String)JOptionPane.showInputDialog(
					this,
					Texts.getText("selectNewLanguage"),
					Texts.getText("menu.addFile"),
					JOptionPane.PLAIN_MESSAGE,
					null,
					languageNames,
					null
					);

			if (newLanguageName == null) {
				return;
			}

			// Find abbreviation of selected language.
			String addedLanguageCode = getLanguageCode(newLanguageName);

			// Create .properties file for new language.
			Properties localProps = (Properties) hintLanguageProperties.clone();

			Enumeration<Object> keys = localProps.keys();
			while(keys.hasMoreElements()) {
				localProps.setProperty((String) keys.nextElement(), "");
			}

			saveProperties(localProps, getFilepathFromLanguageCode(addedLanguageCode));

			reloadGUI();
			return;
		}

		// Export the current language texts to an external file to be chosen by the user.
		if(action.equals("exportFile")) {
			String fileToSave = getFileChooserForSaving(getFilepathFromLanguageCode(newLanguageCode));
			if(fileToSave != null) {
				saveTexts(newLanguageProperties, fileToSave);
			}
			return;
		}

		if (action.equals("languages")) {

			//
			// Create dialog for settings' controls.
			//
			JDialog settingsDialog = new JDialog(parent, Texts.getText("languageSettings.title"));
			settingsDialog.setModal(true);
			settingsDialog.setResizable(false);
			Utilities.setEscapable(settingsDialog);

			// Set dialog location more or less in the middle of parent's window.
			Point parentLocation = parent.getLocation();
			settingsDialog.setLocation(parentLocation.x + 300, parentLocation.y + 200);

			TranslatorLanguages ls = new TranslatorLanguages(settingsDialog);
			ls.setVisible(true);
			settingsDialog.add(ls);

			settingsDialog.pack();
			settingsDialog.setVisible(true);

			return;
		}
	}

	/**
	 * Displays a <code>JFileChooser</code> dialog for letting the user
	 * choose a file and returns its name.
	 *
	 * @param fileName  default name of the file
	 * @return name and location of the chosen file
	 */
	private String getFileChooserForSaving(String fileName) {

		// When the program runs in Mac OS the FileDialog looks better.
		if(OSType.isMac) {
			return getFileChooserForSavingMacOS(fileName);
		}

		// Create JFileCooser.
		JFileChooser fc = new JFileChooser();
		fc.setDialogType(JFileChooser.SAVE_DIALOG);
		fc.setSelectedFile(new File(fileName));
		Dimension d = new Dimension(700, 400);
		fc.setMinimumSize(d);
		fc.setPreferredSize(d);

		//	Filter files for property files.
		fc.setFileFilter(new FileFilter() {
			@Override
			public boolean accept(File f) {
				final String lowerCaseName = f.getName().toLowerCase();
				return lowerCaseName.endsWith(".properties")
						|| f.isDirectory();
			}

			@Override
			public String getDescription() {
				return Texts.getText("menu.languages");
			}
		});

		// If the JFileChooser has been canceled or an error occurred return immediately.
		if (fc.showSaveDialog(this) != JFileChooser.APPROVE_OPTION) {
			return null;
		}

		// Get the chosen file name.
		String filePath = fc.getCurrentDirectory() + System.getProperty("file.separator") + fc.getSelectedFile().getName();

		// Add ".properties" if the file doesn't have a file extension, yet.
		if (filePath.lastIndexOf('.') == -1) {
			filePath += ".properties";
		}

		return filePath;
	}


	/**
	 * Displays a {@code FileDialog} for letting the user choose a file and returns its name.
	 * <p>
	 * The passed path to the file is overwritten with the path of the new chosen file.
	 *
	 * @param fileName path to the directory the user may choose the file from
	 * @return name and location of the chosen file
	 */
	private String getFileChooserForSavingMacOS(String fileName) {

		FileDialog fileDialog = new FileDialog(parent, "", FileDialog.SAVE);
		fileDialog.setFile(fileName);
		fileDialog.setFilenameFilter(new FilenameFilter(){
			@Override
			public boolean accept(File dir, String name){
				return (name.endsWith(".properties"));
			}
		});
		fileDialog.setVisible(true);

		String filename = fileDialog.getFile();
		if (filename != null) {

			filename = fileDialog.getDirectory() + System.getProperty("file.separator") + filename;

			// Add ".properties" if the file doesn't have a file extension, yet.
			if (filename.lastIndexOf('.') == -1) {
				filename += ".properties";
			}
		}
		fileDialog.dispose();

		return filename;
	}


	/**
	 * Initializes project-dependent variables and loads project's content.
	 * <p>
	 * This method copies all default JSoko text files to the user preferences directory.
	 * The JSoko text files aren't changed.
	 */
	private void initContent() {
		warningsEnabled = false;

		// Load default properties.
		hintLanguageCode = Settings.getString("hintTranslationLanguage", "en");
		try {
			hintLanguageProperties = loadPropertiesByLanguageCode(hintLanguageCode);
		} catch (IOException ex) {
			showErrorMessage(Texts.getText("message.fileMissing",  getFilepathFromLanguageCode(hintLanguageCode)), ex);
		}

		// Load new language properties.
		newLanguageCode = Settings.getString("newTranslationLanguage", "en");
		newLanguageProperties = new Properties();
		try {
			newLanguageProperties = loadPropertiesByLanguageCode(newLanguageCode);
		} catch (IOException ex) {
			showErrorMessage(Texts.getText("message.fileMissing", getFilepathFromLanguageCode(newLanguageCode)), ex);
		}

		if(adminMode.isSelected()) {
			// Check for key's cohesion.
			Properties properties = new Properties();

			// Check the language keys for all available text files.
			for (String languageFile : Translator.getAvailableLanguageFiles()) {

				try {
					properties = loadPropertiesByFilename(path + languageFile);
				} catch (IOException ex) {
					showErrorMessage(Texts.getText("message.fileMissing", path + languageFile), ex);
				}

				// Add keys that are present in the English text file but not in the current text file.
				for (Enumeration<Object> en = Translator.hintLanguageProperties.keys(); en.hasMoreElements(); ) {
					String key = (String) en.nextElement();
					if (!properties.containsKey(key)) {
						properties.setProperty(key, "");
					}
				}

				for (Enumeration<Object> en = properties.keys(); en.hasMoreElements(); ) {
					String key = (String) en.nextElement();
					if (!Translator.hintLanguageProperties.containsKey(key)) {
						properties.remove(key);
					}
				}

				// Save the texts in JSoko texts directory.
				saveProperties(properties, path +languageFile);
			}
		}

		loadKeyTexts();
		reloadGUI();

		// As a project was loaded it is now possible to perform most operations.
		newLanguageComboBox.setEnabled(true);
		hintLanguageComboBox.setEnabled(true);
		addKeyButton.setEnabled(true);
		saveButton.setEnabled(true);
		restoreButton.setEnabled(true);
		translationTextArea.setEnabled(true);

		// Only the programmer should add or remove keys.
		if(Debug.isDebugModeActivated == false) {
			adminMode.setVisible(false);
		}

		// Only visible in admin mode.
		addKeyButton.setVisible(adminMode.isSelected());
		removeKeyButton.setVisible(adminMode.isSelected());

		warningsEnabled = true;
	}

	/**
	 * Shows dialog which informs about missing file.
	 * <p>
	 * If error is critical (it is when <code>messageType</code> equals
	 * <code>JOptionPane.ERROR_MESSAGE</code> then application is closed.
	 *
	 * @param filePath	path of the missing file
	 * @param messageType	<code>JOptionPane.WARNING_MESSAGE</code> if missing file is non-critical;
	 *						<code>JOptionPane.ERROR_MESSAGE</code> if missing file is critical
	 */
	public void showMissingFileDialog(String filePath, int messageType) {
		JOptionPane.showMessageDialog(
				parent,
				"File: " + filePath + " is missing!",
				"Error",
				messageType);

		if (messageType == JOptionPane.ERROR_MESSAGE) {
			setBlankProject();
		}
	}

	/**
	 * Returns locale based on <code>userLanguage</code> setting.
	 *
	 * @return	locale based on <code>userLanguage</code> setting
	 */
	public static Locale getUserLocale() {
		return new Locale(Settings.get("currentLanguage"));
	}

	/**
	 * Sets all GUI elements and variables to default values
	 * (as they are right after the program is started).
	 */
	private void setBlankProject() {
		newLanguageCode = null;
		hintLanguageProperties = null;
		newLanguageProperties = null;
		lastSavedNewLanguageProperties = null;
		warningsEnabled = false;

		newLanguageComboBox.removeActionListener(newLanguageActionListener);
		newLanguageComboBox.removeAllItems();

		hintLanguageComboBox.removeActionListener(hintLanguageActionListener);
		hintLanguageComboBox.removeAllItems();

		saveButton.setEnabled(false);
		restoreButton.setEnabled(false);
		addKeyButton.setEnabled(false);
		removeKeyButton.setEnabled(false);


		DefaultTableModel model = (DefaultTableModel) table.getModel();

		model.removeTableModelListener(translationTableModelListener);

		// Clear previous data.
		while (model.getRowCount() > 0) {
			model.removeRow(0);
		}

		// Set new table model.
		table.setModel(model);
		table.removeAll();


		keyLabel.setText(Texts.getText("nullKeyText"));
		currentTextTextArea.setText("");
		translationTextArea.setText("");
		translationTextArea.setEnabled(false);

		// Disable include / exclude files options in the menu.
		JMenu projectMenu = parent.getJMenuBar().getMenu(0);
		for (int i = 0; i < projectMenu.getItemCount(); i++) {
			final JMenuItem menuItem = projectMenu.getItem(i);
			if (menuItem != null
				&& (   menuItem.getActionCommand().equals("includeFiles")
					|| menuItem.getActionCommand().equals("excludeFiles")) ) {
				menuItem.setEnabled(false);
			}
		}
	}


	/**
	 * Load properties from a file.
	 * <p>
	 * The JSoko standard texts are loaded first. Then the texts set by the user
	 * are loaded replacing the standard texts where their keys are the same.<br>
	 *
	 * @param fileName	name of the file
	 * @return	<code>Properties</code> variable containing loaded properties
	 */
	private Properties loadPropertiesByFilename(String fileName) throws IOException {

		// Create new properties.
		Properties texts = new Properties();

		// Load the JSoko standard texts.
		InputStream standardTextsSteam = Utilities.getInputStream(fileName);
		if(standardTextsSteam != null) {
			try {
				texts.load(standardTextsSteam);
			}
			catch(Exception e) {
				throw new IOException(e.getLocalizedMessage());
			} finally {
				standardTextsSteam.close();
			}
		}

		// Load the user texts.
		InputStream userDefinedTexts = Utilities.getInputStream(OSSpecific.getPreferencesDirectory()+fileName);
		if(userDefinedTexts != null) {
			try {
				// Add the user defined texts and thereby replace standard texts with user texts having the same key.
				texts.load(userDefinedTexts);
			}
			catch(Exception e) {
				throw new IOException(e.getLocalizedMessage());
			} finally {
				userDefinedTexts.close();
			}
		}

		// Return the read properties.
		return texts;
	}

	/**
	 * Load the properties corresponding to the passed language code.
	 *
	 * @param languageCode code of the language (example: "en")
	 * @return	<code>Properties</code> variable containing loaded properties
	 */
	private Properties loadPropertiesByLanguageCode(String languageCode) throws IOException {

		// Return the read properties.
		return loadPropertiesByFilename(getFilepathFromLanguageCode(languageCode));

	}

	/**
	 * Save properties to a file.
	 *
	 * @param p	properties to be saved
	 * @param filePath	path of the file
	 */
	private void saveProperties(Properties p, String filePath) {
		if (p == null) {
			return;
		}

		// In admin mode the complete texts are stored in the user folder
		// instead just the differences compared to the standard texts.
		if(adminMode.isSelected()) {
			saveTexts(p, OSSpecific.getPreferencesDirectory()+filePath);
			return;
		}

		// If no standard text file exists then the user edits a new language file.
		// In this case the texts are stored but in the user directory.
		if(!new File(Utilities.getBaseFolder()+filePath).exists()) {
			saveTexts(p, OSSpecific.getPreferencesDirectory()+filePath);
			return;
		}

		// The user has edited texts for which an standard JSoko text file exists.
		// In this case only the texts that differ from the standard texts are stored.
		saveAdditionalUserTexts(p, filePath);
	}

	/**
	 * Save the user texts to a file.
	 * <p>
	 * This method stores all text files in the user preferences directory.
	 *
	 * @param userTexts	the user texts as {@code Properties} to be saved
	 * @param filePath	path of the file
	 */
	private void saveAdditionalUserTexts(Properties userTexts, String filePath) {

		String userTextFilePath = OSSpecific.getPreferencesDirectory() + filePath;

		// Load the standard texts to compare them with the
		Properties standardTexts = new Properties();
		try {
			standardTexts.load(Utilities.getBufferedReader(filePath));
		} catch (IOException ex) {
			showErrorMessage(Texts.getText("errorBySaving"), ex);
			return;
		}

		ArrayList<String> userTextFileContent = new ArrayList<String>();

		// Compare all properties and add those key/texts to the file content that are different.
		Enumeration<Object> userKeys = userTexts.keys();
		while(userKeys.hasMoreElements()) {
			String userKey = (String) userKeys.nextElement();
			String userText = userTexts.getProperty(userKey, "");
			if(userText.isEmpty()) {
				continue;
			}

			String standardText = standardTexts.getProperty(userKey);
			if(!userText.equals(standardText)) {
				userTextFileContent.add(userKey + " = " + Settings.mask(userText));
			}

		}

		//  Create the new text file and store all content.
		PrintWriter userTextsFile = null;
		try {
			if(!userTextFileContent.isEmpty()) {
				userTextsFile = new PrintWriter(userTextFilePath + ".tmp", "UTF8");
				for(String data : userTextFileContent) {
					userTextsFile.println(data);
				}
			}

			// Delete the original user text file.
			new File(userTextFilePath).delete();

			// Rename the new user text file.
			if(!userTextFileContent.isEmpty()) {
				new File(userTextFilePath + ".tmp").renameTo(new File(userTextFilePath));
			}

		} catch (IOException ex) {
			showErrorMessage(Texts.getText("errorBySaving"), ex);
			return;
		} finally {
			if(userTextsFile != null) {
				userTextsFile.close();
			}
		}

	}

	/**
	 * Save the texts to the standard JSoko text file.
	 *
	 * @param p	properties to be saved
	 * @param filePath	path of the file
	 * TODO: check whether this method and the one from settings.java can be merged to one
	 */
	private void saveTexts(Properties p, String filePath) {

		// Clone the settings for having a copy that can be modified.
		Properties currentProperties = (Properties) p.clone();

		// Create BufferedReader to the English file which is taken as a skeleton.
		BufferedReader modelProperties = Utilities.getBufferedReader(Translator.getFilepathFromLanguageCode("en"));

		// Create PrintWriter for creating a new settings file containing the current settings.
		PrintWriter newSettingsFile = null;

		// ArrayList the new texts are added for storing them to a file.
		ArrayList<String> newTextFileContent = new ArrayList<String>();

		// Write the current date to the settings file.
		newTextFileContent.add("# Creation date: " + new Date().toString());

		try {
			// Read line by line from the original settings file
			// and compare it with the current settings in the program.
			String line;
			while ((line = modelProperties.readLine()) != null) {

				// Property key and property value
				String key;
				String value;

				// Trimmed line of the settings file.
				String trimmedLine = line.trim();

				// Just copy empty lines.
				if (trimmedLine.length() == 0) {
					newTextFileContent.add("");
					continue;
				}

				// Copy all comment lines, starting with "#".
				if (trimmedLine.charAt(0) == '#') {
					if (!trimmedLine.startsWith("# Creation date:")) {
						newTextFileContent.add(line);
					}
					continue;
				}

				// Get the first index of an assignment character.
				int index = trimmedLine.indexOf("=");
				if (index == -1) {
					index = trimmedLine.indexOf(":");
				}

				// Just copy all lines without any assignment character.
				if (index == -1) {
					newTextFileContent.add(line);
					continue;
				}

				// All lines starting with "!" are logically deleted properties.
				if (trimmedLine.charAt(0) == '!') {

					// Extract property key and value.
					key = trimmedLine.substring(1, index).trim();

					// Get the value of the property corresponding to the key.
					value = (String) currentProperties.remove(key);

					// If the property doesn't exist in the current settings just copy the old line.
					if (value == null) {
						newTextFileContent.add(line);
						continue;
					}
				} else {
					// Get the key from the current settings file.
					key = trimmedLine.substring(0, index).trim();

					// Get the value of the key from the current settings.
					value = (String) currentProperties.remove(key);

					// If the property doesn't exist in the current settings add it as comment to the new settings file.
					if (value == null) {
						newTextFileContent.add("! " + line);
						continue;
					}

					// Save empty string when the texts hasn't been translated by the user.
					// JSoko will choose the English text when no text is set.
					if(value.equals(nullKeyValue)) {
						value = "";
					}
				}

				// Add the property and its current value to the new file.
				switch (trimmedLine.charAt(index)) {
				case '=':
					newTextFileContent.add(key + " = " + Settings.mask(value));
					break;
				case ':':
					newTextFileContent.add(key + ": "  + Settings.mask(value));
					break;
				}

			}

			// If there are some properties left, they must be new ones. They are appended to the new settings file.
			newTextFileContent.add("");
			for (Object key : currentProperties.keySet()) {
				newTextFileContent.add(key + " = " + Settings.mask(currentProperties.getProperty((String) key)));
			}

			// Create the new text file and store all content.
			newSettingsFile = new PrintWriter(filePath + ".tmp", "UTF8");
			for(String data : newTextFileContent) {
				newSettingsFile.println(data);
			}

			// Delete the original user settings file.
			new File(filePath).delete();

			// Rename the new user settings file.
			new File(filePath + ".tmp").renameTo(new File(filePath));

		} catch (IOException ex) {
			showErrorMessage(Texts.getText("errorBySaving"), ex);
		} finally {
			try {
				modelProperties.close();
			} catch (IOException ex) {
				showErrorMessage(Texts.getText("errorBySaving"), ex);
			}
			if(newSettingsFile != null) {
				newSettingsFile.close();
			}
		}
	}


	/**
	 * Shows the passed message as error message.
	 *
	 * @param message the message to show
	 * @param e  the exception that is the cause of the message
	 */
	private void showErrorMessage(String message, Exception e) {

		if(Debug.isDebugModeActivated && e != null) {
			e.printStackTrace();
		}

		JOptionPane.showMessageDialog(
			parent,
			message,
			Texts.getText("error"),
			JOptionPane.ERROR_MESSAGE
			);
	}

	/**
	 * Creates a list of all program's .properties files.
	 * <p>
	 * This method only returns the file names not the full file path.
	 *
	 * @return list of all program's .properties files
	 */
	public static HashSet<String> getAvailableLanguageFiles() {

		FilenameFilter textsFilter = new FilenameFilter() {
			@Override
			public boolean accept(File dir, String name) {
				return name.endsWith(".properties");
			}
		};

		// Read all language files in the user directory.
		File f = Utilities.getFileFromClassPath(OSSpecific.getPreferencesDirectory()+path);
		HashSet<String> fileNames = new HashSet<String>(Arrays.asList(f.list(textsFilter)));

		// Return the JSoko standard text file names.
		f = Utilities.getFileFromClassPath(path);
		fileNames.addAll(Arrays.asList(f.list(textsFilter)));

		return fileNames;
	}

	/**
	 * Reloads all non-static GUI components.
	 */
	private void reloadGUI() {
		changeLanguageComboBoxes();

		loadLanguageTexts(NEW_LANGUAGE_COLUMN);
		loadLanguageTexts(HINT_LANGUAGE_COLUMN);

		// Set new and hint language combo boxes for default values.
		String language = (new Locale(Settings.get("newTranslationLanguage"))).getDisplayLanguage(getUserLocale());

		newLanguageComboBox.setSelectedItem(language);

		language = (new Locale(Settings.get("hintTranslationLanguage"))).getDisplayLanguage(getUserLocale());
		hintLanguageComboBox.setSelectedItem(language);

		// By default select the first row of the table.
		if (table.getRowCount() > 0) {
			table.changeSelection(0, NEW_LANGUAGE_COLUMN, false, false);
		}
	}

	private class TranslationTableModelListener implements TableModelListener {
		@Override
		public void tableChanged(TableModelEvent e) {
			int row = e.getFirstRow();
			TableModel model = (TableModel)e.getSource();

			// The following changes should not be applied if the change is caused
			// by deletion of a row.
			if (row < table.getRowCount()) {

				table.getModel().removeTableModelListener(translationTableModelListener);

				String data = (String) model.getValueAt(row, NEW_LANGUAGE_COLUMN);

				// Reflect change also in appropriate properties.
				newLanguageProperties.setProperty(
						(String) model.getValueAt(row, KEY_NAME_COLUMN    ),
						(String) model.getValueAt(row, NEW_LANGUAGE_COLUMN) );

				// If user deleted translation or no translation exists then
				// set "new language" cell to "<to be filled by the user>" (the real
				// property value should remain blank).
				if (data == null || data.equals("") || data.equals(nullKeyValue)) {
					table.setValueAt(nullKeyValue, row, NEW_LANGUAGE_COLUMN);
					newLanguageProperties.setProperty((String) model.getValueAt(row, KEY_NAME_COLUMN), "");
				}

				table.getModel().addTableModelListener(translationTableModelListener);
			}

			// As "Current text" is changed it is necessary to refresh also
			// the translation text area.
			refreshTranslationTextAreas();
		}
	}

	// Implementation of ListSelectionListener.
	private class TranslationTableListSelectionListener implements ListSelectionListener {
		@Override
		public void valueChanged(ListSelectionEvent e) {

			ListSelectionModel lsm = (ListSelectionModel)e.getSource();

			if (lsm.isSelectionEmpty()) {
				// It is impossible to remove a key if no key is selected.
				removeKeyButton.setEnabled(false);
			} else {
				// A key is selected so it is possible to remove it.
				removeKeyButton.setEnabled(true);
			}

			// Refresh all values in "translation corner".
			refreshTranslationTextAreas();
		}
	}

	private class TranslationKeyListener implements KeyListener {
		@Override
		public void keyTyped(KeyEvent e) {}

		@Override
		public void keyPressed(KeyEvent e) {}

		@Override
		public void keyReleased(KeyEvent e) {

			// The following code makes it possible to select a piece of translation
			// using only keyboard.
			int keyCode = e.getKeyCode();
			if (       keyCode == KeyEvent.VK_LEFT
					|| keyCode == KeyEvent.VK_RIGHT
					|| keyCode == KeyEvent.VK_UP
					|| keyCode == KeyEvent.VK_DOWN
					|| keyCode == KeyEvent.VK_HOME
					|| keyCode == KeyEvent.VK_END
					|| keyCode == KeyEvent.VK_SHIFT
					|| keyCode == KeyEvent.VK_CONTROL
					|| keyCode == KeyEvent.VK_ALT     ) {
				return;
			}

			// Copy value from text area into appropriate table's cell.
			table.setValueAt(translationTextArea.getText(), table.getSelectedRow(), NEW_LANGUAGE_COLUMN);
		}
	}

	private class NewLanguageActionListener implements ActionListener {

		@Override
		public void actionPerformed(ActionEvent evt) {
			newLanguageComboBoxActionPerformed(evt);
		}
	}

	private class HintLanguageActionListener implements ActionListener {

		@Override
		public void actionPerformed(ActionEvent evt) {
			hintLanguageComboBoxActionPerformed(evt);
		}
	}


	/**
	 * This class makes content of the language dialog.
	 */
	private class TranslatorLanguages extends JPanel {

		/** Reference to the parent component of this panel. */
		private JDialog parent;

		// GUI components
		private JLabel newLanguageLabel;
		private JComboBox<String> newLanguageComboBox;
		private JLabel hintLanguageLabel;
		private JComboBox<String> hintLanguageComboBox;
		private JButton okButton, cancelButton;

		/**
		 * Creates new TranslatorLanguages panel.
		 *
		 * @param parent handle to the dialog which contains the FileManager.
		 * @param gui handle to the Translator main module
		 */
		public TranslatorLanguages(JDialog parent) {
			this.parent = parent;
			initComponents();
		}

		/**
		 * Initializes all GUI components and sets panel's layout.
		 */
		private void initComponents() {
			newLanguageLabel     = new JLabel();
			newLanguageComboBox  = new JComboBox<String>();
			hintLanguageLabel    = new JLabel();
			hintLanguageComboBox = new JComboBox<String>();
			okButton     = new JButton();
			cancelButton = new JButton();


			newLanguageLabel.setText(Texts.getText("languageSettings.newLanguageLabel"));
			hintLanguageLabel.setText(Texts.getText("languageSettings.hintLanguageLabel"));

			okButton.setText(Texts.getText("ok"));
			okButton.addActionListener(new ActionListener() {

				@Override
				public void actionPerformed(ActionEvent e) {

					// Save translation language and hint language in the settings.
					Settings.set("newTranslationLanguage",
							Translator.getLanguageCode((String) newLanguageComboBox.getSelectedItem()));
					Settings.set("hintTranslationLanguage",
							Translator.getLanguageCode((String) hintLanguageComboBox.getSelectedItem()));

					try {
						Settings.saveSettings();
					} catch (IOException ex) {}

					parent.dispose();
				}
			});

			cancelButton.setText(Texts.getText("cancel"));
			cancelButton.addActionListener(new ActionListener() {

				@Override
				public void actionPerformed(ActionEvent e) {
					parent.dispose();
				}
			});

			// Add all available language names to the combo boxes.
			for (String languageName : Translator.getAvailableLanguageNames()) {
				newLanguageComboBox.addItem(languageName);
				hintLanguageComboBox.addItem(languageName);
			}


			// Create layout.
			GroupLayout layout = new GroupLayout(this);

			this.setLayout(layout);

			layout.setAutoCreateGaps(true);
			layout.setAutoCreateContainerGaps(true);

			layout.setHorizontalGroup(
				layout.createSequentialGroup()
					.addGroup(layout.createParallelGroup()
						.addComponent(newLanguageLabel)
						.addComponent(hintLanguageLabel)
						.addGap(30)
						.addComponent(okButton))
					.addGroup(layout.createParallelGroup()
						.addComponent(newLanguageComboBox)
						.addComponent(hintLanguageComboBox)
						.addGap(30)
						.addComponent(cancelButton))
			);

			layout.setVerticalGroup(
				layout.createSequentialGroup()
					.addGroup(layout.createBaselineGroup(true, false)
						.addComponent(newLanguageLabel)
						.addComponent(newLanguageComboBox))
					.addGroup(layout.createBaselineGroup(true, false)
						.addComponent(hintLanguageLabel)
						.addComponent(hintLanguageComboBox))
					.addGap(30)
					.addGroup(layout.createBaselineGroup(true, false)
						.addComponent(okButton)
						.addComponent(cancelButton))
			);

			layout.linkSize(newLanguageLabel, hintLanguageLabel);
			layout.linkSize(newLanguageComboBox, hintLanguageComboBox);
		}
	}
}