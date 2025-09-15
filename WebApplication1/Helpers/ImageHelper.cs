using System.Text.RegularExpressions;

namespace UserRegisterApi.Helpers;

public static class ImageHelper
{
    private static readonly Regex DataUrlHeader =
        new(@"^data:(?<mime>image\/(png|jpeg|jpg|webp));base64,", RegexOptions.IgnoreCase);

    public static (byte[] bytes, string ext) DecodeBase64Image(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Empty image string");

        string ext = ".png";
        string base64 = input;

        var m = DataUrlHeader.Match(input);
        if (m.Success)
        {
            var mime = m.Groups["mime"].Value.ToLower();
            ext = mime switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/webp" => ".webp",
                _ => ".png"
            };
            base64 = input.Substring(m.Value.Length);
        }

        var bytes = Convert.FromBase64String(base64);
        return (bytes, ext);
    }
}
