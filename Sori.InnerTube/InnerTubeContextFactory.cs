namespace InnerTube;

public sealed class InnerTubeContextFactory
{
    private readonly InnerTubeOptions _options;

    public InnerTubeContextFactory(InnerTubeOptions options)
    {
        _options = options;
    }

    public object CreateContext()
    {
        return new
        {
            client = new
            {
                clientName = _options.ClientName,
                clientVersion = _options.ClientVersion,
                hl = _options.H1,
                gl = _options.G1
            }
        };
    }
}