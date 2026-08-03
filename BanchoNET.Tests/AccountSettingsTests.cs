using System.IO.Compression;
using System.Text;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BanchoNET.Tests;

[TestFixture]
public class PlaystyleTests
{
    [Test]
    public void ToNames_ReturnsEverySetFlag()
    {
        Assert.That((Playstyle.Mouse | Playstyle.Tablet).ToNames(), Is.EqualTo(new[] { "mouse", "tablet" }));
        Assert.That(Playstyle.None.ToNames(), Is.Empty);
    }

    [Test]
    public void TryParseNames_RoundTrips()
    {
        Assert.That(PlaystyleExtensions.TryParseNames(["keyboard", "touch"], out var style), Is.True);
        Assert.That(style, Is.EqualTo(Playstyle.Keyboard | Playstyle.Touch));
        Assert.That(style.ToNames(), Is.EqualTo(new[] { "keyboard", "touch" }));
    }

    [Test]
    public void TryParseNames_RejectsUnknownName()
    {
        Assert.That(PlaystyleExtensions.TryParseNames(["mouse", "feet"], out _), Is.False);
    }

    [Test]
    public void TryParseNames_IgnoresBlankEntries()
    {
        // The clear-all case posts a single empty value.
        Assert.That(PlaystyleExtensions.TryParseNames([""], out var style), Is.True);
        Assert.That(style, Is.EqualTo(Playstyle.None));
    }
}

[TestFixture]
public class FormValuesTests
{
    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("on", true)]
    [TestCase("0", false)]
    [TestCase("false", false)]
    [TestCase("", false)]
    public void ParseBool_AcceptsTheFormsClientsActuallySend(string input, bool expected)
    {
        Assert.That(RequestValues.ParseBool(input), Is.EqualTo(expected));
    }

    [Test]
    public void ParseBool_NullMeansNotSubmitted()
    {
        Assert.That(RequestValues.ParseBool(null), Is.Null);
    }
}

[TestFixture]
public class ImageFormatSnifferTests
{
    private static byte[] Png(int width, int height)
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        WriteBigEndian(ihdr, 0, width);
        WriteBigEndian(ihdr, 4, height);
        ihdr[8] = 8;
        ihdr[9] = 2;

        WriteChunk(stream, "IHDR"u8.ToArray(), ihdr);
        WriteChunk(stream, "IDAT"u8.ToArray(), Deflate([0]));
        WriteChunk(stream, "IEND"u8.ToArray(), []);

        return stream.ToArray();
    }

    private static byte[] Gif(int width, int height) =>
    [
        .. "GIF89a"u8.ToArray(),
        (byte)(width & 0xFF), (byte)(width >> 8),
        (byte)(height & 0xFF), (byte)(height >> 8),
        0, 0, 0
    ];

    private static byte[] Jpeg(int width, int height)
    {
        using var stream = new MemoryStream();
        stream.Write([0xFF, 0xD8]);

        // APP0 segment, skipped while walking to the frame header
        stream.Write([0xFF, 0xE0, 0x00, 0x10]);
        stream.Write(new byte[14]);

        // SOF0
        stream.Write([0xFF, 0xC0, 0x00, 0x11, 0x08]);
        stream.Write([(byte)(height >> 8), (byte)(height & 0xFF)]);
        stream.Write([(byte)(width >> 8), (byte)(width & 0xFF)]);
        stream.Write(new byte[10]);

        return stream.ToArray();
    }

    private static void WriteChunk(Stream stream, byte[] type, byte[] data)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, data.Length);

        stream.Write(length);
        stream.Write(type);
        stream.Write(data);
        stream.Write(new byte[4]);
    }

    private static void WriteBigEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
            deflate.Write(data);

        return output.ToArray();
    }

    [Test]
    public void Sniffs_Png()
    {
        Assert.That(ImageFormatSniffer.TrySniff(Png(640, 480), out var image), Is.True);
        Assert.That(image.Extension, Is.EqualTo("png"));
        Assert.That(image.Width, Is.EqualTo(640));
        Assert.That(image.Height, Is.EqualTo(480));
    }

    [Test]
    public void Sniffs_Gif()
    {
        Assert.That(ImageFormatSniffer.TrySniff(Gif(300, 200), out var image), Is.True);
        Assert.That(image.Extension, Is.EqualTo("gif"));
        Assert.That(image.Width, Is.EqualTo(300));
        Assert.That(image.Height, Is.EqualTo(200));
    }

    [Test]
    public void Sniffs_Jpeg_ByWalkingToTheFrameHeader()
    {
        Assert.That(ImageFormatSniffer.TrySniff(Jpeg(1920, 1080), out var image), Is.True);
        Assert.That(image.Extension, Is.EqualTo("jpg"));
        Assert.That(image.Width, Is.EqualTo(1920));
        Assert.That(image.Height, Is.EqualTo(1080));
    }

    [Test]
    public void Rejects_ExecutableRenamedToPng()
    {
        var exe = new byte[512];
        Encoding.ASCII.GetBytes("MZ").CopyTo(exe, 0);

        Assert.That(ImageFormatSniffer.TrySniff(exe, out _), Is.False);
    }

    [Test]
    public void Rejects_TruncatedPng()
    {
        Assert.That(ImageFormatSniffer.TrySniff(Png(64, 64)[..12], out _), Is.False);
    }

    [Test]
    public void Rejects_Empty()
    {
        Assert.That(ImageFormatSniffer.TrySniff([], out _), Is.False);
    }
}

[TestFixture]
public class NotificationOptionsModelBinderTests
{
    private static List<NotificationOptionInput> Bind(Dictionary<string, string[]> formValues)
    {
        var form = new FormCollection(formValues.ToDictionary(
            pair => pair.Key,
            pair => new Microsoft.Extensions.Primitives.StringValues(pair.Value)));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = form;

        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new Microsoft.AspNetCore.Mvc.ActionContext { HttpContext = httpContext }
        };

        new NotificationOptionsModelBinder().BindModelAsync(bindingContext).GetAwaiter().GetResult();

        return (List<NotificationOptionInput>)bindingContext.Result.Model!;
    }

    [Test]
    public void ParsesBooleanDetails()
    {
        var result = Bind(new Dictionary<string, string[]>
        {
            ["user_notification_options[0][name]"] = ["channel_message"],
            ["user_notification_options[0][details][mail]"] = ["0"],
            ["user_notification_options[0][details][push]"] = ["1"]
        });

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("channel_message"));
        Assert.That(result[0].Details["mail"], Is.EqualTo(false));
        Assert.That(result[0].Details["push"], Is.EqualTo(true));
    }

    [Test]
    public void ParsesRepeatedListDetails()
    {
        var result = Bind(new Dictionary<string, string[]>
        {
            ["user_notification_options[0][name]"] = ["news_post"],
            ["user_notification_options[0][details][series][]"] = ["osu", "dev"]
        });

        Assert.That(result[0].Details["series"], Is.EqualTo(new[] { "osu", "dev" }));
    }

    [Test]
    public void KeepsIndexOrderingAndIgnoresUnrelatedKeys()
    {
        var result = Bind(new Dictionary<string, string[]>
        {
            ["user_notification_options[1][name]"] = ["mapping"],
            ["user_notification_options[0][name]"] = ["comment_new"],
            ["user[user_from]"] = ["Warsaw"]
        });

        Assert.That(result.Select(o => o.Name), Is.EqualTo(new[] { "comment_new", "mapping" }));
    }

    [Test]
    public void EmptyListValueMeansClear()
    {
        var result = Bind(new Dictionary<string, string[]>
        {
            ["user_notification_options[0][name]"] = ["beatmapset_disqualify"],
            ["user_notification_options[0][details][modes][]"] = [""]
        });

        Assert.That(result[0].Details["modes"], Is.EqualTo(Array.Empty<string>()));
    }
}

[TestFixture]
public class FormErrorTests
{
    [Test]
    public void FromModelState_NestsOnBrackets()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("user[user_website]", "must be a valid url");
        modelState.AddModelError("user[user_from]", "is too long");

        var payload = FormError.FromModelState(modelState, "user");
        var errors = ErrorsOf(payload);

        Assert.That(errors["user"]["user_website"], Is.EqualTo(new[] { "must be a valid url" }));
        Assert.That(errors["user"]["user_from"], Is.EqualTo(new[] { "is too long" }));
    }

    [Test]
    public void FromModelState_UsesDefaultGroupForBracketlessKeys()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("cover_id", "is not a known cover");

        var errors = ErrorsOf(FormError.FromModelState(modelState, "user"));

        Assert.That(errors["user"]["cover_id"], Is.EqualTo(new[] { "is not a known cover" }));
    }

    [Test]
    public void FromModelState_GroupsSeparateContainers()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("user_profile_customization[beatmapset_download]", "requires supporter");

        var errors = ErrorsOf(FormError.FromModelState(modelState, "user"));

        Assert.That(errors, Does.ContainKey("user_profile_customization"));
        Assert.That(errors["user_profile_customization"]["beatmapset_download"],
            Is.EqualTo(new[] { "requires supporter" }));
    }

    [Test]
    public void Single_ProducesTheSameShape()
    {
        var errors = ErrorsOf(FormError.Single("user", "current_password", "is incorrect"));

        Assert.That(errors["user"]["current_password"], Is.EqualTo(new[] { "is incorrect" }));
    }

    private static Dictionary<string, Dictionary<string, List<string>>> ErrorsOf(object payload)
    {
        var property = payload.GetType().GetProperty("form_error")!;

        return (Dictionary<string, Dictionary<string, List<string>>>)property.GetValue(payload)!;
    }
}