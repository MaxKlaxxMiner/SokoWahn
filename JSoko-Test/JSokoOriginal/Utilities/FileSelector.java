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

import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.event.ActionEvent;
import java.io.File;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Stack;
import java.util.regex.Pattern;

import javax.swing.AbstractAction;
import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTextField;
import javax.swing.SwingWorker;
import javax.swing.tree.DefaultMutableTreeNode;
import javax.swing.tree.DefaultTreeModel;
import javax.swing.tree.TreePath;


import com.jidesoft.swing.CheckBoxTree;
import com.jidesoft.swing.DefaultOverlayable;
import com.jidesoft.swing.FolderChooser;
import com.jidesoft.swing.InfiniteProgressPanel;
import com.jidesoft.swing.JideTitledBorder;
import com.jidesoft.swing.PartialEtchedBorder;
import com.jidesoft.swing.PartialSide;
import com.jidesoft.swing.SearchableUtils;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.resourceHandling.Texts;


/**
 * Class for offering the user a tree of files from which the user can select relevant files.
 */
public class FileSelector {

	/** The tree showing the collections to be selected by the user. */
	CheckBoxTree collectionSelectionTree = null;
	
	/** The pattern all relevant files must match. */
	String relevantFilesPattern = null;
		
	/** Overlayable for the tree that shows the files. */
	DefaultOverlayable overlayTree;
	
	/** SwingWorker for loading the files to be displayed in the tree. */
	SwingWorker<Void, DefaultMutableTreeNode[]> loadingFilesWorker = null;
	
	
	/**
	 * Class for showing specific files to the user who can select the relevant files. 
	 */
	@SuppressWarnings("serial")
	public FileSelector() {
		
		// Create the tree showing the collections.
		collectionSelectionTree = new CheckBoxTree() {
			@Override
			public Dimension getPreferredScrollableViewportSize() {
				return new Dimension(600, 600);
			}
		};		
		collectionSelectionTree.setShowsRootHandles(true);	    
		collectionSelectionTree.setClickInCheckBoxOnly(false);
		
		// The user should be able to search for specific files by typing their names.
		SearchableUtils.installSearchable(collectionSelectionTree);
	
		// Set a model.
		collectionSelectionTree.setModel(new DefaultTreeModel(new DefaultMutableTreeNode()));
	}
		
	
	/**
	 * Shows all files in the passed start folder and its sub-folders
	 * in a tree and returns the files the user has selected in this tree.
	 * 
	 * @param application the <code>Component</code> the dialogs are to be shown in
	 * @param startFolder  root folder to search for files to shown
	 * @param relevantFilesPattern pattern the relevant files must match
	 *                            (e.g.: ".*\\.sok$|.*\\.txt$") for sok and txt files
	 * @return the selected files
	 */
	public ArrayList<File> getSelectedFiles(JSoko application, File startFolder, String relevantFilesPattern) {
						
		// Save the relevant files pattern in a class variable.
		this.relevantFilesPattern = relevantFilesPattern;
		
		// Create a main panel for showing the GUI.
		JPanel mainPanel = new JPanel(new BorderLayout(2, 2));
		mainPanel.setBorder(
				BorderFactory.createCompoundBorder(
						new JideTitledBorder(
								new PartialEtchedBorder(PartialEtchedBorder.LOWERED,
										                PartialSide.NORTH           ),
								Texts.getText("collectionsToBeImported"),
								JideTitledBorder.LEADING,
								JideTitledBorder.ABOVE_TOP),
						BorderFactory.createEmptyBorder(6, 0, 0, 0))
		);
		   
	    mainPanel.add(createFolderBrowserPanel(startFolder), BorderLayout.NORTH);
	    overlayTree = new DefaultOverlayable(new JScrollPane(collectionSelectionTree));
	    mainPanel.add(overlayTree, BorderLayout.CENTER);
							    
	    // Create the tree for the start folder.
	    updateTree(startFolder);
	    
		// Show a model dialog so the user has to select the relevant collections.
		int result = JOptionPane.showOptionDialog(
									application, mainPanel,
									Texts.getText("selectRelevantCollections"),
									JOptionPane.OK_CANCEL_OPTION,
									JOptionPane.PLAIN_MESSAGE,
									null, null, null);
		
		// Ensure there isn't any long running loading process active.
		if(loadingFilesWorker != null) {
			loadingFilesWorker.cancel(true);
		}
		
		return result == JOptionPane.OK_OPTION ? getSelectedFiles() : new ArrayList<File>();		
	}
	
	/**
	 * Returns a <code>CheckBoxTree</code> displaying all files matching the passed pattern.
	 * 
	 * @param startFolder  root folder to search for files to shown
	 * @param relevantFilesPattern  pattern the relevant files must match (for instance: ".*\\.sok$|.*\\.txt$") for sok and txt files
	 */
	/**
	 * @param startFolder
	 */
	private void updateTree(final File startFolder) {
		
		// Create the pattern for filtering the files.
		final Pattern pattern = Pattern.compile(relevantFilesPattern, Pattern.CASE_INSENSITIVE);		

		// Stores all folders and their corresponding tree nodes.
		final HashMap<File, DefaultMutableTreeNode> folderNodes = new HashMap<File, DefaultMutableTreeNode>();
		
		// The root node of the tree.
		DefaultMutableTreeNode treeNode = (DefaultMutableTreeNode) collectionSelectionTree.getModel().getRoot();
		
		// Remove all nodes and any selections.
		treeNode.removeAllChildren();
		collectionSelectionTree.getCheckBoxTreeSelectionModel().clearSelection();
		
		// At the current start folder for the root node.
		treeNode.setUserObject(startFolder);
		
		// Save the node for the start folder.
		folderNodes.put(startFolder, treeNode);
		
		// While the tree is loaded the user mustn't select any
		// already loaded nodes. Hence, set the tree disabled.
		collectionSelectionTree.setEnabled(false);
		
		// Display a progress panel for showing the user that the tree is still being created.		
		@SuppressWarnings("serial")
		final InfiniteProgressPanel progressPanel = new InfiniteProgressPanel() {
			@Override
			public Dimension getPreferredSize() {
				return new Dimension(20, 20);
			}
		};
		overlayTree.addOverlayComponent(progressPanel);
		progressPanel.start();

		// Interrupt the thread updating the tree if there is already one.
		if(loadingFilesWorker != null)
			loadingFilesWorker.cancel(true);

		// Create a SwingWorker for adding all relevant files to the tree.
		loadingFilesWorker = new SwingWorker<Void, DefaultMutableTreeNode[]>() {

			@Override
			protected Void doInBackground() throws Exception {

				DefaultMutableTreeNode newNode = null;
				
				// Stack for storing all found directories that have to be searched for relevant collection files.
				Stack<File> foldersToBeSearchedForFiles = new Stack<File>(); 

				// Push the start folder to the stack.
				foldersToBeSearchedForFiles.push(startFolder); 
				
				// As long as there are more directories: add all files having the correct filename extension to the tree.
				while (foldersToBeSearchedForFiles.size() > 0) {

					// Get the File of the folder and its tree node. 
					File currentFolder = foldersToBeSearchedForFiles.pop();
					DefaultMutableTreeNode currentFolderNode = folderNodes.get(currentFolder);
					
					// Search for files and other folders in the current folder.
					for (File file : currentFolder.listFiles()) {
						
						// Immediately return if this thread has been canceled.
						if(isCancelled())
							return null;
						
						if (file.isDirectory()) {
							foldersToBeSearchedForFiles.push(file); 				// Add the found folder as folder to be searched for files
							newNode = new DefaultMutableTreeNode(file.getName());	// Create a tree node for the folder
							folderNodes.put(file, newNode);							// Save the tree node for this folder
							publish(new DefaultMutableTreeNode[] {currentFolderNode, newNode}); // Add the folder to the tree
						}
						else {
							// Check whether this file matches the pattern. If yes, add it to the folder.
							if (pattern.matcher(file.getName()).matches()) {
								@SuppressWarnings("serial")
								DefaultMutableTreeNode node = new DefaultMutableTreeNode(file) {
									@Override
									public String toString() {
										return ((File) getUserObject()).getName();
									}
								};
								publish(new DefaultMutableTreeNode[] {currentFolderNode, node}); // Add the folder to the tree   
							}
						}
					}		
				}

				return null;
			}			

			@Override
			protected void process(List<DefaultMutableTreeNode[]> nodesList) {

				boolean isUIUpdateNecessary = false;
				
				// Add the new file to the tree. The nodes are passed as follows:
				// nodesList[0] = parent node, nodesList[1] = new child for parent
				for(DefaultMutableTreeNode[] nodes : nodesList) {
					nodes[0].add(nodes[1]);

					// The root node and its direct children are shown.
					// If one of them is changed the change is immediately
					// displayed to the user.
					DefaultMutableTreeNode parentNode = (DefaultMutableTreeNode) nodes[0].getParent();
					if(parentNode == null || parentNode.isRoot())
						isUIUpdateNecessary = true;
				}

				// Update the UI, so the new nodes are shown and displayed in full string length.
				if(isUIUpdateNecessary == true)
					collectionSelectionTree.updateUI();
			}
			
			@Override
			protected void done() {
								
				// Get an iterator for all folders shown in the tree.
				Iterator<DefaultMutableTreeNode> folderNodesIterator = folderNodes.values().iterator();
				
				// Folders without relevant files are removed from the tree.
				while(folderNodesIterator.hasNext()) {
				
					DefaultMutableTreeNode folderNode = folderNodesIterator.next();
					
					while(folderNode.isLeaf() && !folderNode.isRoot()) {						
						DefaultMutableTreeNode nodeToBeRemoved = folderNode;						
						folderNode = (DefaultMutableTreeNode) folderNode.getParent();						
						nodeToBeRemoved.removeFromParent();						
					}
				}
				
				// Set the "Loading ..." information invisible and enable the tree again.
				overlayTree.removeOverlayComponent(progressPanel);
				progressPanel.stop();
				collectionSelectionTree.setEnabled(true);
			}
        };
        Utilities.executor.submit(loadingFilesWorker);
	}
	
	
	/**
	 * Creates a panel for showing the browse button for selecting the relevant folder to be shown in the tree.
	 * 
	 * @param folder  the initial folder
	 * @return the panel for browsing the folders
	 */
	@SuppressWarnings("serial")
	private JPanel createFolderBrowserPanel(final File folder) {
		
		// TextField for displaying the chosen folder path.
		final JTextField chosenFolderTextField = new JTextField(folder.getPath(), 40);
		chosenFolderTextField.setEditable(false);
		
		// Create the FolderChooser of this GUI.
		final FolderChooser fc = new FolderChooser();
		fc.setDialogTitle(Texts.getText("chooseFolder"));
		fc.setRecentListVisible(false);
		
		// Create a browse button for opening the FolderChooser.
		final JButton browseButton = new JButton("Browse");
		browseButton.setMnemonic('B');
		browseButton.addActionListener(new AbstractAction() {
			@SuppressWarnings("synthetic-access")
			public void actionPerformed(ActionEvent e) {
				fc.setCurrentDirectory(folder);
				fc.setFileHidingEnabled(true);
				int result = fc.showOpenDialog(browseButton.getTopLevelAncestor());
				if (result == FolderChooser.APPROVE_OPTION) {
					File newFolder = fc.getSelectedFile();
					chosenFolderTextField.setText(newFolder == null ? "" : newFolder.toString());
					
					// Update the tree by using the selected folder as root folder.
					updateTree(newFolder);
				}
			}
		});
		browseButton.setRequestFocusEnabled(false);
		browseButton.setFocusable(false);
		
		// Create the panel containing a text field showing the selected folder path
		// and a browse button.
		JPanel panel = new JPanel(new BorderLayout(2, 2));
		panel.add(browseButton, BorderLayout.AFTER_LINE_ENDS);
		panel.add(chosenFolderTextField, BorderLayout.CENTER);

		return panel;
	}
	
	/**
	 * Returns all selected files in the passed tree as an <code>ArrayList&#60;File></code>.
	 * 
	 * @return the selected files
	 */
	private ArrayList<File> getSelectedFiles() {

		ArrayList<File> selectedFiles = new ArrayList<File>();

		// Collect the selected files in "selectedFiles".
		TreePath[] treePaths = collectionSelectionTree.getCheckBoxTreeSelectionModel().getSelectionPaths();
		if (treePaths != null) {

			Stack<DefaultMutableTreeNode> treeNodes = new Stack<DefaultMutableTreeNode>();
			DefaultMutableTreeNode treeNode = null;

			for (TreePath path : treePaths) {

				treeNodes.add((DefaultMutableTreeNode) path.getLastPathComponent());
				while(treeNodes.size() > 0) {
					treeNode = treeNodes.pop();
					if(treeNode.isLeaf()) {
						selectedFiles.add((File) treeNode.getUserObject());
					} else {
						for(Enumeration<DefaultMutableTreeNode> en = treeNode.children(); en.hasMoreElements(); )
							treeNodes.add(0, en.nextElement());
					}
				}
			}
		}

		return selectedFiles;
	}
}