using System;
using System.Collections.Generic;

namespace EasyBamlAddin.UidManagement
{
    public sealed class UidCollector
    {
        // Fields
        private readonly List<string> namespacePrefixes = new List<string>();
        private readonly List<Uid> uids = new List<Uid>();
        private readonly IDictionary<string, Uid> uidTable = new Dictionary<string, Uid>();
        private const char UidNamespaceAbbreviation = 'x';
        private readonly IUidGenerationStrategy uidGenerationStrategy;

        // Methods
        public UidCollector(string fileName, IUidGenerationStrategy uidGenerationStrategy)
        {
            this.uidGenerationStrategy = uidGenerationStrategy;
            RootElementLineNumber = -1;
            RootElementLinePosition = -1;
            FileName = fileName;
        }

        public UidCollector(string fileName) : this(fileName, new UidGenerationStrategy())
        {
        }


        public void AddNamespacePrefix(string prefix)
        {
            namespacePrefixes.Add(prefix);
        }

        public void AddUid(Uid uid, bool register)
        {
            uids.Add(uid);

            if (register)
            {
                RegisterUid(uid);
            }
        }

        public void RegisterUid(Uid uid)
        {
            if (uid.Value == null)
            {
                uid.Status = UidStatus.Absent;
            }
            else if (uidTable.ContainsKey(uid.Value))
            {
                uid.Status = UidStatus.Duplicate;
            }
            else
            {
                uid.Status = UidStatus.Valid;
                StoreUid(uid.Value);
            }
        }

        public void RemoveUid(Uid uid)
        {
            uids.Remove(uid);
        }

        private string GeneratePrefix()
        {
            long num = 1L;
            string item = UidNamespaceAbbreviation.ToString();
            try
            {
                while (namespacePrefixes.Contains(item))
                {
                    item = UidNamespaceAbbreviation + num.ToString();
                    num += 1L;
                }
                return item;
            }
            catch (OverflowException)
            {
            }
            return Guid.NewGuid().ToString();
        }

        private string GetAvailableUid(Uid uid)
        {
            return uidGenerationStrategy.GenerateUid(uid, UidAvailabilityChecker);
        }

        private bool UidAvailabilityChecker(string uid)
        {
            return !uidTable.ContainsKey(uid);
        }

        public bool AllAreValid()
        {
            for (int i = 0; i < Count; i++)
            {
                var uid = this[i];
                
                if (uid.Status == UidStatus.Unknown)
                {
                    throw new InvalidOperationException("Uid is not registered in UidCollection");
                }

                if (uid.Status != UidStatus.Valid)
                {
                    return false;
                }
            }
            return true;
        }

        public bool AllAreAbsent()
        {
            for (int i = 0; i < Count; i++)
            {
                var uid = this[i];

                if (uid.Status == UidStatus.Unknown)
                {
                    throw new InvalidOperationException("Uid is not registered in UidCollection");
                }

                if (uid.Status != UidStatus.Absent)
                {
                    return false;
                }
            }
            return true;
        }

        public void ResolveUidErrors()
        {
            foreach (Uid uid in uids)
            {
                if (((uid.Status == UidStatus.Absent) && (uid.NamespacePrefix == null)) && (NamespaceAddedForMissingUid == null))
                {
                    NamespaceAddedForMissingUid = GeneratePrefix();
                }
                if (uid.Status != UidStatus.Valid)
                {
                    uid.Value = GetAvailableUid(uid);
                    StoreUid(uid.Value);
                    //uid.Status = UidStatus.Valid; //Don't update status now - it is needed to update xaml
                }
            }
        }

        private void StoreUid(string value)
        {
            uidTable[value] = null;
        }

        // Properties
        public int Count
        {
            get
            {
                return uids.Count;
            }
        }

        public string FileName { get; private set; }

        public Uid this[int index]
        {
            get
            {
                return uids[index];
            }
        }

        public string NamespaceAddedForMissingUid { get; private set; }

        public int RootElementLineNumber { get; set; }

        public int RootElementLinePosition { get; set; }
    }
}
