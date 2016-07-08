/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2013 by Matthias Meger, Germany
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

import java.awt.Component;

import javax.swing.JOptionPane;

import de.sokoban_online.jsoko.resourceHandling.Texts;

/**
 * This class provides methods for displaying messages in a dialog.
 */
public class MessageDialogs {

	// FFS. for dialogs: Texts.getTextNoFail... returns key instead of null
	/**
	 * Shows an error message dialog titled "failure" (translated) with the
	 * specified message string.
	 * 
	 * @param parent parent component for this dialog
	 * @param msg    text string for the dialog
	 */
	public static final void showFailureString(Component parent, String msg) {
		final String title = Texts.getText("failure");
		JOptionPane.showMessageDialog( parent,
				                       msg,
				                       title,
				                       JOptionPane.ERROR_MESSAGE );
	}

	/**
	 * Shows an error message dialog titled "failure" (translated) with the
	 * message text specified by its key.
	 * 
	 * @param parent  parent component for this dialog
	 * @param textKey text key specifying the message to show
	 */
	public static final void showFailureTextKey(Component parent, String textKey) {
		final String msg = Texts.getText(textKey);
		showFailureString(parent, msg);
	}

	/**
	 * Shows an error message dialog titled "error" (translated) with the
	 * specified message string.
	 * 
	 * @param parent parent component for this dialog
	 * @param msg    text string for the dialog
	 */
	public static final void showErrorString(Component parent, String msg) {
		final String title = Texts.getText("failure");
		JOptionPane.showMessageDialog( parent,
				                       msg,
				                       title,
				                       JOptionPane.ERROR_MESSAGE );
	}

	/**
	 * Shows an error message dialog titled "error" (translated) with the
	 * message text specified by its key.
	 * 
	 * @param parent  parent component for this dialog
	 * @param textKey text key specifying the message to show
	 */
	public static final void showErrorTextKey(Component parent, String textKey) {
		final String msg = Texts.getText(textKey);
		showErrorString(parent, msg);
	}

	/**
	 * Shows an error message dialog with the message from the specified exception,
	 * and the specified title.  This is typically used from a catch phrase, but since
	 * we reflect it to the user, it is not considered an internal failure, so:
	 * no console message, and no stack trace.
	 * 
	 * @param parent parent component for this dialog
	 * @param exn    just caught exception containing message to display
	 * @param title  title string for the dialog
	 */
	public static final void showException(Component parent, Throwable exn, String title) {
		JOptionPane.showMessageDialog( parent,
				                       exn.getLocalizedMessage(),	// their translation
				                       title,
				                       JOptionPane.ERROR_MESSAGE );
	}

	/**
	 * Shows an error message dialog titled "error" with the message from the
	 * specified exception.  This is typically used from a catch phrase, but since
	 * we reflect it to the user, it is not considered an internal failure, so:
	 * no console message, and no stack trace.
	 * 
	 * @param parent parent component for this dialog
	 * @param exn  just caught exception containing message to display
	 */
	public static final void showExceptionError(Component parent, Throwable exn) {
		showException(parent, exn, Texts.getText("error"));
	}

	/**
	 * Shows an error message dialog titled "failure" with the message from the
	 * specified exception.  This is typically used from a catch phrase, but since
	 * we reflect it to the user, it is not considered an internal failure, so:
	 * no console message, and no stack trace.
	 * 
	 * @param parent parent component for this dialog
	 * @param exn  just caught exception containing message to display
	 */
	public static final void showExceptionFailure(Component parent, Throwable exn) {
		showException(parent, exn, Texts.getText("failure"));
	}

	/**
	 * Shows an error message dialog titled "failure" with the message from the
	 * specified exception.  This is typically used from a catch phrase, but since
	 * we reflect it to the user, it is not considered an internal failure, so:
	 * no console message, and no stack trace.
	 * 
	 * @param parent parent component for this dialog
	 * @param exn  just caught exception containing message to display
	 */
	public static final void showExceptionWarning(Component parent, Throwable exn) {
		JOptionPane.showMessageDialog( parent,
	            exn.getLocalizedMessage(),	// their translation
	            Texts.getText("warning"),
	            JOptionPane.WARNING_MESSAGE );
	}

	/**
	 * Shows an information message dialog with the (translated) title "note",
	 * and the specified message text.
	 * 
	 * @param parent parent component for this dialog
	 * @param msg    text string for the dialog
	 */
	public static final void showInfoNoteString(Component parent, String msg) {
		final String title = Texts.getText("note");
		JOptionPane.showMessageDialog(parent, msg, title, JOptionPane.INFORMATION_MESSAGE);
	}

	/**
	 * Shows an information message dialog with the (translated) title "note",
	 * and the text specified by its key.
	 * 
	 * @param parent  parent component for this dialog
	 * @param textKey text key specifying the message to show
	 */
	public static final void showInfoNoteTextKey(Component parent, String textKey) {
		final String msg   = Texts.getText(textKey);
		showInfoNoteString(parent, msg);
	}

	/**
	 * Shows an information message dialog with the (translated) title "congratulations",
	 * and the text specified by its key.
	 * 
	 * @param parent  parent component for this dialog
	 * @param textKey text key specifying the message to show
	 */
	public static final void showCongratsTextKey(Component parent, String textKey) {
		final String title = Texts.getText("congratulations");
		final String msg   = Texts.getText(textKey);
		JOptionPane.showMessageDialog(parent, msg, title, JOptionPane.INFORMATION_MESSAGE);
	}

}
