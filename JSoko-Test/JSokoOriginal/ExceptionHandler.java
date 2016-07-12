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
package de.sokoban_online.jsoko;

import java.lang.Thread.UncaughtExceptionHandler;
import java.lang.ref.WeakReference;
import java.util.Vector;

import javax.swing.JOptionPane;

import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;

/**
 * Handler for all uncaught exceptions thrown in JSoko.
 */
public enum ExceptionHandler implements UncaughtExceptionHandler {

	/** The one and only instance of this class. */
	INSTANCE;

	/** Handlers to be called when an exception has been thrown that hasn't been caught elsewhere. */
	private static final Vector<WeakReference<UncaughtExceptionHandler>> handlers = new Vector<WeakReference<UncaughtExceptionHandler>>();

	private long lastMemoryErrorTime = System.currentTimeMillis();


	/* (non-Javadoc)
	 * @see java.lang.Thread.UncaughtExceptionHandler#uncaughtException(java.lang.Thread, java.lang.Throwable)
	 */
	@Override
	public void uncaughtException(final Thread t, final Throwable e) {

		// Call all registered handlers.
		for(int handlerNo=handlers.size(); --handlerNo >= 0; ) {

			UncaughtExceptionHandler handler = handlers.get(handlerNo).get();
			if(handler != null) {
				handler.uncaughtException(t, e);
			}
			else {
				handlers.remove(handlerNo);
			}
		}

		if (e instanceof OutOfMemoryError) {
			if(System.currentTimeMillis()-lastMemoryErrorTime < 1000) {
				JOptionPane.showMessageDialog(null, Texts.getText("outOfMemory"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
				lastMemoryErrorTime = System.currentTimeMillis();
			}
		}
		else {
			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}
	}


	/**
	 * Adds the passed handler to this exception handler.
	 * <p>.
	 * Everytime an uncaught exception occurs first all added handlers are called
	 * and then the standard method in this object is called.
	 *
	 * @param handler  handler to be added
	 */
	public void addHandler(UncaughtExceptionHandler handler) {

		// There are only a few classes that add handlers. Hence, no need to check whether some
		// of the added objects have already been destroyed by the garbage collection ...
		handlers.add(new WeakReference<UncaughtExceptionHandler>(handler));
	}
}
