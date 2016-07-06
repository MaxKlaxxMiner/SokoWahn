/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2012 by Matthias Meger, Germany
 * 
 *  This file is part of JSoko.
 *
 *    JSoko is free software; you can redistribute it and/or modify
 *    it under the terms of the GNU General Public License as published by
 *    the Free Software Foundation; either version 2 of the License, or
 *    (at your option) any later version.
 *    
 *    This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
package de.sokoban_online.jsoko.leveldata;

import java.text.ParseException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * Converts level data between different formats, such as MF8 and XSB.
 * 
 */
public abstract class FormatConverter {

    // characters that translate to one another must have matching indexes in these arrays
    private final static char[] ALPHABET_XSB = 
        { '@', '$', '#', '.', ' ', '+', '*', '\n' };
    // note that '-' means the same as '_' in MF8. also watch for 'X' in place of 'x'
    private final static char[] ALPHABET_MF8 =
        { 'a', '$', 'H', '.', '_', 'x', '*', '\n' };
    
    
    /**
     * Returns a string representation of a Sokoban level, in MF8 format.
     * If the input is MF8, it is returned as is.
     * 
     * @param inputString a level description, in XSB or MF8
     * @return the MF8 version of the level description 
     * @throws ParseException
     */
    public static String getMF8String( final String inputString ) throws ParseException {
        if ( isMF8( inputString ) )
            return inputString;
        
        return listToString( fromXSBtoMF8( Arrays.asList( inputString.split( "[\n]" ) ) ) );
    }
    
    /**
     * Returns a list representation of a Sokoban level, in MF8 format.
     * If the input is MF8, it is returned as is.
     * 
     * @param inputList  a level description, in XSB or MF8
     * @return the MF8 version of the level description
     * @throws ParseException
     */
    public static List<String> getMF8List( final List<String> inputList ) throws ParseException {
        final String inputAsString = listToString( inputList );
        if ( isMF8( inputAsString ) )
            return inputList;
        
        return fromXSBtoMF8( inputList );
    }
    
    
    /**
     * Returns a string representation of a Sokoban level, in XSB format.
     * If the input is XSB, it is returned as is.
     * 
     * @param inputString a level description, in XSB or MF8
     * @return the XSB version of the data
     * @throws ParseException
     */
    public static String getXSBString( final String inputString ) throws ParseException {
        if ( isXSB( inputString ) )
            return inputString;
        
        return listToString( fromMF8toXSB( Arrays.asList( inputString.split( "[\n]" ) ) ) );
    }
    
    
    /**
     * Returns a list representation of a Sokoban level, in XSB format.
     * If the input is XSB, it is returned as is.
     * 
     * @param inputList a level description, in XSB or MF8
     * @return the XSB version of the data
     * @throws ParseException
     */
    public static List<String> getXSBList( final List<String> inputList ) throws ParseException {
        final String inputAsString = listToString( inputList );
        if ( isXSB( inputAsString ) )
            return inputList;
        
        return fromMF8toXSB( inputList );
    }
    
    
    /**
     * Translates a list representation of a Sokoban level from XSB to MF8.
     * 
     * @param inputList in XSB
     * @return a list in MF8
     */
    private static List<String> fromXSBtoMF8( final List<String> inputList ) throws ParseException {
        final List<String> result = new ArrayList<String>();
        
        int topRow = 0;
        while ( ! inputList.get( topRow ).contains( "#" ) && topRow < inputList.size() - 1 )
            topRow++;
        
        // topRow is now the highest row containing "#", or bottom row if there is no such row
        
        // there must be a row with a "#" before the last row
        if ( topRow >= inputList.size() - 1 )
            throw new ParseException( "", 0 );
        
        int bottomRow = inputList.size() - 1;
        while ( ! inputList.get( bottomRow ).contains( "#" ) && bottomRow > 0 )
            bottomRow--;
        
        // bottomRow is now the lowest row containing "#", or 0 if there is no such row
        
        // there must be a row with a "#" below the top row with a "#"
        if ( bottomRow <= topRow )
            throw new ParseException( "", 0 );
        
        final int height = bottomRow - topRow + 1;
        final int width = getLongestStringLengthInRange( inputList, topRow, bottomRow );
        {
            StringBuilder sb = new StringBuilder();
            sb.append( "[soko=" ).append( width ).append( ',' ).append( height ).append( "]\n" );
            result.add( sb.toString() );
        }
        
        for ( int listIndex = topRow; listIndex <= bottomRow; listIndex++ ) {
            final String currentLine = inputList.get( listIndex );
            
            final StringBuilder sb2 = new StringBuilder();
            for ( int charIndex = 0; charIndex < currentLine.length(); charIndex++ ) {
                sb2.append( fromXSBtoMF8( currentLine.charAt( charIndex ) ) );
            }
            sb2.append( '\n' );
            result.add( sb2.toString() );
        }
        
        result.add( "[/soko]" );
        
        return result;
    }

    
    /**
     * Translates a single character from XSB to MF8.
     * 
     * @param toTranslate a single character in XSB
     * @return the corresponding MF8 character; '_' is always returned, not '-'
     */
    private static char fromXSBtoMF8( char toTranslate ) throws ParseException {
        for ( int i = 0; i < ALPHABET_XSB.length; i++ ) {
            if ( ALPHABET_XSB[ i ] == toTranslate ) {
                return ALPHABET_MF8[ i ];
            }
        }
        
        throw new ParseException( "" + toTranslate, 0 );
    }
    
    /**
     * Translates a single character from MF8 to XSB.
     * 
     * @param toTranslate a single character in MF8
     * @return the corresponding XSB character
     */
    private static char fromMF8toXSB( char toTranslate ) throws ParseException {
        // handle '-', which is not in the alphabetMF8 array
        if ( toTranslate == '-' )
            toTranslate = '_';
        // handle 'X', which is not in the alphabetMF8 array (and, strictly, should not appear)
        if ( toTranslate == 'X' )
            toTranslate = 'x';
        
        for ( int i = 0; i < ALPHABET_MF8.length; i++ ) {
            if ( ALPHABET_MF8[ i ] == toTranslate ) {
                return ALPHABET_XSB[ i ];
            }
        }
        
        throw new ParseException( "" + toTranslate, 0 );
    }

    /**
     * Returns the "width" of a list representation of a Sokoban level.
     * Does not include the width of metadata rows, only of Sokoban level rows.
     * 
     * @param inputList
     * @param firstIndex
     * @param lastIndex
     * @return
     */
    private static int getLongestStringLengthInRange( 
        final List<String> inputList,  
    final int firstIndex,
    final int lastIndex
    ) {
        int max = 0;
        
        for ( int index = firstIndex; index <= lastIndex; index++ ) {
            final String currentString = inputList.get( index );
            if ( currentString.length() > max )
                max = currentString.length();
        }
        
        return max;
    }
    
    
    /**
     * Translates a list representation of a Sokoban level from MF8 to XSB.
     * 
     * @param inputList in MF8
     * @return a list in XSB
     */
    private static List<String> fromMF8toXSB( final List<String> inputList ) throws ParseException {
        final List<String> result = new ArrayList<String>();
        
        // skip over header lines, including the first line with "[soko="
        int listIndex = 0;
        while ( ! inputList.get( listIndex ).contains( "[soko=" ) && listIndex < inputList.size()-1 )
            listIndex++;
        
        listIndex++;
        
        // begin translating at the first line after the header. stop before the footer.
        for ( ; listIndex < inputList.size(); listIndex++ ) {
            final String currentLine = inputList.get( listIndex );
            
            // stop translating before the footer line.
            if ( currentLine.contains( "[/soko]" ) )
                break;
            
            StringBuilder sb = new StringBuilder();
            for ( int i = 0; i < currentLine.length(); i++ ) {
                sb.append( fromMF8toXSB( currentLine.charAt( i ) ) );
            }
//            sb.append( '\n' );
            result.add( sb.toString() );
        }
        
        // error: no "[soko=" header, or no "[/soko]" footer
        if ( listIndex >= inputList.size() )
            throw new ParseException( null, 0 );
                
        return result;
    }
    

    /**
     * This method does not validate the data string, but simply
     * indicates that it is flagged as MF8 rather than XSB.
     * 
     * MF8 files must contain a leading [soko=width,height] tag.
     * 
     * @param dataString
     * @return 
     */
    private static boolean isMF8( final String dataString ) {
        return dataString.contains( "soko" ) && ! dataString.contains( "#" );
    }
    
    
    /**
     * This method does not validate the data string, but simply
     * indicates that it is flagged as XSB rather than MF8.
     * 
     * XSB files must contain "#" indicating walls.
     * 
     * @param dataString
     * @return
     */
    private static boolean isXSB( final String dataString ) {
        return dataString.contains( "#" ) && ! dataString.contains( "soko" );
    }

    
    /**
     * Returns the string produced by concatenating the items from a List<String> in order.
     * 
     * @param dataList the data list describing a level
     * @return the string form of the level description
     */
    private static String listToString( final List<String> dataList ) {
        final StringBuilder sb = new StringBuilder();
        
        for ( final String currentString: dataList ) {
            sb.append( currentString );
        }
        
        return sb.toString();
    }
    
    
    // test code
//    public static void main( String[] args ) {
//        String testString = "########\n#@ * $.#\n########\n";
//        List<String> testResult = Arrays.asList( testString.split( "[\n]" ) );
//        for ( String currentString: testResult )
//            System.out.println( currentString );
//        
//        try {
//            String mf8String = getMF8String( testString );
//        
//            System.out.println();
//            System.out.println( mf8String );
//            System.out.println();
//            System.out.println( getXSBString( mf8String ) );
//        } catch( ParseException ex ) {
//            ex.printStackTrace();
//        }
//        
//        System.out.println();
//        System.out.println();
//        
//        testString =   
//            "   ########" + "\n" +
//            "   #      #" + "\n" +
//            "   # $$#  ####" + "\n" +
//            "#### #  $    #" + "\n" +
//            "#   $#..##$$ #" + "\n" +
//            "#  # .+..  # #" + "\n" +
//            "# #  .... #  #" + "\n" +
//            "# $$##..#$   #" + "\n" +
//            "#    $  # ####" + "\n" +
//            "####  # $ #" + "\n" +
//            "   #   #$ #" + "\n" +
//            "   #      #" + "\n" +
//            "   ########";
//        testResult = Arrays.asList( testString.split( "[\n]" ) );
//        for ( String currentString: testResult )
//            System.out.println( currentString );
//        
//        try {
//            String mf8String = getMF8String( testString );
//        
//            System.out.println();
//            System.out.println( mf8String );
//            System.out.println();
//            System.out.println( getXSBString( mf8String ) );
//        } catch( ParseException ex ) {
//            ex.printStackTrace();
//        }
//        
//        System.out.println();
//        
//        testString = 
//            "Author: Thinking Rabbit" + "\n\n" +
//            "[soko=19,11]" + "\n" +
//            "____HHHHH__________" + "\n" + 
//            "____H___H__________" + "\n" +
//            "____H$__H__________" + "\n" +
//            "__HHH__$HH_________" + "\n" +
//            "__H__$_$_H_________" + "\n" +
//            "HHH_H_HH_H___HHHHHH" + "\n" +
//            "H___H_HH_HHHHH__..H" + "\n" +
//            "H_$__$__________..H" + "\n" +
//            "HHHHH_HHH_HaHH__..H" + "\n" +
//            "____H_____HHHHHHHHH" +"\n" +
//            "____HHHHHHH________" + "\n" +
//            "[/soko]" +"\n\n" +
//            "Title: Classic level 1";
//        testResult = Arrays.asList( testString.split( "[\n]" ) );
//        for ( String currentString: testResult ) {
//            System.out.println( currentString );
//        }
//        
//        try {
//            String xsbString = getXSBString( testString );
//        
//            System.out.println();
//            System.out.println( xsbString );
//            System.out.println();
//            System.out.println( getMF8String( xsbString ) );
//        } catch( ParseException ex ) {
//            ex.printStackTrace();
//        }
//        
//        try {
//        	java.util.Scanner scan = new java.util.Scanner( new java.io.File( "twoLevel" ) );
//            StringBuilder sb = new StringBuilder();
//            while ( scan.hasNextLine() ) {
//                sb.append( scan.nextLine() );
//                sb.append( "\n" );
//            }
//            System.out.println( sb.toString() );
//            
//            String xsbString = getXSBString( sb.toString() );
//            System.out.println();
//            System.out.println( xsbString );
//            System.out.println();
//            System.out.println( getMF8String( xsbString ) );
//            
//        } catch( Exception ex ) {
//            ex.printStackTrace();
//        }
//    }
    
}
