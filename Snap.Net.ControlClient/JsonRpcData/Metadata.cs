using System;
using System.Collections.Generic;
using System.Text;

namespace Snap.Net.ControlClient.JsonRpcData
{
    /// <summary>
    /// see https://github.com/badaix/snapcast/blob/master/doc/json_rpc_api/stream_plugin.md#pluginstreamplayergetproperties
    /// </summary>
    public class Metadata
    {
        /// <summary>
        ///  Base64 encoded image representing the track or album. if artUrl is not specified, Snapserver will decode and cache the image, and will publish the image via artUrl.
        /// </summary>
        public ArtData artData { get; set; }

        /// <summary>
        ///  The location of an image representing the track or album. Clients should not assume this will continue to exist when the media player stops giving out the URL.
        /// </summary>
        public string artUrl { get; set; }

        /// <summary>
        /// The track title.
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// The duration of the song in seconds; may contain a fractional part.
        /// </summary>
        public float duration { get; set; }

        /// <summary>
        /// A unique identity for this track within the context of an MPRIS object (eg: tracklist).
        /// </summary>
        public string trackId { get; set; }

        /// <summary>
        /// The current song.
        /// </summary>
        public string file { get; set; }

        /// <summary>
        /// The track artist(s).
        /// </summary>
        public string[] artist { get; set; }

        /// <summary>
        /// Same as artist, but for sorting. This usually omits prefixes such as “The”.
        /// </summary>
        public string[] artistSort { get; set; }

        /// <summary>
        /// The album name.
        /// </summary>
        public string album { get; set; }

        /// <summary>
        /// Same as album, but for sorting.
        /// </summary>
        public string albumSort { get; set; }

        /// <summary>
        /// The album artist(s).
        /// </summary>
        public string[] albumArtist { get; set; }

        /// <summary>
        /// Same as albumartist, but for sorting.
        /// </summary>
        public string[] albumArtistSort { get; set; }

        /// <summary>
        /// A name for this song. This is not the song title. The exact meaning of this tag is not well-defined. It is often used by badly configured internet radio stations with broken tags to squeeze both the artist name and the song title in one tag.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The song’s release date. This is usually a 4-digit year.
        /// </summary>
        public string date { get; set; }

        /// <summary>
        /// The song’s original release date.
        /// </summary>
        public string originalDate { get; set; }

        /// <summary>
        /// The composer(s) of the track.
        /// </summary>
        public string[] composer { get; set; }

        /// <summary>
        /// The artist who performed the song.
        /// </summary>
        public string performer { get; set; }

        /// <summary>
        /// The conductor who conducted the song.
        /// </summary>
        public string conductor { get; set; }

        /// <summary>
        /// "a work is a distinct intellectual or artistic creation, which can be expressed in the form of one or more audio recordings"
        /// </summary>
        public string work { get; set; }

        /// <summary>
        /// used if the sound belongs to a larger category of sounds/music (from the IDv2.4.0 TIT1 description).
        /// </summary>
        public string grouping { get; set; }

        /// <summary>
        /// A (list of) freeform comment(s)
        /// </summary>
        public string[] comment { get; set; }

        /// <summary>
        /// The name of the label or publisher.
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// The artist id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzArtistId { get; set; }

        /// <summary>
        /// The album id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzAlbumId { get; set; }

        /// <summary>
        /// The album artist id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzAlbumArtistId { get; set; }

        /// <summary>
        /// The track id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzTrackId { get; set; }

        /// <summary>
        /// The release track id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzReleaseTrackId { get; set; }

        /// <summary>
        /// The work id in the MusicBrainz database.
        /// </summary>
        public string musicbrainzWorkId { get; set; }

        /// <summary>
        /// The lyricist(s) of the track
        /// </summary>
        public string[] lyrics { get; set; }

        /// <summary>
        /// The speed of the music, in beats per minute.
        /// </summary>
        public int bpm { get; set; }

        /// <summary>
        /// An automatically-generated rating, based on things such as how often it has been played. This should be in the range 0.0 to 1.0.
        /// </summary>
        public float autoRating { get; set; }

        /// <summary>
        /// Date/Time: When the track was created. Usually only the year component will be useful.
        /// </summary>
        public string contentCreated { get; set; }

        /// <summary>
        /// The disc number on the album that this track is from.
        /// </summary>
        public int discNumber { get; set; }

        /// <summary>
        /// Date/Time When the track was first played.
        /// </summary>
        public string firstUsed { get; set; }

        /// <summary>
        /// List of Strings: The genre(s) of the track.
        /// </summary>
        public string[] genre { get; set; }

        /// <summary>
        /// Date/Time: When the track was last played.
        /// </summary>
        public string lastUsed { get; set; }

        /// <summary>
        /// The lyricist(s) of the track.
        /// </summary>
        public string[] lyricist { get; set; }

        /// <summary>
        /// The track number on the album disc.
        /// </summary>
        public string trackNumber { get; set; }

        /// <summary>
        /// The location of the media file.
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// The number of times the track has been played.
        /// </summary>
        public int useCount { get; set; }

        /// <summary>
        /// A user-specified rating. This should be in the range 0.0 to 1.0.
        /// </summary>
        public float userRating { get; set; }

        /// <summary>
        /// The Spotify Artist ID (https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids)
        /// </summary>
        public string spotifyArtistId { get; set; }

        /// <summary>
        /// The Spotify Track ID (https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids)
        /// </summary>
        public string spotifyTrackId { get; set; }

        public string GetNowPlaying()
        {
            string nowPlaying = "";
            string[] artists = albumArtist;
            if (artists == null)
            {
                artists = artist;
            }

            string artistText = "";
            if (artists != null)
            {
                artistText = string.Join(", ", artists);
            }

            if (string.IsNullOrEmpty(artistText) == false)
            {
                nowPlaying = artistText;
            }

            if (string.IsNullOrEmpty(title) == false)
            {
                if (string.IsNullOrEmpty(nowPlaying) == false)
                {
                    nowPlaying += $" - {title}";
                }
                else
                {
                    nowPlaying = title;
                }
            }

            if (duration > 0 && string.IsNullOrEmpty(nowPlaying) == false)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(duration);
                nowPlaying += $" ({timeSpan.Minutes:D2}:{timeSpan.Seconds:D2})";
            }

            return nowPlaying;
        }
    }

    public class ArtData
    {
        public string data { get; set; } // Base64 encoded image
        public string extension { get; set; } // The image file extension (e.g. "png", "jpg", "svg")
    }
}
