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

import java.awt.FlowLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import javax.swing.JCheckBox;
import javax.swing.JLabel;
import javax.swing.JPanel;

import de.sokoban_online.jsoko.utilities.USpinner;


/**
 * NumberInputTextField bean.
 * This class offers a JPanel, which contains a text field for entering numbers.
 * The value can also be changed by a JSpinner. The value is only "active"
 * when the corresponding check box is set.
 */
 @SuppressWarnings("serial")
public class NumberInputTF extends JPanel implements ActionListener {

	/** Checkbox which indicates whether the number field is "active". */
	private JCheckBox checkboxGUI;

	/** Label for the input field. */
	private String label;

	/** Minimum value of the number field. */
	private double minimumValue;
	/** Maximum value of the number field. */
	private double maximumValue;

	/** Label for the input field. */
	private JLabel labelGUI;

	/**
	 * {@code JSpinner} + mouse wheel control for value selection.
	 */
	private USpinner textfieldGUI;

	
	/**
	 * Creates a new input text field for numbers.
	 * 
	 * @param checkbox default status for the check box
	 * @param label text of the label for the input field
	 * @param min minimum value 
	 * @param max maximum value
	 */
	public NumberInputTF(Boolean checkbox, String label, double min, double max) {
		this(checkbox, label, min, max, (min + max) / 2, false);
	}

	/**
	 * Creates a new input text field for numbers.<br>
	 * If the passed check box is "null", then no check box is shown.
	 * 
	 * @param checkbox default status for the check box
	 * @param label text of the label for the input field
	 * @param min minimum value 
	 * @param max maximum value
	 * @param start start value
	 * @param isInteger whether only integer numbers are allowed
	 */
	public NumberInputTF(Boolean checkbox, String label, double min, double max, double start, boolean isInteger) {

		this.label = label;
		this.minimumValue = (isInteger ? Math.floor(min) : min);
		this.maximumValue = (isInteger ? Math.floor(max) : max);

		checkboxGUI = new JCheckBox();
		labelGUI    = new JLabel(label);

		checkboxGUI.addActionListener(this);

		// Create a JSpinner for the textfield.
		if (isInteger) {
			textfieldGUI = new USpinner((int) start, (int) min, (int) max, 1);
		} else {
			textfieldGUI = new USpinner(start, min, max, 1);
		}

		// Set a layout and add the components.
		setLayout(new FlowLayout(FlowLayout.LEADING));
		add(checkboxGUI);
		add(labelGUI);
		add(textfieldGUI);

		if (checkbox != null) {
			checkboxGUI.setSelected(checkbox.booleanValue());
			checkboxGUI.setVisible(true);
		} else {
			checkboxGUI.setSelected(true);
			checkboxGUI.setVisible(false);
		}

		updateElements();
	}

	/**
	 * Use the current value of the check box to enable or disable
	 * the text field and label components.
	 */
	private void updateElements() {
		final boolean selected = checkboxGUI.isSelected();
		
		textfieldGUI.setEnabled(selected);
		labelGUI.setEnabled(selected);
	}

	/**
	 * Returns the value of the input field as double.
	 * 
	 * @return the value of the input field, or {@code null}
	 */
	public Double getValueAsDouble() {
		return checkboxGUI.isSelected() ? textfieldGUI.getValueAsDouble() : null;
	}

	/**
	 * Returns the value of the input field as integer.
	 * 
	 * @return the value of the input field, or {@code null}
	 */
	public Integer getValueAsInteger() {
		return checkboxGUI.isSelected() ? textfieldGUI.getValueAsInteger() : null;
	}

	/**
	 * Returns the value of the input field as double - even when the field is disabled.
	 * 
	 * @return the value of the input field
	 */
	public Double getValueAsDoubleNoNull() {
		return textfieldGUI.getValueAsDouble();
	}

	/**
	 * Returns the value of the input field as integer - even when the field is disabled.
	 * 
	 * @return the value of the input field
	 */
	public Integer getValueAsIntegerNoNull() {
		return textfieldGUI.getValueAsInteger();
	}

	/**
	 * Returns whether the field is set "active".
	 * 
	 * @return <code>true</code> if the field is active, and
	 *        <code>false</code> otherwise
	 */
	public boolean isFieldActive() {
		return textfieldGUI.isEnabled();
	}

	/**
	 * Sets a new value for the display field.<br>
	 * If the value is higher than the maximum or lower than the minimum,
	 * the maximum or minimum value is used, respectively.
	 * In any case the effectively used value is returned.
	 * 
	 * @param value the value to be set
	 * @return the set value
	 */
	public double setValue(double value) {

		double rc = value;
		if (value > maximumValue) {
			rc = maximumValue;
		} else if (value < minimumValue) {
			rc = minimumValue;
		} else {
			rc = value;
		}
		textfieldGUI.setValue(rc);

		return rc;		
	}

	@Override
	public String toString() {

		StringBuilder rc = new StringBuilder();
		rc.append("[");
		rc.append("checkbox=");
		rc.append(checkboxGUI.isSelected());
		rc.append(", ");
		rc.append("label=");
		rc.append(label);
		rc.append(", ");
		rc.append("min=");
		rc.append(minimumValue);
		rc.append(", ");
		rc.append("max=");
		rc.append(maximumValue);
		rc.append(", ");
		rc.append("currentValue=");
		rc.append(getValueAsDouble() == null ? 0 : getValueAsDouble());
		rc.append(", ");
		rc.append("checkboxGUI=");
		rc.append(checkboxGUI.isSelected());
		rc.append("]");

		return rc.toString();

	}

	/*
	 * (non-Javadoc)
	 * 
	 * @see java.awt.event.ActionListener#actionPerformed(java.awt.event.ActionEvent)
	 */
	public void actionPerformed(ActionEvent e) {

		if (e.getSource().equals(checkboxGUI)) {
			updateElements();
		}
	}	
}