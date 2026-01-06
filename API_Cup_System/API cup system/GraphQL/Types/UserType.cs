using HotChocolate.Types;
using TournamentApi.Domain;

namespace TournamentApi.GraphQL.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        
        descriptor.Field(u => u.PasswordHash).Ignore();
    }
}