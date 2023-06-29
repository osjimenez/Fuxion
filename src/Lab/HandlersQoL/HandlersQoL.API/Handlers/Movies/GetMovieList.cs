using Fuxion.Domain;
using Fuxion.Reflection;

namespace Handlers_QoL.API.Handlers.Movies;

[TypeKey(nameof(GetMovieListQuery))]
public class GetMovieListQuery : IMessage { }

public class GetMovieListQueryResponse { }

public class GetMovieListQueryHandler { }