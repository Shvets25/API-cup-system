using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using TournamentApi.Data;
using TournamentApi.Domain;
using TournamentApi.GraphQL.Inputs;
using TournamentApi.Security;

namespace TournamentApi.GraphQL;

public class Query
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Tournament> GetTournaments([Service] AppDbContext dbContext)
        => dbContext.Tournaments.AsNoTracking();
    
    public Task<Tournament?> GetTournamentById(int id, [Service] AppDbContext dbContext)
        => dbContext.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)
                .ThenInclude(b => b!.Matches)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    
    [Authorize]
    public async Task<User> Me([Service] AppDbContext dbContext, ClaimsPrincipal claims)
    {
        var userId = JwtTokenService.GetUserId(claims);

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Unauthorized")
                    .SetCode("UNAUTHORIZED")
                    .Build()
            );
        }

        return user;
    }
    
    [Authorize]
    public async Task<List<Match>> MyMatches(
        MyMatchFilter filter,
        [Service] AppDbContext dbContext,
        ClaimsPrincipal claims)
    {
        var userId = JwtTokenService.GetUserId(claims);

        var query = dbContext.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1Id == userId || m.Player2Id == userId);

        query = filter switch
        {
            MyMatchFilter.PLAYED => query.Where(m => m.WinnerId != null),
            MyMatchFilter.UNPLAYED => query.Where(m => m.WinnerId == null),
            _ => query
        };

        return await query.AsNoTracking().ToListAsync();
    }
    
    public Task<List<Match>> MatchesForRound(int tournamentId, int round, [Service] AppDbContext dbContext)
        => dbContext.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Bracket.TournamentId == tournamentId && m.Round == round)
            .AsNoTracking()
            .ToListAsync();
}
