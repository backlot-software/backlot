namespace Backlot.Studio.Services;

public class BacklotApiUnauthorizedException : Exception
{
    public BacklotApiUnauthorizedException()
        : base("The Backlot API returned 401 Unauthorized.") { }
}
