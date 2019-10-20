using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace UntisLibrary.Api
{
    class UntisNamingPolicy : JsonNamingPolicy
    {
        private Dictionary<string, string> _nameMappings = new Dictionary<string, string>
        {
            {"InternalId", "id" },
            {"UniqueName", "name" },
            {"SchoolClassId", "klasseId" }
        };
        public override string ConvertName(string name)
        {
            return _nameMappings.TryGetValue(name, out string newName) ? newName : name;
        }
    }
}
