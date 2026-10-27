using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class ChatService(
    IChatRepository chatRepository,
    IChatMemberRepository chatMemberRepository,
    IUserRepository userRepository)
{
    public async Task<GetChatDto> CreateGroupChatAsync(
        Guid currentUserId,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        var chat = Chat.Create(EChatType.Group, groupName);
        await SaveChatAsync(chat, cancellationToken);

        // The creator becomes the admin of the group.
        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Admin, cancellationToken);

        return ToChatDto(chat);
    }

    public async Task<GetChatDto> CreatePrivateChatAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == otherUserId)
            throw new ArgumentException("You cannot start a private chat with yourself.", nameof(otherUserId));

        if (!await userRepository.UserExistsAsync(otherUserId, cancellationToken))
            throw new UserNotFoundException();

        var chat = Chat.Create(EChatType.Private, null);
        await SaveChatAsync(chat, cancellationToken);

        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Member, cancellationToken);
        await AddMemberAsync(chat.Id, otherUserId, EChatMemberRole.Member, cancellationToken);

        return ToChatDto(chat);
    }

    public Task<List<GetChatDto>> GetMyChatsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        return chatRepository.GetChatsForUserAsync(currentUserId, cancellationToken);
    }

    public async Task<GetChatDto> GetChatAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        return await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();
    }

    public async Task<List<GetChatMemberDto>> GetMembersAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        return await chatMemberRepository.GetMembersAsync(chatId, cancellationToken);
    }

    public async Task RenameGroupChatAsync(
        Guid currentUserId,
        Guid chatId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsAdminAsync(chatId, currentUserId, cancellationToken);

        var chatDto = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        // The domain validates the name (groups need one, private chats can't have one).
        var chat = Chat.Restore(chatDto.Id, chatDto.Type, chatDto.Name, chatDto.CreatedAt);
        chat.Rename(newName);

        await chatRepository.UpdateChatNameAsync(chatId, new UpdateChatNameDto { Name = chat.Name }, cancellationToken);
    }

    public async Task AddMemberToGroupAsync(
        Guid currentUserId,
        Guid chatId,
        Guid newMemberUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsAdminAsync(chatId, currentUserId, cancellationToken);

        var chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        if (chat.Type != EChatType.Group)
            throw new InvalidOperationException("Members can only be added to group chats.");

        if (!await userRepository.UserExistsAsync(newMemberUserId, cancellationToken))
            throw new UserNotFoundException();

        if (await chatMemberRepository.IsMemberAsync(chatId, newMemberUserId, cancellationToken))
            return;

        await AddMemberAsync(chatId, newMemberUserId, EChatMemberRole.Member, cancellationToken);
    }

    // Admins can remove anyone; any member can remove themselves (leave).
    public async Task RemoveMemberFromGroupAsync(
        Guid currentUserId,
        Guid chatId,
        Guid memberUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == memberUserId)
            await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);
        else
            await EnsureIsAdminAsync(chatId, currentUserId, cancellationToken);

        var chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        if (chat.Type != EChatType.Group)
            throw new InvalidOperationException("Members can only be removed from group chats.");

        await chatMemberRepository.RemoveMemberAsync(chatId, memberUserId, cancellationToken);
    }

    private async Task SaveChatAsync(Chat chat, CancellationToken cancellationToken)
    {
        await chatRepository.CreateChatAsync(new CreateChatDto
        {
            Id = chat.Id,
            Type = chat.Type,
            Name = chat.Name,
            CreatedAt = chat.CreatedAt
        }, cancellationToken);
    }

    private Task AddMemberAsync(Guid chatId, Guid userId, EChatMemberRole role, CancellationToken cancellationToken)
    {
        return chatMemberRepository.AddMemberAsync(new AddChatMemberDto
        {
            ChatId = chatId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsureIsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await chatMemberRepository.IsMemberAsync(chatId, userId, cancellationToken))
            throw new NotChatMemberException();
    }

    private async Task EnsureIsAdminAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await chatMemberRepository.GetMemberAsync(chatId, userId, cancellationToken)
            ?? throw new NotChatMemberException();

        if (member.Role != EChatMemberRole.Admin)
            throw new NotChatAdminException();
    }

    private static GetChatDto ToChatDto(Chat chat)
    {
        return new GetChatDto
        {
            Id = chat.Id,
            Type = chat.Type,
            Name = chat.Name,
            CreatedAt = chat.CreatedAt
        };
    }
}
