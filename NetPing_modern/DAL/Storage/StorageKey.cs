using System;

namespace NetPing.DAL
{
    internal class StorageKey
    {
        public StorageKey()
        {
            Directory = String.Empty;
        }

        public String Name { get; set; }

        public String Directory { get; set; }

        public static implicit operator StorageKey(String value)
        {
            return new StorageKey()
            {
                Name = value
            };
        }

        public override String ToString()
        {
            return $"Name: {Name} Dir: {Directory}";
        }
    }
}