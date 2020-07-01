using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyBamlAddin.ViewModels
{
    public class CultureItemViewModel : IComparable<CultureItemViewModel>
    {
        public string Name { get; set; }

        public CultureItemViewModel()
        {
        }

        public CultureItemViewModel(string name)
        {
            Name = name;
        }

        public bool Equals(CultureItemViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(CultureItemViewModel)) return false;
            return Equals((CultureItemViewModel)obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public int CompareTo(CultureItemViewModel other)
        {
            return String.Compare(Name, other.Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
