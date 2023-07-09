using Fuxion.Domain;
using Handlers_QoL.API.Handlers.Movies;
using Handlers_QoL.API.Utils.Transport;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InMemoryQueueTransport>();
builder.Services.AddHostedService<InMemoryQueueTransport>(sp => sp.GetRequiredService<InMemoryQueueTransport>());
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
	if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity)) queueCapacity = 10000;

	return new DefaultBackgroundTaskQueue(queueCapacity);
});

builder.Services.AddSingleton<INexus>(sp =>
{
	DefaultNexus nexus = new("HandlersQoL-API");

	var inMemoryPublisher = sp.GetRequiredService<InMemoryQueueTransport>();
	var inMemorySubscriber = sp.GetRequiredService<InMemoryQueueTransport>();
	nexus.RouteDirectory.AddSubscriber(inMemorySubscriber);

	nexus.RouteDirectory.AddPublisher<GetMovieListQuery>(new(""), message => inMemoryPublisher.Publish(message));

	var decoObs = new ObservableSubscriberDecorator<object>(inMemorySubscriber);

	decoObs.Observe(_ => true)
		.Subscribe(msg => { Console.WriteLine("Message observed:\r\n" + msg); });

	nexus.RouteDirectory.AddSubscriber(decoObs);
	return nexus;
});

// APP
var app = builder.Build();

// Configure services
var nexus = app.Services.GetRequiredService<INexus>();
var inMemorySubscriber = app.Services.GetRequiredService<InMemoryQueueTransport>();
var inMemoryPublisher = app.Services.GetRequiredService<InMemoryQueueTransport>();

await nexus.Initialize();

inMemorySubscriber.Attach(nexus);

await inMemorySubscriber.Initialize();

await nexus.OnReceive(msg => { Console.WriteLine("Message received:\r\n" + msg); });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();