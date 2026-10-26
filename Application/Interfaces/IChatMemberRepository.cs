using Application.Dtos;
using Domain;

namespace Application.Interfaces;

public interface IChatMemberRepository
{
    Task AddMemberAsync(AddChatMemberDto member, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<GetChatMemberDto?> GetMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<GetChatMemberDto>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task UpdateMemberRoleAsync(Guid chatId, Guid userId, EChatMemberRole role, CancellationToken cancellationToken = default);
}
