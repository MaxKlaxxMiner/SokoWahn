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
import java.awt.Dimension;
import java.awt.FontMetrics;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.beans.PropertyChangeEvent;
import java.beans.PropertyChangeListener;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.Date;

import javax.swing.Box;
import javax.swing.BoxLayout;
import javax.swing.DefaultListModel;
import javax.swing.JButton;
import javax.swing.JFileChooser;
import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTextField;
import javax.swing.ListCellRenderer;
import javax.swing.SwingUtilities;
import javax.swing.ToolTipManager;
import javax.swing.border.EmptyBorder;
import javax.swing.border.TitledBorder;
import javax.swing.event.AncestorEvent;
import javax.swing.event.AncestorListener;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;

import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;



/**
 * <code>JFileChooser</code> accessory for management of shortcuts
 * to frequently accessed directories and files.
 *
 * @author Slav Boleslawski, Matthias Meger
 */
@SuppressWarnings("serial")
public class ShortCutsAccessory extends JPanel {

	// Delay times for the tooltips.
	private static final int TOOLTIP_DISMISS_DELAY = 2000;		// [ms]
	private static final int TOOLTIP_INITIAL_DELAY =  300;		// [ms]

	// The JFileChooser this accessory is added to.
	JFileChooser chooser;
	int originalInitialDelay;
	int originalDismissDelay;
	JList<DefaultListModel<Shortcut>> list;
	JScrollPane listScrollPane;
	JTextField aliasField;
	DefaultListModel<Shortcut> model;
	boolean shortcutsChanged;
	private JButton addButton;
	private JButton deleteButton;
	private JButton aliasButton;
	private String initialTitle;

	/** The name of the file to store the shortcuts for level collections in. */
	private static final String SHORTCUT_FILEPATHS_FILENAME = "shortcutFilePaths.txt";

	/**
	 * Creates an object of this accessory for adding it to a JFileChooser.
	 *
	 * @param chooser the JFileChooser this accessory is to be added to
	 */
	public ShortCutsAccessory(JFileChooser chooser) {

		super();

		this.chooser = chooser;

		// Displays the current directory path in the title bar of JFileChooser.
		updateTitle();

		// Create the GUI of this accessory.
		setGUI();

		// Add the listeners.
		addListeners();

	}

	/**
	 * Creates GUI for this accessory.
	 */
	final private void setGUI() {

		// Set a title for the shortcuts.
		setBorder(new TitledBorder(" " + Texts.getText("shortcuts") + " "));

		setLayout(new BorderLayout());

		// Create a model for the list of shortcuts.
		model = createModel();

		// Create a JList which shows the shortcuts.
		list = new JList(model) {

			// New ToolTipText method.
			@Override
			public String getToolTipText(MouseEvent mouseEvent) {

				if (model.size() == 0) {
					return null;
				}

				// Get the position of the mouse.
				Point p = mouseEvent.getPoint();

				Rectangle bounds = list.getCellBounds(model.size() - 1, model.size() - 1);

				int lastElementBaseline = bounds.y + bounds.height;

				//Is the mouse pointer below the last element in the list?
				if (lastElementBaseline < p.y) {
					return null;
				}

				// Get the index in the list.
				int index = list.locationToIndex(p);

				// Get the shortcut object stored at the calculated index.
				Shortcut shortcut = model.get(index);

				// Get the path which is stored in the shortcut.
				String path = shortcut.getPath();

				// If the shortcut has an alias the path is shown as tooltip.
				if (shortcut.hasAlias()) {
					return path;
				}

				// Check if the path is too long to be displayed.
				FontMetrics fm = list.getFontMetrics(list.getFont());
				int textWidth = SwingUtilities.computeStringWidth(fm, path);
				if (textWidth <= listScrollPane.getSize().width) {
					return null;
				}

				return path;
			}
		};
		list.setCellRenderer(new ListCellRenderer() {
			@Override
			public Component getListCellRendererComponent(JList list, Object value, int index, boolean isSelected, boolean cellHasFocus) {

				Shortcut shortcut = (Shortcut) value;

				JLabel label = new JLabel(shortcut.getDisplayName());
				label.setBorder(new EmptyBorder(0, 3, 0, 3));
				label.setOpaque(true);

				if (!isSelected) {
					label.setBackground(list.getBackground());
					label.setForeground(shortcut.getColor());
				} else {
					label.setBackground(list.getSelectionBackground());
					label.setForeground(shortcut.getColor());
				}
				return label;
			}
		});

		// Add the list of the shortcuts to a ScrollPane.
		listScrollPane = new JScrollPane(list);

		// Save the original delay values.
		originalInitialDelay = ToolTipManager.sharedInstance().getInitialDelay();
		originalDismissDelay = ToolTipManager.sharedInstance().getDismissDelay();

		// Set the new delay values and register the list.
		ToolTipManager.sharedInstance().setDismissDelay(TOOLTIP_DISMISS_DELAY);
		ToolTipManager.sharedInstance().setInitialDelay(TOOLTIP_INITIAL_DELAY);
		ToolTipManager.sharedInstance().registerComponent(list);

		// Add the list of shortcuts at the center of this panel.
		add(listScrollPane, BorderLayout.CENTER);

		// Create a new JPanel which holds all buttons.
		JPanel southPanel = new JPanel();
		southPanel.setBorder(new EmptyBorder(3, 3, 3, 3));
		southPanel.setLayout(new BoxLayout(southPanel, BoxLayout.X_AXIS));

		// The button for adding a shortcut to the shortcut list.
		addButton = new JButton(Texts.getText("add"));
		addButton.setToolTipText(Texts.getText("addTooltip"));
		addButton.setActionCommand("add");

		// The button for deleting a shortcut from the shortcut list.
		deleteButton = new JButton(Texts.getText("delete"));
		deleteButton.setToolTipText(Texts.getText("deleteTooltip"));
		deleteButton.setActionCommand("delete");

		// The button for setting an alias for a shortcut.
		aliasButton = new JButton(Texts.getText("set"));
		aliasButton.setToolTipText(Texts.getText("setTooltip"));
		aliasButton.setActionCommand("set");

		// Add the elements to the panel in the south.
		southPanel.add(Box.createHorizontalGlue());
		southPanel.add(addButton);
		southPanel.add(Box.createRigidArea(new Dimension(5, 0)));
		southPanel.add(deleteButton);
		southPanel.add(Box.createRigidArea(new Dimension(5, 0)));
		southPanel.add(new JLabel(Texts.getText("alias")));
		southPanel.add(Box.createRigidArea(new Dimension(2, 0)));
		aliasField = new JTextField(10);
		aliasField.setMaximumSize(aliasField.getPreferredSize());
		southPanel.add(aliasField);
		southPanel.add(Box.createRigidArea(new Dimension(5, 0)));
		southPanel.add(aliasButton);
		southPanel.add(Box.createHorizontalGlue());
		add(southPanel, BorderLayout.SOUTH);

		// Make sure the accessory is not resized with addition of entries longer than the current accessory width.
		int southPanelWidth = southPanel.getPreferredSize().width;
		Dimension size = new Dimension(southPanelWidth, 0);
		setPreferredSize(size);
		setMaximumSize(size);
	}

	/**
	 * Adds all listeners required by this accessory.
	 */
	final private void addListeners() {

		// Updates chooser's title.
		chooser.addPropertyChangeListener(new PropertyChangeListener() {
			@Override
			public void propertyChange(PropertyChangeEvent e) {
				String propertyName = e.getPropertyName();
				if (propertyName.equals(JFileChooser.DIRECTORY_CHANGED_PROPERTY)) {
					updateTitle();
				}
			}
		});

		// Saves shortcuts when the chooser is disposed.
		chooser.addAncestorListener(new AncestorListener() {
			@Override
			public void ancestorRemoved(AncestorEvent e) {
				ToolTipManager.sharedInstance().setDismissDelay(originalDismissDelay);
				ToolTipManager.sharedInstance().setInitialDelay(originalInitialDelay);
				if (shortcutsChanged) {
					saveShortcuts();
				}
			}

			@Override
			public void ancestorAdded(AncestorEvent e) {}

			@Override
			public void ancestorMoved(AncestorEvent e) {}
		});

		// Add a mouse listener to enable quick loading a collection by double clicking it.
		list.addMouseListener(new MouseAdapter() {

			@Override
			public void mouseClicked(MouseEvent e){
				if(e.getClickCount() == 2){
					// The selected item is automatically set by the ListSelectionListener.
					// Hence, we just have to approve the current selection.
					// However, the user may have double clicked a folder instead of a file.
					if(chooser.getSelectedFile() != null) {
						chooser.approveSelection();
					}
				}
			}
		});

		// Sets chooser's current directory or file and updates the Alias field
		list.addListSelectionListener(new ListSelectionListener() {
			@Override
			public void valueChanged(ListSelectionEvent e) {
				int selectedIndex = list.getSelectedIndex();
				if (selectedIndex == -1) {
					return;
				}

				// Get the selected shortcut and its data.
				Shortcut shortcut = model.get(selectedIndex);
				String alias = shortcut.getAlias();
				String path  = shortcut.getPath();
				String color = shortcut.getColorString();

				// Set the aliasText. If the color is not "black" then it is encoded as "color"+"#".
				String aliasText = alias;
				if ( ! color.equals("black")) {
					aliasText = color + '#' + alias;
				}
				aliasField.setText(aliasText);

				File file = new File(path);
				if (file.isFile()) {
					chooser.setSelectedFile(file);
				} else {
					chooser.setCurrentDirectory(file);
					chooser.setSelectedFile(null);
				}
			}
		});

		// Adds/deletes/edits a shortcut
		ActionListener actionListener = new ActionListener() {

			@Override
			public void actionPerformed(ActionEvent ae) {
				String command = ae.getActionCommand();

				// Delete a shortcut from the shortcut list.
				if (command.equals("delete")) {
					int[] selectedShortcuts = list.getSelectedIndices();
					for (int index = selectedShortcuts.length; --index >= 0;) {
						model.remove(selectedShortcuts[index]);
					}

					aliasField.setText("");
				}

				// Add a shortcut to the shortcut list.
				if (command.equals("add")) {
					String path;

					// Get the path of the selected file / directory and add it to the shortcut list.
					File file = chooser.getSelectedFile();
					if (file != null) {
						path = file.getAbsolutePath();
					} else {
						File dir = chooser.getCurrentDirectory();
						path = dir.getAbsolutePath();
					}
					insertShortcut(new Shortcut("", path, "black"));
				}

				// Set an alias for a shortcut.
				if (command.equals("set")) {
					setAlias();
				}

				list.clearSelection();
				chooser.setSelectedFile(null);
				shortcutsChanged = true;
			}
		};

		/*
		 * Add listeners to all buttons and the alias field.
		 */
		addButton.addActionListener(actionListener);
		deleteButton.addActionListener(actionListener);
		aliasButton.addActionListener(actionListener);
		aliasField.addKeyListener(new KeyAdapter() {
			@Override
			public void keyPressed(KeyEvent ke) {
				if (ke.getKeyCode() == KeyEvent.VK_ENTER) {
					setAlias();
					shortcutsChanged = true;
				}
			}
		});
	}

	/**
	 * Creates/edits/deletes an alias for a shortcut.
	 */
	final void setAlias() {

		// Get the index of the first selected shortcut.
		int selectionIndex = list.getMinSelectionIndex();

		// If there isn't any shortcut selected return immediately.
		if (selectionIndex == -1) {
			list.requestFocus();

			return;
		}

		// Get the selected shortcut and the new alias name.
		Shortcut shortcut = model.get(selectionIndex);

		// New alias and new color of the shortcut.
		String newAlias = aliasField.getText().trim();
		String newColor = "black";

		// If the text contains the magic character "#" the color is extracted.
		int hashIndex = newAlias.indexOf("#");
		if (hashIndex != -1) {
			newColor = newAlias.substring(0, hashIndex);
			newAlias = newAlias.substring(hashIndex + 1);
		}

		// Remove the old shortcut and insert the new shortcut having the alias name at the correct position.
		model.remove(selectionIndex);
		insertShortcut(new Shortcut(newAlias, shortcut.getPath(), newColor));

		// Erase the alias field for the next alias to be set.
		aliasField.setText("");
	}

	/**
	 * Inserts a new shortcut into the list so that list's alphabetical order is preserved.
	 */
	final void insertShortcut(Shortcut newShortcut) {

		// Do nothing if the new shortcut already exists.
		for (int i = 0; i < model.getSize(); i++) {
			Shortcut shortcut = model.get(i);
			if (shortcut.getPath().equalsIgnoreCase(newShortcut.getPath())) {
				return;
			}
		}

		// Calculate the index of the shortcut to be added (-> preserve alphabetical order).
		int insertIndex = 0;
		String newName2 = newShortcut.getName();
		for (; insertIndex < model.getSize(); insertIndex++) {
			String name = model.get(insertIndex).getName();
			if (name.compareToIgnoreCase(newName2) > 0) {
				break;
			}
		}

		// Add the new shortcut to the shortcut list.
		model.insertElementAt(newShortcut, insertIndex);
	}

	/**
	 * Creates a DefaultListModel and populates it with shortcuts read from a file.
	 */
	private DefaultListModel<Shortcut> createModel() {

		DefaultListModel<Shortcut> listModel = new DefaultListModel<Shortcut>();
		try {
			File file = new File(OSSpecific.getPreferencesDirectory()+SHORTCUT_FILEPATHS_FILENAME);
			if (!file.exists()) {
				file = new File(Utilities.getBaseFolder()+Settings.get("shortcutsFile")); // stay compatible with < 1.74 releases
				if (!file.exists()) {
					return listModel;
				}
			}

			BufferedReader in = new BufferedReader(new FileReader(file));
			String buf = null;
			while ((buf = in.readLine()) != null) {

				// Ignore lines with comments.
				if (buf.startsWith("//")) {
					continue;
				}

				int commaIndex = buf.indexOf(",");

				// Jump over invalid lines.
				if (commaIndex == -1) {
					continue;
				}

				String colorAndAlias = buf.substring(0, commaIndex).trim();
				String alias = colorAndAlias;
				String color = "black";
				String path  = buf.substring(commaIndex + 1).trim();

				// If there is a "#" in the string its a concatenation of the color and the alias.
				int hashIndex = colorAndAlias.indexOf("#");
				if (hashIndex != -1) {
					color = colorAndAlias.substring(0, hashIndex);
					alias = colorAndAlias.substring(hashIndex + 1);
				}

				// Add the shortcut
				listModel.addElement(new Shortcut(alias, path, color));
			}
			in.close();
		} catch (IOException e) {

			return null;
		}

		return listModel;
	}

	/**
	 * Saves the shortcuts list to a file in the JSoko folder.
	 */
	void saveShortcuts() {

		try {
			// Create a new file for saving the shortcuts.
			PrintWriter shortcutsFile = new PrintWriter(new BufferedWriter(
					new FileWriter(OSSpecific.getPreferencesDirectory()+SHORTCUT_FILEPATHS_FILENAME)));

			shortcutsFile.println(  "//Directory Shortcuts for "
								  + " [" + new Date().toString() + ']' );
			for (int i = 0; i < model.size(); i++) {
				Shortcut shortcut = model.get(i);
				String alias = shortcut.getAlias();
				String path  = shortcut.getPath();
				String color = shortcut.getColorString();
				shortcutsFile.println(color + '#' + alias + ',' + path);
			}
			shortcutsFile.close();
		} catch (IOException e) {

			return;
		}
	}

	/**
	 * Displays the current directory path in the title bar of JFileChooser.
	 */
	void updateTitle() {

		if (initialTitle == null) {
			initialTitle = chooser.getUI().getDialogTitle(chooser);
		}
		chooser.setDialogTitle(   initialTitle
								+ " ("
								+ chooser.getCurrentDirectory().getPath()
								+ ")"  );
	}
}

/**
 * This class defines a shortcut object in the list.
 */

final class Shortcut {

	// Data of this shortcut.
	private String alias;
	private String path;
	private Color color;

	/**
	 * Creates a new shortcut object holding the passed data.
	 *
	 * @param alias	alias of the stored path
	 * @param path  path to the file
	 * @param color color this shortcut is to be shown in the list
	 */
	public Shortcut(String alias, String path, String color) {
		this.alias = alias;
		this.path  = path;
		this.color = parseColor(color);
	}

	/**
	 * Returns whether this shortcut has an alias.
	 *
	 * @return <code>true</code> if this shortcut has an alias,
	 *        <code>false</code> otherwise
	 */
	public boolean hasAlias() {
		return (alias.length() > 0);
	}

	/**
	 * Returns the alias of this shortcut.
	 *
	 * @return the alias of this shortcut
	 */
	public String getAlias() {
		return alias;
	}

	/**
	 * Sets the alias of this shortcut.
	 *
	 * @param newAlias the new alias of this shortcut
	 */
	public void setAlias(String newAlias) {
		alias = newAlias;
	}

	/**
	 * Returns the path stored in this shortcut.
	 *
	 * @return the path
	 */
	public String getPath() {
		return path;
	}

	/**
	 * Returns the name of this shortcut.
	 * <p>
	 * If there is an alias this alias is returned. Otherwise the path is returned.
	 *
	 * @return the name of this shortcut
	 */
	public String getName() {
		if (hasAlias()) {
			return alias;
		}
		return path;
	}

	/**
	 * Returns formatted shortcut's name for display.
	 *
	 * @return the display name of this shortcut
	 */
	public String getDisplayName() {
		if (hasAlias()) {
			return '[' + alias + ']';
		}
		return path;
	}

	/**
	 * Returns the color of this shortcut.
	 *
	 * @return the color of this shortcut
	 */
	public Color getColor() {
		return color;
	}

	/**
	 * Sets the color of this shortcut.
	 *
	 * @param color color to be set
	 */
	public void setColor(String color) {
		this.color = parseColor(color);
	}

	/**
	 * Returns the <code>String</code> representation of the shortcut's color.
	 *
	 * @return color of this shortcut as <code>String</code>
	 */
	public String getColorString() {
		return colorToString(color);
	}

	/**
	 * Converts color to <code>String</code> and returns the <code>String</code>.
	 *
	 * Some colors defined in Color are used as is (for instance, Color.blue).
	 * Green, teal (aquamarine) and yellow colors are defined in this method.
	 * Other colors are represented as an RGB hexadecimal string (without
	 * the alpha component).
	 *
	 * @see #parseColor(String)
	 */
	private String colorToString(Color color) {
		if (color == Color.blue) {
			return "blue";
		}
		if (color == Color.cyan) {
			return "cyan";
		}
		if (color == Color.gray) {
			return "gray";
		}
		if (color == Color.magenta) {
			return "magenta";
		}
		if (color == Color.orange) {
			return "orange";
		}
		if (color == Color.pink) {
			return "pink";
		}
		if (color == Color.red) {
			return "red";
		}
		if (color == Color.black) {
			return "black";
		}

		String fullColorStr = Integer.toHexString(color.getRGB());

		//The first two digits in fullColorStr are ignored in colorStr (alpha component)
		String colorStr = fullColorStr.substring(2);
		if (colorStr.equals("339933")) {
			return "green";
		}
		if (colorStr.equals("cccc33")) {
			return "yellow";
		}
		if (colorStr.equals("66cc99")) {
			return "teal";
		}

		return colorStr;
	}

	/**
	 * Returns the <code>Color</code> represented by the passed <code>String</code>.
	 *
	 * @param colorString the color as <code>String</code>
	 * @return the <code>Color</code> corresponding to the passed <code>String</code>
	 *
	 * @see #colorToString(Color)
	 */
	private Color parseColor(String colorString) {

		try {
			return new Color(Integer.parseInt(colorString, 16));
		} catch (NumberFormatException e) {
		}

		if (colorString.equals("blue")) {
			return Color.blue;
		}
		if (colorString.equals("cyan")) {
			return Color.cyan;
		}
		if (colorString.equals("gray")) {
			return Color.gray;
		}
		if (colorString.equals("green")) {
			return new Color(0x33, 0x99, 0x33);
		}
		if (colorString.equals("magenta")) {
			return Color.magenta;
		}
		if (colorString.equals("orange")) {
			return Color.orange;
		}
		if (colorString.equals("pink")) {
			return Color.pink;
		}
		if (colorString.equals("red")) {
			return Color.red;
		}
		if (colorString.equals("teal")) {
			return new Color(0x66, 0xcc, 0x99);
		}
		if (colorString.equals("yellow")) {
			return new Color(0xcc, 0xcc, 0x33);
		}

		return Color.black;
	}

	/* (non-Javadoc)
	 * @see java.lang.Object#toString()
	 */
	@Override
	public String toString() {
		return "[" + alias + "," + path + "," + colorToString(color) + "]";
	}
}
