using Ardalis.SmartEnum;

namespace TrackManagement.Domain.Tracks;

public sealed class Genre : SmartEnum<Genre>
{
    public static readonly Genre Pop = new("Pop", 1);
    public static readonly Genre HipHop = new("Hip-Hop", 2);
    public static readonly Genre Rock = new("Rock", 3);
    public static readonly Genre Electronic = new("Electronic", 4);
    public static readonly Genre RnB = new("R&B", 5);
    public static readonly Genre Jazz = new("Jazz", 6);
    public static readonly Genre Classical = new("Classical", 7);
    public static readonly Genre ArabicPop = new("Arabic Pop", 8);
    public static readonly Genre Khaleeji = new("Khaleeji", 9);
    public static readonly Genre Mahraganat = new("Mahraganat", 10);
    public static readonly Genre Rai = new("Rai", 11);
    public static readonly Genre Tarab = new("Tarab", 12);
    public static readonly Genre Indie = new("Indie", 13);

    private Genre(string name, int value) : base(name, value) { }
}
