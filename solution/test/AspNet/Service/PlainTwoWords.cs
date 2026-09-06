namespace Test.AspNet.Service;

/// <summary>
/// A plain (non-Fuxion) payload with a multi-word property, so snake_case/kebab-case transcoding is
/// distinguishable from an untouched (Newtonsoft) bind - a single-word property would look identical either way.
/// </summary>
public record PlainTwoWords(string FirstName);
