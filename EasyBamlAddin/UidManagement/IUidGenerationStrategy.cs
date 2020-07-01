using System;

namespace EasyBamlAddin.UidManagement
{
    public interface IUidGenerationStrategy
    {
        string GenerateUid(Uid uid, Func<string, bool> uidAvailabilityChecker);
    }
}
