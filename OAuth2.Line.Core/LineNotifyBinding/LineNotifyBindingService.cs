using Microsoft.EntityFrameworkCore;
using OAuth2.Line.Core.Database;

namespace OAuth2.Line.Core.LineNotifyBinding;

public class LineNotifyBindingService
{
    private readonly LineNotifyBindingContext _context;

    public LineNotifyBindingService(LineNotifyBindingContext context)
    {
        _context = context;
    }

    public async Task UpdateLoginAsync(string sub, string name,string picture, string lineLoginAccessToken, string lineLoginRefreshToken, string idToken)
    {
        var binding = _context.LineNotifyBindings.FirstOrDefault(x => x.Sub == sub);
        if (binding is null)
        {
            binding = new Database.LineNotifyBinding
            {
                Sub = sub,
                Name = name,
                Picture = picture,
                LineLoginAccessToken = lineLoginAccessToken,
                LineLoginRefreshToken = lineLoginRefreshToken,
                LineLoginIdToken = idToken,
            };
            _context.LineNotifyBindings.Add(binding);
        }
        else
        {
            binding.LineLoginAccessToken = lineLoginAccessToken;
            binding.LineLoginRefreshToken = lineLoginRefreshToken;
            binding.LineLoginIdToken = idToken;
        }
        await _context.SaveChangesAsync();
    }

    public bool IsLineNotifyAccessTokenBinded(string sub)
    {
        var binding = _context.LineNotifyBindings.FirstOrDefault(x => x.Sub == sub);
        return binding is not null && !String.IsNullOrEmpty(binding.LineNotifyAccessToken);
    }

    public async Task ClearLineNotifyAccessTokenAsync(string sub)
    {
        var binding = _context.LineNotifyBindings.FirstOrDefault(x => x.Sub == sub);
        if (binding is not null)
        {
            binding.LineNotifyAccessToken = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLineNotifyAccessTokenAsync(string sub, string lineNotifyAccessToken)
    {
        var binding = _context.LineNotifyBindings.Find(sub);
        if (binding is null)
        {
            throw new Exception("Binding not found");
        }

        binding.LineNotifyAccessToken = lineNotifyAccessToken;
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetLineNotifyAccessTokenAsync(string sub)
    {
        var binding = await _context.LineNotifyBindings.FindAsync(sub);
        if (binding is null)
        {
            throw new Exception("Binding not found");
        }

        return binding.LineNotifyAccessToken;
    }

    public IEnumerable<Database.LineNotifyBinding> GetLineNotifyBindings()
    {
        return _context.LineNotifyBindings;
    }
}