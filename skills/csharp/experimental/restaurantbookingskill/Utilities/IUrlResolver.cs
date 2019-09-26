namespace RestaurantBookingSkill.Utilities
{
    public interface IUrlResolver
    {
        string ServerUrl { get; }

        string GetImageUrl(string imagePath);
    }
}
