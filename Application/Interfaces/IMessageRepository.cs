using Application.Dtos;

namespace Application.Interfaces;

public interface IMessageRepository
{
    Task CreateMessageAsync(CreateMessageDto message, CancellationToken cancellationToken = default);
    Task<GetMessageDto?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<List<GetMessageDto>> GetMessagesForChatAsync(
        Guid chatId,
        int limit = 50,
        DateTime? before = null,
        CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}
