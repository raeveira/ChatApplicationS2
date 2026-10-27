using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class MessageService(
    IMessageRepository messageRepository,
    IChatMemberRepository chatMemberRepository)
{
    public async Task<GetMessageDto> SendMessageAsync(
        Guid currentUserId,
        Guid chatId,
        string content,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        // The domain validates and trims the content.
        var message = Message.Create(currentUserId, chatId, content);

        await messageRepository.CreateMessageAsync(new CreateMessageDto
        {
            Id = message.Id,
            SentById = message.SentById,
            SentInId = message.SentInId,
            Content = message.Content,
            SentAt = message.SentAt
        }, cancellationToken);

        return new GetMessageDto
        {
            Id = message.Id,
            SentById = message.SentById,
            SentInId = message.SentInId,
            Content = message.Content,
            SentAt = message.SentAt
        };
    }

    // Returns the latest messages, oldest first. Pass the SentAt of the last message there is
    // as "onlyNewerThan" to fetch just the new ones (refresh).
    public async Task<List<GetMessageDto>> GetMessagesAsync(
        Guid currentUserId,
        Guid chatId,
        DateTime? onlyNewerThan = null,
        int maxMessages = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        var newestFirst = await messageRepository.GetMessagesForChatAsync(
            chatId,
            limit: maxMessages,
            after: onlyNewerThan,
            cancellationToken: cancellationToken);

        newestFirst.Reverse();
        return newestFirst;
    }

    // Loads older messages for scrolling back. Pass the SentAt of the oldest message there is.
    public async Task<List<GetMessageDto>> GetOlderMessagesAsync(
        Guid currentUserId,
        Guid chatId,
        DateTime olderThan,
        int maxMessages = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        var newestFirst = await messageRepository.GetMessagesForChatAsync(
            chatId,
            limit: maxMessages,
            before: olderThan,
            cancellationToken: cancellationToken);

        newestFirst.Reverse();
        return newestFirst;
    }

    public async Task DeleteMessageAsync(
        Guid currentUserId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await messageRepository.GetMessageAsync(messageId, cancellationToken)
            ?? throw new MessageNotFoundException();

        // Authors can delete their own messages; chat admins can delete anyone's.
        if (message.SentById != currentUserId)
        {
            var member = await chatMemberRepository.GetMemberAsync(message.SentInId, currentUserId, cancellationToken)
                ?? throw new NotChatMemberException();

            if (member.Role != EChatMemberRole.Admin)
                throw new NotChatAdminException();
        }

        await messageRepository.DeleteMessageAsync(messageId, cancellationToken);
    }

    private async Task EnsureIsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await chatMemberRepository.IsMemberAsync(chatId, userId, cancellationToken))
            throw new NotChatMemberException();
    }
}
