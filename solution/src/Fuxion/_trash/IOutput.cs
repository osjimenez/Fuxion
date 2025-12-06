namespace Fuxion;

using System;
using System.Collections.Generic;
using System.Text;

public interface IScope
{
	IOutput Output { get; }
	IProfile Profile { get; }
	IOperation Operation { get; }
}
public interface IProfile
{

}

public interface IOperationMeta;

public interface IOperation
{
	public Guid Id { get; }
}
public interface IOperation<out TMeta> : IOperation where TMeta : IOperationMeta
{
	public TMeta Meta { get; }
}
public interface IOutput
{
	IOutputConsole Console { get; }
	IOutputLog Log { get; }

	void Metric(string metric);
	void Audit(string message);
	IScope Scope { get; }
}

public interface IOutputConsole
{
	void Trace(string message);
	void Debug(string message);
	void Info(string message);
	void Warn(string message);
	void Error(string message);
	IOutput Output { get; }
}
public interface IOutputLog
{
	void Trace(string message);
	void Debug(string message);
	void Info(string message);
	void Warn(string message);
	void Error(string message);
	IOutput Output { get; }
}

public interface IOutputResponse
{

}


public class Test
{
	public void Method(IOutput output)
	{

	}
}