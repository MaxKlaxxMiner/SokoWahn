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
package de.sokoban_online.jsoko.utilities;

import java.awt.AlphaComposite;
import java.awt.Color;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.Transparency;
import java.awt.image.BufferedImage;

import javax.swing.Icon;
import javax.swing.ImageIcon;

import com.jidesoft.icons.ColorFilter;

/**
 * This class provides a mixture of helper functions and tools used throughout JSoko for graphics.
 */
public class GraphicUtilities {

	/**
	 * Makes a color transparent in a <code>BufferedImage</code>.
	 *
	 * @param image image to be made transparent
	 * @param color color to be make transparent
	 * @return <code>BufferedImage</code> with transparent color
	 * @see #makeColorTransparent00(BufferedImage)
	 */
	public static BufferedImage makeColorTransparent(BufferedImage image, Color color) {

		// Be sure something has been passed.
		if(image == null || color == null) {
			return null;
		}

		BufferedImage imageWithTransparency = new BufferedImage(image.getWidth(),
				                                                image.getHeight(),
				                                                BufferedImage.TYPE_INT_ARGB);
		Graphics2D g = imageWithTransparency.createGraphics();
		g.setComposite(AlphaComposite.Src);
		g.drawImage(image, null, 0, 0);				// copy "image" to new object
		g.dispose();

		// Now we set the alpha component to 0 (totally transparent) at selected pixels.
		// That is possible since we use TYPE_INT_ARGB (see above).
		// See also: http://en.wikipedia.org/wiki/RGBA_color_space
		final int height = imageWithTransparency.getHeight();
		final int width  = imageWithTransparency.getWidth();
		final int oldRGB = color.getRGB();
		final int newRGB = (oldRGB & 0x00FFFFFF);	// delete alpha byte
		for(int i = 0; i < height; i++) {
			for(int j = 0; j < width; j++) {
				if( imageWithTransparency.getRGB(j, i) == oldRGB ) {
					imageWithTransparency.setRGB(j, i, newRGB);
				}
			}
		}
		return imageWithTransparency;
	}

	/**
	 * Makes a color transparent in a <code>BufferedImage</code>.
	 * The color is taken from the first pixel of the image (at (0,0)).
	 *
	 * @param image image to be made transparent
	 * @return <code>BufferedImage</code> with transparent color
	 * @see #makeColorTransparent(BufferedImage, Color)
	 */
	public static BufferedImage makeColorTransparent00(BufferedImage image) {
		return makeColorTransparent(image, new Color(image.getRGB(0, 0)));
	}

	/**
	 * Returns a transparent version of the passed <code>BufferedImage</code>.
	 *
	 * @param image image to be made transparent
	 * @param transperancy value of the transparency
	 * @return transparent <code>BufferedImage</code>
	 */
	public static BufferedImage getTransparentImage(BufferedImage image, float transperancy) {

		// Create the image using the
		BufferedImage transparentImage = new BufferedImage(image.getWidth(), image.getHeight(), Transparency.TRANSLUCENT);

		// Get the images graphics.
		Graphics2D g = transparentImage.createGraphics();

		// Set the graphics composite to Alpha.
		g.setComposite(AlphaComposite.getInstance(AlphaComposite.SRC_OVER, transperancy));

		// Draw the passed image into the new transparent image.
		g.drawImage(image, null, 0, 0);

		// Free all system resources.
		g.dispose();

		// Return the image
		return transparentImage;
	}

	/**
	 * Manipulates the lightness of the passed <code>BufferedImage</code>.
	 *
	 * @param bi			<code>BufferedImage</code> to be manipulated
	 * @param changingValue	value the colors are changed by
	 */
	public static void changeLightness(BufferedImage bi, int changingValue) {

		int width  = bi.getWidth();
		int height = bi.getHeight();
		for (int x = 0; x < width; ++x) {
			for (int y = 0; y < height; ++y) {

				int rgbValue = bi.getRGB(x, y);
				Color color = changeLightness(new Color(rgbValue), changingValue);

				bi.setRGB(x, y, color.getRGB());
			}
		}

	}

	/**
	 * Returns a new <code>BufferedImage</code> where the colors are manipulates by the passed
	 * changingValue.
	 *
	 * @param bi  the source <code>BufferedImage</code>
	 * @param changingValue	 value the colors are changed by
	 * @return the new created <code>BufferedImage</code>
	 */
	public static BufferedImage getLightnessChangedImage(BufferedImage bi, int changingValue) {

		BufferedImage newImage = new BufferedImage(bi.getColorModel(), bi.getRaster().createCompatibleWritableRaster(),
				 				bi.getColorModel().isAlphaPremultiplied(), null);
		 Graphics2D context = newImage.createGraphics();
		 context.drawImage(bi, 0, 0, null);
		 changeLightness(newImage, changingValue);

		 return newImage;
	}

	/**
	 * Returns a new <code>Color</code> by adding the passed changingValue to the the passed color.
	 *
	 * @param color the color to be taken as basis color
	 * @param changingValue value of which the color is changed
	 * @return created {@code Color}
	 */
	public static Color changeLightness(Color color, int changingValue) {

		// Get the color values.
		int red   = color.getRed();
		int green = color.getGreen();
		int blue  = color.getBlue();

		// Change the color values.
		red   += changingValue;
		green += changingValue;
		blue  += changingValue;

		// Check value ranges.
		blue  = blue  > 255 ? 255 : blue;
		red   = red   > 255 ? 255 : red;
		green = green > 255 ? 255 : green;
		red   = red   < 0   ? 0 : red;
		green = green < 0   ? 0 : green;
		blue  = blue  < 0   ? 0 : blue;

		return new Color(red, green, blue);
	}

	/**
	 * Returns an <code>ImageIcon</code> which is a lightened version of the passed <code>Icon</code>.
	 *
	 * @param icon  the basis icon which is copied to the returned icon
	 * @param changingValue value the colors are changed by (-255 to +255)
	 * @return new icon which changed lightness
	 */
	public static ImageIcon createIconImageAndChangeLightness(Icon icon, int changingValue) {

		BufferedImage b = new BufferedImage(icon.getIconWidth(),icon.getIconHeight(),BufferedImage.TYPE_INT_ARGB);
		icon.paintIcon(null, b.createGraphics(),0,0);

		changeLightness(b, changingValue);

		return new ImageIcon(b);
	}

	/**
	 * Returns an <code>ImageIcon</code> which is a lightened version of the passed <code>Icon</code>.
	 *
	 * @param icon  the basis icon which is copied to the returned icon
	 * @param percentage percentage the returned image icon is lightened
	 * @return new icon which changed lightness
	 */
	public static ImageIcon createBrigtherIconImage(Icon icon, int percentage) {

		BufferedImage b = new BufferedImage(icon.getIconWidth(),icon.getIconHeight(),BufferedImage.TYPE_INT_ARGB);
		icon.paintIcon(null, b.createGraphics(), 0, 0);

		return new ImageIcon(ColorFilter.createBrighterImage(b, percentage));
	}

	/**
	 * Returns an <code>ImageIcon</code> which is a lightened version of the passed <code>Icon</code>.
	 *
	 * @param icon  the basis icon which is copied to the returned icon
	 * @param percentage percentage the returned image icon is lightened
	 * @return new icon which changed lightness
	 */
	public static ImageIcon createDarkerIconImage(Icon icon, int percentage) {

		BufferedImage b = new BufferedImage(icon.getIconWidth(),icon.getIconHeight(),BufferedImage.TYPE_INT_ARGB);
		icon.paintIcon(null, b.createGraphics(), 0, 0);

		return new ImageIcon(ColorFilter.createDarkerImage(b, percentage));
	}

	/**
	 * Returns a scaled instance of the provided <code>BufferedImage</code>.
	 * <p>
	 * To achieve high quality the scaling is done in multiple steps.
	 *
	 * @param bi the original image to be scaled
	 * @param targetWidth the desired width of the scaled instance, in pixels
	 * @param targetHeight the desired height of the scaled instance, in pixels
	 *
	 * @return a scaled version of the original <code>BufferedImage</code>
	 */
	public final static BufferedImage getScaledInstance(BufferedImage bi, int targetWidth, int targetHeight) {

		// Be sure the passed image is not null.
		if(bi == null) {
			return null;
		}

		int width  = bi.getWidth();
		int height = bi.getHeight();

		BufferedImage bufferedImage = bi;

		// Scale the image until it has the proper size.
		do {
			width >>= 1;
			if (width < targetWidth) {
				width = targetWidth;
			}

			height >>= 1;
			if (height < targetHeight) {
				height = targetHeight;
			}

			int imageType = (bi.getType() != 0) ? bi.getType()
					                            : BufferedImage.TYPE_4BYTE_ABGR ;
			BufferedImage tmp = new BufferedImage(width, height, imageType);

			Graphics2D g2 = tmp.createGraphics();
			g2.setRenderingHint( RenderingHints.KEY_INTERPOLATION, RenderingHints.VALUE_INTERPOLATION_BICUBIC );
			g2.setRenderingHint( RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON );
			g2.setRenderingHint( RenderingHints.KEY_ALPHA_INTERPOLATION, RenderingHints.VALUE_ALPHA_INTERPOLATION_QUALITY );
			g2.setRenderingHint( RenderingHints.KEY_COLOR_RENDERING, RenderingHints.VALUE_COLOR_RENDER_QUALITY );
			g2.setRenderingHint( RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY );
			g2.drawImage(bufferedImage, 0, 0, width, height, null);
			g2.dispose();

			bufferedImage = tmp;
		} while (width != targetWidth || height != targetHeight);

		// Return the scaled image.
		return bufferedImage;
	}

}
