using Grpc.Core;
using UserService.Protos;
using UserService.Services;

namespace UserService.GrpcServices;

public class UserGrpcService : UserGrpc.UserGrpcBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(IUserService userService, ILogger<UserGrpcService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetUserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetUser called for ID: {UserId}", request.Id);

        if (!Guid.TryParse(request.Id, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
        }

        var user = await _userService.GetByIdAsync(userId);

        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {request.Id} not found"));
        }

        return new GetUserReply
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            Email = user.Email
        };
    }
}
