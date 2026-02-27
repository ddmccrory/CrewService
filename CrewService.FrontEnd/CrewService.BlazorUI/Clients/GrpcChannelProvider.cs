using Grpc.Net.Client;
using Grpc.Net.Client.Web;

namespace CrewService.BlazorUI.Clients;

public sealed class GrpcChannelProvider : IDisposable
{
    private readonly GrpcChannel _channel;

    public GrpcChannelProvider(IConfiguration configuration)
    {
        var baseAddress = configuration["CrewServiceApiUrl"] ??
            throw new Exception("CrewServiceApiUrl is not defined.");

        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());

        _channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
    }

    public GrpcChannel Channel => _channel;

    public void Dispose() => _channel.Dispose();
}
