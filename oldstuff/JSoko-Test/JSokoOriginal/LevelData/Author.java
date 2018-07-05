/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2014 by Matthias Meger, Germany
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
package de.sokoban_online.jsoko.leveldata;

import de.sokoban_online.jsoko.resourceHandling.Texts;

/**
 * Data for an author.
 * <p>
 * This class manages all data for an author of a level, level collection, ...
 * This class is immutable.
 */
public final class Author {

	private final int    databaseID;  // ID of the author in the database
	private final String name;
	private final String email;
	private final String websiteURL;
	private final String comment;

	/**
	 * Creates a new author using the passed data.
	 *
	 * @param databaseID  the ID of the author in the database or {@link Database#NO_ID} if the author isn't stored in the database
	 * @param name  the name of the author
	 * @param email  the e-mail address of the author
	 * @param websiteURL  the url of the web site of the author
	 * @param comment  a comment for the author
	 */
	public Author(int databaseID, String name, String email, String websiteURL, String comment) {
		this.databaseID = databaseID < 0 ? Database.NO_ID : databaseID;
		this.name 		= name 	   	 == null ? Texts.getText("unknown") : name;
		this.email 		= email 	 == null ? "" : email;
		this.websiteURL = websiteURL == null ? "" : websiteURL;
		this.comment  	= comment    == null ? "" : comment;
	}


	/**
	 * Creates a new "empty" author.
	 */
	public Author() {
		this(Database.NO_ID, null, null, null, null);
	}

	/**
	 * Builder for building an {@code Author}.
	 */
	public static class Builder {
		private int    databaseID = Database.NO_ID;
		private String name = Texts.getText("unknown");
		private String email = "";
		private String websiteURL = "";
		private String comment = "";


		/**
		 * Sets the database ID of the author constructed by this builder.
		 *
		 * @param databaseID  the ID of this author in the database
		 * @return the builder object
		 */
		public Builder setDatabaseID(int databaseID) {
			this.databaseID = databaseID < 0 ? Database.NO_ID : databaseID;
			return this;
		}

		/**
		 * Sets the name of the {@code Author} to be constructed by this builder.
		 *
		 * @param name  name of the {@code Author}
		 * @return the builder object
		 */
		public Builder setName(String name) {
			if(name != null && !name.isEmpty()) {
				this.name = name;
			}
			return this;
		}

		/**
		 * Sets the email address of the {@code Author} to be constructed by this builder.
		 *
		 * @param email  email address of the {@code Author}
		 * @return the builder object
		 */
		public Builder setEmail(String email) {
			this.email = email == null ? "" : email;
			return this;
		}

		/**
		 * Sets the website url of the {@code Author} to be constructed by this builder.
		 *
		 * @param websiteURL  website address of the {@code Author}
		 * @return the builder object
		 */
		public Builder setWebsiteURL(String websiteURL) {
			this.websiteURL = websiteURL == null ? "" : websiteURL;
			return this;
		}

		/**
		 * Sets the comment text of the {@code Author} constructed by this builder.
		 *
		 * @param comment the comment text to be set
		 * @return the builder object
		 */
		public Builder setComment(String comment) {
			this.comment = comment == null ? "" : comment;
			return this;
		}

		/**
		 * Creates an instance of {@link LevelCollection} based on the properties set on this builder.
		 *
		 * @return the created {@code LevelCollection}
		 */
		public Author build() {
			return new Author(this);
		}
	}

	/**
	 * Creates a new {@code Author} object for managing all data of an author.
	 *
	 * @param builder builder object containing the data to build the {@code Author}
	 */
	private Author(Builder builder) {
		databaseID 	= builder.databaseID;
		name 	 	= builder.name;
		email 		= builder.email;
		websiteURL	= builder.websiteURL;
		comment 	= builder.comment;
	}

	/**
	 * Creates a builder for this {@code Author} for creating a new {@code Author}.
	 * <p>
	 * getBuilder().build() would result in clone of this author.
	 *
	 * @return the {@code Builder}
	 */
	public Builder getBuilder() {
		return new Builder()
				.setDatabaseID(databaseID)
				.setName(name)
				.setEmail(email)
				.setWebsiteURL(websiteURL)
				.setComment(comment);
	}

	/**
	 * Returns the ID of the author in the database.
	 *
	 * @return ID in the database or {@code Database.NO_ID} if the author isn't stored in the database.
	 */
	public int getDatabaseID() {
		return databaseID;
	}

	/**
	 * Sets the ID in the database.
	 *
	 * @param databaseID  the ID to be set
	 * @return the resulting {@code Author}
	 */
	public Author setDatabaseID(int databaseID) {
		return getBuilder().setDatabaseID(databaseID).build();
	}

	/**
	 * Returns the name of the author.
	 *
	 * @return name of the author
	 */
	public String getName() {
		return name;
	}

	/**
	 * Returns the e-mail address of the author.
	 *
	 * @return e-mail address of the author
	 */
	public String getEmail() {
		return email;
	}

	/**
	 * Returns the URL of the author's website.
	 *
	 * @return URL of the author's website
	 */
	public String getWebsiteURL() {
		return websiteURL;
	}

	/**
	 * Returns the comment stored for the author.
	 *
	 * @return comment for the author
	 */
	public String getComment() {
		return comment;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + ((name == null) ? 0 : name.hashCode());
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj) {
			return true;
		}
		if (obj == null) {
			return false;
		}
		if (getClass() != obj.getClass()) {
			return false;
		}
		Author other = (Author) obj;
		if (name == null) {
			if (other.name != null) {
				return false;
			}
		} else if (!name.equals(other.name)) {
			return false;
		}
		return true;
	}

	@Override
	public String toString() {
		return getName();
	}
}
