namespace Karayote.Models
{
    public interface ISongRequest
    {
        DateTime RequestTime { get; }
        public string UserId { get; set; }
        User User { get; }

        string? ExtraName { get; set; } // for if they're singing with someone, or signed up on the tablet
        ISong Song { get; }


    }
}
