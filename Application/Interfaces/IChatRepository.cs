using Application.Dtos;

namespace Application.Interfaces;

public interface IChatRepository
{
    Task<bool> ChatExistsAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task CreateChatAsync(CreateChatDto chat, CancellationToken cancellationToken = default);
    Task<GetChatDto?> GetChatAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<List<GetChatDto>> GetChatsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GetChatDto?> GetPrivateChatBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);
    Task UpdateChatNameAsync(Guid chatId, UpdateChatNameDto updateChatNameDto, CancellationToken cancellationToken = default);
    Task DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default);
}
