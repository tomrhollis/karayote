namespace Karayote.Models
{
    public interface ISongRequest
    {
        DateTime RequestTime { get; }
        User Singer { get; }
        string? ExtraName { get; set; } // for if they're singing with someone, or signed up on the tablet
        ISong Song { get; }


    }
}
