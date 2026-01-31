namespace CrewService.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    string GetUserName();
}