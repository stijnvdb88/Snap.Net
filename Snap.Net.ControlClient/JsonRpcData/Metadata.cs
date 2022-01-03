using System;
using System.Collections.Generic;
using System.Text;

namespace Snap.Net.ControlClient.JsonRpcData
{
    
    public class Metadata
    {
        public ArtData artData { get; set; } // Base64 encoded image representing the track or album. if artUrl is not specified, Snapserver will decode and cache the image, and will publish the image via artUrl.
        public string artUrl { get; set; } // The location of an image representing the track or album. Clients should not assume this will continue to exist when the media player stops giving out the URL.
        public string title { get; set; } // The track title.
        public float duration { get; set; } // The duration of the song in seconds; may contain a fractional part.

        // see https://github.com/badaix/snapcast/blob/master/doc/json_rpc_api/stream_plugin.md#pluginstreamplayergetproperties
        public string trackId { get; set; } // A unique identity for this track within the context of an MPRIS object (eg: tracklist).
        public string file { get; set; } // The current song.
        public string[] artist { get; set; } //  The track artist(s).
        public string[] artistSort { get; set; } // Same as artist, but for sorting. This usually omits prefixes such as “The”.
        public string album { get; set; } // The album name.
        public string albumSort { get; set; } // Same as album, but for sorting.
        public string[] albumArtist { get; set; } // The album artist(s).
        public string[] albumArtistSort { get; set; } // Same as albumartist, but for sorting.
        public string name { get; set; } // A name for this song. This is not the song title. The exact meaning of this tag is not well-defined. It is often used by badly configured internet radio stations with broken tags to squeeze both the artist name and the song title in one tag.
        public string date { get; set; } // The song’s release date. This is usually a 4-digit year.
        public string originalDate { get; set; } // The song’s original release date.
        public string[] composer { get; set; } // The composer(s) of the track.
        public string performer { get; set; } // The artist who performed the song.
        public string conductor { get; set; } // The conductor who conducted the song.
        public string work { get; set; } // "a work is a distinct intellectual or artistic creation, which can be expressed in the form of one or more audio recordings"
        public string grouping { get; set; } // |used if the sound belongs to a larger category of sounds/music| (from the IDv2.4.0 TIT1 description).
        public string[] comment { get; set; } // A (list of) freeform comment(s)
        public string label { get; set; } // The name of the label or publisher.
        public string musicbrainzArtistId { get; set; } // The artist id in the MusicBrainz database.
        public string musicbrainzAlbumId { get; set; } // The album id in the MusicBrainz database.
        public string musicbrainzAlbumArtistId { get; set; } // The album artist id in the MusicBrainz database.
        public string musicbrainzTrackId { get; set; } // The track id in the MusicBrainz database.
        public string musicbrainzReleaseTrackId { get; set; } // The release track id in the MusicBrainz database.
        public string musicbrainzWorkId { get; set; } // The work id in the MusicBrainz database.
        public string[] lyrics { get; set; } // The lyricist(s) of the track
        public int bpm { get; set; } // The speed of the music, in beats per minute.
        public float autoRating { get; set; } // An automatically-generated rating, based on things such as how often it has been played. This should be in the range 0.0 to 1.0.
        public string contentCreated { get; set; } // Date/Time: When the track was created. Usually only the year component will be useful.
        public int discNumber { get; set; } // The disc number on the album that this track is from.
        public string firstUsed { get; set; } // Date/Time When the track was first played.
        public string[] genre { get; set; } // List of Strings: The genre(s) of the track.
        public string lastUsed { get; set; } // Date/Time: When the track was last played.
        public string[] lyricist { get; set; } // The lyricist(s) of the track.
        public string trackNumber { get; set; } // The track number on the album disc.
        public string url { get; set; } // The location of the media file.
        public int useCount { get; set; } // The number of times the track has been played.
        public float userRating { get; set; } // A user-specified rating. This should be in the range 0.0 to 1.0.
        public string spotifyArtistId { get; set; } // The Spotify Artist ID (https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids)
        public string spotifyTrackId { get; set; } // The Spotify Track ID (https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids)
    }

    public class ArtData
    {
        public string data { get; set; } // Base64 encoded image
        public string extension { get; set; } // The image file extension (e.g. "png", "jpg", "svg")
    }
}
