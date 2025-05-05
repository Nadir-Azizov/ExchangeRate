namespace BambooCard.Tests.Helper;

public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_responder(request));
}
