﻿using Fuxion.Identity.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fuxion.Identity.Test.Helpers.TypeDiscriminatorIds;
namespace Fuxion.Identity.Test.Dao
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    [TypeDiscriminated(Base, AdditionalInclusions = new[] { Media })]
    public abstract class BaseDao
    {
        public string Id { get; set; } = string.Empty.RandomString(10);
        public virtual string Name { get; set; }

        public override string ToString() => $"{Name} - {Id}";
    }
}
