using System;

namespace Fuxion.Linq.Test.Data.Daos;

public class RelationNotLoadedException(string propertyName) : Exception($"Property '{propertyName}' is not loaded.");