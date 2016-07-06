package de.sokoban_online.jsoko.leveldata;

/**
 * Used for encoding and decoding the run length format.
 * <p>
 * The level data and solution data can be run length encoded according
 * to the file format specification.
 */
public class RunLengthFormat {

	/**
	 * Run length encodes the given <code>String</code> and
	 * returns the encoded representation.
	 * Additionally, some simple transformations are performed:
	 * <br>(a) blanks are translated into minus (<code>' ' -> '-'</code>),
	 * <br>(b) newlines are translated into bars (<code>'\n' -> '|'</code>), and
	 * <br>(c) trailing blanks are deleted from each line (not encoded before a newline).
	 * <br>
	 * That way this encoding method can be used for levels as well as for
	 * solutions or snapshots.
	 * <p>
	 * Example: "uurrr" will become "uu3r".
	 *
	 * @param givenString the <code>String</code> to encode
	 * @return the encoded <code>String</code>
	 */
	public static String runLengthEncode(String givenString) {

		if(givenString == null) {
			return "";
		}

		// Convert the given string to a standard form: character '\n' will become '|'
		StringBuilder standard = new StringBuilder(givenString);
		for (int index = 0; index < standard.length(); index++) {
			if (standard.charAt(index) == '\n') {
				standard.setCharAt(index, '|');
			}
		}
		givenString = standard.toString();

		StringBuilder encoded = new StringBuilder();

		for (int i=0, length = givenString.length(); i < length; i++) {

			char currentChar = givenString.charAt(i);

			// Calculate the number of repeated characters.
			int runLength = 1;
			while(i+1 < length && givenString.charAt(i) == givenString.charAt(i+1) ) {
				runLength++;
				i++;
			}

			// Just a single character.
			if (runLength == 1) {
				encoded.append(currentChar);
			}

			// Two characters.
			if (runLength == 2){
				encoded.append(currentChar);
				encoded.append(currentChar);
			}

			// More than 2 characters in a row -> run length encoding.
			if (runLength > 2){
				encoded.append(runLength);
				encoded.append(currentChar);
			}
		}

		// Remove all the trailing blanks in every row of the board data
		// and return the new string.  By now newlines are seen as bars '|',
		// and may already be prefixed by a repetition count (i.e. digits).
		// But, we do not expect consecutive newlines, since levels
		// just cannot contain completely empty lines.
		String encodedString = encoded.toString().replaceAll("[0-9]*[ ]+[|]", "|");
		encodedString = encodedString.replaceAll("[0-9]*[ ]$", "");
		encodedString = encodedString.replaceAll("[ ]*$", "").replace(' ', '-');
		return encodedString;
	}


	/**
	 * The maximal repetition count which we accept during
	 * run length decoding.  Larger values are clipped down to this maximum.
	 */
	public static final int	MAX_RLE_REPCOUNT = 99999;

	/**
	 * Decodes a run length encoded <code>String</code> and returns
	 * the decoded representation.
	 * <p>
	 * Example: "uu3r" will become "uurrr".
	 *
	 * @param givenString the <code>String</code> to decode
	 * @return the decoded <code>String</code>
	 */
	public static String runLengthDecode(String givenString){

		if (givenString == null) {
			return "";
		}

		if (givenString.length() <= 1) {
			// A string of length at most one cannot really encode
			// anything containing a meaningful repetition.
			// We could check for a trailing 1-digit number, and omit it,
			// but we are not really interested.
			// Hence we conclude: this short string is just literal.
			return givenString;
		}

		// Here we will build up our result
		StringBuilder decodedString = new StringBuilder();

		// We may want to collect the digits of a number in a separate
		// buffer, and we want to reuse the same object for further numbers.
		// But, up to now, we do not need such a buffer.
		// We just provide the anchor to remember one such buffer.
		StringBuilder numberAsText = null;

		// Iterates through the givenString
		for (int index = 0, maxIndex = givenString.length(); index < maxIndex; index++) {

			char character = givenString.charAt(index);

			if( ! Character.isDigit(character)) {
				decodedString.append(character);
			} else {
				// start to reuse our buffer for the digits of a number
				if (numberAsText == null) {
					numberAsText = new StringBuilder();
				} else {
					// We just recycle the already allocated builder object.
					numberAsText.setLength(0);
				}
				while(index < maxIndex && Character.isDigit(givenString.charAt(index))) {
					numberAsText.append(givenString.charAt(index++));
				}

				// Append the next character "numberAsText" times ...
				//
				// There are 3 possible problems:
				// (a) There is no further character left to repeat.
				//     We will just omit this digit trailer.
				// (b) The digit sequence specifies a number which is
				//     too large for an int.  We will get a
				//     NumberFormatException, and decide to reduce the number.
				// (c) The digits form a legal int value, but it is so huge,
				//     that we refuse to append that many chars.
				if(index < maxIndex) {
					//  Ok, at least we have a character to repeat
					character = givenString.charAt(index);

					// By construction we know, that there are only decimal
					// characters in the buffer, which is not empty.
					// Up to a maximal length of 9 we are sure, we can
					// convert that into a number.  Longer digit sequences
					// may or may not have a problem (leading zeros may be harmless).
					int repcount = 0;
					if (numberAsText.length() <= 9) {
						repcount = Integer.parseInt(numberAsText.toString());
					} else {
						try {
							repcount = Integer.parseInt(numberAsText.toString());
						} catch (NumberFormatException e) {
							// Must have been an overflow... clip down
							repcount = MAX_RLE_REPCOUNT;
						}
					}

					// Even if legally converted, the repcount may still
					// be too large, and must be clipped down.
					repcount = Math.min(repcount, MAX_RLE_REPCOUNT);

					// We have completely decided what to do...
					while (repcount > 0) {
						decodedString.append(character);
						--repcount;
					}
				} else {
					// We have a trailing number, but no character to repeat.
					// We choose to omit the trailing number.
				}
			}
		}

		// Convert the given string back to its non-standard form:
		// Character '|' will become '\n'.
		for (int index = 0; index < decodedString.length(); index++) {
			if (decodedString.charAt(index) == '|') {
				decodedString.setCharAt(index, '\n');
			}
		}

		return decodedString.toString();
	}
}
