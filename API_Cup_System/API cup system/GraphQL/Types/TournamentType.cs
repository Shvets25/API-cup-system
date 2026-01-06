using HotChocolate.Types;
using TournamentApi.Domain;

namespace TournamentApi.GraphQL.Types;

public class TournamentType : ObjectType<Tournament>
{
    protected override void Configure(IObjectTypeDescriptor<Tournament> descriptor)
    {
        descriptor.Field(t => t.Status)
            .Type<EnumType<TournamentStatus>>();
    }
}