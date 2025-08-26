using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitUsername = builder.AddParameter("rabbit-username", secret: true);
var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
var rabbit = builder
	.AddRabbitMQ("rabbit", rabbitUsername, rabbitPassword)
	.WithDataVolume(isReadOnly: false)
	.WithManagementPlugin();

var ms1 = builder
	.AddProject<Projects.Fuxion_Lab_Cloud_MS1>("ms1")
	.WithReference(rabbit);
var ms2 = builder
	.AddProject<Projects.Fuxion_Lab_Cloud_MS2>("ms2")
	.WithReference(rabbit);

builder
	.AddProject<Projects.Fuxion_Lab_Cloud_GATE>("gateway")
	.WithExternalHttpEndpoints()
	.WithReference(ms1)
	.WithReference(ms2)
	.WaitFor(ms1)
	.WaitFor(ms2);

builder.Build().Run();
