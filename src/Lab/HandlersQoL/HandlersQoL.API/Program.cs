using System.Reactive.Linq;
using Fuxion.Domain;
using Handlers_QoL.API.Handlers.Movies;
using Handlers_QoL.API.Utils.Transport;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<InMemoryPublisher>();
builder.Services.AddSingleton<InMemorySubscriber>();
builder.Services.AddSingleton<INexus>(sp =>
{
	DefaultNexus nexus = new("HandlersQoL-API");
	var inMemoryPublisher = sp.GetRequiredService<InMemoryPublisher>();
	nexus.RouteDirectory.AddPublisher<GetMovieListQuery>(new(""), message => inMemoryPublisher.Publish(message));
	var inMemorySubscriber = sp.GetRequiredService<InMemorySubscriber>();
	// nexus.RouteDirectory.AddSubscriber(rabbitSubscriber);
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
var inMemorySubscriber = app.Services.GetRequiredService<InMemorySubscriber>();
var inMemoryPublisher = app.Services.GetRequiredService<InMemoryPublisher>();
await nexus.Initialize();
inMemorySubscriber.Attach(nexus);
await inMemorySubscriber.Initialize();
await nexus.OnReceive(msg => { Console.WriteLine("Message received:\r\n" + msg); });
nexus.Observe(obj => true)
	.Buffer(2)
	.Subscribe(list =>
	{
		foreach (var msg in list) Console.WriteLine("Message observed from nexus extensions:\r\n" + msg);
	});
new ObservableNexusDecorator(nexus).Observe(obj => true)
	.Subscribe(msg => { Console.WriteLine("Message observed from nexus:\r\n" + msg); });

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