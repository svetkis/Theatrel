using System;
using theatrel.Common;
using theatrel.DataAccess.Structures.Entities;
using System.Collections.Generic;

namespace theatrel.Lib.DescriptionService;

internal class ActorComparer : IComparer<ActorInRoleEntity>
{
    private bool isAscending;

    public ActorComparer(bool inAscendingOrder = true)
    {
        isAscending = inAscendingOrder;
    }

    int IComparer<ActorInRoleEntity>.Compare(ActorInRoleEntity x, ActorInRoleEntity y)
    {
        bool isAfishaX = string.Equals(x.Role.CharacterName, CommonTags.Afisha, StringComparison.InvariantCultureIgnoreCase);
        bool isAfishaY = string.Equals(y.Role.CharacterName, CommonTags.Afisha, StringComparison.InvariantCultureIgnoreCase);

        if (isAfishaX && isAfishaY)
            return 0;

        if (isAfishaX)
            return isAscending ? 1 : -1;

        if (isAfishaY)
            return isAscending ? -1 : 1;

        bool isConductorX = string.Equals(x.Role.CharacterName, CommonTags.Conductor, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(x.Role.CharacterName, CommonTags.Conductor1, StringComparison.InvariantCultureIgnoreCase);

        bool isConductorY = string.Equals(y.Role.CharacterName, CommonTags.Conductor, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(y.Role.CharacterName, CommonTags.Conductor1, StringComparison.InvariantCultureIgnoreCase);

        if (isConductorX && isConductorY)
            return 0;

        if (isConductorX)
            return isAscending ? 1 : -1;

        if (isConductorY)
            return isAscending ? -1 : 1;

        int result = string.Compare(x.Role.CharacterName, y.Role.CharacterName, true);

        return isAscending ? result : -result;
    }
}
