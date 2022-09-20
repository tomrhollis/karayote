namespace Karayote.Models
{
    public interface ISong
    {
        string Title { get; } // combine artist into title if Karafun
        float Duration { get; } // youtube comes in ms I think, will have to convert
    }
}
