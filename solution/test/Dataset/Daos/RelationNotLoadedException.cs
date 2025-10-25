using System;

namespace Test.Dataset.Daos;

public class RelationNotLoadedException(string propertyName) : Exception($"Property '{propertyName}' is not loaded.");