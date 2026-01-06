using HotChocolate.Types;
using TournamentApi.Domain;

namespace TournamentApi.GraphQL.Types;

public class MatchObjectType : ObjectType<Match>
{
    protected override void Configure(IObjectTypeDescriptor<Match> descriptor)
    {
        descriptor.Field(m => m.WinnerId)
            .Type<IntType>();
        
        descriptor.Field(m => m.Winner)
            .Type<UserType>();
    }
}