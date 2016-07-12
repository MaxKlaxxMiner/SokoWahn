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

import java.sql.Timestamp;
import java.text.DateFormat;
import java.util.Date;

import javax.swing.JTable;
import javax.swing.table.DefaultTableCellRenderer;

import de.sokoban_online.jsoko.utilities.Debug;


/**
 * Our table cell renderer for objects of type {@link Date} or
 * {@link Timestamp}, Inspired directly by the Java Tutorial.
 * <p>
 * The type {@link java.util.Date} implements {@link Comparable},
 * which is perfectly fine for sorting rows by such a column.
 * But when we enter objects of type {@code Date} (or the extended
 * {@code Timestamp}) directly into the table model, we do not get exactly
 * that conversion to the displayed string, which we want to have.
 * The default conversion of {@link JTable} is based on
 * {@code DateFormat#getDateInstance()}, and omits the time.
 * <p>
 * Hence, we override the string conversion in this renderer class,
 * which shall be used either as a default renderer for the table,
 * or for a column, or for the type Date or Timestamp.
 * 
 * @author Heiner Marxen
 */
@SuppressWarnings("serial")
public class DateTimeRenderer extends DefaultTableCellRenderer {
	
	/**
	 * The local converter to string, allocated lazily.
	 */
	private DateFormat formatter;
	
	/**
	 * Constructs a new default renderer for {@code Date} instances,
	 * formatting date and time.
	 * 
	 * @see DateFormat#getDateTimeInstance()
	 */
	public DateTimeRenderer() {
		super();
	}
	
	// FFS/hm: offer more options for conversion to String
	
	/**
	 * This conversion method is the main part of {@code #setValue(Object)}.
	 * We offer it separately to enable non-table code to agree with
	 * the conversion we do for tables.
	 * <p>
	 * We expect to get a {@code Date}.
	 * A {@code null} converts to an empty string.
	 * 
	 * @param value the table model data to be displayed
	 * @return the string we would display (render)
	 * @see #setValue(Object)
	 */
	public String valueToString(Object value) {
		String txt = "";
		
		if (value != null) {
			
			// Now we are going to need a converter: assert its existence...
			if (formatter == null) {
				formatter = DateFormat.getDateTimeInstance();
				// We use a slightly different formatter than the original,
				// which would omit the "time".
			}
			
			// Convert the "value" to the "txt" to be displayed ...
			if (value instanceof java.util.Date) {
				// This is the normal (expected) case
				txt = formatter.format( (java.util.Date)value );
			} else {
				// Although "value" is not the expected type,
				// we still try to make sense of it...
				txt = formatter.format( value );
			}
			if (Debug.isDebugModeActivated) {
				txt = "X:" + txt;		// marks source of conversion
			}
		}
		
		return txt;
	}
	
	/**
	 * Sets the string value (of the table cell) from the given object
	 * from the table model.  We expect to get a {@code Date}.
	 * A {@code null} renders as an empty string.
	 * 
	 * @param value the table model data to be displayed
	 * @see #valueToString(Object)
	 */
	@Override
	public void setValue(Object value) {
		
		// The string that is to be displayed
		String txt = valueToString(value);
				
		// Tell the JLabel (indirectly we are one) which text is to be shown.
		setText(txt);
	}
	
	/**
	 * Convenience function: enters this object as the default table cell
	 * renderer for type {@link java.util.Date}.
	 * This is the expected usage for this class.
	 * 
	 * @param table
	 */
	public void enterMeForTypeDate(JTable table) {
		table.setDefaultRenderer(java.util.Date.class, this);
	}
}
