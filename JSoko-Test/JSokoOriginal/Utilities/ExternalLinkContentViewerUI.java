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

import java.awt.Desktop;
import java.net.URI;
import java.net.URL;

import javax.help.JHelpContentViewer;
import javax.help.plaf.basic.BasicContentViewerUI;
import javax.swing.JComponent;
import javax.swing.event.HyperlinkEvent;
import javax.swing.plaf.ComponentUI;


/**
 * A new UI for the JHelpContentViewer that will open external links
 * (web site or mail links) using the desktop programs. 
 */
@SuppressWarnings("serial")
public class ExternalLinkContentViewerUI extends BasicContentViewerUI {

	/**
	 * Creates UI class for the JHelpContentViewer.
	 * 
	 * @param helpViewer viewer to create the UI for
	 */
	public ExternalLinkContentViewerUI(JHelpContentViewer helpViewer) {
		super(helpViewer);
	}

	/**
	 * Creates UI class for the JHelpContentViewer.
	 * 
	 * @param component  JHelpContentViewer to create the UI for
	 * @return  the created UI for the JHelpContentViewer
	 */
	public static ComponentUI createUI(JComponent component) {
		return new ExternalLinkContentViewerUI((JHelpContentViewer)component);
	}

	/* (non-Javadoc)
	 * @see javax.help.plaf.basic.BasicContentViewerUI#hyperlinkUpdate(javax.swing.event.HyperlinkEvent)
	 */
	public void hyperlinkUpdate(HyperlinkEvent hle ) {

		// Override super class behavior when a hyper link has been activated.
		if(hle.getEventType()==HyperlinkEvent.EventType.ACTIVATED) {
			try {
				// Special coding for the JSoko help: on the web site 
				// www.sokoban-online.de the e-mails are encrypted using javascript.
				// They have to be decrypted before the e-mail program is started.
				if(hle.getDescription().startsWith("javascript:linkTo_UnCryptMailto"))  {

					// Example string that might occur: javascript:linkTo_UnCryptMailto('ocknvq,emaildecrypted');
					String[] descriptionParts = hle.getDescription().split("'");
					if(descriptionParts.length == 3) {
						String email = decryptMailTo(descriptionParts[1], -2);
						Desktop.getDesktop().browse(new URI(email));
						return;
					}

				}		

				// Handle not encrypted links and mailTo protocols.
				URL url = hle.getURL();

				if(url.getProtocol().equalsIgnoreCase("mailto") ||
						url.getProtocol().equalsIgnoreCase("http")) {
					Desktop.getDesktop().browse(url.toURI());
					return;
				}
			}
			catch(Throwable t) {
				t.printStackTrace();
			}
		}
		super.hyperlinkUpdate(hle);
	}

	/**
	 * Decrypts a string which has been "shifted". 
	 * 
	 * @param mailTo  the <code>String</code> to be decrypted
	 * @param offset  the offset used to encrypt the <code>String</code>
	 * @return the decrypted <code>String</code>
	 */
	private String decryptMailTo(String mailTo, int offset) {

		StringBuilder decryptedString = new StringBuilder();
		
		// Shift every character of the passed string by offset.
		for(int i =0; i<mailTo.length(); i++) {
			int c = mailTo.charAt(i);
			
			/*
			 * Check for special characters.
			 */
			
			// Characters: + , - . / 0-9 and :
			if(c >= 0x2B && c <= 0x3A) {
				decryptedString.append(decryptCharcode(c, 0x2B, 0x3A, offset));
			}
			// Characters @ and A-Z
			else if(c >= 0x40 && c <= 0x5A) { 
				decryptedString.append(decryptCharcode(c, 0x40, 0x5A, offset));
			}
			// Characters a-z
			else if(c >= 0x61 && c <= 0x7A) { 
				decryptedString.append(decryptCharcode(c, 0x61, 0x7A, offset));
			}
			else{
				decryptedString.append(c);
			}
		}
		return decryptedString.toString();}


	/**
	 * Decrypts special character regions of the ascii code.
	 * 
	 * @param c  <code>Character</code> to be decrypted
	 * @param start minimum ascii value to be used for decrypting
	 * @param end   maximum ascii value to be used for decrypting
	 * @param offset  offset the character has to be shifted by
	 * @return the decrypted <code>Character</code>
	 */
	private char decryptCharcode(int c, int start, int end, int offset) {
		
		c+=offset;
		if(offset > 0 && c > end){
			c= (start+(c-end-1));
		}
		else if(offset < 0 && c < start){
			c= (end-(start-c-1));
		}
		return (char) c;
	}
}