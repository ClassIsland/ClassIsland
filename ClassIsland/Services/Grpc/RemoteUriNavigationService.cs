using System;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.IPC.Protobuf.Client;
using ClassIsland.Shared.IPC.Protobuf.Enum;
using ClassIsland.Shared.IPC.Protobuf.Server;
using Grpc.Core;

namespace ClassIsland.Services.Grpc;

public class RemoteUriNavigationService(IUriNavigationService uriNavigationService) : Shared.IPC.Protobuf.Service.UriNavigationService.UriNavigationServiceBase
{
    private IUriNavigationService UriNavigationService { get; } = uriNavigationService;
    
    public override async Task<UriNavigationScRsp> Navigate(UriNavigationScReq request, ServerCallContext context)
    {
        Exception? exception = null;
        await Task.Run(() =>
        {
            UriNavigationService.NavigateWrapped(new Uri(request.Uri), out exception);
        });
        if (exception != null)
        {
            return new UriNavigationScRsp()
            {
                RetCode = UriNavigationRetCodes.NavigationError,
                Message = exception.Message
            };
        }
        
        return new UriNavigationScRsp()
        {
            RetCode = UriNavigationRetCodes.Ok,
            Message = "OK"
        };
    }
}