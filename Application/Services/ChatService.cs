using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class ChatService(
    IChatRepository chatRepository,
    IChatMemberRepository chatMemberRepository,
    IUserRepository userRepository,
    IFriendshipRepository friendshipRepository)
{
    // Only friends can be added to a group. The creator becomes the admin.
    public async Task<GetChatDto> CreateGroupChatAsync(
        Guid currentUserId,
        string groupName,
        IEnumerable<Guid> friendUserIds,
        CancellationToken cancellationToken = default)
    {
        // The domain validates the name before anything is saved.
        var chat = Chat.Create(EChatType.Group, groupName);

        var memberUserIds = friendUserIds.Where(userId => userId != currentUserId).Distinct().ToList();
        foreach (var memberUserId in memberUserIds)
        {
            if (!await AreFriendsAsync(currentUserId, memberUserId, cancellationToken))
                throw new NotFriendsException();
        }

        await SaveChatAsync(chat, cancellationToken);
        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Admin, cancellationToken);
        foreach (var memberUserId in memberUserIds)
        {
            await AddMemberAsync(chat.Id, memberUserId, EChatMemberRole.Member, cancellationToken);
        }

        return ToChatDto(chat);
    }

    public async Task<GetChatDto> OpenPrivateChatAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        var otherUser = await userRepository.GetUserAsync(otherUserId, cancellationToken)
            ?? throw new UserNotFoundException();

        return await OpenPrivateChatWithUserAsync(currentUserId, otherUser, cancellationToken);
    }

    public async Task<GetChatDto> OpenPrivateChatByUsernameAsync(
        Guid currentUserId,
        string otherUsername,
        CancellationToken cancellationToken = default)
    {
        var otherUser = await userRepository.GetUserByUsernameAsync(
                            otherUsername.Trim().ToLowerInvariant(), cancellationToken)
                        ?? throw new UserNotFoundException();

        return await OpenPrivateChatWithUserAsync(currentUserId, otherUser, cancellationToken);
    }

    public async Task<List<GetChatSummaryDto>> GetMyChatSummariesAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var chats = await chatRepository.GetChatsForUserAsync(currentUserId, cancellationToken);

        var summaries = new List<GetChatSummaryDto>();
        foreach (var chat in chats)
        {
            summaries.Add(await BuildSummaryAsync(currentUserId, chat, cancellationToken));
        }

        return summaries;
    }

    public async Task<GetChatSummaryDto> GetChatSummaryAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        var chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        return await BuildSummaryAsync(currentUserId, chat, cancellationToken);
    }

    public async Task<List<GetChatMemberProfileDto>> GetMemberProfilesAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        var members = await chatMemberRepository.GetMembersAsync(chatId, cancellationToken);

        var profiles = new List<GetChatMemberProfileDto>();
        foreach (var member in members)
        {
            var user = await userRepository.GetUserAsync(member.UserId, cancellationToken);
            if (user is null)
                continue;

            profiles.Add(new GetChatMemberProfileDto
            {
                UserId = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName ?? user.Username,
                IconUrl = user.IconUrl,
                Role = member.Role
            });
        }

        return profiles;
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

    // Admins can add their friends to a group.
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

        if (!await AreFriendsAsync(currentUserId, newMemberUserId, cancellationToken))
            throw new NotFriendsException();

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

    private async Task<GetChatDto> OpenPrivateChatWithUserAsync(
        Guid currentUserId,
        GetUserDto otherUser,
        CancellationToken cancellationToken)
    {
        if (currentUserId == otherUser.Id)
            throw new ArgumentException("You cannot start a private chat with yourself.");

        var existingChat = await chatRepository.GetPrivateChatBetweenAsync(currentUserId, otherUser.Id, cancellationToken);
        if (existingChat is not null)
            return existingChat;

        if (!otherUser.AllowDirectMessagesFromEveryone
            && !await AreFriendsAsync(currentUserId, otherUser.Id, cancellationToken))
            throw new DirectMessagesNotAllowedException();

        var chat = Chat.Create(EChatType.Private, null);
        await SaveChatAsync(chat, cancellationToken);

        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Member, cancellationToken);
        await AddMemberAsync(chat.Id, otherUser.Id, EChatMemberRole.Member, cancellationToken);

        return ToChatDto(chat);
    }

    private async Task<GetChatSummaryDto> BuildSummaryAsync(
        Guid currentUserId,
        GetChatDto chat,
        CancellationToken cancellationToken)
    {
        var members = await chatMemberRepository.GetMembersAsync(chat.Id, cancellationToken);

        if (chat.Type == EChatType.Group)
        {
            return new GetChatSummaryDto
            {
                ChatId = chat.Id,
                Type = chat.Type,
                Title = chat.Name ?? "Group",
                Subtitle = $"{members.Count} users",
                MemberCount = members.Count
            };
        }

        var otherUserId = members.FirstOrDefault(member => member.UserId != currentUserId)?.UserId;
        var otherUser = otherUserId is null
            ? null
            : await userRepository.GetUserAsync(otherUserId.Value, cancellationToken);

        return new GetChatSummaryDto
        {
            ChatId = chat.Id,
            Type = chat.Type,
            Title = otherUser?.DisplayName ?? otherUser?.Username ?? "Unknown user",
            Subtitle = otherUser?.Username ?? string.Empty,
            IconUrl = otherUser?.IconUrl,
            MemberCount = members.Count
        };
    }

    private Task SaveChatAsync(Chat chat, CancellationToken cancellationToken)
    {
        return chatRepository.CreateChatAsync(new CreateChatDto
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

    private async Task<bool> AreFriendsAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        var friendship = await friendshipRepository.GetFriendshipBetweenAsync(firstUserId, secondUserId, cancellationToken);
        return friendship?.Status == EFriendshipStatus.Accepted;
    }

    private async Task EnsureIsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await chatMemberRepository.IsMemberAsync(chatId, userId, cancellationToken))
            throw new NotChatMemberException();
    }

    private async Task EnsureIsAdminAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await chatMemberRepository.GetMemberAsync(chatId, userId, cancellationToken)
            ?? throw new NotChatAdminException();

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
