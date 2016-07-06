/*
 * Copyright (c) 1995, 2000, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */
package de.sokoban_online.jsoko.utilities;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PushbackInputStream;
import java.nio.ByteBuffer;


/**
 * This class implements a BASE64 Character decoder as specified in RFC1521.
 *
 * This RFC is part of the MIME specification which is published by the
 * Internet Engineering Task Force (IETF). Unlike some other encoding
 * schemes there is nothing in this encoding that tells the decoder
 * where a buffer starts or stops, so to use it you will need to isolate
 * your encoded data into a single chunk and then feed them this decoder.
 * The simplest way to do that is to read all of the encoded data into a
 * string and then use:
 * <pre>
 *      byte    mydata[];
 *      BASE64 base64 = new BASE64();
 *
 *      mydata = base64.decodeBuffer(bufferString);
 * </pre>
 * This will decode the String in <i>bufferString</i> and give you an array
 * of bytes in the array <i>myData</i>.
 *
 * On errors, this class throws a CEFormatException with the following detail
 * strings:
 * <pre>
 *    "BASE64: Not enough bytes for an atom."
 * </pre>
 *
 * @author      Chuck McManis
 * @see         CharacterEncoder
 * @see         BASE64
 */

public class BASE64 extends CharacterDecoder {

	/** This class has 4 bytes per atom */
	protected int bytesPerAtom() {
		return (4);
	}

	/** Any multiple of 4 will do, 72 might be common */
	protected int bytesPerLine() {
		return (72);
	}

	/**
	 * This character array provides the character to value map
	 * based on RFC1521.
	 */
	private final static char pem_array[] = {
		//       0   1   2   3   4   5   6   7
		'A','B','C','D','E','F','G','H', // 0
		'I','J','K','L','M','N','O','P', // 1
		'Q','R','S','T','U','V','W','X', // 2
		'Y','Z','a','b','c','d','e','f', // 3
		'g','h','i','j','k','l','m','n', // 4
		'o','p','q','r','s','t','u','v', // 5
		'w','x','y','z','0','1','2','3', // 6
		'4','5','6','7','8','9','+','/'  // 7
	};

	private final static byte pem_convert_array[] = new byte[256];

	static {
		for (int i = 0; i < 255; i++) {
			pem_convert_array[i] = -1;
		}
		for (int i = 0; i < pem_array.length; i++) {
			pem_convert_array[pem_array[i]] = (byte) i;
		}
	}

	byte decode_buffer[] = new byte[4];

	/**
	 * Decode one BASE64 atom into 1, 2, or 3 bytes of data.
	 */
	protected void decodeAtom(PushbackInputStream inStream, OutputStream outStream, int rem)  throws IOException {
		int     i;
		byte    a = -1, b = -1, c = -1, d = -1;

		if (rem < 2) {
			throw new IOException("BASE64: Not enough bytes for an atom.");
		}
		do {
			i = inStream.read();
			if (i == -1) {
				throw new IOException();
			}
		} while (i == '\n' || i == '\r');
		decode_buffer[0] = (byte) i;

		i = readFully(inStream, decode_buffer, 1, rem-1);
		if (i == -1) {
			throw new IOException();
		}

		if (rem > 3 && decode_buffer[3] == '=') {
			rem = 3;
		}
		if (rem > 2 && decode_buffer[2] == '=') {
			rem = 2;
		}
		switch (rem) {
		case 4:
			d = pem_convert_array[decode_buffer[3] & 0xff];
			// NOBREAK
			//$FALL-THROUGH$
		case 3:
			c = pem_convert_array[decode_buffer[2] & 0xff];
			// NOBREAK
			//$FALL-THROUGH$
		case 2:
			b = pem_convert_array[decode_buffer[1] & 0xff];
			a = pem_convert_array[decode_buffer[0] & 0xff];
			break;
		}

		switch (rem) {
		case 2:
			outStream.write( (byte)(((a << 2) & 0xfc) | ((b >>> 4) & 3)) );
			break;
		case 3:
			outStream.write( (byte) (((a << 2) & 0xfc) | ((b >>> 4) & 3)) );
			outStream.write( (byte) (((b << 4) & 0xf0) | ((c >>> 2) & 0xf)) );
			break;
		case 4:
			outStream.write( (byte) (((a << 2) & 0xfc) | ((b >>> 4) & 3)) );
			outStream.write( (byte) (((b << 4) & 0xf0) | ((c >>> 2) & 0xf)) );
			outStream.write( (byte) (((c << 6) & 0xc0) | (d  & 0x3f)) );
			break;
		}
		return;
	}

}


	/*
	 * Copyright (c) 1995, 2004, Oracle and/or its affiliates. All rights reserved.
	 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
	 *
	 * This code is free software; you can redistribute it and/or modify it
	 * under the terms of the GNU General Public License version 2 only, as
	 * published by the Free Software Foundation.  Oracle designates this
	 * particular file as subject to the "Classpath" exception as provided
	 * by Oracle in the LICENSE file that accompanied this code.
	 *
	 * This code is distributed in the hope that it will be useful, but WITHOUT
	 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
	 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
	 * version 2 for more details (a copy is included in the LICENSE file that
	 * accompanied this code).
	 *
	 * You should have received a copy of the GNU General Public License version
	 * 2 along with this work; if not, write to the Free Software Foundation,
	 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
	 *
	 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
	 * or visit www.oracle.com if you need additional information or have any
	 * questions.
	 */


	/**
	 * This class defines the decoding half of character encoders.
	 * A character decoder is an algorithm for transforming 8 bit
	 * binary data that has been encoded into text by a character
	 * encoder, back into original binary form.
	 *
	 * The character encoders, in general, have been structured
	 * around a central theme that binary data can be encoded into
	 * text that has the form:
	 *
	 * <pre>
	 *      [Buffer Prefix]
	 *      [Line Prefix][encoded data atoms][Line Suffix]
	 *      [Buffer Suffix]
	 * </pre>
	 *
	 * Of course in the simplest encoding schemes, the buffer has no
	 * distinct prefix of suffix, however all have some fixed relationship
	 * between the text in an 'atom' and the binary data itself.
	 *
	 * In the CharacterEncoder and CharacterDecoder classes, one complete
	 * chunk of data is referred to as a <i>buffer</i>. Encoded buffers
	 * are all text, and decoded buffers (sometimes just referred to as
	 * buffers) are binary octets.
	 *
	 * To create a custom decoder, you must, at a minimum,  overide three
	 * abstract methods in this class.
	 * <DL>
	 * <DD>bytesPerAtom which tells the decoder how many bytes to
	 * expect from decodeAtom
	 * <DD>decodeAtom which decodes the bytes sent to it as text.
	 * <DD>bytesPerLine which tells the encoder the maximum number of
	 * bytes per line.
	 * </DL>
	 *
	 * In general, the character decoders return error in the form of a
	 * CEFormatException. The syntax of the detail string is
	 * <pre>
	 *      DecoderClassName: Error message.
	 * </pre>
	 *
	 *
	 * @author      Chuck McManis
	 */
	abstract class CharacterDecoder {

		/** Return the number of bytes per atom of decoding */
		abstract protected int bytesPerAtom();

		/** Return the maximum number of bytes that can be encoded per line */
		abstract protected int bytesPerLine();

		/** decode the beginning of the buffer, by default this is a NOP. */
		protected void decodeBufferPrefix(PushbackInputStream aStream, OutputStream bStream) throws IOException { }

		/** decode the buffer suffix, again by default it is a NOP. */
		protected void decodeBufferSuffix(PushbackInputStream aStream, OutputStream bStream) throws IOException { }

		/**
		 * This method should return, if it knows, the number of bytes
		 * that will be decoded. Many formats such as uuencoding provide
		 * this information. By default we return the maximum bytes that
		 * could have been encoded on the line.
		 */
		protected int decodeLinePrefix(PushbackInputStream aStream, OutputStream bStream) throws IOException {
			return (bytesPerLine());
		}

		/**
		 * This method post processes the line, if there are error detection
		 * or correction codes in a line, they are generally processed by
		 * this method. The simplest version of this method looks for the
		 * (newline) character.
		 */
		protected void decodeLineSuffix(PushbackInputStream aStream, OutputStream bStream) throws IOException { }

		/**
		 * This method does an actual decode. It takes the decoded bytes and
		 * writes them to the OutputStream. The integer <i>l</i> tells the
		 * method how many bytes are required. This is always <= bytesPerAtom().
		 */
		protected void decodeAtom(PushbackInputStream aStream, OutputStream bStream, int l) throws IOException {
			throw new IOException();
		}

		/**
		 * This method works around the bizarre semantics of BufferedInputStream's
		 * read method.
		 */
		protected int readFully(InputStream in, byte buffer[], int offset, int len)
				throws java.io.IOException {
			for (int i = 0; i < len; i++) {
				int q = in.read();
				if (q == -1)
					return ((i == 0) ? -1 : i);
				buffer[i+offset] = (byte)q;
			}
			return len;
		}

		/**
		 * Decode the text from the InputStream and write the decoded
		 * octets to the OutputStream. This method runs until the stream
		 * is exhausted.
		 * @exception IOException An error has occurred while decoding or the input stream is unexpectedly out of data
		 */
		public void decodeBuffer(InputStream aStream, OutputStream bStream) throws IOException {
			int     i;

			PushbackInputStream ps = new PushbackInputStream (aStream);
			decodeBufferPrefix(ps, bStream);
			while (true) {
				int length;

				try {
					length = decodeLinePrefix(ps, bStream);
					for (i = 0; (i+bytesPerAtom()) < length; i += bytesPerAtom()) {
						decodeAtom(ps, bStream, bytesPerAtom());
					}
					if ((i + bytesPerAtom()) == length) {
						decodeAtom(ps, bStream, bytesPerAtom());
					} else {
						decodeAtom(ps, bStream, length - i);
					}
					decodeLineSuffix(ps, bStream);
				} catch (IOException e) {
					break;
				}
			}
			decodeBufferSuffix(ps, bStream);
		}

		/**
		 * Alternate decode interface that takes a String containing the encoded
		 * buffer and returns a byte array containing the data.
		 * @exception CEFormatException An error has occurred while decoding
		 */
		public byte decodeBuffer(String inputString)[] throws IOException {
			byte    inputBuffer[] = new byte[inputString.length()];
			ByteArrayInputStream inStream;
			ByteArrayOutputStream outStream;

			inputBuffer = inputString.getBytes();
			inStream = new ByteArrayInputStream(inputBuffer);
			outStream = new ByteArrayOutputStream();
			decodeBuffer(inStream, outStream);
			return (outStream.toByteArray());
		}

		/**
		 * Decode the contents of the inputstream into a buffer.
		 */
		public byte decodeBuffer(InputStream in)[] throws IOException {
			ByteArrayOutputStream outStream = new ByteArrayOutputStream();
			decodeBuffer(in, outStream);
			return (outStream.toByteArray());
		}

		/**
		 * Decode the contents of the String into a ByteBuffer.
		 */
		public ByteBuffer decodeBufferToByteBuffer(String inputString)
				throws IOException {
			return ByteBuffer.wrap(decodeBuffer(inputString));
		}

		/**
		 * Decode the contents of the inputStream into a ByteBuffer.
		 */
		public ByteBuffer decodeBufferToByteBuffer(InputStream in)
				throws IOException {
			return ByteBuffer.wrap(decodeBuffer(in));
		}
	}

