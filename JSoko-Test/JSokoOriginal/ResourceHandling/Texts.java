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
package de.sokoban_online.jsoko.resourceHandling;

import java.io.File;
import java.net.URL;
import java.net.URLClassLoader;
import java.util.Locale;
import java.util.MissingResourceException;
import java.util.ResourceBundle;

import javax.help.HelpBroker;
import javax.help.HelpSet;
import javax.help.SwingHelpUtilities;
import javax.swing.JComponent;
import javax.swing.JOptionPane;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;



/**
 * This class handles all the texts for the program.
 * This includes the online help texts,
 * and all normal text strings to be presented to the user.
 * The currently set language is considered here, when loading the texts.
 */
public final class Texts {

	private static volatile Texts ref = null;

	/** The ResourceBundle which holds all the texts. */
	private static ResourceBundle texts;

	/** The ResourceBundle which holds all the texts. */
	private static ResourceBundle englishFallbackTexts;

	/** The help broker for showing the help texts of JSoko. */
	public static HelpBroker helpBroker = null;


	private Texts() {}

	/**
	 * Returns an object of this class.
	 *
	 * @return	a <code>Settings</code> object
	 */
	public static Texts getSingletonObject() {

		if (ref == null) {
            synchronized(Texts.class) {
                if (ref == null) {
                	ref = new Texts();
                }
            }
        }
        return ref;
	}

	// Avoid cloning.
	@Override
	public Object clone() throws CloneNotSupportedException {
		throw new CloneNotSupportedException();
	}

	/*
	 * Error handling and normal texts ...
	 *
	 * Most kinds of diagnostics reflected to the user do use this module
	 * to retrieve a diagnostic text via some text key.  Hence, we here
	 * have to consider two special cases:
	 *
	 * (1) When we detect some error condition inside of this module,
	 *     and we want to reflect the problem to the user (tell her, what
	 *     is going on) we cannot always use the translation service
	 *     provided by this very implementation to tell about the special way
	 *     in which it does not work.
	 *
	 * (2) When the requested combination of current language and requested
	 *     text key cannot be retrieved (for whatever reason), and the text
	 *     was intended to utter an error condition, we should not just
	 *     come up with an empty diagnostic, or, even worse, we should not
	 *     stumble over a null pointer exception and crash the program.
	 *     If we would not have tried to translate all texts (including
	 *     diagnostic ones) into different languages, and would pester
	 *     everybody with fixed English diagnostics, we would be better off
	 *     in such a case, since the intended diagnostic would be there,
	 *     and the program would not crash without a meaningful comment.
	 *
	 * Case (1) is currently solved this way:
	 *     All errors detected inside this module do NOT cause the usual
	 *     diagnostic popups, but rather restrain to low level diagnostics
	 *     on System.err, and also are just fixed English text.
	 *
	 * Case (2) is more difficult to solve...
	 *
	 * Currently (2011-03-25) nothing special is done about it.
	 * It is not very probable, that a required diagnostic text is not found,
	 * but in case it does happen... the program may crash indeed.
	 *
	 * FFS/hm: to be continued
	 */

	/**
	 * Load the texts from the hard disk, according to the currently set
	 * language.
	 * @see JSoko#setLanguage(String)
	 */
	public static final void loadAndSetTexts() {

		// Load the texts for the translation of the texts in JSoko.
		try {
			// Load the English texts. They are used every time a text doesn't exist in the text file
			// of the user language. This may happen if the user hasn't translated all of the texts using
			// the Translator class. The translator then saves the not translated texts as empty strings.
			englishFallbackTexts = ResourceBundle.getBundle("texts.texts", new Locale("EN"));

			// Get the locale from the settings.
			Locale locale = new Locale(Settings.getString("currentLanguage", "EN"));

			// The user may have edited the texts using the Translator tool.
			// Therefore, we have to check first whether there is a user texts file.
			try {
				File appDir = new File(OSSpecific.getPreferencesDirectory());
				texts = ResourceBundle.getBundle("texts.texts", locale, new URLClassLoader(new URL[] {appDir.toURI().toURL()}, null)); // only own URL, no parent loader!
			} catch (Exception e) {
				// No user specific text file found. Load the JSoko text file.
				texts = ResourceBundle.getBundle("texts.texts", locale);
			}

			// Set default language for components.
			JComponent.setDefaultLocale(locale);

			// Tell it to the JRE.
			Locale.setDefault(locale);

		} catch (MissingResourceException e) {
			JOptionPane.showMessageDialog(null, "Fatal error: texts couldn't be loaded! Folder: /texts");
		}

		// Load the help texts for JSoko.
		helpBroker = getHelpBroker();
	}

	/**
	 * Returns the text represented by the passed key in the currently set
	 * language. This method also replaces specific parts of the texts
	 * (format specifiers) by the passed data.
	 * <p>
	 * Example: <code>getText("example", 1, 5);</code><br>
	 * if the text for the key <code>"example"</code> is
	 * <code>"range: %d-%d"</code> then the returned <code>String</code> is:
	 * <code>"range: 1-5"</code>
	 *
	 * In a release, as build via the "build.xml" we have not only the
	 * locale specific files "texts_XY.properties", but also a default
	 * file "texts.properties", which contains "texts-en.properties",
	 * so that ResourceBundle.getString() will fall back to the English
	 * texts, where the locale specific file does not contain the search key.
	 *
	 * @param textKey     key of the text to be returned
	 * @param replaceData optional substitution data
	 * @return	   text in the currently set language
	 *
	 * @see String#format(String, Object...)
	 */
	public static String getText(String textKey, Object ... replaceData) {

		try {
			// Get the text corresponding to the text key from the text file.
			// Check first using "contains" because otherwise a "MissingResource" exception is thrown.
			String text = texts.containsKey(textKey) ? texts.getString(textKey) : "";

			// If the text is empty or no text is stored for that key then load the English translation.
			// The fall back texts are the JSoko default texts as delivered in the JSoko package.
			if("".equals(text)) {
				text = englishFallbackTexts.getString(textKey);
			}

			// The text may contain other text keys which have to be replaced
			// by the corresponding texts.
			text = substEmbeddedKeys(textKey, text);

			// Replace all place holders with the passed data and return the string.
			return String.format(text, replaceData);

		} catch (java.util.MissingResourceException e) {
			// Did not find the key "textKey"

			// There are some texts which are loaded until no further text is found.
			// These texts have a counter at their end.
			if (Debug.isDebugModeActivated && !textkeyMaybeComputed(textKey) ) {
				System.out.println("Text for key: " + textKey + " not available!");
			}

			return "???"; // Give the user a hint that there is something missing
		} catch (Exception e) {
			System.out.println("Text key \"" + textKey + "\" caused an error.");
			return "???"; // Give the user a hint that there is something missing
		}
	}

	/**
	 * Replaces all embedded text keys in the passed text.
	 * Embedded text keys are surrounded by "@".
	 * Example: <code>"The @solution@ is invalid"</code>.
	 *
	 * @param textKey original key for the text (for diagnostics, only)
	 * @param text    the resulting text so far
	 * @return the text with all embedded keys substituted
	 * @throws MissingResourceException
	 */
	public static String substEmbeddedKeys(String textKey, String text) {

		// FFS/hm: we could use a subst-counter to detect endless loops and force termination
		for(int index=0; index<text.length(); ) {
			// Search the first '@' introducing an embedded key
			int beginIndex = text.indexOf('@', index);

			// Leave the loop if no replace-escape character has been found.
			if (beginIndex == -1) {
				break;
			}

			// Search the second '@', terminating the embedded key
			int endIndex   = text.indexOf('@', beginIndex+1);

			// If there is no second '@' we terminate the loop after dropping
			// an error notice, and leave the first '@' alone.
			if (endIndex == -1) {
				System.out.println("Property file error: missing @ for text: " + textKey);
				break;
			}

			// Two @ next to each other ("@@") are the escape sequence
			// for a single @. Just continue behind them.  See below.
			if(beginIndex == endIndex-1) {
				index = endIndex + 1;
				continue;
			}

			// Get the string to be replaced and the contained text key.
			String replaceTextSrc = text.substring(beginIndex, endIndex+1);
			String replaceTextKey = replaceTextSrc.replace("@", "");


			// Get the text corresponding to the key.
			try {
				String replaceText = texts.getString(replaceTextKey);

				// Replace all occurrences of the key with the replace text.
				// FFS/hm: we should replace only the first occurrence
				// Otherwise we may misinterpret the text between 2 keys as a key.
				text = text.replace(replaceTextSrc, replaceText);
			}catch(Exception e) { }

			// The replaced text may contain further text keys.
			// Hence, continue at the start of the replaced text.
			index = beginIndex;
		}

		// "@@" is the escape sequence for a single @ in the text.
		// We skipped them above, and reduce them here to a single "@".
		text = text.replace("@@", "@");
		return text;
	}

	/**
	 * Some text keys are generated systematically, and are retrieved,
	 * until no more data is found.  Such text keys end in a number,
	 * and are recognized here. Not finding data for such a text key
	 * is not considered an error, unlike most other requested text keys.
	 *
	 * @param textKey the text key to be judged
	 * @return whether the the text key looks like a generated one
	 */
	private static boolean textkeyMaybeComputed(String textKey) {

		if (textKey != null && textKey.length() >= 1) {
			// The text key is non-empty, i.e. it does have a last character.
			char lastChar = textKey.charAt(textKey.length() - 1);
			return Character.isDigit(lastChar);
		}
		// Condition not recognized ...
		return false;
	}

	/**
	 * Loads the help set for showing the help texts of JSoko.
	 * <p>
	 * The help is stored in a <code>JavaHelp</code>.
	 *
	 * @return the <code>HelpBroker</code> for the help of JSoko
	 */
	public static HelpBroker getHelpBroker() {

		// Find the HelpSet file and create the HelpSet object:
		HelpSet helpset = null;
		try {
			// External links in the help are to be displayed in the desktop browser.
			SwingHelpUtilities.setContentViewerUI("de.sokoban_online.jsoko.utilities.ExternalLinkContentViewerUI");

			URL hsURL = HelpSet.findHelpSet(Utilities.class.getClassLoader(), "helpset.hs");
			helpset = new HelpSet(null, hsURL);
		} catch (Exception e) {
			e.printStackTrace();
			return null;
		}

		// Create a HelpBroker object.
		return helpset.createHelpBroker();
	}
}