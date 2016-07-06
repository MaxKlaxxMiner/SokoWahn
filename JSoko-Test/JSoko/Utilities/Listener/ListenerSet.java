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
package de.sokoban_online.jsoko.utilities.listener;

import java.awt.event.ActionListener;
import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.WeakHashMap;


/**
 * This class provides support for the typical situation, that multiple
 * listeners want to register (as a listener) at some object of the
 * model domain.  Whenever the model changes, it shall notify (inform)
 * all currently registered listeners about the change, so that the
 * listening GUI objects can update their view.
 *
 * Logically, this may just be a queue or set of listeners, but we have
 * several issues with the simple approach:
 * <ul>
 *  <li>From the multiple listening GUI objects we should expect that their
 *      registering and unregistering requests occur concurrently.
 *      I.e. this class should be thread safe.
 *  <li>We want to be prepared for the case that some listening GUI object
 *      fails to properly unregister at this class.  Then, this listener set
 *      would hold the last reference which stops the garbage collector from
 *      collecting the logically already dead GUI object.
 *      Hence, here we shall use a {@link WeakReference} to remember the
 *      registered listener objects.
 * </ul>
 * As a consequence of synchronization we shall offer a way to notify all
 * the listeners either in a synchronized way, or in a decoupled way.
 *
 * Also, we want to offer a type safe but general way to connect the
 * event generator with the event listener. That causes us to use
 * the classes {@link ListenerCaller} and {@link Generator}.
 * The latter one is necessary since in general event objects are not immutable.
 *
 * In order to have the {@link WeakReference}s to work in the intended way,
 * we must not have / implement any further strong references to that same
 * GUI listener object.
 *
 * Note that we implement a set and not a list: we assume that we may inform
 * the registered listeners in no specific order.
 *
 * The main usage inside of JSoko is for {@link ActionListener}s.
 *
 * @param <L> the type of the listeners to be registered
 * @param <E> the type of the events consumed by the listeners
 *
 * @see ListenerCaller
 * @see Generator
 *
 * @author Heiner Marxen
 */
public class ListenerSet<L,E> {
	/*
	 * Since we want to generalize over the exact type of event and the
	 * interface which is used to note the listeners, we need some helper
	 * class which encapsulates the details, and offers something fixed
	 * just for our service.
	 * But, we must not accept helper objects which embed a reference to
	 * the listener instance, since that would defeat our use of weak refs.
	 * (But we could use the weak ref at multiple places)
	 *
	 * Hence we let the constructor remember a Caller object, which does
	 * not bind the concrete listener, but just its type and notify method.
	 */

	/**
	 * This caller is provided by the constructor and encapsulates (abstracts)
	 * the method to be called in the listeners.
	 */
	private final ListenerCaller<L,E> caller;

	/**
	 * This weak hashmap is used to build a set of weak references.
	 * It does not contain null keys, but all values are null,
	 * since we do not need any associated value.
	 *
	 * Using a hash map to implement a set may look like overkill, but this
	 * is the standard implementation with the desired properties from
	 * using weak references: we need not handle them at all anymore.
	 *
	 * Note: all usage of this member shall be synchronized!
	 */
	private final WeakHashMap<L,Void> weakhashmap;

	/**
	 * Constructs a new ListenerSet and remembers the caller to be used
	 * to later pass events to listeners in order to inform them.
	 *
	 * @param caller used to inform the listeners
	 */
	public ListenerSet(ListenerCaller<L,E> caller) {
		this.caller      = caller;
		this.weakhashmap = new WeakHashMap<L,Void>();
	}

	/**
	 * Tells the number of listeners in this set.  Most often that number
	 * may change each time you ask for it.
	 *
	 * @return the current number of registered listeners
	 */
	public synchronized int size() {
		return weakhashmap.size();
	}

	/**
	 * Adds the passed listener to the set of listeners.
	 * If the passed listener is already contained in our set, we do not
	 * really do something.
	 *
	 * @param listener to be added to the set
	 */
	public synchronized void register(L listener) {
		if (listener != null) {
			weakhashmap.put(listener, null);
		}
	}

	/**
	 * Deletes the passed listener from the set of listeners.
	 * When the listener is {@code null} or not an element of the set,
	 * just nothing happens.
	 *
	 * @param listener to be deleted from the set
	 */
	public synchronized void unregister(L listener) {
		if (listener != null) {
			weakhashmap.remove(listener);
		}
	}

	/**
	 * Informs all listeners using objects generated by the passed generator,
	 * in a completely synchronized way.
	 *
	 * @param eventgen generates the events to be passed to the listeners
	 *                  ({@code null} is taken to generate {@code nulls})
	 */
	public synchronized void informAllSync(Generator<E> eventgen) {
		for (L listener : weakhashmap.keySet()) {
			final E evt = ((eventgen != null) ? eventgen.generate() : null);
			caller.call(listener, evt);
		}
	}

	/**
	 * Computes a snapshot of the set of listeners as a list of strong
	 * references.
	 *
	 * @return the current list of listeners
	 */
	private synchronized ArrayList<L> getListenersCopy() {
		return new ArrayList<L>(weakhashmap.keySet());
	}

	/**
	 * Informs all listeners using objects generated by the passed generator.
	 * While calling the listener method for this notification this
	 * ListenerSet object is not synchronized, any more.
	 * Only the initial copying of the listener list is synchronized.
	 *
	 * @param eventgen generates the events to be passed to the listeners
	 *                  ({@code null} is taken to generate {@code nulls})
	 */
	public void informAllUnsync(Generator<E> eventgen) {
		/*
		 * We must not directly use (weakhashmap.keySet()), since it still
		 * is coupled to the weakhashmap itself, which needs synchronization.
		 * Hence we first must obtain a copy.
		 */
		for (L listener : getListenersCopy()) {
			final E evt = ((eventgen != null) ? eventgen.generate() : null);
			caller.call(listener, evt);
		}
	}
}
