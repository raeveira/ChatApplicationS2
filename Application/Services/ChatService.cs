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
        Chat chat = Chat.Create(EChatType.Group, groupName);

        List<Guid> memberUserIds = friendUserIds.Where(userId => userId != currentUserId).Distinct().ToList();
        foreach (Guid memberUserId in memberUserIds)
        {
            if (!await AreFriendsAsync(currentUserId, memberUserId, cancellationToken))
                throw new NotFriendsException();
        }

        await SaveChatAsync(chat, cancellationToken);
        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Admin, cancellationToken);
        foreach (Guid memberUserId in memberUserIds)
        {
            await AddMemberAsync(chat.Id, memberUserId, EChatMemberRole.Member, cancellationToken);
        }

        return ToChatDto(chat);
    }

    // Opens a private chat between the current user and another user. Creates the chat if it doesn't exist.
    public async Task<GetChatDto> OpenPrivateChatAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        GetUserDto otherUser = await userRepository.GetUserAsync(otherUserId, cancellationToken)
            ?? throw new UserNotFoundException();

        return await OpenPrivateChatWithUserAsync(currentUserId, otherUser, cancellationToken);
    }

    // Opens a private chat between the current user and another user identified by their username. Creates the chat if it doesn't exist.
    public async Task<GetChatDto> OpenPrivateChatByUsernameAsync(
        Guid currentUserId,
        string otherUsername,
        CancellationToken cancellationToken = default)
    {
        GetUserDto otherUser = await userRepository.GetUserByUsernameAsync(
                            otherUsername.Trim().ToLowerInvariant(), cancellationToken)
                        ?? throw new UserNotFoundException();

        return await OpenPrivateChatWithUserAsync(currentUserId, otherUser, cancellationToken);
    }

    // Retrieves a list of chat summaries for the current user.
    public async Task<List<GetChatSummaryDto>> GetMyChatSummariesAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        List<GetChatDto> chats = await chatRepository.GetChatsForUserAsync(currentUserId, cancellationToken);

        List<GetChatSummaryDto> summaries = new List<GetChatSummaryDto>();
        foreach (GetChatDto chat in chats)
        {
            summaries.Add(await BuildSummaryAsync(currentUserId, chat, cancellationToken));
        }

        return summaries;
    }

    // Retrieves the summary of a specific chat for the current user.
    public async Task<GetChatSummaryDto> GetChatSummaryAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        GetChatDto chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        return await BuildSummaryAsync(currentUserId, chat, cancellationToken);
    }

    // Retrieves the profiles of all members in a specific chat for the current user.
    public async Task<List<GetChatMemberProfileDto>> GetMemberProfilesAsync(
        Guid currentUserId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsMemberAsync(chatId, currentUserId, cancellationToken);

        List<GetChatMemberDto> members = await chatMemberRepository.GetMembersAsync(chatId, cancellationToken);

        List<GetChatMemberProfileDto> profiles = new List<GetChatMemberProfileDto>();
        foreach (GetChatMemberDto member in members)
        {
            GetUserDto? user = await userRepository.GetUserAsync(member.UserId, cancellationToken);
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

    // Renames a group chat. Only admins can perform this action.
    public async Task RenameGroupChatAsync(
        Guid currentUserId,
        Guid chatId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsAdminAsync(chatId, currentUserId, cancellationToken);

        GetChatDto chatDto = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        // The domain validates the name (groups need one, private chats can't have one).
        Chat chat = Chat.Restore(chatDto.Id, chatDto.Type, chatDto.Name, chatDto.CreatedAt);
        chat.Rename(newName);

        await chatRepository.UpdateChatNameAsync(chatId, new UpdateChatNameDto { Name = chat.Name }, cancellationToken);
    }

    // Admins can add their friends to a group chat.
    public async Task AddMemberToGroupAsync(
        Guid currentUserId,
        Guid chatId,
        Guid newMemberUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureIsAdminAsync(chatId, currentUserId, cancellationToken);

        GetChatDto chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
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

        GetChatDto chat = await chatRepository.GetChatAsync(chatId, cancellationToken)
            ?? throw new ChatNotFoundException();

        if (chat.Type != EChatType.Group)
            throw new InvalidOperationException("Members can only be removed from group chats.");

        await chatMemberRepository.RemoveMemberAsync(chatId, memberUserId, cancellationToken);
    }

    // Opens a private chat with the specified user. Creates the chat if it doesn't exist.
    private async Task<GetChatDto> OpenPrivateChatWithUserAsync(
        Guid currentUserId,
        GetUserDto otherUser,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Opening private chat with user {otherUser.Id} for current user {currentUserId}");
        if (currentUserId == otherUser.Id)
            throw new ArgumentException("You cannot start a private chat with yourself.");

        Console.WriteLine($"Checking for existing private chat between {currentUserId} and {otherUser.Id}");

        GetChatDto? existingChat = await chatRepository.GetPrivateChatBetweenAsync(currentUserId, otherUser.Id, cancellationToken);
        Console.WriteLine($"Existing chat: {existingChat?.Id}");
        if (existingChat is not null)
            return existingChat;

        Console.WriteLine($"No existing private chat found between {currentUserId} and {otherUser.Id}");

        if (!otherUser.AllowDirectMessagesFromEveryone
            && !await AreFriendsAsync(currentUserId, otherUser.Id, cancellationToken))
        {
            throw new DirectMessagesNotAllowedException();
        }

        Console.WriteLine($"Creating private chat between {currentUserId} and {otherUser.Id}");

        Chat chat = Chat.Create(EChatType.Private, null);
        await SaveChatAsync(chat, cancellationToken);

        await AddMemberAsync(chat.Id, currentUserId, EChatMemberRole.Member, cancellationToken);
        await AddMemberAsync(chat.Id, otherUser.Id, EChatMemberRole.Member, cancellationToken);

        return ToChatDto(chat);
    }

    // Builds a summary of the specified chat for the current user.
    private async Task<GetChatSummaryDto> BuildSummaryAsync(
        Guid currentUserId,
        GetChatDto chat,
        CancellationToken cancellationToken)
    {
        List<GetChatMemberDto> members = await chatMemberRepository.GetMembersAsync(chat.Id, cancellationToken);

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

        Guid? otherUserId = members.FirstOrDefault(member => member.UserId != currentUserId)?.UserId;
        GetUserDto? otherUser = otherUserId is null
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

    // Saves a new chat to the repository.
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

    // Adds a member to a chat with the specified role.
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

    // Checks if two users are friends.
    private async Task<bool> AreFriendsAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        GetFriendshipDto? friendship = await friendshipRepository.GetFriendshipBetweenAsync(firstUserId, secondUserId, cancellationToken);
        return friendship?.Status == EFriendshipStatus.Accepted;
    }

    // Ensures that the specified user is a member of the chat. Throws an exception if not.
    private async Task EnsureIsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await chatMemberRepository.IsMemberAsync(chatId, userId, cancellationToken))
            throw new NotChatMemberException();
    }

    // Ensures that the specified user is an admin of the chat. Throws an exception if not.
    private async Task EnsureIsAdminAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        GetChatMemberDto? member = await chatMemberRepository.GetMemberAsync(chatId, userId, cancellationToken)
            ?? throw new NotChatAdminException();

        if (member.Role != EChatMemberRole.Admin)
            throw new NotChatAdminException();
    }

    // Converts a Chat domain object to a GetChatDto.
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
