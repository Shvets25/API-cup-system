using Microsoft.EntityFrameworkCore;
using TournamentApi.Data;
using TournamentApi.Domain;

namespace TournamentApi.Services;

public class TournamentService
{
    private readonly AppDbContext _db;

    public TournamentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tournament> CreateTournament(string name, DateTime startDate)
    {
        var t = new Tournament
        {
            Name = name.Trim(),
            StartDate = startDate,
            Status = TournamentStatus.Draft
        };

        _db.Tournaments.Add(t);
        await _db.SaveChangesAsync();

        return t;
    }

    public async Task<Tournament> AddParticipant(int tournamentId, int userId)
    {
        var t = await _db.Tournaments
            .Include(x => x.Participants)
            .Include(x => x.Bracket)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t == null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament not found.")
                .SetCode("NOT_FOUND")
                .Build());

        if (t.Status != TournamentStatus.Draft)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Cannot add participants after start.")
                .SetCode("TOURNAMENT_STARTED")
                .Build());

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("User not found.")
                .SetCode("USER_NOT_FOUND")
                .Build());

        if (t.Participants.Any(p => p.Id == userId))
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("User already participating.")
                .SetCode("ALREADY_PARTICIPATING")
                .Build());

        t.Participants.Add(user);
        await _db.SaveChangesAsync();

        return t;
    }

    public async Task<Tournament> StartTournament(int tournamentId)
    {
        var t = await _db.Tournaments
            .Include(x => x.Participants)
            .Include(x => x.Bracket)
                .ThenInclude(b => b!.Matches)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        ValidateTournamentForStart(t);

        var bracket = new Bracket { TournamentId = t.Id };
        _db.Brackets.Add(bracket);
        await _db.SaveChangesAsync();

        var participantIds = t.Participants.Select(p => p.Id).ToList();

        ShuffleList(participantIds);

        AddInitialMatches(bracket.Id, participantIds);

        t.Status = TournamentStatus.Started;
        await _db.SaveChangesAsync();

        return t;
    }

    public async Task<Tournament> FinishTournament(int tournamentId)
    {
        var t = await _db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t == null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament not found.")
                .SetCode("NOT_FOUND")
                .Build());

        if (t.Status != TournamentStatus.Started)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament must be started.")
                .SetCode("INVALID_STATUS")
                .Build());

        t.Status = TournamentStatus.Finished;
        await _db.SaveChangesAsync();

        return t;
    }

    public async Task<List<Match>> GetMatchesForRound(int tournamentId, int round)
    {
        return await _db.Matches
            .Where(m => m.Bracket.TournamentId == tournamentId && m.Round == round)
            .ToListAsync();
    }

    

    private void ValidateTournamentForStart(Tournament? t)
    {
        if (t == null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament not found.")
                .SetCode("NOT_FOUND")
                .Build());

        if (t.Status != TournamentStatus.Draft)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Tournament already started/finished.")
                .SetCode("INVALID_STATUS")
                .Build());

        if (t.Participants.Count < 2)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Need at least 2 participants.")
                .SetCode("NOT_ENOUGH_PARTICIPANTS")
                .Build());

        if (t.Bracket != null)
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Bracket already exists.")
                .SetCode("BRACKET_EXISTS")
                .Build());
    }

    private void ShuffleList(List<int> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void AddInitialMatches(int bracketId, List<int> participants)
    {
        int round = 1;

        for (int i = 0; i < participants.Count; i += 2)
        {
            if (i + 1 < participants.Count)
            {
                _db.Matches.Add(new Match
                {
                    BracketId = bracketId,
                    Round = round,
                    Player1Id = participants[i],
                    Player2Id = participants[i + 1],
                    WinnerId = null
                });
            }
            else
            {
                int pid = participants[i];
                _db.Matches.Add(new Match
                {
                    BracketId = bracketId,
                    Round = round,
                    Player1Id = pid,
                    Player2Id = pid,
                    WinnerId = pid
                });
            }
        }
    }
}
