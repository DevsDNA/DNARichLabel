namespace RichLabel.iOS
{
    public enum RichLabelKey
    {
        LinkType = 0,

        Range = 1,

        Link = 2
    }

    /**
     *  Constants for identifying link types we can detect
     */
    public enum RichLabelLinkType
    {
        /**
         *  Usernames starting with "@" token
         */
        UserHandle,

        /**
         *  Hashtags starting with "#" token
         */
        Hashtag,

        /**
         *  URLs, http etc
         */
        URL,

        /**
         *  Open menu, open modal, ... etc
         */
        Action
    }

    /**
     *  Flags for specifying combinations of link types as a bitmask
     */

    public enum RichLabelLinkTypeOption
    {
        /**
         *  No links
         */
        None,

        /**
         *  Specifies to include UserHandle links
         */
        UserHandle,

        /**
         *  Specifies to include Hashtag links
         */
        Hashtag,

        /**
         *  Specifies to include URL links
         */
        URL,

        /**
         *  Specifies to include Action links
         */
        Action,

        /**
         *  Convenience contstant to include all link types
         */
        All
    }
}
