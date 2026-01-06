using Microsoft.EntityFrameworkCore;
using TournamentApi.Data;
using TournamentApi.Domain;

namespace TournamentApi.Services;

public class MatchService
{
    private readonly AppDbContext _db;

    public MatchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Match> PlayMatch(int matchId, int winnerUserId)
    {
        var match = await _db.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        ValidateMatch(match, winnerUserId);

        match.WinnerId = winnerUserId;
        await _db.SaveChangesAsync();

        await AdvanceNextRoundIfReady(match);

        return match;
    }

  

    private void ValidateMatch(Match? match, int winnerUserId)
    {
        if (match == null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Match not found.")
                .SetCode("NOT_FOUND")
                .Build());

        if (match.Bracket.Tournament.Status != TournamentStatus.Started)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament is not started.")
                .SetCode("INVALID_STATUS")
                .Build());

        if (match.WinnerId != null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Match already played.")
                .SetCode("ALREADY_PLAYED")
                .Build());

        if (winnerUserId != match.Player1Id && winnerUserId != match.Player2Id)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Winner must be one of the players.")
                .SetCode("INVALID_WINNER")
                .Build());
    }

    private async Task AdvanceNextRoundIfReady(Match finishedMatch)
    {
        var bracketId = finishedMatch.BracketId;
        var round = finishedMatch.Round;

        var roundMatches = await _db.Matches
            .Where(m => m.BracketId == bracketId && m.Round == round)
            .OrderBy(m => m.Id)
            .ToListAsync();

        
        if (roundMatches.Any(m => m.WinnerId == null))
            return;

        var winners = roundMatches.Select(m => m.WinnerId!.Value).ToList();

        
        if (winners.Count <= 1)
            return;

        int nextRound = round + 1;

       
        var nextRoundExists = await _db.Matches.AnyAsync(m => m.BracketId == bracketId && m.Round == nextRound);
        if (nextRoundExists)
            return;

       
        CreateMatchesForRound(bracketId, nextRound, winners);

        await _db.SaveChangesAsync();
    }

    private void CreateMatchesForRound(int bracketId, int round, List<int> playerIds)
    {
        int i = 0;
        while (i < playerIds.Count)
        {
            if (i + 1 < playerIds.Count)
            {
                _db.Matches.Add(new Match
                {
                    BracketId = bracketId,
                    Round = round,
                    Player1Id = playerIds[i],
                    Player2Id = playerIds[i + 1],
                    WinnerId = null
                });
            }
            else
            {
                int pid = playerIds[i];
                _db.Matches.Add(new Match
                {
                    BracketId = bracketId,
                    Round = round,
                    Player1Id = pid,
                    Player2Id = pid,
                    WinnerId = pid
                });
            }

            i += 2;
        }
    }
}
