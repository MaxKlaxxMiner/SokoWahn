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

#region # using *.*

using System.Collections.Generic;
using JSoko.ResourceHandling;

#endregion

namespace JSoko.Leveldata
{
  /// <summary>
  /// Manages the data of a level collection.
  /// 
  /// This includes:
  /// - the data which is found in the DB (table collectionData, except "lastChanged")
  /// - the list of contained levels (tables collectionLevel and levelData)
  /// </summary>
  public class LevelCollection // implements Iterable<Level> {
  {
    /// <summary>
    /// The ID of the collection in the database.
    /// </summary>
    private int databaseId;

    /// <summary>
    /// The file this collection is located on the hard disk as String.
    /// </summary>
    private string collectionFile;

    /// <summary>
    /// The data of all levels of this collection as ArrayList.
    /// </summary>
    private List<Level> collectionLevels;

    /// <summary>
    /// Title of the collection.
    /// </summary>
    private string title;

    /// <summary>
    /// Comment of the collection.
    /// </summary>
    private string comment;

    /// <summary>
    /// Author of the collection.
    /// </summary>
    private Author author;

    /// <summary>
    /// Flag indicating whether this collection stores delegate levels.
    /// </summary>
    private bool isDelegateLevelsCollection;

    /// <summary>
    /// Builder for building a level collection.
    /// </summary>
    public class Builder
    {
      private int databaseId = Database.NoId;
      private string collectionFile = "";
      private List<Level> collectionLevels = new List<Level>();
      private string title = Texts.GetText("unknown");
      private string comment = "";
      private Author author = new Author();
      private bool isDelegateLevelsCollection = false;

      //    /**
      //     * Sets the database ID of the collection constructed by this builder.
      //     *
      //     * @param databaseID  the ID of this collection in the database
      //     * @return the builder object
      //     */
      public Builder SetDatabaseID(int databaseID)
      {
            this.databaseId = databaseID < 0 ? Database.NO_ID : databaseID;
            return this;
       }

      //    /**
      //     * Sets the file path of the file the collection to be constructed has been loaded from.
      //     *
      //     * @param collectionFile path of the file the collection has been loaded from
      //     * @return the builder object
      //     */
      //    public Builder setCollectionFile(String collectionFile) {
      //      this.collectionFile = collectionFile == null ? "" : collectionFile;
      //      return this;
      //    }

      //    /**
      //     * Sets the title of the collection constructed by this builder.
      //     *
      //     * @param title the title to be set
      //     * @return the builder object
      //     */
      //    public Builder setTitle(String title) {
      //      this.title = title == null ? "" : title;
      //      return this;
      //    }

      //    /**
      //     * Sets the comment text of the collection constructed by this builder.
      //     *
      //     * @param comment the comment text to be set
      //     * @return the builder object
      //     */
      //    public Builder setComment(String comment) {
      //      this.comment = comment == null ? "" : comment;
      //      return this;
      //    }

      //    /**
      //     * Sets the {@code Author} of the collection constructed by this builder.
      //     *
      //     * @param author the author data of this collection
      //     * @return the builder object
      //     */
      //    public Builder setAuthor(Author author) {
      //      this.author = author == null ? new Author() : author;
      //      return this;
      //    }

      //    /**
      //     * Sets the levels for the level collection constructed by this builder.
      //     *
      //     * @param levels the {@code Level}s of this collection
      //     * @return the builder object
      //     */
      //    public Builder setLevels(List<Level> levels) {
      //      this.collectionLevels = Collections.unmodifiableList(levels == null ? new ArrayList<Level>() : new ArrayList<Level>(levels));

      //      // Calculate the level numbers.
      //      int levelNumber = 1;
      //      for(Level level : collectionLevels) {
      //        level.setNumber(levelNumber++);
      //      }

      //      return this;
      //    }

      //    /**
      //     * Sets the levels for the level collection constructed by this builder.
      //     *
      //     * @param levels the {@code Level}s of this collection
      //     * @return the builder object
      //     */
      //    public Builder setLevels(Level ... levels) {
      //      this.collectionLevels = Collections.unmodifiableList(Arrays.asList(levels));
      //      return this;
      //    }

      //    /**
      //     * Marks this collection as collection for storing delegate levels which
      //     * are automatically stored in the DB by JSoko itself.
      //     *
      //     * @param isDelegate indicating whether this is a delegate levels collection
      //     * @return the builder object
      //     */
      //    public Builder setDelegate(boolean isDelegate) {
      //      this.isDelegateLevelsCollection = isDelegate;
      //      return this;
      //    }

      //    /**
      //     * Creates an instance of {@link LevelCollection} based on the properties set on this builder.
      //     *
      //     * @return the created {@code LevelCollection}
      //     */
      //    public LevelCollection build() {
      //      return new LevelCollection(this);
      //    }
    }

    //  /**
    //   * Creates a new object for managing all data of a level collection.
    //   *
    //   * @param builder builder object containing the data to build the {@code LevelCollection}
    //   */
    //  private LevelCollection(Builder builder) {
    //    databaseID 		 			= builder.databaseID;
    //    collectionFile 	 			= builder.collectionFile;
    //    title 			 			= builder.title;
    //    comment 		 			= builder.comment;
    //    author 			 		   	= builder.author;
    //    collectionLevels 		   	= builder.collectionLevels;
    //    isDelegateLevelsCollection 	= builder.isDelegateLevelsCollection;
    //  }

    //  /**
    //   * Creates a builder for this {@code LevelCollection} for creating a new {@code LevelCollection}.
    //   * <p>
    //   * getBuilder().build() would result in clone of this level collection.
    //   *
    //   * @return the {@code Builder}
    //   */
    //  public Builder getBuilder() {
    //    return new Builder().setDatabaseID(databaseID)
    //        .setCollectionFile(collectionFile)
    //        .setTitle(title)
    //        .setComment(comment)
    //        .setAuthor(author)
    //        .setLevels(collectionLevels)
    //        .setDelegate(isDelegateLevelsCollection);
    //  }

    //  /**
    //   * Returns the number of levels of this collection.
    //   *
    //   * @return	number of Levels
    //   */
    //  public int getLevelsCount() {
    //    return collectionLevels.size();
    //  }

    //  /**
    //   * Returns the <code>LevelData</code> object of the level represented
    //   * by the passed level number.
    //   *
    //   * @param levelNo the number of the level to be returned (first is 1)
    //   * @return the <code>LevelData</code> of the requested level,
    //   *         or {@code null}
    //   */
    //  public Level getLevel(int levelNo) {

    //    if (levelNo < 1 || levelNo > collectionLevels.size()) {
    //      return null;
    //    }

    //    return collectionLevels.get(levelNo - 1);
    //  }

    //  /**
    //   * Returns the level number of the passed {@code Level} in this collection.
    //   *
    //   * @param level  level to return the number for
    //   * @return levelNo the number of the level or -1 if it isn't part of this collection
    //   */
    //  public int getLevelNo(Level level) {
    //    return collectionLevels.indexOf(level)+1;
    //  }

    //  /**
    //   * Returns a {@code List} of the levels of this level collection.
    //   *
    //   * @return a {@code List} of the levels of this level collection
    //   */
    //  public List<Level> getLevels() {
    //    return new ArrayList<Level>(collectionLevels);
    //  }

    //  /**
    //   * Returns an array of Strings containing all level names of this collection.
    //   *
    //   * @return <code>String</code> array containing the names of all levels
    //   */
    //  public List<String> getLevelTitles() {

    //    ArrayList<String> levelNames = new ArrayList<String>(collectionLevels.size());

    //    for (Level levelData : collectionLevels) {
    //      levelNames.add(levelData.getTitle());
    //    }

    //    return levelNames;
    //  }

    //  /**
    //   * Returns the file this collection is stored in as <code>String</code>.
    //   *
    //   * @return the file this collection is stored in
    //   */
    //  public String getFile() {
    //    return collectionFile;
    //  }

    //  /**
    //   * Returns the title of this collection.
    //   *
    //   * @return the collection title
    //   */
    //  public String getTitle() {
    //    return title;
    //  }

    //  /**
    //   * Returns the collection ID in the database.
    //   *
    //   * @return the collectionID
    //   */
    //  public int getDatabaseID() {
    //    return databaseID;
    //  }

    //  /**
    //   * Sets the passed database ID.
    //   *
    //   * @param databaseID  ID to be set
    //   * @return the resulting {@code LevelCollection}
    //   */
    //  public LevelCollection setDatabaseID(int databaseID) {
    //    return getBuilder().setDatabaseID(databaseID).build();
    //  }

    //  /**
    //   * Returns the author of this collection.
    //   *
    //   * @return the collections author
    //   */
    //  public Author getAuthor() {
    //    return author;
    //  }

    //  /**
    //   * Sets the passed {@code Author} as new author.
    //   *
    //   * @param author {@code Author} to be set
    //   * @return the resulting {@code LevelCollection}
    //   */
    //  public LevelCollection setAuthor(Author author) {
    //    return getBuilder().setAuthor(author).build();
    //  }

    //  /**
    //   * Returns the comment of this collection.
    //   *
    //   * @return the collection comment
    //   */
    //  public String getComment() {
    //    return comment;
    //  }

    //  /**
    //   * Whether this level collection is "connected" with the data base.
    //   *
    //   * @return whether we know the data base ID
    //   */
    //  public boolean isConnectedWithDatabase() {
    //    return getDatabaseID() != -1;
    //  }

    //  /**
    //   * Returns whether this collection is the collection which contains the
    //   * delegate levels which have been saved automatically by JSoko.
    //   *
    //   * @return
    //   */
    //  public boolean isDelegateLevelsCollection() {
    //    return isDelegateLevelsCollection;
    //  }

    //  /**
    //   * Returns the level with the specified database ID.
    //   *
    //   * @param levelID data base ID to look for
    //   * @return {@code null}, or our level with the ID
    //   */
    //  public Level getLevelByID(int levelID) {
    //    for (Level level : collectionLevels) {
    //      if (level.getDatabaseID() == levelID) {
    //        return level;
    //      }
    //    }
    //    return null;
    //  }

    //  /**
    //   * Sets the file this collation is stored on the hard disk as {@code String}.
    //   * <p>
    //   * Example: /games/Sokoban/levels/mycollection.sok
    //   *
    //   * @param filePath  file path of the file this collection is stored in
    //   * @return the resulting {@code LevelCollection}
    //   */
    //  public LevelCollection setFileLocation(String filePath) {
    //    return getBuilder().setCollectionFile(filePath).build();
    //  }

    //  /**
    //   * Returns whether this collection contains any levels.
    //   *
    //   * @return <code>true</code> if there is at least one {@code Level} in this collection,
    //   *  <code>false</code> otherwise
    //   */
    //  public boolean isEmpty() {
    //    return getLevelsCount() == 0;
    //  }

    //  @Override
    //  public Iterator<Level> iterator() {
    //    return collectionLevels.iterator();
    //  }

    //  @Override
    //  public int hashCode() {
    //    final int prime = 31;
    //    int result = 1;
    //    result = prime * result + ((author == null) ? 0 : author.hashCode());
    //    result = prime * result + ((collectionLevels == null) ? 0 : collectionLevels.hashCode());
    //    result = prime * result + ((title == null) ? 0 : title.hashCode());
    //    return result;
    //  }

    //  @Override
    //  public boolean equals(Object obj) {
    //    if (this == obj) {
    //      return true;
    //    }
    //    if (obj == null) {
    //      return false;
    //    }
    //    if (getClass() != obj.getClass()) {
    //      return false;
    //    }
    //    LevelCollection other = (LevelCollection) obj;
    //    if (author == null) {
    //      if (other.author != null) {
    //        return false;
    //      }
    //    } else if (!author.equals(other.author)) {
    //      return false;
    //    }
    //    if (collectionLevels == null) {
    //      if (other.collectionLevels != null) {
    //        return false;
    //      }
    //    } else if (!collectionLevels.equals(other.collectionLevels)) {
    //      return false;
    //    }
    //    if (title == null) {
    //      if (other.title != null) {
    //        return false;
    //      }
    //    } else if (!title.equals(other.title)) {
    //      return false;
    //    }
    //    return true;
    //  }

    //  @Override
    //  public String toString() {
    //    return "LevelCollection [collectionFile=" + collectionFile + ", title=" + title + "]";
    //  }

    //}
  }
}
