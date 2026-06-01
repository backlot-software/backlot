// ReSharper disable UnusedAutoPropertyAccessor.Global : used by the external api.
namespace Backlot.Services.Postmark;

internal class EmailMessage
{
    public string Bcc { get; set; }
    public string Cc { get; set; }
    public string From { get; set; }
    public object To { get; set; }
    public string ReplyTo { get; set; }
    public object Subject { get; set; }
    public object TextBody { get; set; }
    public object HtmlBody { get; set; }
    public string MessageStream { get; set; }
}