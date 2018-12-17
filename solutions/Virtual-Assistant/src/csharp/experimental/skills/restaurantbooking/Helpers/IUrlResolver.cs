namespace RestaurantBooking.Helpers
{
    public interface IUrlResolver
    {
        string ServerUrl { get; }

        string GetImageUrl(string imagePath);
    }
}
