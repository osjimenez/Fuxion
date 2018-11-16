﻿#if (NET45 || NET472)
using Fuxion.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using static Fuxion.ServiceModel.ServiceBuilderFluentExtensions;

namespace Fuxion.ServiceModel
{
    public static class ServiceBuilder
    {
        #region Static methods
        public static IHost Host<TService>() => new _Host(typeof(TService));
        public static IProxy<TContract> Proxy<TContract>() => new _Proxy<TContract>(e => new ChannelFactory<TContract>(e));
        public static IProxy<TContract> Proxy<TContract>(object callbackInstance) => new _Proxy<TContract>(callbackInstance, (i, e) => new DuplexChannelFactory<TContract>(callbackInstance, e));
        public static IProxy<TContract> Proxy<TContract>(Func<ServiceEndpoint, ChannelFactory<TContract>> createCustomChannelFactoryFunction) => new _Proxy<TContract>(createCustomChannelFactoryFunction);
        public static IProxy<TContract> Proxy<TContract>(object callbackInstance, Func<object, ServiceEndpoint, ChannelFactory<TContract>> createCustomDuplexChannelFactoryFunction) => new _Proxy<TContract>(callbackInstance, createCustomDuplexChannelFactoryFunction);
        public static IDiscoveryManager DiscoverServices<TContract>() => new DiscoveryManager<TContract>();
        #endregion
    }
    public static class ServiceBuilderFluentExtensions
    { 
        #region Host
        public static IHost AddEndpoint(this IHost me, Action<IEndpoint> action)
        {
            var end = new _Endpoint();
            action(end);
            (me as _Host).ServiceHost.AddServiceEndpoint(end.ServiceEndpoint);
            return me;
        }
        public static IHost WithCredentials(this IHost me, Action<IServiceCredentials> action)
        {
            var host = (me as _Host);
            var cre = new _ServiceCredentials
            {
                ServiceCredentials = host.ServiceHost.Credentials
            };
            action(cre);
            return me;
        }
        public static IHost ConfigureHost(this IHost me, Action<ServiceHost> action)
        {
            action((me as _Host).ServiceHost);
            return me;
        }
        public static IHost MakeDiscoverable(this IHost me,
            DiscoveryMetadataElement metadata = DiscoveryMetadataElement.NetBiosName | DiscoveryMetadataElement.IpV4Addresses,
            Action<EndpointDiscoveryBehavior> configureEndpointBehavior = null)
        {
            var edb = new EndpointDiscoveryBehavior();
            if (metadata.HasFlag(DiscoveryMetadataElement.NetBiosName))
                edb.Extensions.Add(new XElement("NetBiosName", Environment.MachineName));
            var dns = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in dns.AddressList)
            {
                if (metadata.HasFlag(DiscoveryMetadataElement.IpV4Addresses) && 
                    ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    edb.Extensions.Add(new XElement("IpV4Address", ip.ToString()));
                if (metadata.HasFlag(DiscoveryMetadataElement.IpV6Addresses) && 
                    ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    edb.Extensions.Add(new XElement("IpV6Address", ip.ToString()));
            }
            configureEndpointBehavior?.Invoke(edb);
            (me as _Host).ServiceHost.Description.Endpoints.First().EndpointBehaviors.Add(edb);
            (me as _Host).ServiceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            (me as _Host).ServiceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());
            return me;
        }
        public static IHost InstanceContextMode(this IHost me, InstanceContextMode mode)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().InstanceContextMode = mode;
            return me;
        }
        public static IHost IncludeExceptionDetailInFaults(this IHost me, bool includeExceptionDetailInFaults)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().IncludeExceptionDetailInFaults = includeExceptionDetailInFaults;
            return me;
        }
        public static IHost AddressFilterMode(this IHost me, AddressFilterMode mode)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().AddressFilterMode = mode;
            return me;
        }
        public static IHost AutomaticSessionShutdown(this IHost me, bool automaticSessionShutdown)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().AutomaticSessionShutdown = automaticSessionShutdown;
            return me;
        }
        public static IHost ConcurrencyMode(this IHost me, ConcurrencyMode mode)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().ConcurrencyMode = mode;
            return me;
        }
        public static IHost EnsureOrderedDispatch(this IHost me, bool ensureOrderedDispatch)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().EnsureOrderedDispatch = ensureOrderedDispatch;
            return me;
        }
        public static IHost IgnoreExtensionDataObject(this IHost me, bool ignoreExtensionDataObject)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().IgnoreExtensionDataObject = ignoreExtensionDataObject;
            return me;
        }
        public static IHost MaxItemsInObjectGraph(this IHost me, int maxItemsInObjectGraph)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().MaxItemsInObjectGraph = maxItemsInObjectGraph;
            return me;
        }
        public static IHost ReleaseServiceInstanceOnTransactionComplete(this IHost me, bool releaseServiceInstanceOnTransactionComplete)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().ReleaseServiceInstanceOnTransactionComplete = releaseServiceInstanceOnTransactionComplete;
            return me;
        }
        public static IHost TransactionAutoCompleteOnSessionClose(this IHost me, bool transactionAutoCompleteOnSessionClose)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().TransactionAutoCompleteOnSessionClose = transactionAutoCompleteOnSessionClose;
            return me;
        }
        public static IHost TransactionIsolationLevel(this IHost me, IsolationLevel level)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().TransactionIsolationLevel = level;
            return me;
        }
        public static IHost TransactionTimeout(this IHost me, string transactionTimeout)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().TransactionTimeout = transactionTimeout;
            return me;
        }
        public static IHost UseSynchronizationContext(this IHost me, bool useSynchronizationContext)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().UseSynchronizationContext = useSynchronizationContext;
            return me;
        }
        public static IHost ValidateMustUnderstand(this IHost me, bool validateMustUnderstand)
        {
            (me as _Host).ServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().ValidateMustUnderstand = validateMustUnderstand;
            return me;
        }
        public static IHost DefaultTcpUnsecureHost<TContract>(this IHost me, int port, string path,
            Func<ITcpBinding, ITcpBinding> configureTcpBinding = null,
            Func<IServiceCredentials, IServiceCredentials> configureServiceCredentials = null)
        {
            var host = me.AddEndpoint(e => {
                e = e.WithContractOfType<TContract>();
                e = e.WithTcpBinding(b => {
                    b = b.SecurityMode(System.ServiceModel.SecurityMode.None);
                    if (configureTcpBinding != null)
                        b = configureTcpBinding(b);
                });
                e = e.WithAddress($"net.tcp://localhost:{port}/{path}");
            });
            host = host.WithCredentials(c => {
                if (configureServiceCredentials != null)
                    c = configureServiceCredentials(c);
            });
            return host;
        }
        public static IHost DefaultTcpSecurizedHost<TContract>(this IHost me, int port, string path, X509Certificate2 certificate, 
            Action<string,string> userNamePasswordValidationAction,
            Func<ITcpBinding, ITcpBinding> configureTcpBinding = null,
            Func<IServiceCredentials, IServiceCredentials> configureServiceCredentials = null)
        {
            var host = me.AddEndpoint(e =>
            {
                e = e.WithContractOfType<TContract>();
                e = e.WithTcpBinding(b =>
                {
                    b = b.SecurityMode(System.ServiceModel.SecurityMode.Message);
                    b = b.ClientCredentialType(MessageCredentialType.UserName);
                    if (configureTcpBinding != null)
                        b = configureTcpBinding(b);
                });
                e = e.WithAddress($"net.tcp://localhost:{port}/{path}");
            });
            host = host.WithCredentials(c => {
                c = c.ServiceCertificate(certificate);
                c = c.UserNamePasswordValidationMode(System.ServiceModel.Security.UserNamePasswordValidationMode.Custom);
                c = c.CustomUserNamePasswordValidator(userNamePasswordValidationAction);
                if (configureServiceCredentials != null)
                    c = configureServiceCredentials(c);
            });
            return host;
        }
        public static ServiceHost Create(this IHost me)
        {
            return (me as _Host).ServiceHost;
        }
        public static ServiceHost Open(this IHost me, TimeSpan? timeout = null, Action<ServiceHost> beforeOpenAction = null, Action<ServiceHost> afterOpenAction = null)
        {
            var host = me.Create();
            beforeOpenAction?.Invoke(host);
            if (timeout != null && timeout.HasValue) host.Open(timeout.Value);
            else host.Open();
            afterOpenAction?.Invoke(host);
            return host;
        }
        public static Task<ServiceHost> OpenAsync(this IHost me, TimeSpan? timeout = null, Action<ServiceHost> beforeOpenAction = null, Action<ServiceHost> afterOpenAction = null)
        {
            return TaskManager.StartNew((iHost, time, before, after) => iHost.Open(time, before, after), me, timeout, beforeOpenAction, afterOpenAction);
        }
        #region Host classes
        internal class _Host : IHost
        {
            public _Host(Type serviceType)
            {
                ServiceHost = new ServiceHost(serviceType);
            }
            public ServiceHost ServiceHost { get; set; }
        }
        #endregion
        #endregion
        #region Proxy
        public static IProxy<TContract> AddEndpoint<TContract>(this IProxy<TContract> me, Action<IEndpoint> action)
        {
            var end = new _Endpoint();
            action(end);
            var pro = (me as _Proxy<TContract>);
            pro.SetEndpoint(end.ServiceEndpoint);
            return me;
        }
        public static IProxy<TContract> WithCredentials<TContract>(this IProxy<TContract> me, Action<IClientCredentials> action)
        {
            var proxy = (me as _Proxy<TContract>);
            var cre = new _ClientCredentials
            {
                ClientCredentials = proxy.ChannelFactory.Credentials
            };
            action(cre);
            return me;
        }
        public static IProxy<TContract> ConfigureChannelFactory<TContract>(this IProxy<TContract> me, Action<ChannelFactory<TContract>> action)
        {
            action((me as _Proxy<TContract>).ChannelFactory);
            return me;
        }
        public static IProxy<TContract> DefaultTcpUnsecureProxy<TContract>(this IProxy<TContract> me, string host, int port, string path,
            Func<ITcpBinding, ITcpBinding> configureTcpBinding = null,
            Func<IClientCredentials, IClientCredentials> configureClientCredentials = null,
            Action<ChannelFactory<TContract>> configureChannelFactory = null)
        {
            var proxy = me.AddEndpoint(e => {
                e = e.WithContractOfType<TContract>();
                e = e.WithTcpBinding(b => {
                    b = b.SecurityMode(System.ServiceModel.SecurityMode.None);
                    if (configureTcpBinding != null)
                        b = configureTcpBinding(b);
                });
                e = e.WithAddress($"net.tcp://{host}:{port}/{path.Trim('/')}");
            });
            proxy = proxy.WithCredentials(c => {
                if (configureClientCredentials != null)
                    c = configureClientCredentials(c);
            });
            configureChannelFactory?.Invoke((proxy as _Proxy<TContract>).ChannelFactory);
            return proxy;
        }
        public static IProxy<TContract> DefaultTcpSecurizedProxy<TContract>(this IProxy<TContract> me,
            string host, int port, string path, string dnsName, string username, string password, string certificateThumbprint,
            Func<ITcpBinding, ITcpBinding> configureTcpBinding = null,
            Func<IClientCredentials, IClientCredentials> configureClientCredentials = null,
            Action<ChannelFactory<TContract>> configureChannelFactory = null)
        {
            var proxy = me.AddEndpoint(e => {
                e = e.WithContractOfType<TContract>();
                e = e.WithTcpBinding(b => {
                    b = b.ClientCredentialType(MessageCredentialType.UserName);
                    b = b.SecurityMode(System.ServiceModel.SecurityMode.Message);
                    if (configureTcpBinding != null)
                        b = configureTcpBinding(b);
                });
                e = e.WithAddress($"net.tcp://{host}:{port}/{path.Trim('/')}", dnsName);
            });
            proxy = proxy.WithCredentials(c => {
                c = c.UserName(username);
                c = c.Password(password);
                c = c.ServiceCertificate_ValidationMode(X509CertificateValidationMode.Custom);
                c = c.ServiceCertificate_CustomCertificateValidator(certificateThumbprint);
                if (configureClientCredentials != null)
                    c = configureClientCredentials(c);
            });
            configureChannelFactory?.Invoke((proxy as _Proxy<TContract>).ChannelFactory);
            return proxy;
        }
        public static IProxy<TContract> MaxItemsInObjectGraph<TContract>(this IProxy<TContract> me, int maxItemsInObjectGraph)
        {
            var proxy = (me as _Proxy<TContract>);
            foreach (var op in proxy.ChannelFactory.Endpoint.Contract.Operations)
            {
                var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>() as DataContractSerializerOperationBehavior;
                if (dataContractBehavior != null)
                {
                    dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                }
            }

            return me;
        }
        public static TContract Create<TContract>(this IProxy<TContract> me)
        {
            return (me as _Proxy<TContract>).ChannelFactory.CreateChannel();
        }
        public static TContract Open<TContract>(this IProxy<TContract> me, TimeSpan? timeout = null, Action<TContract> beforeOpenAction = null, Action<TContract> afterOpenAction = null)
        {
            var fac = (me as _Proxy<TContract>).ChannelFactory;
            var chan = fac.CreateChannel();
            beforeOpenAction?.Invoke(chan);
            if (timeout != null && timeout.HasValue)
                ((ICommunicationObject)chan).Open(timeout.Value);
            else
            {
                ((ICommunicationObject)chan).Open();
            }
            afterOpenAction?.Invoke(chan);
            return chan;
        }
        public static Task<TContract> OpenAsync<TContract>(this IProxy<TContract> me, TimeSpan? timeout = null, Action<TContract> beforeOpenAction = null, Action<TContract> afterOpenAction = null)
        {
            return TaskManager.StartNew((iProxy, time, before, after) => iProxy.Open(time, before, after), me, timeout, beforeOpenAction, afterOpenAction);
        }
        #region Proxy classes
        internal class _Proxy<TContract> : IProxy<TContract>
        {
            public _Proxy(Func<ServiceEndpoint, ChannelFactory<TContract>> createCustomChannelFactoryFunction)
            {
                CreateCustomChannelFactoryFunction = createCustomChannelFactoryFunction;
            }
            public _Proxy(object callbackInstance, Func<object, ServiceEndpoint, ChannelFactory<TContract>> createCustomDuplexChannelFactoryFunction)
            {
                CallbackInstance = callbackInstance;
                CreateCustomDuplexChannelFactoryFunction = createCustomDuplexChannelFactoryFunction;
            }
            internal object CallbackInstance { get; set; }
            internal Func<ServiceEndpoint, ChannelFactory<TContract>> CreateCustomChannelFactoryFunction { get; set; }
            internal Func<object, ServiceEndpoint, ChannelFactory<TContract>> CreateCustomDuplexChannelFactoryFunction { get; set; }
            public ChannelFactory<TContract> ChannelFactory { get; set; }
            public void SetEndpoint(ServiceEndpoint endpoint)
            {
                if (CallbackInstance != null)
                    ChannelFactory = CreateCustomDuplexChannelFactoryFunction(CallbackInstance, endpoint);
                else
                    ChannelFactory = CreateCustomChannelFactoryFunction(endpoint);
            }
        }
        #endregion
        #endregion
        #region Endpoint
        public static IEndpoint WithContract(this IEndpoint me, Func<ContractDescription> contractFunction)
        {
            var se = (me as _Endpoint);
            var con = contractFunction();
            if (se.ServiceEndpoint == null)
                se.ServiceEndpoint = new ServiceEndpoint(con);
            else
                se.ServiceEndpoint.Contract = con;
            return me;
        }
        public static IEndpoint WithContractOfType<TContract>(this IEndpoint me)
        {
            return me.WithContract(() => ContractDescription.GetContract(typeof(TContract)));
        }
        public static IEndpoint WithTcpBinding(this IEndpoint me, Action<ITcpBinding> action)
        {
            var bin = new _TcpBinding();
            action(bin);
            (me as _Endpoint).ServiceEndpoint.Binding = bin.CreateBinding();
            return me;
        }
        public static IEndpoint ConfigureEndpoint(this IEndpoint me, Action<ServiceEndpoint> action)
        {
            action((me as _Endpoint).ServiceEndpoint);
            return me;
        }
        public static IEndpoint WithAddress(this IEndpoint me, string url)
        {
            (me as _Endpoint).ServiceEndpoint.Address = new EndpointAddress(url);
            return me;
        }
        public static IEndpoint WithAddress(this IEndpoint me, string url, string dnsName)
        {
            (me as _Endpoint).ServiceEndpoint.Address = new EndpointAddress(new Uri(url), new DnsEndpointIdentity(dnsName));
            return me;
        }
        #region Endpoint classes
        class _Endpoint : IEndpoint
        {
            public ServiceEndpoint ServiceEndpoint { get; set; }
        }
        #endregion
        #endregion
        #region Binding
        public static TBinding OpenTimeout<TBinding>(this TBinding me, TimeSpan openTimeout) where TBinding : IBinding
        {
            (me as _Binding).OpenTimeout = openTimeout;
            return me;
        }
        public static TBinding CloseTimeout<TBinding>(this TBinding me, TimeSpan closeTimeout) where TBinding : IBinding
        {
            (me as _Binding).CloseTimeout = closeTimeout;
            return me;
        }
        public static TBinding ReceiveTimeout<TBinding>(this TBinding me, TimeSpan receiveTimeout) where TBinding : IBinding
        {
            (me as _Binding).ReceiveTimeout = receiveTimeout;
            return me;
        }
        public static TBinding SendTimeout<TBinding>(this TBinding me, TimeSpan sendTimeout) where TBinding : IBinding
        {
            (me as _Binding).SendTimeout = sendTimeout;
            return me;
        }
        public static TBinding LocalServiceMaxClockSkew<TBinding>(this TBinding me, TimeSpan localServiceMaxClockSkew) where TBinding : IBinding
        {
            (me as _Binding).LocalServiceMaxClockSkew = localServiceMaxClockSkew;
            return me;
        }
        public static TBinding LocalClientMaxClockSkew<TBinding>(this TBinding me, TimeSpan localClientMaxClockSkew) where TBinding : IBinding
        {
            (me as _Binding).LocalClientMaxClockSkew = localClientMaxClockSkew;
            return me;
        }
        #region Binding classes
        abstract class _Binding : IBinding
        {
            public abstract Binding CreateBinding();
            public virtual void ConfigureBinding(Binding binding)
            {
                binding.CloseTimeout = CloseTimeout;
            }

            public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan SendTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan LocalServiceMaxClockSkew { get; set; } = TimeSpan.FromMinutes(5);
            public TimeSpan LocalClientMaxClockSkew { get; set; } = TimeSpan.FromMinutes(5);
        }
        #endregion
        #endregion
        #region TcpBinding
        public static TBinding InactivityTimeout<TBinding>(this TBinding me, TimeSpan inactivityTimeout) where TBinding : IBinding
        {
            (me as _TcpBinding).InactivityTimeout = inactivityTimeout;
            return me;
        }
        public static ITcpBinding MaxBufferSize(this ITcpBinding me, int maxBufferSize)
        {
            (me as _TcpBinding).MaxBufferSize = maxBufferSize;
            return me;
        }
        public static ITcpBinding MaxBufferPoolSize(this ITcpBinding me, long maxBufferPoolSize)
        {
            (me as _TcpBinding).MaxBufferPoolSize = maxBufferPoolSize;
            return me;
        }
        public static ITcpBinding MaxConnections(this ITcpBinding me, int maxConnections)
        {
            (me as _TcpBinding).MaxConnections = maxConnections;
            return me;
        }
        public static ITcpBinding MaxReceivedMessageSize(this ITcpBinding me, long maxReceivedMessageSize)
        {
            (me as _TcpBinding).MaxReceivedMessageSize = maxReceivedMessageSize;
            return me;
        }
        public static ITcpBinding SecurityMode(this ITcpBinding me, SecurityMode securityMode)
        {
            (me as _TcpBinding).SecurityMode = securityMode;
            return me;
        }
        public static ITcpBinding ClientCredentialType(this ITcpBinding me, MessageCredentialType clientCredentialType)
        {
            (me as _TcpBinding).ClientCredentialType = clientCredentialType;
            return me;
        }
        public static ITcpBinding TransferMode(this ITcpBinding me, TransferMode transferMode)
        {
            (me as _TcpBinding).TransferMode = transferMode;
            return me;
        }
        public static ITcpBinding ReaderQuotas_MaxStringContentLength(this ITcpBinding me, int readerQuotas_MaxStringContentLength)
        {
            (me as _TcpBinding).ReaderQuotas_MaxStringContentLength = readerQuotas_MaxStringContentLength;
            return me;
        }
        public static ITcpBinding ReaderQuotas_MaxArrayLength(this ITcpBinding me, int readerQuotas_MaxArrayLength)
        {
            (me as _TcpBinding).ReaderQuotas_MaxArrayLength = readerQuotas_MaxArrayLength;
            return me;
        }
        public static ITcpBinding ReaderQuotas_MaxBytesPerRead(this ITcpBinding me, int readerQuotas_MaxBytesPerRead)
        {
            (me as _TcpBinding).ReaderQuotas_MaxBytesPerRead = readerQuotas_MaxBytesPerRead;
            return me;
        }
        public static ITcpBinding ConfigureTcpBinding(this ITcpBinding me, Action<NetTcpBinding> action)
        {
            (me as _TcpBinding).ConfigureTcpBindingAction = action;
            return me;
        }
        #region TcpBinding classes
        class _TcpBinding : _Binding, ITcpBinding
        {
            public override Binding CreateBinding()
            {
                Binding bin;
                var tcpBin = new NetTcpBinding();
                tcpBin.ReliableSession.InactivityTimeout = InactivityTimeout;
                tcpBin.MaxBufferPoolSize = MaxBufferPoolSize;
                tcpBin.MaxBufferSize = MaxBufferSize;
                tcpBin.MaxConnections = MaxConnections;
                tcpBin.MaxReceivedMessageSize = MaxReceivedMessageSize;
                tcpBin.ReaderQuotas.MaxBytesPerRead = ReaderQuotas_MaxBytesPerRead;
                tcpBin.ReaderQuotas.MaxStringContentLength = ReaderQuotas_MaxStringContentLength;
                tcpBin.ReaderQuotas.MaxArrayLength = ReaderQuotas_MaxArrayLength;
                tcpBin.Security.Mode = SecurityMode;
                tcpBin.Security.Message.ClientCredentialType = ClientCredentialType;
                tcpBin.TransferMode = TransferMode;
                ConfigureTcpBindingAction?.Invoke(tcpBin);
                bin = tcpBin;

                // Check if ClockSkew has default values (300 segundos)
                if (LocalClientMaxClockSkew != TimeSpan.FromMinutes(5) || LocalServiceMaxClockSkew != TimeSpan.FromMinutes(5))
                {
                    CustomBinding cusBin = new CustomBinding(tcpBin);
					SymmetricSecurityBindingElement symetricSecurity = cusBin.Elements.Find<SymmetricSecurityBindingElement>();
                    if (symetricSecurity != null)
                    {
						//symetricSecurity.LocalServiceSettings.DetectReplays = false;
						//symetricSecurity.LocalClientSettings.DetectReplays = false;
						symetricSecurity.LocalServiceSettings.MaxClockSkew = LocalServiceMaxClockSkew;
						symetricSecurity.LocalClientSettings.MaxClockSkew = LocalClientMaxClockSkew;
						if (symetricSecurity.ProtectionTokenParameters is SecureConversationSecurityTokenParameters tokens)
						{
							//tokens.BootstrapSecurityBindingElement.LocalClientSettings.DetectReplays = false;
							//tokens.BootstrapSecurityBindingElement.LocalServiceSettings.DetectReplays = false;
							tokens.BootstrapSecurityBindingElement.LocalClientSettings.MaxClockSkew = LocalClientMaxClockSkew;
							tokens.BootstrapSecurityBindingElement.LocalServiceSettings.MaxClockSkew = LocalServiceMaxClockSkew;
						}
					}
					AsymmetricSecurityBindingElement asymetricSecurity = cusBin.Elements.Find<AsymmetricSecurityBindingElement>();
					if (asymetricSecurity != null)
					{
						asymetricSecurity.LocalServiceSettings.MaxClockSkew = LocalServiceMaxClockSkew;
						asymetricSecurity.LocalClientSettings.MaxClockSkew = LocalClientMaxClockSkew;
						if (asymetricSecurity.InitiatorTokenParameters is SecureConversationSecurityTokenParameters tokens)
						{
							tokens.BootstrapSecurityBindingElement.LocalClientSettings.MaxClockSkew = LocalClientMaxClockSkew;
							tokens.BootstrapSecurityBindingElement.LocalServiceSettings.MaxClockSkew = LocalServiceMaxClockSkew;
						}
					}
					bin = cusBin;
                }
                bin.CloseTimeout = CloseTimeout;
                bin.OpenTimeout = OpenTimeout;
                bin.ReceiveTimeout = ReceiveTimeout;
                bin.SendTimeout = SendTimeout;
                return bin;
            }
            public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromMinutes(10);
            public long MaxBufferPoolSize { get; set; } = 524288;
            public int MaxBufferSize { get; set; } = 65536;
            public int MaxConnections { get; set; } = 10;
            public long MaxReceivedMessageSize { get; set; } = 65536;
            public SecurityMode SecurityMode { get; set; } = SecurityMode.None;
            public MessageCredentialType ClientCredentialType { get; set; } = MessageCredentialType.None;
            public TransferMode TransferMode { get; set; } = TransferMode.Buffered;
            public int ReaderQuotas_MaxStringContentLength { get; set; } = 8192;
            public int ReaderQuotas_MaxArrayLength { get; set; } = 16384;
            public int ReaderQuotas_MaxBytesPerRead { get; set; } = 4096;
            public Action<NetTcpBinding> ConfigureTcpBindingAction { get; set; }
        }
        #endregion
        #endregion
        #region ServiceCredentials
        public static IServiceCredentials ServiceCertificate(this IServiceCredentials me, X509Certificate2 certificate)
        {
            var cre = me as _ServiceCredentials;
            cre.ServiceCredentials.ServiceCertificate.Certificate = certificate;
            return me;
        }
        public static IServiceCredentials UserNamePasswordValidationMode(this IServiceCredentials me, UserNamePasswordValidationMode userNamePasswordValidationMode)
        {
            var cre = me as _ServiceCredentials;
            cre.ServiceCredentials.UserNameAuthentication.UserNamePasswordValidationMode = userNamePasswordValidationMode;
            return me;
        }
        public static IServiceCredentials CustomUserNamePasswordValidator(this IServiceCredentials me, UserNamePasswordValidator userNamePasswordValidator)
        {
            var cre = me as _ServiceCredentials;
            cre.ServiceCredentials.UserNameAuthentication.CustomUserNamePasswordValidator = userNamePasswordValidator;
            return me;
        }
        public static IServiceCredentials CustomUserNamePasswordValidator(this IServiceCredentials me, Action<string, string> userNameValidationAction)
        {
            var cre = me as _ServiceCredentials;
            cre.ServiceCredentials.UserNameAuthentication.CustomUserNamePasswordValidator = new ServiceValidator(userNameValidationAction);
            return me;
        }
        #region ServiceCredentials classes
        class _ServiceCredentials : IServiceCredentials
        {
            public ServiceCredentials ServiceCredentials { get; set; }
        }
        class ServiceValidator : UserNamePasswordValidator
        {
            public ServiceValidator(Action<string, string> userNameValidationAction)
            {
                this.userNameValidationAction = userNameValidationAction;
            }
            Action<string, string> userNameValidationAction;
            public override void Validate(string userName, string password)
            {
                userNameValidationAction(userName, password);
            }
        }
        #endregion
        #endregion
        #region ClientCrdentials
        public static IClientCredentials UserName(this IClientCredentials me, string userName)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.UserName.UserName = userName;
            return me;
        }
        public static IClientCredentials Password(this IClientCredentials me, string password)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.UserName.Password = password;
            return me;
        }
        public static IClientCredentials ServiceCertificate_ValidationMode(this IClientCredentials me, X509CertificateValidationMode certificateValidationMode)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = certificateValidationMode;
            return me;
        }
        public static IClientCredentials ServiceCertificate_CustomCertificateValidator(this IClientCredentials me, X509CertificateValidator certificateValidator)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator = certificateValidator;
            return me;
        }
        public static IClientCredentials ServiceCertificate_CustomCertificateValidator(this IClientCredentials me, Action<X509Certificate2> certificateValidationAction)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator = new CertificateValidator(certificateValidationAction);
            return me;
        }
        public static IClientCredentials ServiceCertificate_CustomCertificateValidator(this IClientCredentials me, string certificateThumbprint)
        {
            var cre = me as _ClientCredentials;
            cre.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator = new CertificateValidator(certificate=> {
                if (certificate.Thumbprint != certificateThumbprint)
                    throw new AuthenticationException("Certificate thumbprint validation failed");
            });
            return me;
        }
        #region ClientCredentials
        class _ClientCredentials : IClientCredentials
        {
            public ClientCredentials ClientCredentials { get; set; }
        }
        class CertificateValidator : X509CertificateValidator
        {
            public CertificateValidator(Action<X509Certificate2> certificateValidationAction)
            {
                this.certificateValidationAction = certificateValidationAction;
            }
            Action<X509Certificate2> certificateValidationAction;
            public override void Validate(X509Certificate2 certificate)
            {
                certificateValidationAction(certificate);
            }
        }
        #endregion
        #endregion
    }
    public interface IHost { }
    public interface IProxy<TProxy> { }
    public interface IEndpoint { }
    public interface IBinding { }
    public interface ITcpBinding : IBinding { }
    public interface IServiceCredentials { }
    public interface IClientCredentials { }
    #region Discovery classes
    [Flags]
    public enum DiscoveryMetadataElement
    {
        NetBiosName = 1,
        IpV4Addresses = 2,
        IpV6Addresses = 4,
    }
    public class DiscoveryResult
    {
        public IDiscoveryManager Manager { get; set; }
        public EndpointDiscoveryMetadata Metadata { get; set; }
        public string NetBiosName { get; set; }
        public List<string> IpV4Addresses { get; set; }
        public List<string> IpV6Addresses { get; set; }
        public int? Port { get; set; }
    }
    public interface IDiscoveryManager
    {
        IDiscoveryManager Start();
        IDiscoveryManager OnFind(Action<DiscoveryResult> action);
        IDiscoveryManager Stop();
    }
    public class DiscoveryManager<TContract> : IDiscoveryManager
    {
        public DiscoveryManager()
        {
            dis = new DiscoveryClient(new UdpDiscoveryEndpoint());
            dis.FindProgressChanged += FindProgressChanged;
            dis.FindCompleted += FindCompleted;
        }
        List<Action<DiscoveryResult>> onFindActions = new List<Action<DiscoveryResult>>();
        DiscoveryClient dis;
        string userState = "".RandomString(10);
        public FindCriteria Criteria { get; set; } = new FindCriteria(typeof(TContract));
        public IDiscoveryManager OnFind(Action<DiscoveryResult> action)
        {
            onFindActions.Add(action);
            return this;
        }
        public IDiscoveryManager Start()
        {
            dis.FindAsync(Criteria, userState);
            return this;
        }
        public IDiscoveryManager Stop()
        {
            dis.CancelAsync(userState);
            return this;
        }
        private void FindProgressChanged(object sender, FindProgressChangedEventArgs e)
        {
            var res = new DiscoveryResult
            {
                Manager = this,
                Metadata = e.EndpointDiscoveryMetadata,
                NetBiosName = e.EndpointDiscoveryMetadata?.Extensions?.FirstOrDefault(ex => ex.Name == "NetBiosName")?.Value,
                Port = e.EndpointDiscoveryMetadata?.Address?.Uri?.Port,
                IpV4Addresses = e.EndpointDiscoveryMetadata?.Extensions?.Where(ex => ex.Name == "IpV4Address").Select(ex => ex.Value).ToList(),
                IpV6Addresses = e.EndpointDiscoveryMetadata?.Extensions?.Where(ex => ex.Name == "IpV6Address").Select(ex => ex.Value).ToList()
            };
            foreach (var action in onFindActions) action(res);
        }
        private void FindCompleted(object sender, FindCompletedEventArgs e)
        {
            if (!e.Cancelled)
                Start();
        }
    }
    #endregion
}
#endif