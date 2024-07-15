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
        try
        {
            await Task.Run(() =>
            {
                UriNavigationService.Navigate(new Uri(request.Uri));
            });
        }
        // TODO: 捕获 notfound 的情况
        catch (Exception ex)
        {
            return new UriNavigationScRsp()
            {
                RetCode = UriNavigationRetCodes.NavigationError,
                Message = ex.Message
            };
        }

        return new UriNavigationScRsp()
        {
            RetCode = UriNavigationRetCodes.Ok,
            Message = "OK"
        };
    }
}