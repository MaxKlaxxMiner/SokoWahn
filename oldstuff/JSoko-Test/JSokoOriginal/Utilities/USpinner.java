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
package de.sokoban_online.jsoko.utilities;

import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.util.Vector;

import javax.swing.JSpinner;
import javax.swing.SpinnerNumberModel;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;


/**
 * Own JSpinner class with mouse wheel support.
 */
@SuppressWarnings("serial")
public class USpinner extends JSpinner {

	private Vector<ChangeListener> changeListeners;
	
	protected SpinnerNumberModel model;
	protected double current;

	/**
	 * Own JSpinner for doubles.
	 * 
	 * @param init initial value
	 * @param min  minimum value
	 * @param max  maximum value
	 * @param step step size
	 */
	public USpinner(double init, double min, double max, double step) {

		current = init;
		changeListeners = new Vector<ChangeListener>();

		// Set a new model.
		model = new SpinnerNumberModel(init, min, max, step);
		this.setModel(model);

		// Enable setting the value using the mouse wheel.
		MouseWheelListener mouseListener = new MouseWheelListener() {
			public void mouseWheelMoved(MouseWheelEvent arg0) {
				JSpinner sp = (JSpinner) arg0.getSource();

				double newCurrent = (new Double(sp.getValue().toString())).doubleValue();
				newCurrent -= arg0.getWheelRotation() * (Double) model.getStepSize();

				if (newCurrent < (Double) model.getMinimum()) {
					newCurrent = (Double) model.getMinimum();
				} else if (newCurrent > (Double) model.getMaximum()) {
					newCurrent = (Double) model.getMaximum();
				}

				// has the value been changed?
				if (Math.abs(current - newCurrent) < .0000001 && isEnabled()) {
					current = newCurrent;
					setValue(newCurrent);
					stateChanged();
				}
			}
		};
		addMouseWheelListener(mouseListener);
	}

	/**
	 * Own JSpinner for integers.
	 * 
	 * @param init initial value
	 * @param min  minimum value
	 * @param max  maximum value
	 * @param step step size
	 */
	public USpinner(int init, int min, int max, int step) {

		current = init;
		changeListeners = new Vector<ChangeListener>();

		// Set a new model.
		model = new SpinnerNumberModel(init, min, max, step);
		setModel(model);

		// Enable setting the value using the mouse wheel.
		MouseWheelListener mouseListener = new MouseWheelListener() {
			public void mouseWheelMoved(MouseWheelEvent arg0) {
				JSpinner sp = (JSpinner) arg0.getSource();

				double newCurrent = (new Double(sp.getValue().toString())).intValue();
				newCurrent -= arg0.getWheelRotation() * (Integer) model.getStepSize();

				if (newCurrent < (Integer) model.getMinimum()) {
					newCurrent = (Integer) model.getMinimum();
				} else if (newCurrent > (Integer) model.getMaximum()) {
					newCurrent = (Integer) model.getMaximum();
				}

				// has the value been changed?
				if (current != newCurrent && isEnabled()) {
					current = newCurrent;
					setValue(newCurrent);
					stateChanged();
				}

			}
		};
		addMouseWheelListener(mouseListener);
	}

	/**
	 * Inform every listener about the change of the state.
	 */
	protected void stateChanged() {
		ChangeEvent e = new ChangeEvent(this);
		for (ChangeListener listener : changeListeners) {
			listener.stateChanged(e);
		}
	}

	/**
	 * Removes the value listener.
	 * 
	 * @param listener  listener to be removed
	 */
	public void removeValueListener(ChangeListener listener) {
		changeListeners.remove(listener);
	}

	/**
	 * Adds the passed listener.
	 * 
	 * @param listener  listener to be added
	 */
	public void addValueListener(ChangeListener listener) {
		changeListeners.add(listener);
	}

	/**
	 * Returns the value of the spinner as double.
	 * 
	 * @return the value
	 */
	public double getValueAsDouble() {
		Object currentValue = getValue();
		if (currentValue instanceof Double) {
			return (Double) currentValue;
		}
		if (currentValue instanceof Integer) {
			return ((Integer) currentValue).doubleValue();
		}

		return 0;
	}

	/**
	 * Returns the value of the spinner as integer.
	 * 
	 * @return the value
	 */
	public int getValueAsInteger() {
		Object currentValue = getValue();
		if (currentValue instanceof Double) {
			return ((Double) currentValue).intValue();
		}
		if (currentValue instanceof Integer) {
			return (Integer) currentValue;
		}

		return 0;
	}
}
