namespace CampusCore.Application.Abstractions;

public interface ICurrentUser
{
    string UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
