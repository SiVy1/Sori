using InnerTube.Auth;

namespace Sori.InnerTube.Tests.Auth;

public class BrowserHeadersParserTests
{
    [Fact]
    public void WithValidHeaders_ReturnsCredentials()
    {
        var raw = @"
accept: */*
authorization: SAPISIDHASH fake_hash_123
content-type: application/json
x-goog-authuser: 0
x-origin: https://music.youtube.com
cookie: SID=fake; HSID=fake; SSID=fake;
";

        var credentials = BrowserHeadersParser.Parse(raw);

        Assert.True(credentials.IsValid);
        Assert.Equal("SAPISIDHASH fake_hash_123", credentials.Authorization);
        Assert.Equal("SID=fake; HSID=fake; SSID=fake;", credentials.Cookie);
        Assert.Equal("0", credentials.XGoogAuthUser);
        Assert.Equal("https://music.youtube.com", credentials.XOrigin);
    }

    [Fact]
    public void WithLowercaseHeaders_ReturnsCredentials()
    {
        var raw = @"
authorization: SAPISIDHASH lower_hash
cookie: SID=lower; HSID=lower;
";

        var credentials = BrowserHeadersParser.Parse(raw);

        Assert.True(credentials.IsValid);
        Assert.Equal("SAPISIDHASH lower_hash", credentials.Authorization);
    }

    [Fact]
    public void MissingAuthorization_Throws()
    {
        var raw = @"
cookie: SID=fake;
x-origin: https://music.youtube.com
";

        Assert.Throws<InvalidOperationException>(() => BrowserHeadersParser.Parse(raw));
    }

    [Fact]
    public void MissingCookie_Throws()
    {
        var raw = @"
authorization: SAPISIDHASH fake
x-origin: https://music.youtube.com
";

        Assert.Throws<InvalidOperationException>(() => BrowserHeadersParser.Parse(raw));
    }

    [Fact]
    public void DefaultsAuthUserToZero()
    {
        var raw = @"
authorization: SAPISIDHASH no_auth_user
cookie: SID=test;
";

        var credentials = BrowserHeadersParser.Parse(raw);

        Assert.Equal("0", credentials.XGoogAuthUser);
        Assert.Equal("https://music.youtube.com", credentials.XOrigin);
    }
}
