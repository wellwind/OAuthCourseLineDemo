using Microsoft.EntityFrameworkCore;
using OAuth2.Line.Core.Database;

namespace OAuth2.Line.Core.Message;

public class MessageService
{
    private readonly LineNotifyBindingContext _context;

    public MessageService(LineNotifyBindingContext context)
    {
        _context = context;
    }

    public async Task<int> CreateMessage(string message)
    {
        var messageEntity = new Database.Message()
        {
            MessageText = message,
            CreatedAt = DateTime.Now,
        };
        _context.Messages.Add(messageEntity);
        await _context.SaveChangesAsync();
        return messageEntity.Id;
    }

    public async Task UpdateMessageStatusAsync(string sub, int messageId, bool success, string errorMessage)
    {
        var statusEntity = _context.MessageStatuses.Find(messageId, sub);
        if (statusEntity is null)
        {
            var entity = new MessageStatus()
            {
                MessageId = messageId,
                Sub = sub,
                Status = success ? 1 : 0,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.Now,
            };
            await _context.MessageStatuses.AddAsync(entity);
        }
        else
        {
            statusEntity.Status = success ? 1 : 0;
            statusEntity.ErrorMessage = errorMessage;
        }

        await _context.SaveChangesAsync();
    }

    public IEnumerable<Database.Message> GetMessages()
    {
        return _context.Messages;
    }

    public IEnumerable<Database.MessageStatus> GetMessageStatuses(int id)
    {
        return _context.MessageStatuses
            .Include(item => item.SubNavigation)
            .Where(item => item.MessageId == id);
    }
}